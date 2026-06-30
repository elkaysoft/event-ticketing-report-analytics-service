using ETS.Domain.Common;
using ETS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ETS.Domain.Entities
{
    public class Order : Entity<Guid>
    {
        public Guid EventId { get; private set; }
        public string EventName { get; private set; }
        public string FullName { get; private set; }
        public string EmailAddress { get; private set; }
        public string PhoneNumber { get; private set; }
        public string OrderNumber { get; private set; }
        public string PaystackAccessCode { get; private set; }
        public OrderStatus OrderStatus { get; private set; }
        public DateTime? PaymentConfirmedAt { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public TicketStatus RedemptionStatus { get; private set; }
        public DateTime? RedemptionDate { get; private set; }        
        public string? CancelletionReason { get; private set; }
        public decimal SubTotal { get; private set; }
        public decimal TaxAmount { get; private set; }
        public decimal TotalAmount { get; private set; }
        public int TotalTickets { get; private set; }

        public ICollection<OrderItem> OrderItems { get; set; }
        public Events Event { get; set; }
        
    }
}
