using ETS.Domain.Common;
using ETS.Domain.Enums;

namespace ETS.Domain.Entities
{
    public class EmailLog : Entity<long>
    {
        public string Sender { get; private set; } = string.Empty;
        public string Recipient { get; private set; } = string.Empty;
        public string Subject { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;
        public NotificationStatusEnum NotificationStatus { get; private set; }
        public NotificationTargetEnum NotificationTarget { get; private set; }
        public string? ResponseData { get; private set; }
        public int RetryCount { get; private set; }

        public static EmailLog Create(string sender, string recipient, string subject, string body)
        {
            return new EmailLog
            {
                Sender = sender,
                Recipient = recipient,
                Subject = subject,
                Body = body,
                NotificationStatus = NotificationStatusEnum.Pending,
                NotificationTarget = NotificationTargetEnum.TicketReceipt
            };
        }


        public void UpdateNotificationStatus(string responseData, NotificationStatusEnum notificationStatus)
        {
            ResponseData = responseData;
            NotificationStatus = notificationStatus;
        }

    }
}
