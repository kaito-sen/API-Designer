using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using API_Integarated.Core.Models;
using API_Integarated.Core.Services;

namespace API_Integarated.Tests
{
    [TestClass]
    public class CoreEngineTests
    {
        [TestMethod]
        public void VariableResolver_ShouldResolveCustomAndDynamicVariables()
        {
            var vars = new Dictionary<string, string>
            {
                { "baseUrl", "https://api.example.com" },
                { "deviceId", "DEV-999" }
            };

            var template = "{{baseUrl}}/v1/devices/{{deviceId}}?token={{$guid}}&ts={{$timestamp}}";
            var resolved = VariableResolver.Resolve(template, vars);

            Assert.IsTrue(resolved.StartsWith("https://api.example.com/v1/devices/DEV-999?token="));
            Assert.IsFalse(resolved.Contains("{{baseUrl}}"));
            Assert.IsFalse(resolved.Contains("{{deviceId}}"));
            Assert.IsFalse(resolved.Contains("{{$guid}}"));
            Assert.IsFalse(resolved.Contains("{{$timestamp}}"));
        }

        [TestMethod]
        public void JsonHelper_ShouldFormatJsonAndExtractPath()
        {
            var rawJson = "{\"status\":\"OK\",\"data\":{\"deviceId\":\"D100\",\"info\":{\"firmware\":\"v1.2\"}}}";
            var formatted = JsonHelper.FormatJson(rawJson);

            Assert.IsTrue(formatted.Contains("\n"));

            var extractedStatus = JsonHelper.ExtractValueByPath(rawJson, "status");
            Assert.AreEqual("OK", extractedStatus);

            var extractedDevId = JsonHelper.ExtractValueByPath(rawJson, "data.deviceId");
            Assert.AreEqual("D100", extractedDevId);

            var extractedFw = JsonHelper.ExtractValueByPath(rawJson, "$.data.info.firmware");
            Assert.AreEqual("v1.2", extractedFw);
        }

        [TestMethod]
        public async Task ApiStorageService_ShouldSaveAndLoadCollection()
        {
            var collection = ApiStorageService.CreateDefaultSampleCollection();
            var tempPath = Path.Combine(Path.GetTempPath(), $"api_coll_test_{Guid.NewGuid():N}.json");

            try
            {
                await ApiStorageService.SaveToFileAsync(collection, tempPath);
                Assert.IsTrue(File.Exists(tempPath));

                var loaded = await ApiStorageService.LoadFromFileAsync(tempPath);
                Assert.IsNotNull(loaded);
                Assert.AreEqual(collection.Name, loaded.Name);
                Assert.AreEqual(collection.Endpoints.Count, loaded.Endpoints.Count);
                Assert.AreEqual(collection.Endpoints[0].Method, loaded.Endpoints[0].Method);
                Assert.AreEqual(collection.Endpoints[1].Body.BodyType, loaded.Endpoints[1].Body.BodyType);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        [TestMethod]
        public async Task HttpExecutionEngine_ShouldHandleLocalFailureGracefully()
        {
            var engine = new HttpExecutionEngine();
            var endpoint = new ApiEndpointDefinition
            {
                Method = ApiMethod.GET,
                Url = "https://invalid-non-existent-domain-12345678.com/api",
                TimeoutSeconds = 2
            };

            var response = await engine.ExecuteAsync(endpoint);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNotNull(response.ErrorMessage);
        }

        [TestMethod]
        public async Task ApiClient_ShouldExecuteEndpointByNameAndInjectParams()
        {
            var collection = ApiStorageService.CreateDefaultSampleCollection();
            var client = new ApiClient(collection);

            // Execute "1. Test GET Request" by name and override query params dynamically
            var response = await client.ExecuteAsync("1. Test GET Request", new 
            { 
                deviceType = "TestScanner",
                customId = "9988"
            });

            // Endpoint was found and executed
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.ResponseBody);
        }

        [TestMethod]
        public void CSharpModelGenerator_ShouldGenerateClassesForComplexJsonArray()
        {
            var json = @"[
              {
                ""id"": ""1"",
                ""name"": ""Google Pixel 6 Pro"",
                ""data"": {
                  ""color"": ""Cloudy White"",
                  ""capacity"": ""128 GB""
                }
              },
              {
                ""id"": ""2"",
                ""name"": ""Apple iPhone 12 Mini"",
                ""data"": null
              },
              {
                ""id"": ""3"",
                ""name"": ""Apple iPhone 12 Pro Max"",
                ""data"": {
                  ""color"": ""Cloudy White"",
                  ""capacity GB"": 512
                }
              },
              {
                ""id"": ""7"",
                ""name"": ""Apple MacBook Pro 16"",
                ""data"": {
                  ""year"": 2019,
                  ""price"": 1849.99,
                  ""CPU model"": ""Intel Core i9"",
                  ""Hard disk size"": ""1 TB""
                }
              }
            ]";

            var csharpCode = CSharpModelGenerator.Generate(json, "DeviceItem", "MyProject.Models");

            Assert.IsNotNull(csharpCode);
            Assert.IsTrue(csharpCode.Contains("public class DeviceItem"));
            Assert.IsTrue(csharpCode.Contains("[JsonPropertyName(\"id\")]"));
            Assert.IsTrue(csharpCode.Contains("[JsonPropertyName(\"name\")]"));
            Assert.IsTrue(csharpCode.Contains("[JsonPropertyName(\"capacity GB\")]"));
            Assert.IsTrue(csharpCode.Contains("public long? CapacityGB { get; set; }") || csharpCode.Contains("CapacityGB"));
            Assert.IsTrue(csharpCode.Contains("[JsonPropertyName(\"CPU model\")]"));
            Assert.IsTrue(csharpCode.Contains("CPUModel") || csharpCode.Contains("CpuModel"));
        }

