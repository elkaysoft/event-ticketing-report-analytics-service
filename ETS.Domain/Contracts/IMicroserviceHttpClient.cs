namespace ETS.Domain.Contracts
{
    public interface IMicroserviceHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string> additionalHeaders = null!,
           CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> PostAsync<TRequest>(string url, TRequest requestData, Dictionary<string, string> additionalHeaders = null!,
            CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> PostWithFileAsync(string url, IDictionary<string, string> requestData, Stream fileStream, string fileName,
            Dictionary<string, string> additionalHeaders = null!, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> PutAsync<TRequest>(string url, TRequest requestData, Dictionary<string, string> additionalHeaders = null);

        Task<HttpResponseMessage> DeleteAsync<TRequest>(string url, TRequest requestData, Dictionary<string, string> additionalHeaders = null!);
    }
}
