using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using API_Integarated.Core.Models;

namespace API_Integarated.Core.Services
{
    public class PythonCodeGenerator
    {
        private static readonly HashSet<string> PythonKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "False", "None", "True", "and", "as", "assert", "async", "await", "break",
            "class", "continue", "def", "del", "elif", "else", "except", "finally",
            "for", "from", "global", "if", "import", "in", "is", "lambda", "nonlocal",
            "not", "or", "pass", "raise", "return", "try", "while", "with", "yield",
            "id", "type", "format", "input", "print", "len", "open"
        };

        private class ModelDefinition
        {
            public string ModelName { get; set; } = string.Empty;
            public Dictionary<string, FieldDefinition> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private class FieldDefinition
        {
            public string JsonName { get; set; } = string.Empty;
            public string PyFieldName { get; set; } = string.Empty;
            public string TypeName { get; set; } = "Any";
            public bool NeedsAlias { get; set; } = false;
            public ModelDefinition? NestedModel { get; set; }
        }

        /// <summary>
        /// Phân tích JSON và sinh ra mã nguồn Python Pydantic v2 Models
        /// </summary>
        public static string Generate(string json, string rootModelName = "ResponseData")
        {
            if (string.IsNullOrWhiteSpace(json))
                return "# No response content to generate Python models.";

            try
            {
                var node = JsonNode.Parse(json);
                if (node == null)
                    return "# Unable to parse JSON structure.";

                var models = new List<ModelDefinition>();
                bool isRootArray = node is JsonArray;

                var cleanRootName = ToPascalCase(rootModelName);
                if (string.IsNullOrEmpty(cleanRootName)) cleanRootName = "ResponseData";

                var rootModel = new ModelDefinition { ModelName = cleanRootName };
                models.Add(rootModel);

                if (node is JsonArray rootArray)
                {
                    foreach (var item in rootArray)
                    {
                        if (item is JsonObject obj)
                        {
                            MergeObjectProperties(obj, rootModel, models);
                        }
                    }
                }
                else if (node is JsonObject rootObj)
                {
                    MergeObjectProperties(rootObj, rootModel, models);
                }

                return RenderModels(models, isRootArray, cleanRootName);
            }
            catch (Exception ex)
            {
                return $"# Error generating Python models: {ex.Message}";
            }
        }

        /// <summary>
        /// Sinh Python Pydantic models từ Request Body
        /// </summary>
        public static string GenerateFromEndpointRequest(ApiEndpointDefinition endpoint, string rootModelName = "RequestData")
        {
            var cleanRootName = ToPascalCase(rootModelName);
            if (string.IsNullOrEmpty(cleanRootName)) cleanRootName = "RequestData";

            if (endpoint.Body.BodyType == ApiBodyType.Json && !string.IsNullOrWhiteSpace(endpoint.Body.RawContent))
            {
                return Generate(endpoint.Body.RawContent, cleanRootName);
            }

            if (endpoint.Body.BodyType == ApiBodyType.FormUrlEncoded && endpoint.Body.FormUrlEncodedItems.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Auto-generated Python Pydantic Model for Form-UrlEncoded Request");
                sb.AppendLine("from typing import Optional");
                sb.AppendLine("from pydantic import BaseModel, Field");
                sb.AppendLine();
                sb.AppendLine($"class {cleanRootName}(BaseModel):");
                foreach (var item in endpoint.Body.FormUrlEncodedItems.Where(i => i.IsEnabled))
                {
                    var pyName = ToSnakeCase(item.Key);
                    bool needsAlias = pyName != item.Key || PythonKeywords.Contains(pyName);
                    if (PythonKeywords.Contains(pyName)) pyName += "_";

                    if (needsAlias)
                    {
                        sb.AppendLine($"    {pyName}: Optional[str] = Field(default=None, alias=\"{item.Key.Replace("\"", "\\\"")}\")");
                    }
                    else
                    {
                        sb.AppendLine($"    {pyName}: Optional[str] = None");
                    }
                }
                return sb.ToString();
            }