        [TestMethod]
        public void ApiResponseData_ShouldSupportDynamicJsonArrayParsingWithoutModel()
        {
            var json = @"[
              {
                ""id"": ""1"",
                ""name"": ""Google Pixel 6 Pro"",
                ""data"": {
                  ""color"": ""Cloudy White""
                }
              }
            ]";

            var response = new ApiResponseData
            {
                StatusCode = 200,
                IsSuccess = true,
                ResponseBody = json
            };

            var arr = response.AsJsonArray();
            Assert.IsNotNull(arr);
            Assert.AreEqual(1, arr.Count);

            var firstItemName = arr[0]?["name"]?.ToString();
            Assert.AreEqual("Google Pixel 6 Pro", firstItemName);

            var firstItemColor = arr[0]?["data"]?["color"]?.ToString();
            Assert.AreEqual("Cloudy White", firstItemColor);
        }

        [TestMethod]
        public void CSharpModelGenerator_ShouldGenerateRequestModelFromEndpointBody()
        {
            var endpoint = new ApiEndpointDefinition
            {
                Name = "CreateDevice",
                Method = ApiMethod.POST,
                Url = "https://api.restful-api.dev/objects",
                Body = new ApiRequestBodyDefinition
                {
                    BodyType = ApiBodyType.Json,
                    RawContent = "{\n  \"name\": \"Apple iPad Air\",\n  \"data\": {\n    \"year\": 2019,\n    \"price\": 1199.99,\n    \"CPU model\": \"Intel Core i9\"\n  }\n}"
                }
            };

            var requestCode = CSharpModelGenerator.GenerateFromEndpointRequest(endpoint, "CreateDeviceRequest", "MyProject.Models");

            Assert.IsNotNull(requestCode);
            Assert.IsTrue(requestCode.Contains("public class CreateDeviceRequest"));
            Assert.IsTrue(requestCode.Contains("[JsonPropertyName(\"name\")]"));
            Assert.IsTrue(requestCode.Contains("[JsonPropertyName(\"CPU model\")]"));

            var fullSnippet = CSharpModelGenerator.GenerateFullIntegrationSnippet(endpoint, "{\"id\": \"100\", \"createdAt\": \"2026-10-08\"}");
            Assert.IsNotNull(fullSnippet);
            Assert.IsTrue(fullSnippet.Contains("CreateDeviceRequest"));
            Assert.IsTrue(fullSnippet.Contains("CreateDeviceResponse"));
            Assert.IsTrue(fullSnippet.Contains("api.ExecuteAsync<CreateDeviceResponse>"));
        }

        [TestMethod]
        public void TypeScriptCodeGenerator_ShouldGenerateInterfacesAndSnippet()
        {
            var json = @"[
              {
                ""id"": ""1"",
                ""name"": ""Google Pixel 6 Pro"",
                ""data"": {
                  ""color"": ""Cloudy White"",
                  ""CPU model"": ""Tensor G1"",
                  ""capacity_gb"": 128
                }
              }
            ]";

            var tsCode = TypeScriptCodeGenerator.Generate(json, "DeviceItem");
            Assert.IsNotNull(tsCode);
            Assert.IsTrue(tsCode.Contains("export interface DeviceItem"));
            Assert.IsTrue(tsCode.Contains("id?: string;"));
            Assert.IsTrue(tsCode.Contains("name?: string;"));
            Assert.IsTrue(tsCode.Contains("\"CPU model\"?: string;"));
            Assert.IsTrue(tsCode.Contains("export interface Data"));

