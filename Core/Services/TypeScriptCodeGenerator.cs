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
    public class TypeScriptCodeGenerator
    {
        private class InterfaceDefinition
        {
            public string InterfaceName { get; set; } = string.Empty;
            public Dictionary<string, PropertyDefinition> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private class PropertyDefinition
        {
            public string JsonName { get; set; } = string.Empty;
            public string TsPropertyName { get; set; } = string.Empty;
            public string TypeName { get; set; } = "any";
            public bool IsOptional { get; set; } = true;
            public InterfaceDefinition? NestedInterface { get; set; }
        }

        /// <summary>
        /// Phân tích JSON và sinh ra mã nguồn TypeScript Interfaces/Types
        /// </summary>
        public static string Generate(string json, string rootTypeName = "ResponseData")
        {
            if (string.IsNullOrWhiteSpace(json))
                return "// No response content to generate TypeScript types.";

            try
            {
                var node = JsonNode.Parse(json);
                if (node == null)
                    return "// Unable to parse JSON structure.";

                var interfaces = new List<InterfaceDefinition>();
                bool isRootArray = node is JsonArray;

                var cleanRootName = ToPascalCase(rootTypeName);
                if (string.IsNullOrEmpty(cleanRootName)) cleanRootName = "ResponseData";

                var rootInterface = new InterfaceDefinition { InterfaceName = cleanRootName };
                interfaces.Add(rootInterface);

                if (node is JsonArray rootArray)
                {
                    foreach (var item in rootArray)
                    {
                        if (item is JsonObject obj)
                        {
                            MergeObjectProperties(obj, rootInterface, interfaces);
                        }
                    }
                }
                else if (node is JsonObject rootObj)
                {
                    MergeObjectProperties(rootObj, rootInterface, interfaces);
                }

                return RenderInterfaces(interfaces, isRootArray, cleanRootName);
            }
            catch (Exception ex)
            {
                return $"// Error generating TypeScript types: {ex.Message}";
            }
        }

        /// <summary>
        /// Sinh TypeScript types từ Request Body
        /// </summary>
        public static string GenerateFromEndpointRequest(ApiEndpointDefinition endpoint, string rootTypeName = "RequestData")
        {
            var cleanRootName = ToPascalCase(rootTypeName);
            if (string.IsNullOrEmpty(cleanRootName)) cleanRootName = "RequestData";

            if (endpoint.Body.BodyType == ApiBodyType.Json && !string.IsNullOrWhiteSpace(endpoint.Body.RawContent))
            {
                return Generate(endpoint.Body.RawContent, cleanRootName);
            }

            if (endpoint.Body.BodyType == ApiBodyType.FormUrlEncoded && endpoint.Body.FormUrlEncodedItems.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("// Auto-generated TypeScript types for Form-UrlEncoded Request");
                sb.AppendLine();
                sb.AppendLine($"export interface {cleanRootName} {{");
                foreach (var item in endpoint.Body.FormUrlEncodedItems.Where(i => i.IsEnabled))
                {
                    var propName = FormatTsPropertyKey(item.Key);
                    sb.AppendLine($"  {propName}?: string;");
                }
                sb.AppendLine("}");
                return sb.ToString();
            }

            if (endpoint.Body.BodyType == ApiBodyType.FormData && endpoint.Body.FormDataItems.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("// Auto-generated TypeScript types for Form-Data Request");
                sb.AppendLine();
                sb.AppendLine($"export interface {cleanRootName} {{");
                foreach (var item in endpoint.Body.FormDataItems.Where(i => i.IsEnabled))
                {
                    var propName = FormatTsPropertyKey(item.Key);
                    sb.AppendLine($"  {propName}?: string | Blob | File;");
                }
                sb.AppendLine("}");
                return sb.ToString();
            }

            return $"// Request body type is {endpoint.Body.BodyType}. No TypeScript model needed or body is empty.";
        }

        /// <summary>
        /// Sinh đoạn mã TypeScript hoàn chỉnh (Client Fetch)
        /// </summary>
        public static string GenerateFullIntegrationSnippet(ApiEndpointDefinition endpoint, string? sampleResponseJson, string rootTypeName = "ResponseData")
        {
            var cleanRootName = ToPascalCase(rootTypeName);
            if (string.IsNullOrEmpty(cleanRootName)) cleanRootName = "ResponseData";

            var sb = new StringBuilder();
            sb.AppendLine("// =============================================================================");
            sb.AppendLine($"// TypeScript API Client for: {endpoint.Name}");
            sb.AppendLine($"// Method: {endpoint.Method} | URL: {endpoint.Url}");
            sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// =============================================================================");
            sb.AppendLine();

            // 1. Generate Models
            if (!string.IsNullOrWhiteSpace(sampleResponseJson))
            {
                sb.AppendLine("// --- Data Contracts ---");
                sb.AppendLine(Generate(sampleResponseJson, cleanRootName));
                sb.AppendLine();
            }

            // 2. Request Type if applicable
            bool hasJsonBody = endpoint.Body.BodyType == ApiBodyType.Json && !string.IsNullOrWhiteSpace(endpoint.Body.RawContent);
            string requestTypeName = $"{cleanRootName}Request";
            if (hasJsonBody)
            {
                sb.AppendLine("// --- Request Payload ---");
                sb.AppendLine(Generate(endpoint.Body.RawContent, requestTypeName));
                sb.AppendLine();
            }

            // 3. Execution Function
            sb.AppendLine("// --- API Invocation Function ---");
            string returnType = !string.IsNullOrWhiteSpace(sampleResponseJson) 
                ? (sampleResponseJson.TrimStart().StartsWith('[') ? $"{cleanRootName}[]" : cleanRootName)
                : "any";

            string methodParam = hasJsonBody ? $"payload: {requestTypeName}" : "";

            sb.AppendLine($"export async function execute{cleanRootName}({methodParam}): Promise<{returnType}> {{");
            sb.AppendLine($"  const baseUrl = \"{endpoint.Url.Replace("\"", "\\\"")}\";");
            
            // Query params
            var enabledParams = endpoint.QueryParameters.Where(p => p.IsEnabled).ToList();
            if (enabledParams.Count > 0)
            {
                sb.AppendLine("  const url = new URL(baseUrl);");
                foreach (var q in enabledParams)
                {
                    sb.AppendLine($"  url.searchParams.append(\"{q.Key.Replace("\"", "\\\"")}\", \"{q.Value.Replace("\"", "\\\"")}\");");
                }
            }
            else
            {
                sb.AppendLine("  const url = baseUrl;");
            }
            sb.AppendLine();

            // Headers
            sb.AppendLine("  const headers: Record<string, string> = {");
            if (hasJsonBody)
            {
                sb.AppendLine("    \"Content-Type\": \"application/json\",");
            }
            else if (endpoint.Body.BodyType == ApiBodyType.FormUrlEncoded)
            {
                sb.AppendLine("    \"Content-Type\": \"application/x-www-form-urlencoded\",");
            }

            foreach (var h in endpoint.Headers.Where(h => h.IsEnabled))
            {
                sb.AppendLine($"    \"{h.Key.Replace("\"", "\\\"")}\": \"{h.Value.Replace("\"", "\\\"")}\",");
            }

            // Auth
            switch (endpoint.Auth.Type)
            {
                case ApiAuthType.BearerToken when !string.IsNullOrWhiteSpace(endpoint.Auth.BearerToken):
                    sb.AppendLine($"    \"Authorization\": \"Bearer {endpoint.Auth.BearerToken.Replace("\"", "\\\"")}\",");
                    break;
                case ApiAuthType.BasicAuth when !string.IsNullOrWhiteSpace(endpoint.Auth.BasicUsername):
                    sb.AppendLine($"    \"Authorization\": `Basic ${'{'}btoa(\"{endpoint.Auth.BasicUsername}:{endpoint.Auth.BasicPassword}\"){'}'}`,");
                    break;
                case ApiAuthType.ApiKey when endpoint.Auth.ApiKeyLocation == ApiKeyLocation.Header && !string.IsNullOrWhiteSpace(endpoint.Auth.ApiKeyName):
                    sb.AppendLine($"    \"{endpoint.Auth.ApiKeyName.Replace("\"", "\\\"")}\": \"{endpoint.Auth.ApiKeyValue.Replace("\"", "\\\"")}\",");
                    break;
                case ApiAuthType.CustomHeader when !string.IsNullOrWhiteSpace(endpoint.Auth.CustomHeaderName):
                    sb.AppendLine($"    \"{endpoint.Auth.CustomHeaderName.Replace("\"", "\\\"")}\": \"{endpoint.Auth.CustomHeaderValue.Replace("\"", "\\\"")}\",");
                    break;
            }
            sb.AppendLine("  };");
            sb.AppendLine();

            // Options
            sb.AppendLine("  const options: RequestInit = {");
            sb.AppendLine($"    method: \"{endpoint.Method}\",");
            sb.AppendLine("    headers,");

            if (hasJsonBody)
            {
                sb.AppendLine("    body: JSON.stringify(payload),");
            }
            else if (endpoint.Body.BodyType == ApiBodyType.FormUrlEncoded && endpoint.Body.FormUrlEncodedItems.Count > 0)
            {
                sb.AppendLine("    body: new URLSearchParams({");
                foreach (var item in endpoint.Body.FormUrlEncodedItems.Where(i => i.IsEnabled))
                {
                    sb.AppendLine($"      \"{item.Key.Replace("\"", "\\\"")}\": \"{item.Value.Replace("\"", "\\\"")}\",");
                }
                sb.AppendLine("    }).toString(),");
            }

            sb.AppendLine("  };");
            sb.AppendLine();
            sb.AppendLine("  const response = await fetch(url.toString(), options);");
            sb.AppendLine("  if (!response.ok) {");
            sb.AppendLine("    throw new Error(`API Error: ${response.status} ${response.statusText}`);");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine($"  return await response.json() as {returnType};");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void MergeObjectProperties(JsonObject obj, InterfaceDefinition currentInterface, List<InterfaceDefinition> interfaces)
        {
            foreach (var kvp in obj)
            {
                var jsonPropName = kvp.Key;
                var valueNode = kvp.Value;
                var tsPropName = FormatTsPropertyKey(jsonPropName);

                if (!currentInterface.Properties.TryGetValue(jsonPropName, out var propDef))
                {
                    propDef = new PropertyDefinition
                    {
                        JsonName = jsonPropName,
                        TsPropertyName = tsPropName,
                        IsOptional = true
                    };
                    currentInterface.Properties[jsonPropName] = propDef;
                }

                DeterminePropertyType(valueNode, propDef, currentInterface, interfaces);
            }
        }

        private static void DeterminePropertyType(JsonNode? node, PropertyDefinition prop, InterfaceDefinition parentInterface, List<InterfaceDefinition> interfaces)
        {
            if (node == null)
            {
                if (prop.TypeName == "any") prop.TypeName = "any";
                return;
            }

            if (node is JsonValue val)
            {
                if (val.TryGetValue<bool>(out _))
                {
                    prop.TypeName = "boolean";
                }
                else if (val.TryGetValue<long>(out _) || val.TryGetValue<double>(out _))
                {
                    prop.TypeName = "number";
                }
                else
                {
                    prop.TypeName = "string";
                }
            }
            else if (node is JsonObject childObj)
            {
                var nestedName = ToPascalCase(prop.JsonName);
                if (nestedName.Equals(parentInterface.InterfaceName, StringComparison.OrdinalIgnoreCase))
                {
                    nestedName += "Detail";
                }

                var existing = interfaces.FirstOrDefault(c => c.InterfaceName.Equals(nestedName, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    existing = new InterfaceDefinition { InterfaceName = nestedName };
                    interfaces.Add(existing);
                }

                MergeObjectProperties(childObj, existing, interfaces);
                prop.TypeName = existing.InterfaceName;
                prop.NestedInterface = existing;
            }
            else if (node is JsonArray arr)
            {
                if (arr.Count == 0)
                {
                    prop.TypeName = "any[]";
                }
                else
                {
                    var firstElem = arr.FirstOrDefault(x => x != null);
                    if (firstElem is JsonObject itemObj)
                    {
                        var singularName = ToPascalCase(GetSingularName(prop.JsonName));
                        if (singularName.Equals(parentInterface.InterfaceName, StringComparison.OrdinalIgnoreCase))
                        {
                            singularName += "Item";
                        }

                        var existing = interfaces.FirstOrDefault(c => c.InterfaceName.Equals(singularName, StringComparison.OrdinalIgnoreCase));
                        if (existing == null)
                        {
                            existing = new InterfaceDefinition { InterfaceName = singularName };
                            interfaces.Add(existing);
                        }

                        foreach (var elem in arr)
                        {
                            if (elem is JsonObject elObj)
                            {
                                MergeObjectProperties(elObj, existing, interfaces);
                            }
                        }

                        prop.TypeName = $"{existing.InterfaceName}[]";
                        prop.NestedInterface = existing;
                    }
                    else if (firstElem is JsonValue itemVal)
                    {
                        if (itemVal.TryGetValue<bool>(out _)) prop.TypeName = "boolean[]";
                        else if (itemVal.TryGetValue<long>(out _) || itemVal.TryGetValue<double>(out _)) prop.TypeName = "number[]";
                        else prop.TypeName = "string[]";
                    }
                    else
                    {
                        prop.TypeName = "any[]";
                    }
                }
            }
        }

        private static string RenderInterfaces(List<InterfaceDefinition> interfaces, bool isRootArray, string rootTypeName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Auto-generated TypeScript Definitions");
            sb.AppendLine();

            // Render root alias if root is an array
            if (isRootArray)
            {
                sb.AppendLine($"export type {rootTypeName}List = {rootTypeName}[];");
                sb.AppendLine();
            }

            // Render dependencies first (bottom-up)
            var rendered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = interfaces.Count - 1; i >= 0; i--)
            {
                var iface = interfaces[i];
                if (!rendered.Add(iface.InterfaceName)) continue;

                sb.AppendLine($"export interface {iface.InterfaceName} {{");

                if (iface.Properties.Count == 0)
                {
                    sb.AppendLine("  [key: string]: any;");
                }
                else
                {
                    foreach (var prop in iface.Properties.Values)
                    {
                        var opt = prop.IsOptional ? "?" : "";
                        sb.AppendLine($"  {prop.TsPropertyName}{opt}: {prop.TypeName};");
                    }
                }

                sb.AppendLine("}");
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        public static string FormatTsPropertyKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "unknown";

            // If identifier is valid JavaScript variable name
            bool isValidIdentifier = char.IsLetter(name[0]) || name[0] == '_' || name[0] == '$';
            if (isValidIdentifier)
            {
                for (int i = 1; i < name.Length; i++)
                {
                    char c = name[i];
                    if (!char.IsLetterOrDigit(c) && c != '_' && c != '$')
                    {
                        isValidIdentifier = false;
                        break;
                    }
                }
            }

            if (isValidIdentifier)
            {
                // CamelCase preferred for standard TS properties
                return ToCamelCase(name);
            }

            // Quote if it has spaces, dashes, or special characters
            return $"\"{name.Replace("\"", "\\\"")}\"";
        }

        public static string ToCamelCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var pascal = ToPascalCase(text);
            if (pascal.Length > 0)
            {
                return char.ToLowerInvariant(pascal[0]) + pascal[1..];
            }
            return text;
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
