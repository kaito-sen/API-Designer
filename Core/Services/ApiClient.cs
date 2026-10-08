using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using API_Integarated.Core.Models;

namespace API_Integarated.Core.Services
{
    /// <summary>
    /// High-level API Client để gọi các API đã được thiết kế sẵn từ file cấu hình JSON.
    /// Giúp bất kỳ project nào cũng có thể gọi API mà không cần viết lại mã HTTP thủ công.
    /// </summary>
    public class ApiClient
    {
        private readonly ApiCollection _collection;
        private readonly IApiExecutionEngine _engine;

        /// <summary>
        /// Bộ biến môi trường dùng chung (có thể ghi đè lúc runtime)
        /// </summary>
        public Dictionary<string, string> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ApiClient(ApiCollection collection, IApiExecutionEngine? engine = null)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _engine = engine ?? new HttpExecutionEngine();

            // Nạp biến môi trường từ collection
            foreach (var v in collection.EnvironmentVariables)
            {
                if (v.IsEnabled && !string.IsNullOrWhiteSpace(v.Key))
                {
                    Variables[v.Key] = v.Value;
                }
            }

            // Lắng nghe biến trích xuất từ response để tự động cập nhật
            _engine.OnVariableExtracted += (key, val) =>
            {
                Variables[key] = val;
            };
        }

        /// <summary>
        /// Tải cấu hình API đã thiết kế từ file JSON
        /// </summary>
        public static async Task<ApiClient> LoadFromFileAsync(string jsonFilePath, IApiExecutionEngine? engine = null)
        {
            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException($"Không tìm thấy file cấu hình API: {jsonFilePath}");

            var collection = await ApiStorageService.LoadFromFileAsync(jsonFilePath);
            if (collection == null)
                throw new InvalidOperationException($"File cấu hình không hợp lệ hoặc rỗng: {jsonFilePath}");

            return new ApiClient(collection, engine);
        }

        /// <summary>
        /// Thực thi gọi API theo tên đã thiết kế trong Tool
        /// </summary>
        /// <param name="endpointName">Tên endpoint đã đặt trong UI (hoặc ID)</param>
        /// <param name="parameters">Đối tượng ẩn danh (new { id = 10, token = "..." }) hoặc Dictionary chứa biến thay thế</param>
        /// <param name="cancellationToken">Token hủy gọi</param>
        public async Task<ApiResponseData> ExecuteAsync(
            string endpointName, 
            object? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var endpoint = _collection.Endpoints.FirstOrDefault(e => 
                e.Name.Equals(endpointName, StringComparison.OrdinalIgnoreCase) ||
                e.Id.Equals(endpointName, StringComparison.OrdinalIgnoreCase));

            if (endpoint == null)
            {
                throw new ArgumentException($"Không tìm thấy endpoint có tên hoặc ID: '{endpointName}' trong cấu hình.");
            }

            // Gộp biến môi trường chung và tham số truyền vào lúc gọi
            var mergedVariables = new Dictionary<string, string>(Variables, StringComparer.OrdinalIgnoreCase);

            if (parameters != null)
            {
                ExtractParametersIntoDict(parameters, mergedVariables);
            }

            return await _engine.ExecuteAsync(endpoint, mergedVariables, cancellationToken);
        }

        /// <summary>
        /// Thực thi gọi API và tự động parse ResponseBody sang kiểu dữ liệu Class C# mong muốn
        /// </summary>
        public async Task<T?> ExecuteAsync<T>(
            string endpointName, 
            object? parameters = null, 
            JsonSerializerOptions? jsonOptions = null,
            CancellationToken cancellationToken = default)
        {
            var response = await ExecuteAsync(endpointName, parameters, cancellationToken);

            if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.ResponseBody))
            {
                return default;
            }

            var options = jsonOptions ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<T>(response.ResponseBody, options);
        }

        /// <summary>
        /// Thực thi gọi API và trả về JsonNode động mà không cần tạo class trước
        /// </summary>
        public async Task<System.Text.Json.Nodes.JsonNode?> ExecuteAsJsonNodeAsync(
            string endpointName, 
            object? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var res = await ExecuteAsync(endpointName, parameters, cancellationToken);
            return res.AsJsonNode();
        }

        /// <summary>
        /// Thực thi gọi API và trả về JsonArray (dành cho danh sách [ ... ])
        /// </summary>
        public async Task<System.Text.Json.Nodes.JsonArray?> ExecuteAsJsonArrayAsync(
            string endpointName, 
            object? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var res = await ExecuteAsync(endpointName, parameters, cancellationToken);
            return res.AsJsonArray();
        }

        /// <summary>
        /// Thực thi gọi API và trả về JsonObject (dành cho đối tượng { ... })
        /// </summary>
        public async Task<System.Text.Json.Nodes.JsonObject?> ExecuteAsJsonObjectAsync(
            string endpointName, 
            object? parameters = null, 
            CancellationToken cancellationToken = default)
        {
            var res = await ExecuteAsync(endpointName, parameters, cancellationToken);
            return res.AsJsonObject();
        }

        /// <summary>
        /// Lấy danh sách tất cả các API có sẵn trong cấu hình
        /// </summary>
        public IReadOnlyList<ApiEndpointDefinition> GetEndpoints() => _collection.Endpoints.AsReadOnly();

        private static void ExtractParametersIntoDict(object parameters, Dictionary<string, string> dict)
        {
            if (parameters is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key != null && entry.Value != null)
                    {
                        dict[entry.Key.ToString()!] = entry.Value.ToString()!;
                    }
                }
                return;
            }

            // Extract từ Anonymous Object hoặc Class bằng Reflection
            var props = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (prop.CanRead)
                {
                    var val = prop.GetValue(parameters);
                    if (val != null)
                    {
                        dict[prop.Name] = val.ToString()!;
                    }
                }
            }
        }
    }
}
