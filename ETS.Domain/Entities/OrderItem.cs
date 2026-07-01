using ETS.Domain.Common;
using ETS.Domain.Enums;

namespace ETS.Domain.Entities
{
    public class OrderItem : Entity<Guid>
    {       
        public Guid OrderId { get; private set; }
        public Guid EventCategoryId { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public int Unit { get; private set; }
        public decimal UnitPrice { get; private set; }
        public string QRCodeReference { get; private set; } = string.Empty;
        public string? QRCodeUrl { get; private set; }
        public TicketStatus RedemptionStatus { get; private set; }
        public TicketGenerationStatus TicketGenerationStatus { get; set; }
        public OrderStatus PaymentStatus { get; set; }
        public string? LockOwnerId { get; set; }
        public DateTime? LockTimeStamp { get; set; }
        public int RetryCount { get; set; }
        public virtual Order Order { get; set; }

        public void SetQRCodeUrl(string url)
        {
            QRCodeUrl = url;
        }

        public void MarkAsUsed() => RedemptionStatus = TicketStatus.Redeemed;

        public void UpdateTicketGenerationStatus(TicketGenerationStatus status)
        {
            TicketGenerationStatus = status;
        }

        public void ClearLock()
        {
            LockOwnerId = null;
        }

        public void IncrementRetryCount()
        {
            RetryCount++;
        }

        
    }
}
