using ETS.Domain.Contracts;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Text;
using System.Text.Json;

namespace ETS.Infrastructure
{
    public class MicroserviceHttpClient : IMicroserviceHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MicroserviceHttpClient> _logger;

        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;


        private readonly int RetryCount = 3;
        private readonly int CircuitBreakerFailureThreshold = 5;
        private readonly int CircuitBreakerDurationSeconds = 60;
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        public MicroserviceHttpClient(HttpClient httpClient, ILogger<MicroserviceHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.Timeout = DefaultTimeout;

            _retryPolicy = CreateRetryPolicy();
            _circuitBreakerPolicy = CreateCircuitBreakerPolicy();

        }

        public async Task<HttpResponseMessage> DeleteAsync<TRequest>(string url, TRequest requestData, Dictionary<string, string> additionalHeaders = null)
        {
            if (additionalHeaders != null)
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                foreach (KeyValuePair<string, string> header in additionalHeaders)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var response = await _httpClient.DeleteAsync(url);
            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(string url,
            Dictionary<string, string> additionalHeaders = null!,
            CancellationToken cancellationToken = default)
        {
            if (additionalHeaders != null)
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                foreach (KeyValuePair<string, string> header in additionalHeaders)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var response = await _retryPolicy.ExecuteAsync(() =>
                    _circuitBreakerPolicy.ExecuteAsync(async () =>
                    await _httpClient.GetAsync(url, cancellationToken)
                ));
            return response;
        }

        public async Task<HttpResponseMessage> PostAsync<TRequest>(string url, TRequest requestData,
            Dictionary<string, string> additionalHeaders = null!, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = null!;

            try
            {
                if (additionalHeaders != null)
                {
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                    foreach (KeyValuePair<string, string> header in additionalHeaders)
                    {
                        _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                var requestContent = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
                response = await _retryPolicy.ExecuteAsync(() =>
                    _circuitBreakerPolicy.ExecuteAsync(async () =>
                    await _httpClient.PostAsync(url, requestContent, cancellationToken)
                ));

            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            return response;
        }

        public async Task<HttpResponseMessage> PostWithFileAsync(string url, IDictionary<string, string> requestData,
            Stream fileStream, string fileName, Dictionary<string, string> additionalHeaders = null!,
            CancellationToken cancellationToken = default)
        {
            if (additionalHeaders != null)
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                foreach (KeyValuePair<string, string> header in additionalHeaders)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            var formData = new MultipartFormDataContent();
            foreach (var keyValuePair in requestData)
            {
                if (keyValuePair.Value is string stringValue)
                    formData.Add(new StringContent(stringValue), keyValuePair.Key);
            }

            var fileContent = new StreamContent(fileStream);
            formData.Add(fileContent, "file", fileName);

            var response = await _retryPolicy.ExecuteAsync(() =>
                _circuitBreakerPolicy.ExecuteAsync(async () =>
                await _httpClient.PostAsync(url, formData, cancellationToken)
                ));
            return response;
        }

        public async Task<HttpResponseMessage> PutAsync<TRequest>(string url, TRequest requestData, Dictionary<string, string> additionalHeaders = null)
        {
            if (additionalHeaders != null)
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                foreach (KeyValuePair<string, string> header in additionalHeaders)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var requestContent = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, requestContent);

            return response;
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod httpMethod,
            string url,
            object? body,
            Dictionary<string, string>? headers,
            CancellationToken cancellationToken,
            bool isMultipart = false)
        {
            using var request = new HttpRequestMessage(httpMethod, url);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (body != null)
            {
                request.Content = isMultipart
                    ? (HttpContent)body
                    : new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            }

            return await _retryPolicy.ExecuteAsync(() =>
                _circuitBreakerPolicy.ExecuteAsync(() =>
                    _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                ));
        }


        private AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy()
        {
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>() //timeout
                .OrResult(r => (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(
                    RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (outcome, delay, retryCount, _) =>
                    {
                        _logger.LogWarning(
                         "HTTP retry {Retry}/{Max} after {Delay}s. Reason: {Reason}",
                         retryCount,
                         RetryCount,
                         delay.TotalSeconds,
                         outcome.Exception?.Message ??
                         outcome.Result?.StatusCode.ToString());
                    });
        }

        private AsyncCircuitBreakerPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
        {
            return Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .Or<TaskCanceledException>()
                    .OrResult(r => (int)r.StatusCode == 500)
                    .CircuitBreakerAsync(
                        CircuitBreakerFailureThreshold,
                        TimeSpan.FromSeconds(CircuitBreakerDurationSeconds),
                        onBreak: (outcome, duration) =>
                        {
                            _logger.LogError(
                               "HTTP circuit opened for {Duration}s. Reason: {Reason}",
                               duration.TotalSeconds,
                               outcome.Exception?.Message ??
                               outcome.Result?.StatusCode.ToString());
                        },
                        onReset: () =>
                        {
                            _logger.LogInformation("HTTP circuit reset");
                        },
                        onHalfOpen: () =>
                        {
                            _logger.LogWarning("HTTP circuit half-open");
                        });
        }

    }
}
