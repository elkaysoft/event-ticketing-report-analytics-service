using ETS.Domain.Models.Requests;
using ETS.Domain.Models.Responses;

namespace ETS.Domain.Contracts
{
    public interface IEmailService
    {
        Task<SendEmailResponse> SendEmail(SendEmailRequest request, CancellationToken cancellationToken);
    }
}
