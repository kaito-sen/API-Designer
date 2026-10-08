using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API_Integarated.Core.Models;

namespace API_Integarated.Core.Services
{
    public class HttpExecutionEngine : IApiExecutionEngine
    {
        public event Action<string, string>? OnVariableExtracted;

        public async Task<ApiResponseData> ExecuteAsync(
            ApiEndpointDefinition endpoint,
            IDictionary<string, string>? variables = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. Resolve raw URL with variables
                var resolvedUrl = VariableResolver.Resolve(endpoint.Url, variables);
                if (string.IsNullOrWhiteSpace(resolvedUrl))
                {
                    return ApiResponseData.CreateError("Endpoint URL cannot be empty.", 0);
                }

                // 2. Build full URL with Query Parameters & ApiKey (if in query)
                var uriBuilder = BuildUriWithQuery(resolvedUrl, endpoint, variables);

                // 3. Configure HttpClient with SSL bypass if needed
                var handler = new HttpClientHandler();
                if (endpoint.IgnoreSslErrors)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(Math.Max(1, endpoint.TimeoutSeconds))
                };

                // 4. Create HttpRequestMessage
                var httpMethod = ConvertMethod(endpoint.Method);
                using var request = new HttpRequestMessage(httpMethod, uriBuilder.Uri);

                // 5. Apply Headers
                ApplyHeaders(request, endpoint, variables);

                // 6. Apply Authentication
                ApplyAuthentication(request, endpoint.Auth, variables);

                // 7. Apply Body (for methods that support body)
                if (endpoint.Method != ApiMethod.GET && endpoint.Method != ApiMethod.HEAD)
                {
                    request.Content = CreateHttpContent(endpoint.Body, variables);
                }

                // 8. Execute HTTP Request
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
                stopwatch.Stop();

                // 9. Read Response Body & Headers
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var headersDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var h in response.Headers)
                {
                    headersDict[h.Key] = string.Join(", ", h.Value);
                }
                foreach (var h in response.Content.Headers)
                {
                    headersDict[h.Key] = string.Join(", ", h.Value);
                }

                var contentType = response.Content.Headers.ContentType?.ToString();
                var contentLength = response.Content.Headers.ContentLength;

                // 10. Variable Extraction from Response
                ProcessExtractionRules(responseBody, endpoint.ExtractionRules);

                return new ApiResponseData
                {
                    StatusCode = (int)response.StatusCode,
                    StatusDescription = response.ReasonPhrase ?? response.StatusCode.ToString(),
                    IsSuccess = response.IsSuccessStatusCode,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    ResponseBody = responseBody,
                    Headers = headersDict,
                    ContentType = contentType,
                    ContentLength = contentLength,
                    Timestamp = DateTime.Now
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return ApiResponseData.CreateError($"Request timed out after {endpoint.TimeoutSeconds}s.", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiResponseData.CreateError($"Request failed: {ex.Message}", stopwatch.ElapsedMilliseconds);
            }
        }

