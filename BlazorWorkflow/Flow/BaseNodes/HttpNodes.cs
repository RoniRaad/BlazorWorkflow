using System.Text;
using BlazorWorkflow.Flow.Attributes;
using BlazorWorkflow.Models;

namespace BlazorWorkflow.Flow.BaseNodes
{
    /// <summary>
    /// HTTP request nodes for making web API calls.
    /// Supports all standard HTTP methods with customizable headers, timeouts, and content types.
    /// </summary>
    public static class HttpNodes
    {
        private static readonly HttpClient _httpClient = new();

        /// <summary>
        /// Applies custom headers to an HTTP request.
        /// Handles both request headers and content headers appropriately.
        /// </summary>
        private static void ApplyHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
        {
            if (headers == null) return;

            foreach (var kvp in headers)
            {
                var key = kvp.Key;
                var value = kvp.Value ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(key))
                {
                    // Try to add to request headers first
                    if (!request.Headers.TryAddWithoutValidation(key, value))
                    {
                        // If it fails, try adding to content headers (if content exists)
                        request.Content?.Headers.TryAddWithoutValidation(key, value);
                    }
                }
            }
        }

        
        /// <summary>
        /// Performs an HTTP POST request with response status tracking.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<HttpResponse> HttpPost(
            [BlazorFlowInputField] string url,
            string body,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] string contentType = "application/json",
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return new HttpResponse { Body = string.Empty, StatusCode = 0, Success = false };

            body ??= string.Empty;
            contentType ??= "application/json";

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(body, Encoding.UTF8, contentType);
                ApplyHeaders(request, headers);

                var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

                return new HttpResponse
                {
                    Body = responseBody,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpPost failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Performs an HTTP Get request with response status tracking.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<HttpResponse> HttpGet(
            [BlazorFlowInputField] string url,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return new HttpResponse { StatusCode = 0, Success = false };

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyHeaders(request, headers);

                var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

                return new HttpResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode,
                    Body = responseBody
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpPost failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Performs an HTTP PUT request with response status tracking.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<HttpResponse> HttpPut(
            [BlazorFlowInputField] string url,
            string body,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] string contentType = "application/json",
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return new HttpResponse { Body = string.Empty, StatusCode = 0, Success = false };

            body ??= string.Empty;
            contentType ??= "application/json";

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Content = new StringContent(body, Encoding.UTF8, contentType);
                ApplyHeaders(request, headers);

                var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

                return new HttpResponse
                {
                    Body = responseBody,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpPut failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Performs an HTTP DELETE request with response status tracking.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<HttpResponse> HttpDelete(
            [BlazorFlowInputField] string url,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return new HttpResponse { Body = string.Empty, StatusCode = 0, Success = false };

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                ApplyHeaders(request, headers);

                var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

                return new HttpResponse
                {
                    Body = body,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpDelete failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }


        /// <summary>
        /// Performs an HTTP PATCH request with response status tracking.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<HttpResponse> HttpPatch(
            [BlazorFlowInputField] string url,
            string body,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] string contentType = "application/json",
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return new HttpResponse { Body = string.Empty, StatusCode = 0, Success = false };

            body ??= string.Empty;
            contentType ??= "application/json";

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Patch, url);
                request.Content = new StringContent(body, Encoding.UTF8, contentType);
                ApplyHeaders(request, headers);

                var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

                return new HttpResponse
                {
                    Body = responseBody,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpPatch failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Performs an HTTP HEAD request to retrieve headers without body.
        /// Useful for checking resource existence or metadata.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<HttpResponse> HttpHead(
            [BlazorFlowInputField] string url,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return new HttpResponse { Body = string.Empty, StatusCode = 0, Success = false };

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                ApplyHeaders(request, headers);

                var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);

                return new HttpResponse
                {
                    Body = string.Empty, // HEAD responses don't have a body
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpHead failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }
    }

    /// <summary>
    /// Response object for HTTP requests with status tracking.
    /// </summary>
    public class HttpResponse
    {
        public string Body { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public bool Success { get; set; }
    }
}
