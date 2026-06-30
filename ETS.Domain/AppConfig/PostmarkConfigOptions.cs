namespace ETS.Domain.AppConfig
{
    public class PostmarkConfigOptions
    {
        public string BaseUrl { get; set; }
        public string ServerToken { get; set; }
        public string Environment { get; set; } = string.Empty;
        public string TestEmailAddress { get; set; } = string.Empty;
        public string SenderEmail { get; set; }
    }
}
