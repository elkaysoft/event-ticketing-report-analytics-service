using ETS.Application.TicketNotification.Queries.ProcessTicketNotification;
using ETS.Domain.Entities;
using ETS.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ETS.Application.TicketNotification.BackgroundServices
{
    public class TicketGenerationService : BackgroundService
    {
        private readonly ILogger<TicketGenerationService> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private const int MaxRetryCount = 3;
        private readonly TimeSpan _idleDelay = TimeSpan.FromSeconds(600);
        private readonly TimeSpan _activeDelay = TimeSpan.FromSeconds(0); // Poll instantly if there is more work
        private readonly TimeSpan _coolDownDelay = TimeSpan.FromSeconds(5); // Breather delay

        private const int MaxBatchSize = 5; // Items processsed per single database trip
        private const int MaxBatchesPerBurst = 10; // Maximum number of consecutive batches allowed (50 items total)

        public TicketGenerationService(ILogger<TicketGenerationService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _contextFactory = contextFactory;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TicketGenerationService started..");

            while (!stoppingToken.IsCancellationRequested) 
            {
                int consecutiveBatchesProcessed = 0;
                bool queueHasMoreWork = true;

                // Inner processing cycle controlling the maximum burst ceiling
                while (queueHasMoreWork && consecutiveBatchesProcessed < MaxBatchSize 
                    && !stoppingToken.IsCancellationRequested) 
                {
                    // Run a single chunk processing block
                    queueHasMoreWork = await RunTicketProcessingAsync(stoppingToken);

                    if (queueHasMoreWork)
                    {
                        consecutiveBatchesProcessed++;
                        _logger.LogDebug("Batch {Count} completed. Moving immediately to next chunk.", consecutiveBatchesProcessed);
                    }
                }

                
                //Determine delay dynamically based on queue activity
                TimeSpan nextDelay;

                if (consecutiveBatchesProcessed >= MaxBatchesPerBurst && queueHasMoreWork) 
                {
                    // Ceiling hit! Attemp to force a breather even though there is more data waiting
                    nextDelay = _coolDownDelay;
                    _logger.LogWarning("Burst limit reached ({Total}) items. Forcing a {Seconds}s cool-down breather.",
                        MaxBatchesPerBurst * MaxBatchSize, _coolDownDelay.TotalSeconds);
                }
                else if(consecutiveBatchesProcessed > 0)
                {
                    // Queue was successfully drained below the burst threshold
                    nextDelay = _activeDelay;
                }
                else
                {
                    // No more work found at all during the first check
                    nextDelay = _idleDelay;
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }

        private async Task<bool> RunTicketProcessingAsync(CancellationToken stoppingToken)
        {
            List<Guid> pendingNotificationIds;
            var lockExpiryThreshold = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));
            string leaseOwnerId = $"Pod-{Environment.MachineName}-{Guid.NewGuid()}";

            using (var lockContext = await _contextFactory.CreateDbContextAsync()) 
            {
                var rowsAffected = await lockContext.OrderItem                     
                    .Where(x => x.PaymentStatus == Domain.Enums.OrderStatus.Completed &&
                    (x.TicketGenerationStatus == Domain.Enums.TicketGenerationStatus.Pending ||
                    (x.TicketGenerationStatus == Domain.Enums.TicketGenerationStatus.Processing &&
                     x.LockTimeStamp < lockExpiryThreshold)))
                    .OrderBy(x => x.LockTimeStamp)
                    .Take(5)
                    .ExecuteUpdateAsync(p => p
                    .SetProperty(x => x.TicketGenerationStatus, Domain.Enums.TicketGenerationStatus.Processing)
                    .SetProperty(x => x.LockOwnerId, leaseOwnerId)
                    .SetProperty(x => x.LockTimeStamp, DateTime.UtcNow),
                    stoppingToken);
                
                // if now row(s) were returned, notifiy loop to sleep for 300 seconds
                if (rowsAffected == 0) return false;

                pendingNotificationIds = await lockContext.OrderItem
                    .AsNoTracking()
                    .Where(x => x.LockOwnerId == leaseOwnerId && x.TicketGenerationStatus == Domain.Enums.TicketGenerationStatus.Processing)
                    .Select(x => x.Id)
                    .ToListAsync(stoppingToken);
            }

            foreach(var notificationId in pendingNotificationIds)
            {
                if (stoppingToken.IsCancellationRequested) return true;

                // The reason for ScopeFactory is to cleanly handle MediatR dependency resolution
                using var scope = _serviceScopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                using var itemContext = await _contextFactory.CreateDbContextAsync();
                using var transaction = await itemContext.Database.BeginTransactionAsync(stoppingToken);

                try
                {
                    var notification = await itemContext.OrderItem
                        .Include(o => o.Order)
                        .ThenInclude(ev => ev.Event)
                        .FirstOrDefaultAsync(x => x.Id ==  notificationId, stoppingToken);

                    if (notification == null) continue;

                    // Ensure context not leaked into the MediatR query parameters
                    var query = new ProcessTicketNotificationQuery(notificationId);
                    var success = (await mediator.Send(query, stoppingToken)).Value;

                    if (success)
                    {
                        notification.UpdateTicketGenerationStatus(Domain.Enums.TicketGenerationStatus.Completed);
                        notification.ClearLock();
                        await itemContext.SaveChangesAsync(stoppingToken);
                        await transaction.CommitAsync(stoppingToken);
                    }
                    else
                    {
                        await HandleProcessingFailureAync(itemContext, notification, stoppingToken);
                        await transaction.CommitAsync(stoppingToken);
                    }

                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "System crash during processing of ticket {Id}. Rolling back.", notificationId);
                    await transaction.RollbackAsync(stoppingToken);
                    await FallbackToPendingOnCrashAsync(notificationId);
                }
            }

            return true;
        }

        private async Task HandleProcessingFailureAync(ApplicationDbContext context, OrderItem notification,
            CancellationToken stoppingToken)
        {
            notification.IncrementRetryCount();
            notification.UpdateTicketGenerationStatus(notification.RetryCount >= MaxRetryCount
                ? Domain.Enums.TicketGenerationStatus.Failed : Domain.Enums.TicketGenerationStatus.Pending);
            notification.ClearLock();
            await context.SaveChangesAsync(stoppingToken);
        }

        private async Task FallbackToPendingOnCrashAsync(Guid notificationId)
        {
            using var fallbackContext = await _contextFactory.CreateDbContextAsync();
            await fallbackContext.OrderItem
                .Where(x => x.Id == notificationId)
                .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.TicketGenerationStatus, Domain.Enums.TicketGenerationStatus.Pending)
                .SetProperty(x => x.LockOwnerId, (string?)null)
                .SetProperty(x => x.LockTimeStamp, (DateTime?)null));

        }

    }
}
