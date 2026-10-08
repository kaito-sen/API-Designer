using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using API_Integarated.Core.Models;

namespace API_Integarated.Core.Services
{
    public class CSharpModelGenerator
    {
        private class ClassDefinition
        {
            public string ClassName { get; set; } = string.Empty;
            public Dictionary<string, PropertyDefinition> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private class PropertyDefinition
        {
            public string JsonName { get; set; } = string.Empty;
            public string CSharpName { get; set; } = string.Empty;
            public string TypeName { get; set; } = "object?";
            public bool IsNullable { get; set; } = true;
            public ClassDefinition? NestedClass { get; set; }
        }

        /// <summary>
        /// Phân tích JSON và sinh ra mã nguồn các C# Class (POCO / DTO)
        /// </summary>
        /// <param name="json">Chuỗi JSON cần phân tích</param>
        /// <param name="rootClassName">Tên lớp gốc (ví dụ: DeviceItem hoặc ResponseModel)</param>
        /// <param name="namespaceName">Namespace của các class được sinh</param>
        public static string Generate(string json, string rootClassName = "ResponseModel", string namespaceName = "YourProject.Models")
        {
            if (string.IsNullOrWhiteSpace(json))
                return "// No response content to generate model.";

            try
            {
                var node = JsonNode.Parse(json);
                if (node == null)
                    return "// Unable to parse JSON structure.";

                var classes = new List<ClassDefinition>();
                bool isRootArray = node is JsonArray;

                var cleanRootName = ToPascalCase(rootClassName);
                if (string.IsNullOrEmpty(cleanRootName)) cleanRootName = "ResponseModel";

                var rootClass = new ClassDefinition { ClassName = cleanRootName };
                classes.Add(rootClass);

                if (node is JsonArray rootArray)
                {
                    // Merge all array elements to discover all possible properties
                    foreach (var item in rootArray)
                    {
                        if (item is JsonObject obj)
                        {
                            MergeObjectProperties(obj, rootClass, classes);
                        }
                    }
                }
                else if (node is JsonObject rootObj)
                {
                    MergeObjectProperties(rootObj, rootClass, classes);
                }

                return RenderClasses(classes, isRootArray, cleanRootName, namespaceName);
            }
            catch (Exception ex)
            {
                return $"// Error generating C# model: {ex.Message}";
            }
        }

        private static void MergeObjectProperties(JsonObject obj, ClassDefinition currentClass, List<ClassDefinition> classes)
        {
            foreach (var kvp in obj)
            {
                var jsonPropName = kvp.Key;
                var valueNode = kvp.Value;
                var csharpPropName = ToPascalCase(jsonPropName);

                // Tránh trường hợp tên property trùng tên class
                if (csharpPropName.Equals(currentClass.ClassName, StringComparison.OrdinalIgnoreCase))
                {
                    csharpPropName += "Value";
                }

                if (!currentClass.Properties.TryGetValue(jsonPropName, out var propDef))
                {
                    propDef = new PropertyDefinition
                    {
                        JsonName = jsonPropName,
                        CSharpName = csharpPropName,
                        IsNullable = true
                    };
                    currentClass.Properties[jsonPropName] = propDef;
                }

                if (valueNode == null)
                {
                    propDef.IsNullable = true;
                    continue;
                }

                if (valueNode is JsonObject nestedObj)
                {
                    var nestedClassName = $"{currentClass.ClassName}_{csharpPropName}";
                    var existingNested = classes.FirstOrDefault(c => c.ClassName.Equals(nestedClassName, StringComparison.OrdinalIgnoreCase));
                    if (existingNested == null)
                    {
                        existingNested = new ClassDefinition { ClassName = nestedClassName };
                        classes.Add(existingNested);
                    }

                    propDef.TypeName = $"{nestedClassName}?";
                    propDef.NestedClass = existingNested;
                    MergeObjectProperties(nestedObj, existingNested, classes);
                }
                else if (valueNode is JsonArray nestedArr)
                {
                    // Check first non-null element
                    var sampleElem = nestedArr.FirstOrDefault(x => x != null);
                    if (sampleElem is JsonObject arrObj)
                    {
                        var itemClassName = $"{currentClass.ClassName}_{csharpPropName}Item";
                        var existingNested = classes.FirstOrDefault(c => c.ClassName.Equals(itemClassName, StringComparison.OrdinalIgnoreCase));
                        if (existingNested == null)
                        {
                            existingNested = new ClassDefinition { ClassName = itemClassName };
                            classes.Add(existingNested);
                        }

                        foreach (var elem in nestedArr.OfType<JsonObject>())
                        {
                            MergeObjectProperties(elem, existingNested, classes);
                        }

                        propDef.TypeName = $"List<{itemClassName}>?";
                    }
                    else if (sampleElem is JsonValue jv)
                    {
                        var elemType = InferPrimitiveType(jv);
                        propDef.TypeName = $"List<{elemType}>?";
                    }
                    else
                    {
                        propDef.TypeName = "List<object>?";
                    }
                }
                else if (valueNode is JsonValue val)
                {
                    var inferred = InferPrimitiveType(val);
                    // If previously object?, upgrade to inferred type
                    if (propDef.TypeName == "object?" || propDef.TypeName == "object")
                    {
                        propDef.TypeName = inferred;
                    }
                    else if (propDef.TypeName != inferred && !propDef.TypeName.StartsWith("List<"))
                    {
                        // Mixed types, fallback to object?
                        propDef.TypeName = "object?";
                    }
                }
            }
        }

