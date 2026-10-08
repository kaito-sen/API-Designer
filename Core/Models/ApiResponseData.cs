using System;
using System.Collections.Generic;

namespace API_Integarated.Core.Models
{
    public class ApiResponseData
    {
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public long? ContentLength { get; set; }
        public string? ContentType { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public static ApiResponseData CreateError(string errorMessage, long elapsedMs = 0)
        {
            return new ApiResponseData
            {
                StatusCode = 0,
                StatusDescription = "Client Error",
                IsSuccess = false,
                ElapsedMilliseconds = elapsedMs,
                ErrorMessage = errorMessage,
                ResponseBody = errorMessage,
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// Phân tích ResponseBody thành JsonNode để truy cập dữ liệu động mà không cần tạo trước Class Model
        /// </summary>
        public System.Text.Json.Nodes.JsonNode? AsJsonNode()
        {
            if (string.IsNullOrWhiteSpace(ResponseBody)) return null;
            try
            {
                return System.Text.Json.Nodes.JsonNode.Parse(ResponseBody);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy ResponseBody dạng JsonArray (cho các API trả về mảng danh sách [ ... ])
        /// </summary>
        public System.Text.Json.Nodes.JsonArray? AsJsonArray() => AsJsonNode() as System.Text.Json.Nodes.JsonArray;

        /// <summary>
        /// Lấy ResponseBody dạng JsonObject (cho các API trả về đối tượng { ... })
        /// </summary>
        public System.Text.Json.Nodes.JsonObject? AsJsonObject() => AsJsonNode() as System.Text.Json.Nodes.JsonObject;
    }
}
