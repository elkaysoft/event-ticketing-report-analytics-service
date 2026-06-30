using System;
using System.Collections.Generic;
using System.Text;

namespace ETS.Domain.Errors
{
    public class OrdersError
    {
        public static readonly Error NotFound = new("Order.NotFound", "The order was not found");
        public static readonly Error TicketAlreadyGenerated = new("Ticket.AlreadyGenerated", "Ticket has already being generated.");
    }
}
