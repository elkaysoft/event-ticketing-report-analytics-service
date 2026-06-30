namespace ETS.Domain.Models.Responses
{
    public class SendEmailResponse
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public string MessageID { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string To { get; set; }
    }
}
