using ETS.Domain.Enums;

namespace ETS.Application.TicketNotification.Queries.GetPagedNotification
{
    public class GetPagedNotificatonDto
    {
        public long Id { get; set; }
        public string Subject { get; set; }
        public string Recipient { get; set; }
        public DateTime RequestDate { get; set; }
        public NotificationStatusEnum Status { get; set; }
    }
}