            var endpoint = new ApiEndpointDefinition
            {
                Name = "GetDevice",
                Method = ApiMethod.GET,
                Url = "https://api.restful-api.dev/objects",
                QueryParameters = new List<ApiParameter>
                {
                    new("id", "1", null, true)
                }
            };

            var fullSnippet = TypeScriptCodeGenerator.GenerateFullIntegrationSnippet(endpoint, json, "DeviceItem");
            Assert.IsNotNull(fullSnippet);
            Assert.IsTrue(fullSnippet.Contains("export async function executeDeviceItem"));
            Assert.IsTrue(fullSnippet.Contains("url.searchParams.append(\"id\", \"1\");"));
            Assert.IsTrue(fullSnippet.Contains("fetch(url.toString(), options)"));
            Assert.IsTrue(fullSnippet.Contains("as DeviceItem[]"));
        }

        [TestMethod]
        public void TypeScriptCodeGenerator_ShouldHandleFormUrlEncodedAndFormDataRequests()
        {
            var formUrlEndpoint = new ApiEndpointDefinition
            {
                Name = "LoginEndpoint",
                Method = ApiMethod.POST,
                Url = "https://api.example.com/oauth/token",
                Body = new ApiRequestBodyDefinition
                {
                    BodyType = ApiBodyType.FormUrlEncoded,
                    FormUrlEncodedItems = new List<ApiParameter>
                    {
                        new("username", "admin", null, true),
                        new("client_secret", "sec123", null, true)
                    }
                }
            };

            var formUrlTs = TypeScriptCodeGenerator.GenerateFromEndpointRequest(formUrlEndpoint, "LoginRequest");
            Assert.IsTrue(formUrlTs.Contains("export interface LoginRequest"));
            Assert.IsTrue(formUrlTs.Contains("username?: string;"));
            Assert.IsTrue(formUrlTs.Contains("clientSecret?: string;"));

            var formDataEndpoint = new ApiEndpointDefinition
            {
                Name = "UploadEndpoint",
                Method = ApiMethod.POST,
                Url = "https://api.example.com/upload",
                Body = new ApiRequestBodyDefinition
                {
                    BodyType = ApiBodyType.FormData,
                    FormDataItems = new List<ApiParameter>
                    {
                        new("file", "doc.pdf", null, true),
                        new("tags", "contract", null, true)
                    }
                }
            };

            var formDataTs = TypeScriptCodeGenerator.GenerateFromEndpointRequest(formDataEndpoint, "UploadRequest");
            Assert.IsTrue(formDataTs.Contains("export interface UploadRequest"));
            Assert.IsTrue(formDataTs.Contains("file?: string | Blob | File;"));
            Assert.IsTrue(formDataTs.Contains("tags?: string | Blob | File;"));
        }

        [TestMethod]
        public void PythonCodeGenerator_ShouldGeneratePydanticModelsAndSnippet()
        {
            var json = @"[
              {
                ""id"": ""1"",
                ""name"": ""Google Pixel 6 Pro"",
                ""from"": ""US"",
                ""data"": {
                  ""color"": ""Cloudy White"",
                  ""CPU model"": ""Tensor G1"",
                  ""capacity_gb"": 128
                }
              }
            ]";

            var pyCode = PythonCodeGenerator.Generate(json, "DeviceItem");
            Assert.IsNotNull(pyCode);
            Assert.IsTrue(pyCode.Contains("class DeviceItem(BaseModel):"));
            Assert.IsTrue(pyCode.Contains("from_: Optional[str] = Field(default=None, alias=\"from\")"));
            Assert.IsTrue(pyCode.Contains("cpu_model: Optional[str] = Field(default=None, alias=\"CPU model\")"));
            Assert.IsTrue(pyCode.Contains("class Data(BaseModel):"));
            Assert.IsTrue(pyCode.Contains("DeviceItemList = List[DeviceItem]"));

            var endpoint = new ApiEndpointDefinition
            {
                Name = "CreateDevice",
                Method = ApiMethod.POST,
                Url = "https://api.restful-api.dev/objects",
                Body = new ApiRequestBodyDefinition
                {
                    BodyType = ApiBodyType.Json,
                    RawContent = "{\"name\": \"MacBook Pro\", \"data\": {\"year\": 2023}}"
                }
            };

            var fullSnippet = PythonCodeGenerator.GenerateFullIntegrationSnippet(endpoint, "{\"id\": \"123\", \"name\": \"MacBook Pro\"}", "CreateDevice");
            Assert.IsNotNull(fullSnippet);
            Assert.IsTrue(fullSnippet.Contains("def execute_create_device("));
            Assert.IsTrue(fullSnippet.Contains("requests.request("));
            Assert.IsTrue(fullSnippet.Contains("CreateDevice.model_validate(raw_data)"));
            Assert.IsTrue(fullSnippet.Contains("if __name__ == \"__main__\":"));
        }

        [TestMethod]
        public void PythonCodeGenerator_ShouldHandleSnakeCaseAndPythonKeywords()
        {
            Assert.AreEqual("cpu_model", PythonCodeGenerator.ToSnakeCase("CPU model"));
            Assert.AreEqual("user_id", PythonCodeGenerator.ToSnakeCase("userId"));
            Assert.AreEqual("total_cost_usd", PythonCodeGenerator.ToSnakeCase("TotalCostUSD"));
            Assert.AreEqual("device_item", PythonCodeGenerator.ToSnakeCase("DeviceItem"));

            var endpoint = new ApiEndpointDefinition
            {
                Name = "LoginForm",
                Method = ApiMethod.POST,
                Url = "https://example.com/login",
                Body = new ApiRequestBodyDefinition
                {
                    BodyType = ApiBodyType.FormUrlEncoded,
                    FormUrlEncodedItems = new List<ApiParameter>
                    {
                        new("class", "math", null, true),
                        new("user name", "alice", null, true)
                    }
                }
            };

            var pyForm = PythonCodeGenerator.GenerateFromEndpointRequest(endpoint, "LoginPayload");
            Assert.IsTrue(pyForm.Contains("class LoginPayload(BaseModel):"));
            Assert.IsTrue(pyForm.Contains("class_: Optional[str] = Field(default=None, alias=\"class\")"));
            Assert.IsTrue(pyForm.Contains("user_name: Optional[str] = Field(default=None, alias=\"user name\")"));
        }

        [TestMethod]
        public void ApiStudioViewModel_ShouldSupportLanguageSwitchingAndCodeGeneration()
        {
            var vm = new UI.ViewModels.ApiStudioViewModel();
            Assert.AreEqual("C#", vm.SelectedLanguageName);
            Assert.AreEqual("C#", vm.CurrentSyntaxLanguage);
            Assert.AreEqual("Export .cs File", vm.ExportButtonText);
            Assert.AreEqual("Gen C# Model", vm.GenerateModelButtonText);

            // Switch to TypeScript
            vm.SelectedLanguageIndex = 1;
            Assert.AreEqual("TypeScript", vm.SelectedLanguageName);
            Assert.AreEqual("JavaScript", vm.CurrentSyntaxLanguage);
            Assert.AreEqual("Export .ts File", vm.ExportButtonText);
            Assert.AreEqual("Gen TypeScript Model", vm.GenerateModelButtonText);

            // Switch to Python
            vm.SelectedLanguageIndex = 2;
            Assert.AreEqual("Python", vm.SelectedLanguageName);
            Assert.AreEqual("Python", vm.CurrentSyntaxLanguage);
            Assert.AreEqual("Export .py File", vm.ExportButtonText);
            Assert.AreEqual("Gen Python Model", vm.GenerateModelButtonText);

            // Test code generation with selected endpoint and response
            var endpointVm = new UI.ViewModels.EndpointItemViewModel
            {
                Name = "GetDevice",
                Method = ApiMethod.GET,
                Url = "https://api.restful-api.dev/objects"
            };
            vm.Endpoints.Add(endpointVm);
            vm.SelectedEndpoint = endpointVm;
            vm.LastResponse = new ApiResponseData
            {
                StatusCode = 200,
                IsSuccess = true,
                ResponseBody = "{\"id\": \"1\", \"name\": \"MacBook\"}"
            };

            // Python mode
            vm.SelectedLanguageIndex = 2;
            vm.RefreshGeneratedCode();
            Assert.IsTrue(vm.GeneratedCSharpCode.Contains("class GetDevice(BaseModel):"));

            // TypeScript mode
            vm.SelectedLanguageIndex = 1;
            Assert.IsTrue(vm.GeneratedCSharpCode.Contains("export interface GetDevice"));

            // C# mode
            vm.SelectedLanguageIndex = 0;
            Assert.IsTrue(vm.GeneratedCSharpCode.Contains("public class GetDevice"));
        }
    }
}
