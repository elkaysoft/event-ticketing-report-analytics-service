using ETS.Domain.AppConfig;
using ETS.Domain.Contracts;
using ETS.Domain.Models.Requests;
using ETS.Domain.Models.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ETS.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IMicroserviceHttpClient _httpClient;
        private readonly ILogger<EmailService> _logger;
        private readonly PostmarkConfigOptions _postmarkConfig;

        public EmailService(IMicroserviceHttpClient httpClient,
            ILogger<EmailService> logger,
            IOptions<PostmarkConfigOptions> postmarkConfig)
        {
            _httpClient = httpClient;
            _logger = logger;
            _postmarkConfig = postmarkConfig.Value;
        }


        public async Task<SendEmailResponse> SendEmail(SendEmailRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var apiHeader = new Dictionary<string, string>
                {
                    ["X-Postmark-Server-Token"] = _postmarkConfig.ServerToken,
                    ["Accept"] = "application/json"
                };

                var emailResult = await _httpClient.PostAsync(_postmarkConfig.BaseUrl,
                    request,
                    apiHeader,
                    cancellationToken);

                if (emailResult.IsSuccessStatusCode)
                {
                    var content = await emailResult.Content.ReadAsStringAsync();
                    var deserializedResult = JsonConvert.DeserializeObject<SendEmailResponse>(content);
                    return deserializedResult!;
                }

                return new SendEmailResponse { ErrorCode = 22, Message = "Unable to send email" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occured at {nameof(SendEmail)}");
                return new SendEmailResponse { ErrorCode = 21, Message = "An error occured, pls try again later" };
            }
        }
    }
}
