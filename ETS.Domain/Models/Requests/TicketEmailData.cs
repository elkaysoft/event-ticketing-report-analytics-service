namespace ETS.Domain.Models.Requests
{
    public record TicketEmailData(
     string CustomerName,
     string CustomerEmail,
     string EventTitle,
     string EventDescription,
     DateTime EventDate,
     string StartTime,
     string EndTime,
     string Location,
     string TicketCategory,
     int Unit,     
     decimal TicketPrice,
     string OrderReference,
     string QRCodeReference,
     string QRCodeUrl
 );
}
