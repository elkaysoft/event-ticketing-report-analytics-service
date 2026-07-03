using ETS.Domain.Contracts;
using ETS.Domain.Entities;
using ETS.Domain.Enums;
using ETS.Domain.Models.Requests;
using ETS.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ETS.Application.TicketNotification.Commands.ResendNotification
{
    public record ResendNotificationCommand(long Id) : IRequest<bool>;


    public class ResendNotificationCommandHandler(ApplicationDbContext _dbContext, 
        IEmailService _emailService,
        ILogger<ResendNotificationCommand> _logger) : IRequestHandler<ResendNotificationCommand, bool>
    {
        public async Task<bool> Handle(ResendNotificationCommand request, CancellationToken cancellationToken)
        {
            var emailLog = await _dbContext.EmailLogs.FindAsync(request.Id, cancellationToken);
            if(emailLog == null)
            {
                return false;
            }

            var emailRequest = new SendEmailRequest 
            { 
                HtmlBody = emailLog.Body,
                Subject = emailLog.Subject, 
                To = emailLog.Recipient, 
                From = emailLog.Sender 
            };

            _logger.LogInformation("About to resend ticket notification for {0}", emailLog.Id);

            var emailResult = await _emailService.SendEmailAsync(emailRequest, cancellationToken);

            _logger.LogInformation($"Email delivery response for {JsonSerializer.Serialize(emailResult)}");

            var notificationStatus = emailResult.Message.Equals("OK") ? NotificationStatusEnum.Sent : NotificationStatusEnum.Failed;
            emailLog.UpdateNotificationStatus(notificationStatus, JsonSerializer.Serialize(emailResult));

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

}
