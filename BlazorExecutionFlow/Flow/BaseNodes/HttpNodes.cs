using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Models;

namespace BlazorExecutionFlow.Flow.BaseNodes
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

        // ==========================================
        // GET
        // ==========================================

        /// <summary>
        /// Performs an HTTP GET request and returns the response as a string.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<string> HttpGet(
            [BlazorFlowInputField] string url,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyHeaders(request, headers);

                var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpGet failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Performs an HTTP GET request with response status tracking.
        /// Returns the response body and status code.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP/Advanced")]
        public static async Task<HttpResponse> HttpGetWithStatus(
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
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyHeaders(request, headers);

                var response = await _httpClient.SendAsync(request, cts.Token);
                var body = await response.Content.ReadAsStringAsync(cts.Token);

                return new HttpResponse
                {
                    Body = body,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpGetWithStatus failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }

        // ==========================================
        // POST
        // ==========================================

        /// <summary>
        /// Performs an HTTP POST request with a string body.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<string> HttpPost(
            [BlazorFlowInputField] string url,
            string body,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] string contentType = "application/json",
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

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

                var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpPost failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Performs an HTTP POST request with response status tracking.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP/Advanced")]
        public static async Task<HttpResponse> HttpPostWithStatus(
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

                var response = await _httpClient.SendAsync(request, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                return new HttpResponse
                {
                    Body = responseBody,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpPostWithStatus failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }

        // ==========================================
        // PUT
        // ==========================================

        /// <summary>
        /// Performs an HTTP PUT request for updating resources.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<string> HttpPut(
            [BlazorFlowInputField] string url,
            string body,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] string contentType = "application/json",
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

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

                var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpPut failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Performs an HTTP PUT request with response status tracking.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP/Advanced")]
        public static async Task<HttpResponse> HttpPutWithStatus(
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

                var response = await _httpClient.SendAsync(request, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                return new HttpResponse
                {
                    Body = responseBody,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpPutWithStatus failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }

        // ==========================================
        // DELETE
        // ==========================================

        /// <summary>
        /// Performs an HTTP DELETE request for removing resources.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<string> HttpDelete(
            [BlazorFlowInputField] string url,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                ApplyHeaders(request, headers);

                var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpDelete failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Performs an HTTP DELETE request with response status tracking.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP/Advanced")]
        public static async Task<HttpResponse> HttpDeleteWithStatus(
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

                var response = await _httpClient.SendAsync(request, cts.Token);
                var body = await response.Content.ReadAsStringAsync(cts.Token);

                return new HttpResponse
                {
                    Body = body,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpDeleteWithStatus failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }

        // ==========================================
        // PATCH
        // ==========================================

        /// <summary>
        /// Performs an HTTP PATCH request for partial resource updates.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP")]
        public static async Task<string> HttpPatch(
            [BlazorFlowInputField] string url,
            string body,
            [BlazorFlowDictionaryMapping] Dictionary<string, string>? headers = null,
            [BlazorFlowInputField] string contentType = "application/json",
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

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

                var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpPatch failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Performs an HTTP PATCH request with response status tracking.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP/Advanced")]
        public static async Task<HttpResponse> HttpPatchWithStatus(
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

                var response = await _httpClient.SendAsync(request, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                return new HttpResponse
                {
                    Body = responseBody,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpPatchWithStatus failed: {ex.Message}");
                return new HttpResponse
                {
                    Body = ex.Message,
                    StatusCode = 0,
                    Success = false
                };
            }
        }

        // ==========================================
        // HEAD
        // ==========================================

        /// <summary>
        /// Performs an HTTP HEAD request to retrieve headers without body.
        /// Useful for checking resource existence or metadata.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP/Advanced")]
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

                var response = await _httpClient.SendAsync(request, cts.Token);

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

        // ==========================================
        // OPTIONS
        // ==========================================

        /// <summary>
        /// Performs an HTTP OPTIONS request to discover allowed methods.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "HTTP/Advanced")]
        public static async Task<HttpResponse> HttpOptions(
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
                using var request = new HttpRequestMessage(HttpMethod.Options, url);
                ApplyHeaders(request, headers);

                var response = await _httpClient.SendAsync(request, cts.Token);
                var body = await response.Content.ReadAsStringAsync(cts.Token);

                return new HttpResponse
                {
                    Body = body,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] HttpOptions failed: {ex.Message}");
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
