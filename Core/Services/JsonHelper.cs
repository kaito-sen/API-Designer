using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace API_Integarated.Core.Services
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions IndentedOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string FormatJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, IndentedOptions);
            }
            catch
            {
                return json; // Return as-is if parsing fails
            }
        }

        public static bool IsValidJson(string? json, out string? errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(json))
                return true;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static string? ExtractValueByPath(string json, string path)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                var cleanPath = path.Trim();
                if (cleanPath.StartsWith("$.", StringComparison.Ordinal))
                    cleanPath = cleanPath[2..];
                else if (cleanPath.StartsWith("$", StringComparison.Ordinal))
                    cleanPath = cleanPath[1..];

                var node = JsonNode.Parse(json);
                if (node == null) return null;

                var segments = cleanPath.Split(new[] { '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                JsonNode? current = node;

                foreach (var segment in segments)
                {
                    if (current == null) return null;

                    // Handle array indexing like items[0]
                    if (segment.Contains('[') && segment.EndsWith(']'))
                    {
                        var openBracket = segment.IndexOf('[');
                        var propName = segment[..openBracket];
                        var indexStr = segment[(openBracket + 1)..^1];

                        if (!string.IsNullOrEmpty(propName))
                        {
                            current = current[propName];
                        }

                        if (int.TryParse(indexStr, out int arrayIndex) && current is JsonArray arr && arrayIndex >= 0 && arrayIndex < arr.Count)
                        {
                            current = arr[arrayIndex];
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        current = current[segment];
                    }
                }

                if (current == null) return null;

                if (current is JsonValue val)
                {
                    return val.ToString();
                }

                return current.ToJsonString(IndentedOptions);
            }
            catch
            {
                return null;
            }
        }
    }
}
