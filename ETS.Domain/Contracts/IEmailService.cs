using ETS.Domain.Models.Requests;
using ETS.Domain.Models.Responses;

namespace ETS.Domain.Contracts
{
    public interface IEmailService
    {
        Task<SendEmailResponse> SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken);
    }
}
