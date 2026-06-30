using System.ComponentModel;

namespace ETS.Domain.Enums
{
    public enum OrderStatus
    {
        [Description("Pending - Awaiting Payment")]
        Pending,
        [Description("Confirmed - Payment Received")]
        Confirmed,
        [Description("Cancelled - Order Cancelled")]
        Cancelled,
        [Description("Completed - Order Completed")]
        Completed
    }
}
