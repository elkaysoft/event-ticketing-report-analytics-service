namespace ETS.Domain.Models.Requests
{
    public class SendEmailRequest
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string MessageStream { get; set; } = "outbound";
    }
}