        private static string InferPrimitiveType(JsonValue val)
        {
            if (val.TryGetValue<bool>(out _))
                return "bool?";

            if (val.TryGetValue<long>(out _))
                return "long?";

            if (val.TryGetValue<double>(out _))
                return "double?";

            return "string?";
        }

        public static string ToPascalCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Property";

            // Replace spaces, hyphens, and non-alphanumeric chars with space
            var sb = new StringBuilder();
            bool nextUpper = true;

            foreach (var ch in input)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(nextUpper ? char.ToUpper(ch, CultureInfo.InvariantCulture) : ch);
                    nextUpper = false;
                }
                else
                {
                    nextUpper = true;
                }
            }

            var result = sb.ToString();
            if (string.IsNullOrEmpty(result))
                return "Property";

            // If starts with digit, prepend 'Field'
            if (char.IsDigit(result[0]))
                result = "Field" + result;

            return result;
        }

        /// <summary>
        /// Sinh mã C# Model cho Request Body từ cấu hình của Endpoint
        /// </summary>
        public static string GenerateFromEndpointRequest(
            ApiEndpointDefinition endpoint, 
            string? className = null, 
            string namespaceName = "YourProject.Models")
        {
            var targetClassName = !string.IsNullOrWhiteSpace(className) 
                ? ToPascalCase(className) 
                : $"{ToPascalCase(endpoint.Name)}Request";

            switch (endpoint.Body.BodyType)
            {
                case ApiBodyType.Json:
                    if (string.IsNullOrWhiteSpace(endpoint.Body.RawContent))
                    {
                        return $"// Request Body is set to JSON but content is currently empty.\n// Please input sample JSON into the Body tab.";
                    }
                    return Generate(endpoint.Body.RawContent, targetClassName, namespaceName);

                case ApiBodyType.FormUrlEncoded:
                case ApiBodyType.FormData:
                    var items = endpoint.Body.BodyType == ApiBodyType.FormUrlEncoded
                        ? endpoint.Body.FormUrlEncodedItems
                        : endpoint.Body.FormDataItems;

                    if (items == null || items.Count == 0)
                    {
                        return $"// Request Body is set to {endpoint.Body.BodyType} but has no fields configured.\n// Add fields in the Body tab.";
                    }

                    var jsonObj = new JsonObject();
                    foreach (var item in items.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
                    {
                        jsonObj[item.Key] = string.IsNullOrEmpty(item.Value) ? "string" : item.Value;
                    }
                    return Generate(jsonObj.ToJsonString(), targetClassName, namespaceName);

                case ApiBodyType.RawText:
                    if (JsonHelper.IsValidJson(endpoint.Body.RawContent, out _))
                    {
                        return Generate(endpoint.Body.RawContent, targetClassName, namespaceName);
                    }
                    return $"// Request Body is plain text (not JSON). C# Model generation is designed for structured JSON or Form payloads.";

                case ApiBodyType.None:
                default:
                    return $"// This endpoint '{endpoint.Name}' does not use a Request Body (HTTP {endpoint.Method}).\n// Methods like GET typically send parameters via Query String or Headers instead of Body.";
            }
        }

        /// <summary>
        /// Sinh đoạn mã hoàn chỉnh kết hợp cả Request Model, Response Model và Code gọi mẫu
        /// </summary>
        public static string GenerateFullIntegrationSnippet(
            ApiEndpointDefinition endpoint, 
            string? responseJson, 
            string namespaceName = "YourProject.Models")
        {
            var baseName = ToPascalCase(endpoint.Name);
            var reqClassName = $"{baseName}Request";
            var respClassName = $"{baseName}Response";

            var sb = new StringBuilder();
            sb.AppendLine("// ===========================================================================");
            sb.AppendLine($"// AUTO-GENERATED C# INTEGRATION SNIPPET FOR: {endpoint.Name} ({endpoint.Method})");
            sb.AppendLine("// ===========================================================================");
            sb.AppendLine();
            sb.AppendLine("/*");
            sb.AppendLine("   === CÁCH GỌI TRONG BẤT KỲ DỰ ÁN NÀO ===");
            sb.AppendLine("   var api = await ApiClient.LoadFromFileAsync(\"YourApis.json\");");
            sb.AppendLine();

            bool hasRequestBody = endpoint.Body.BodyType != ApiBodyType.None &&
                                  (endpoint.Method == ApiMethod.POST || 
                                   endpoint.Method == ApiMethod.PUT || 
                                   endpoint.Method == ApiMethod.PATCH);

            if (hasRequestBody)
            {
                sb.AppendLine($"   var requestPayload = new {reqClassName}");
                sb.AppendLine("   {");
                sb.AppendLine("       // Khởi tạo các giá trị tham số...");
                sb.AppendLine("   };");
                sb.AppendLine();
                sb.AppendLine($"   var response = await api.ExecuteAsync<{respClassName}>(\"{endpoint.Name}\", requestPayload);");
            }
            else
            {
                sb.AppendLine($"   var response = await api.ExecuteAsync<{respClassName}>(\"{endpoint.Name}\");");
            }

            sb.AppendLine("   if (response != null)");
            sb.AppendLine("   {");
            sb.AppendLine("       // Xử lý kết quả trả về...");
            sb.AppendLine("   }");
            sb.AppendLine("*/");
            sb.AppendLine();

            // 1. Request Model (nếu có body)
            if (hasRequestBody)
            {
                sb.AppendLine("// ----------------- REQUEST BODY MODEL -----------------");
                var reqCode = GenerateFromEndpointRequest(endpoint, reqClassName, namespaceName);
                sb.AppendLine(reqCode);
                sb.AppendLine();
            }

            // 2. Response Model (nếu có response)
            if (!string.IsNullOrWhiteSpace(responseJson))
            {
                sb.AppendLine("// ----------------- RESPONSE BODY MODEL ----------------");
                var respCode = Generate(responseJson, respClassName, namespaceName);
                sb.AppendLine(respCode);
            }

            return sb.ToString();
        }

        private static string RenderClasses(List<ClassDefinition> classes, bool isRootArray, string rootClassName, string namespaceName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// This code was generated by REST API Designer Studio");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");

            // How to deserialize comment
            if (isRootArray)
            {
                sb.AppendLine($"    // To deserialize: var items = await api.ExecuteAsync<List<{rootClassName}>>(\"EndpointName\");");
            }
            else
            {
                sb.AppendLine($"    // To deserialize: var item = await api.ExecuteAsync<{rootClassName}>(\"EndpointName\");");
            }
            sb.AppendLine();

            foreach (var cls in classes)
            {
                sb.AppendLine($"    public class {cls.ClassName}");
                sb.AppendLine("    {");

                foreach (var prop in cls.Properties.Values)
                {
                    sb.AppendLine($"        [JsonPropertyName(\"{prop.JsonName}\")]");
                    sb.AppendLine($"        public {prop.TypeName} {prop.CSharpName} {{ get; set; }}");
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
