using ETS.Application.Helpers;
using ETS.Domain.Entities;
using ETS.Domain.Enums;
using ETS.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ETS.Application.TicketNotification.Queries.GetPagedNotification
{
    public record GetPagedNotificationQuery(string? SearchText,
        NotificationStatusEnum? Status,
        string? SortField) : 
        PaginationQuery, IRequest<PaginatedList<GetPagedNotificatonDto>>;

    public class GetPagedNotificationQueryHandler :
        IRequestHandler<GetPagedNotificationQuery, PaginatedList<GetPagedNotificatonDto>>
    {
        private readonly ApplicationDbContext _context;

        public GetPagedNotificationQueryHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        private static Expression<Func<EmailLog, bool>> GetQueryExpression(GetPagedNotificationQuery request) => x =>
                (string.IsNullOrWhiteSpace(request.SearchText)
                    || EF.Functions.Like(x.Subject, $"%{request.SearchText}%")
                    || EF.Functions.Like(x.Recipient, $"%{request.SearchText}%"))
                    && (request.Status == null || x.NotificationStatus == request.Status.Value);

        private static Expression<Func<EmailLog, object>> GetSortProperty(GetPagedNotificationQuery request) =>
            request.SortField?.ToLower() switch
            {
                "subject" => x => x.Subject,
                "recipient" => x => x.Recipient,
                "status" => x => x.NotificationStatus,
                _ => x => x.CreatedAt
            };

        private static Expression<Func<EmailLog, GetPagedNotificatonDto>> Selector() => x => new GetPagedNotificatonDto
        {
            Id = x.Id,
            Subject = x.Subject,
            Recipient = x.Recipient,
            Status = x.NotificationStatus,
            RequestDate = x.CreatedAt
        };

        public async Task<PaginatedList<GetPagedNotificatonDto>> Handle(GetPagedNotificationQuery request, CancellationToken cancellationToken)
        {
            var filter = GetQueryExpression(request);
            var sort = GetSortProperty(request);

            var query = _context.EmailLogs
                .AsNoTracking()
                .Where(filter)
                .OrderByDescending(sort)
                .Select(Selector());

            var notifications = await PaginatedListedExtender<GetPagedNotificatonDto>.CreateAsync(query,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return notifications;
        }
    }
}