        private static UriBuilder BuildUriWithQuery(string resolvedUrl, ApiEndpointDefinition endpoint, IDictionary<string, string>? variables)
        {
            if (!resolvedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !resolvedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                resolvedUrl = "https://" + resolvedUrl;
            }

            var uriBuilder = new UriBuilder(resolvedUrl);
            var queryParams = new List<string>();

            // Keep existing query if any
            if (!string.IsNullOrWhiteSpace(uriBuilder.Query))
            {
                var existingQuery = uriBuilder.Query.TrimStart('?');
                if (!string.IsNullOrEmpty(existingQuery))
                {
                    queryParams.Add(existingQuery);
                }
            }

            // Append enabled query parameters
            foreach (var q in endpoint.QueryParameters.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Key)))
            {
                var key = Uri.EscapeDataString(VariableResolver.Resolve(q.Key, variables));
                var val = Uri.EscapeDataString(VariableResolver.Resolve(q.Value, variables));
                queryParams.Add($"{key}={val}");
            }

            // Check if API Key is set to QueryString
            if (endpoint.Auth.Type == ApiAuthType.ApiKey &&
                endpoint.Auth.ApiKeyLocation == ApiKeyLocation.QueryString &&
                !string.IsNullOrWhiteSpace(endpoint.Auth.ApiKeyName))
            {
                var key = Uri.EscapeDataString(VariableResolver.Resolve(endpoint.Auth.ApiKeyName, variables));
                var val = Uri.EscapeDataString(VariableResolver.Resolve(endpoint.Auth.ApiKeyValue, variables));
                queryParams.Add($"{key}={val}");
            }

            if (queryParams.Count > 0)
            {
                uriBuilder.Query = string.Join("&", queryParams);
            }

            return uriBuilder;
        }

        private static void ApplyHeaders(HttpRequestMessage request, ApiEndpointDefinition endpoint, IDictionary<string, string>? variables)
        {
            foreach (var header in endpoint.Headers.Where(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.Key)))
            {
                var key = VariableResolver.Resolve(header.Key, variables).Trim();
                var val = VariableResolver.Resolve(header.Value, variables);

                // Ignore restricted headers handled by Content or Framework
                if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                request.Headers.TryAddWithoutValidation(key, val);
            }
        }

        private static void ApplyAuthentication(HttpRequestMessage request, ApiAuthDefinition auth, IDictionary<string, string>? variables)
        {
            switch (auth.Type)
            {
                case ApiAuthType.BearerToken:
                    var token = VariableResolver.Resolve(auth.BearerToken, variables).Trim();
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                    break;

                case ApiAuthType.BasicAuth:
                    var user = VariableResolver.Resolve(auth.BasicUsername, variables);
                    var pass = VariableResolver.Resolve(auth.BasicPassword, variables);
                    var rawCreds = $"{user}:{pass}";
                    var base64Creds = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCreds));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Creds);
                    break;

                case ApiAuthType.ApiKey:
                    if (auth.ApiKeyLocation == ApiKeyLocation.Header && !string.IsNullOrWhiteSpace(auth.ApiKeyName))
                    {
                        var keyName = VariableResolver.Resolve(auth.ApiKeyName, variables).Trim();
                        var keyVal = VariableResolver.Resolve(auth.ApiKeyValue, variables);
                        request.Headers.TryAddWithoutValidation(keyName, keyVal);
                    }
                    break;

                case ApiAuthType.CustomHeader:
                    if (!string.IsNullOrWhiteSpace(auth.CustomHeaderName))
                    {
                        var cName = VariableResolver.Resolve(auth.CustomHeaderName, variables).Trim();
                        var cVal = VariableResolver.Resolve(auth.CustomHeaderValue, variables);
                        request.Headers.TryAddWithoutValidation(cName, cVal);
                    }
                    break;

                case ApiAuthType.None:
                default:
                    break;
            }
        }

        private static HttpContent? CreateHttpContent(ApiRequestBodyDefinition body, IDictionary<string, string>? variables)
        {
            switch (body.BodyType)
            {
                case ApiBodyType.Json:
                    var resolvedJson = VariableResolver.Resolve(body.RawContent, variables);
                    return new StringContent(resolvedJson, Encoding.UTF8, "application/json");

                case ApiBodyType.FormUrlEncoded:
                    var pairs = body.FormUrlEncodedItems
                        .Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Key))
                        .Select(p => new KeyValuePair<string, string>(
                            VariableResolver.Resolve(p.Key, variables),
                            VariableResolver.Resolve(p.Value, variables)))
                        .ToList();
                    return new FormUrlEncodedContent(pairs);

                case ApiBodyType.FormData:
                    var multipart = new MultipartFormDataContent();
                    foreach (var item in body.FormDataItems.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Key)))
                    {
                        var key = VariableResolver.Resolve(item.Key, variables);
                        var val = VariableResolver.Resolve(item.Value, variables);
                        multipart.Add(new StringContent(val), key);
                    }
                    return multipart;

                case ApiBodyType.RawText:
                    var resolvedText = VariableResolver.Resolve(body.RawContent, variables);
                    return new StringContent(resolvedText, Encoding.UTF8, "text/plain");

                case ApiBodyType.None:
                default:
                    return null;
            }
        }

        private void ProcessExtractionRules(string responseBody, List<VariableExtractionRule> rules)
        {
            if (string.IsNullOrWhiteSpace(responseBody) || rules == null || rules.Count == 0)
                return;

            foreach (var rule in rules.Where(r => r.IsEnabled && !string.IsNullOrWhiteSpace(r.VariableName) && !string.IsNullOrWhiteSpace(r.JsonPath)))
            {
                var extracted = JsonHelper.ExtractValueByPath(responseBody, rule.JsonPath);
                if (extracted != null)
                {
                    OnVariableExtracted?.Invoke(rule.VariableName, extracted);
                }
            }
        }

        private static HttpMethod ConvertMethod(ApiMethod method) => method switch
        {
            ApiMethod.GET => HttpMethod.Get,
            ApiMethod.POST => HttpMethod.Post,
            ApiMethod.PUT => HttpMethod.Put,
            ApiMethod.DELETE => HttpMethod.Delete,
            ApiMethod.PATCH => HttpMethod.Patch,
            ApiMethod.HEAD => HttpMethod.Head,
            ApiMethod.OPTIONS => HttpMethod.Options,
            _ => HttpMethod.Get
        };
    }
}
