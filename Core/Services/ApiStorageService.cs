using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using API_Integarated.Core.Models;

namespace API_Integarated.Core.Services
{
    public class ApiStorageService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() }
        };

        public static async Task SaveToFileAsync(ApiCollection collection, string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(collection, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        public static async Task<ApiCollection?> LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<ApiCollection>(json, JsonOptions);
        }

        public static ApiCollection CreateDefaultSampleCollection()
        {
            var collection = new ApiCollection
            {
                Name = "Sample REST API Collection",
                Description = "Pre-configured sample endpoints to test GET, POST, PUT, DELETE and Auth",
                BaseUrl = "https://httpbin.org",
                EnvironmentVariables = new()
                {
                    new ApiParameter("baseUrl", "https://httpbin.org", "Base URL for the API"),
                    new ApiParameter("token", "my-secret-bearer-token-12345", "Auth Bearer token"),
                    new ApiParameter("apiKey", "API_SEC_KEY_8899", "Custom API Key")
                }
            };

            // 1. GET Example
            var getEndpoint = new ApiEndpointDefinition
            {
                Name = "1. Test GET Request",
                Description = "Fetches information with query parameters",
                Method = ApiMethod.GET,
                Url = "{{baseUrl}}/get",
                QueryParameters = new()
                {
                    new ApiParameter("deviceType", "CameraScanner", "Device Type query parameter"),
                    new ApiParameter("status", "active", "Device status query")
                },
                Headers = new()
                {
                    new ApiParameter("Accept", "application/json", "Expected response format")
                },
                Auth = new ApiAuthDefinition
                {
                    Type = ApiAuthType.BearerToken,
                    BearerToken = "{{token}}"
                }
            };

            // 2. POST Example with JSON Body
            var postEndpoint = new ApiEndpointDefinition
            {
                Name = "2. Test POST with JSON Body",
                Description = "Creates a new resource with JSON payload",
                Method = ApiMethod.POST,
                Url = "{{baseUrl}}/post",
                Headers = new()
                {
                    new ApiParameter("X-Client-Version", "1.0.0", "Custom client header")
                },
                Auth = new ApiAuthDefinition
                {
                    Type = ApiAuthType.ApiKey,
                    ApiKeyName = "X-Api-Key",
                    ApiKeyValue = "{{apiKey}}",
                    ApiKeyLocation = ApiKeyLocation.Header
                },
                Body = new ApiRequestBodyDefinition
                {
                    BodyType = ApiBodyType.Json,
                    RawContent = "{\n  \"deviceName\": \"Sensor_A1\",\n  \"sensorType\": \"LaserDistance\",\n  \"samplingRate\": 500,\n  \"isActive\": true\n}"
                }
            };

            // 3. PUT Example with Body
            var putEndpoint = new ApiEndpointDefinition
            {
                Name = "3. Test PUT Update",
                Description = "Updates an existing resource",
                Method = ApiMethod.PUT,
                Url = "{{baseUrl}}/put",
                Body = new ApiRequestBodyDefinition
                {
                    BodyType = ApiBodyType.Json,
                    RawContent = "{\n  \"status\": \"CALIBRATING\",\n  \"updatedAt\": \"{{$isoTimestamp}}\"\n}"
                }
            };

            // 4. DELETE Example
            var deleteEndpoint = new ApiEndpointDefinition
            {
                Name = "4. Test DELETE Resource",
                Description = "Deletes an item by ID",
                Method = ApiMethod.DELETE,
                Url = "{{baseUrl}}/delete",
                QueryParameters = new()
                {
                    new ApiParameter("id", "101", "ID to delete")
                }
            };

            collection.Endpoints.Add(getEndpoint);
            collection.Endpoints.Add(postEndpoint);
            collection.Endpoints.Add(putEndpoint);
            collection.Endpoints.Add(deleteEndpoint);

            return collection;
        }
    }
}