            return $"# Request body type is {endpoint.Body.BodyType}. No Python model needed or body is empty.";
        }

        /// <summary>
        /// Sinh đoạn mã Python hoàn chỉnh (Requests + Pydantic)
        /// </summary>
        public static string GenerateFullIntegrationSnippet(ApiEndpointDefinition endpoint, string? sampleResponseJson, string rootModelName = "ResponseData")
        {
            var cleanRootName = ToPascalCase(rootModelName);
            if (string.IsNullOrEmpty(cleanRootName)) cleanRootName = "ResponseData";

            var sb = new StringBuilder();
            sb.AppendLine("# =============================================================================");
            sb.AppendLine($"# Python API Client for: {endpoint.Name}");
            sb.AppendLine($"# Method: {endpoint.Method} | URL: {endpoint.Url}");
            sb.AppendLine($"# Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("# =============================================================================");
            sb.AppendLine();
            sb.AppendLine("import json");
            sb.AppendLine("from typing import Optional, List, Dict, Any");
            sb.AppendLine("import requests");
            sb.AppendLine("from pydantic import BaseModel, Field");
            sb.AppendLine();

            // 1. Data Models
            bool isArrayResponse = !string.IsNullOrWhiteSpace(sampleResponseJson) && sampleResponseJson.TrimStart().StartsWith('[');
            if (!string.IsNullOrWhiteSpace(sampleResponseJson))
            {
                sb.AppendLine("# --- Pydantic Data Models ---");
                // Strip the standard imports from generate output to avoid duplication
                var modelCode = Generate(sampleResponseJson, cleanRootName);
                var lines = modelCode.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("from ") || line.StartsWith("import ")) continue;
                    sb.AppendLine(line);
                }
                sb.AppendLine();
            }

            // 2. Request Model if JSON
            bool hasJsonBody = endpoint.Body.BodyType == ApiBodyType.Json && !string.IsNullOrWhiteSpace(endpoint.Body.RawContent);
            string requestModelName = $"{cleanRootName}Request";
            if (hasJsonBody)
            {
                sb.AppendLine("# --- Request Payload Model ---");
                var reqCode = Generate(endpoint.Body.RawContent, requestModelName);
                var lines = reqCode.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("from ") || line.StartsWith("import ")) continue;
                    sb.AppendLine(line);
                }
                sb.AppendLine();
            }

            // 3. Execution Function
            sb.AppendLine("# --- API Client Function ---");
            string funcParam = hasJsonBody ? $"payload: Optional[{requestModelName}] = None" : "";
            string returnType = !string.IsNullOrWhiteSpace(sampleResponseJson)
                ? (isArrayResponse ? $"List[{cleanRootName}]" : cleanRootName)
                : "Any";

            sb.AppendLine($"def execute_{ToSnakeCase(cleanRootName)}({funcParam}) -> {returnType}:");
            sb.AppendLine($"    url = \"{endpoint.Url.Replace("\"", "\\\"")}\"");
            sb.AppendLine();

            // Query params
            var enabledParams = endpoint.QueryParameters.Where(p => p.IsEnabled).ToList();
            if (enabledParams.Count > 0)
            {
                sb.AppendLine("    params = {");
                foreach (var q in enabledParams)
                {
                    sb.AppendLine($"        \"{q.Key.Replace("\"", "\\\"")}\": \"{q.Value.Replace("\"", "\\\"")}\",");
                }
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine("    params = None");
            }
            sb.AppendLine();

            // Headers
            sb.AppendLine("    headers = {");
            if (hasJsonBody)
            {
                sb.AppendLine("        \"Content-Type\": \"application/json\",");
            }
            else if (endpoint.Body.BodyType == ApiBodyType.FormUrlEncoded)
            {
                sb.AppendLine("        \"Content-Type\": \"application/x-www-form-urlencoded\",");
            }

            foreach (var h in endpoint.Headers.Where(h => h.IsEnabled))
            {
                sb.AppendLine($"        \"{h.Key.Replace("\"", "\\\"")}\": \"{h.Value.Replace("\"", "\\\"")}\",");
            }

            switch (endpoint.Auth.Type)
            {
                case ApiAuthType.BearerToken when !string.IsNullOrWhiteSpace(endpoint.Auth.BearerToken):
                    sb.AppendLine($"        \"Authorization\": \"Bearer {endpoint.Auth.BearerToken.Replace("\"", "\\\"")}\",");
                    break;
                case ApiAuthType.ApiKey when endpoint.Auth.ApiKeyLocation == ApiKeyLocation.Header && !string.IsNullOrWhiteSpace(endpoint.Auth.ApiKeyName):
                    sb.AppendLine($"        \"{endpoint.Auth.ApiKeyName.Replace("\"", "\\\"")}\": \"{endpoint.Auth.ApiKeyValue.Replace("\"", "\\\"")}\",");
                    break;
                case ApiAuthType.CustomHeader when !string.IsNullOrWhiteSpace(endpoint.Auth.CustomHeaderName):
                    sb.AppendLine($"        \"{endpoint.Auth.CustomHeaderName.Replace("\"", "\\\"")}\": \"{endpoint.Auth.CustomHeaderValue.Replace("\"", "\\\"")}\",");
                    break;
            }
            sb.AppendLine("    }");
            sb.AppendLine();

            // Auth for Basic Auth
            if (endpoint.Auth.Type == ApiAuthType.BasicAuth && !string.IsNullOrWhiteSpace(endpoint.Auth.BasicUsername))
            {
                sb.AppendLine($"    auth = (\"{endpoint.Auth.BasicUsername}\", \"{endpoint.Auth.BasicPassword}\")");
            }
            else
            {
                sb.AppendLine("    auth = None");
            }
            sb.AppendLine();

            // Request call
            string dataOrJson = "";
            if (hasJsonBody)
            {
                sb.AppendLine("    json_body = payload.model_dump(by_alias=True) if payload else None");
                dataOrJson = ", json=json_body";
            }
            else if (endpoint.Body.BodyType == ApiBodyType.FormUrlEncoded && endpoint.Body.FormUrlEncodedItems.Count > 0)
            {
                sb.AppendLine("    form_data = {");
                foreach (var item in endpoint.Body.FormUrlEncodedItems.Where(i => i.IsEnabled))
                {
                    sb.AppendLine($"        \"{item.Key.Replace("\"", "\\\"")}\": \"{item.Value.Replace("\"", "\\\"")}\",");
                }
                sb.AppendLine("    }");
                dataOrJson = ", data=form_data";
            }

            sb.AppendLine($"    response = requests.request(");
            sb.AppendLine($"        method=\"{endpoint.Method}\",");
            sb.AppendLine("        url=url,");
            sb.AppendLine("        params=params,");
            sb.AppendLine("        headers=headers,");
            sb.AppendLine($"        auth=auth{dataOrJson},");
            sb.AppendLine($"        timeout={endpoint.TimeoutSeconds},");
            sb.AppendLine($"        verify={(!endpoint.IgnoreSslErrors).ToString().ToLower()}");
            sb.AppendLine("    )");
            sb.AppendLine("    response.raise_for_status()");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(sampleResponseJson))
            {
                sb.AppendLine("    raw_data = response.json()");
                if (isArrayResponse)
                {
                    sb.AppendLine($"    return [{cleanRootName}.model_validate(item) for item in raw_data]");
                }
                else
                {
                    sb.AppendLine($"    return {cleanRootName}.model_validate(raw_data)");
                }
            }
            else
            {
                sb.AppendLine("    return response.json()");
            }
            sb.AppendLine();

            // Test execution entrypoint
            sb.AppendLine("if __name__ == \"__main__\":");
            sb.AppendLine($"    print(\"Calling {endpoint.Name}...\")");
            sb.AppendLine($"    result = execute_{ToSnakeCase(cleanRootName)}()");
            sb.AppendLine("    print(result)");

            return sb.ToString();
        }

        private static void MergeObjectProperties(JsonObject obj, ModelDefinition currentModel, List<ModelDefinition> models)
        {
            foreach (var kvp in obj)
            {
                var jsonPropName = kvp.Key;
                var valueNode = kvp.Value;
                var pyFieldName = ToSnakeCase(jsonPropName);
                bool needsAlias = pyFieldName != jsonPropName || PythonKeywords.Contains(pyFieldName);

                if (PythonKeywords.Contains(pyFieldName))
                {
                    pyFieldName += "_";
                }

                if (!currentModel.Fields.TryGetValue(jsonPropName, out var fieldDef))
                {
                    fieldDef = new FieldDefinition
                    {
                        JsonName = jsonPropName,
                        PyFieldName = pyFieldName,
                        NeedsAlias = needsAlias
                    };
                    currentModel.Fields[jsonPropName] = fieldDef;
                }

                DetermineFieldType(valueNode, fieldDef, currentModel, models);
            }
        }

        private static void DetermineFieldType(JsonNode? node, FieldDefinition field, ModelDefinition parentModel, List<ModelDefinition> models)
        {
            if (node == null)
            {
                if (field.TypeName == "Any") field.TypeName = "Optional[Any]";
                return;
            }

            if (node is JsonValue val)
            {
                if (val.TryGetValue<bool>(out _))
                {
                    field.TypeName = "Optional[bool]";
                }
                else if (val.TryGetValue<long>(out _))
                {
                    field.TypeName = "Optional[int]";
                }
                else if (val.TryGetValue<double>(out _))
                {
                    field.TypeName = "Optional[float]";
                }
                else
                {
                    field.TypeName = "Optional[str]";
                }
            }
            else if (node is JsonObject childObj)
            {
                var nestedName = ToPascalCase(field.JsonName);
                if (nestedName.Equals(parentModel.ModelName, StringComparison.OrdinalIgnoreCase))
                {
                    nestedName += "Detail";
                }

                var existing = models.FirstOrDefault(c => c.ModelName.Equals(nestedName, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    existing = new ModelDefinition { ModelName = nestedName };
                    models.Add(existing);
                }

                MergeObjectProperties(childObj, existing, models);
                field.TypeName = $"Optional[{existing.ModelName}]";
                field.NestedModel = existing;
            }
            else if (node is JsonArray arr)
            {
                if (arr.Count == 0)
                {
                    field.TypeName = "Optional[List[Any]]";
                }
                else
                {
                    var firstElem = arr.FirstOrDefault(x => x != null);
                    if (firstElem is JsonObject itemObj)
                    {
                        var singularName = ToPascalCase(GetSingularName(field.JsonName));
                        if (singularName.Equals(parentModel.ModelName, StringComparison.OrdinalIgnoreCase))
                        {
                            singularName += "Item";
                        }

                        var existing = models.FirstOrDefault(c => c.ModelName.Equals(singularName, StringComparison.OrdinalIgnoreCase));
                        if (existing == null)
                        {
                            existing = new ModelDefinition { ModelName = singularName };
                            models.Add(existing);
                        }

                        foreach (var elem in arr)
                        {
                            if (elem is JsonObject elObj)
                            {
                                MergeObjectProperties(elObj, existing, models);
                            }
                        }

                        field.TypeName = $"Optional[List[{existing.ModelName}]]";
                        field.NestedModel = existing;
                    }
                    else if (firstElem is JsonValue itemVal)
                    {
                        if (itemVal.TryGetValue<bool>(out _)) field.TypeName = "Optional[List[bool]]";
                        else if (itemVal.TryGetValue<long>(out _)) field.TypeName = "Optional[List[int]]";
                        else if (itemVal.TryGetValue<double>(out _)) field.TypeName = "Optional[List[float]]";
                        else field.TypeName = "Optional[List[str]]";
                    }
                    else
                    {
                        field.TypeName = "Optional[List[Any]]";
                    }
                }
            }
        }

        private static string RenderModels(List<ModelDefinition> models, bool isRootArray, string rootModelName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("from typing import Optional, List, Dict, Any");
            sb.AppendLine("from pydantic import BaseModel, Field");
            sb.AppendLine();

            var rendered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = models.Count - 1; i >= 0; i--)
            {
                var model = models[i];
                if (!rendered.Add(model.ModelName)) continue;

                sb.AppendLine($"class {model.ModelName}(BaseModel):");

                if (model.Fields.Count == 0)
                {
                    sb.AppendLine("    pass");
                }
                else
                {
                    foreach (var field in model.Fields.Values)
                    {
                        if (field.NeedsAlias)
                        {
                            sb.AppendLine($"    {field.PyFieldName}: {field.TypeName} = Field(default=None, alias=\"{field.JsonName.Replace("\"", "\\\"")}\")");
                        }
                        else
                        {
                            sb.AppendLine($"    {field.PyFieldName}: {field.TypeName} = None");
                        }
                    }
                }

                sb.AppendLine();
            }

            if (isRootArray)
            {
                sb.AppendLine($"# Root response is an array:");
                sb.AppendLine($"{rootModelName}List = List[{rootModelName}]");
            }

            return sb.ToString().TrimEnd();
        }

        public static string ToSnakeCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "data";

            var sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsLetterOrDigit(c))
                {
                    if (char.IsUpper(c))
                    {
                        if (i > 0 && (char.IsLower(text[i - 1]) || (i + 1 < text.Length && char.IsLower(text[i + 1]))))
                        {
                            sb.Append('_');
                        }
                        sb.Append(char.ToLowerInvariant(c));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (sb.Length > 0 && sb[^1] != '_')
                    {
                        sb.Append('_');
                    }
                }
            }

            var result = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(result) ? "data" : result;
        }

        public static string ToPascalCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var sb = new StringBuilder();
            bool capitalizeNext = true;

            foreach (var ch in text)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(capitalizeNext ? char.ToUpper(ch, CultureInfo.InvariantCulture) : ch);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }

            return sb.ToString();
        }

        private static string GetSingularName(string name)
        {
            if (name.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
                return name[..^3] + "y";
            if (name.EndsWith("es", StringComparison.OrdinalIgnoreCase))
                return name[..^2];
            if (name.EndsWith('s') && !name.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
                return name[..^1];
            return name;
        }
    }
}
