using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using API_Integarated.Core.Models;
using API_Integarated.Core.Services;
using API_Integarated.UI.ViewModels;
using API_Integarated.UI.Views;

namespace API_Integarated.Tests
{
    [TestClass]
    public class GenerateDocsVisuals
    {
        [TestMethod]
        public void RenderScreenshots()
        {
            var baseDir = AppContext.BaseDirectory;
            while (!Directory.Exists(Path.Combine(baseDir, "Core")) && Directory.GetParent(baseDir) != null)
            {
                baseDir = Directory.GetParent(baseDir)!.FullName;
            }
            var outputDir = Path.Combine(baseDir, "docs", "images");
            Directory.CreateDirectory(outputDir);

            var sampleJson = @"[
  {
    ""id"": ""1"",
    ""name"": ""Google Pixel 6 Pro"",
    ""data"": {
      ""color"": ""Cloudy White"",
      ""capacity"": ""128 GB"",
      ""price"": 899.00
    }
  },
  {
    ""id"": ""2"",
    ""name"": ""Apple iPhone 15 Pro"",
    ""data"": {
      ""color"": ""Natural Titanium"",
      ""CPU model"": ""A17 Pro"",
      ""price"": 999.99
    }
  }
]";

            var thread = new Thread(() =>
            {
                if (Application.Current == null)
                {
                    new Application();
                }

                var vm = new ApiStudioViewModel();
                var control = new ApiStudioControl { DataContext = vm };
                var window = new Window
                {
                    Width = 1280,
                    Height = 800,
                    Content = control,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000
                };

                window.Show();
                DoEvents();

                // Setup endpoints
                vm.Endpoints.Clear();
                var getEp = new EndpointItemViewModel
                {
                    Name = "GetDevices",
                    Method = ApiMethod.GET,
                    Url = "https://api.restful-api.dev/objects"
                };
                getEp.QueryParameters.Add(new ParameterItemViewModel("limit", "10", "Max results"));
                getEp.QueryParameters.Add(new ParameterItemViewModel("sort", "desc", "Sort direction"));
                vm.Endpoints.Add(getEp);

                var postEp = new EndpointItemViewModel
                {
                    Name = "CreateDevice",
                    Method = ApiMethod.POST,
                    Url = "https://api.restful-api.dev/objects",
                    BodyType = ApiBodyType.Json,
                    RawBody = "{\n  \"name\": \"Apple MacBook Pro 16\",\n  \"data\": {\n    \"year\": 2026,\n    \"price\": 2499.99,\n    \"CPU model\": \"Apple M4 Max\"\n  }\n}"
                };
                vm.Endpoints.Add(postEp);

                var putEp = new EndpointItemViewModel
                {
                    Name = "UpdateDevice",
                    Method = ApiMethod.PUT,
                    Url = "https://api.restful-api.dev/objects/1"
                };
                vm.Endpoints.Add(putEp);

                var delEp = new EndpointItemViewModel
                {
                    Name = "DeleteDevice",
                    Method = ApiMethod.DELETE,
                    Url = "https://api.restful-api.dev/objects/1"
                };
                vm.Endpoints.Add(delEp);

                vm.SelectedEndpoint = getEp;

                var resp = new ApiResponseData
                {
                    StatusCode = 200,
                    StatusDescription = "OK",
                    IsSuccess = true,
                    ElapsedMilliseconds = 42,
                    ResponseBody = sampleJson,
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json; charset=utf-8" },
                        { "Server", "cloudflare" },
                        { "Date", DateTime.UtcNow.ToString("R") }
                    }
                };
                vm.LastResponse = resp;
                vm.FormattedResponseBody = JsonHelper.FormatJson(sampleJson);
                vm.StatusMessage = "Completed in 42 ms (Status: 200 OK)";
                vm.RootModelClassName = "DeviceItem";

                // Frame 1: Pretty JSON Response
                vm.SelectedRequestTabIndex = 0;
                vm.SelectedResponseTabIndex = 0;
                DoEvents();
                SaveVisual(control, Path.Combine(outputDir, "frame1_json.png"));
                SaveVisual(control, Path.Combine(outputDir, "studio_preview.png"));

                // Frame 2: Code Generator - C#
                vm.SelectedResponseTabIndex = 3; // Code Generator
                vm.SelectedLanguageIndex = 0;   // C#
                vm.SelectedCodeModeIndex = 0;   // Response Model
                vm.RefreshGeneratedCode();
                DoEvents();
                SaveVisual(control, Path.Combine(outputDir, "frame2_csharp.png"));

                // Frame 3: Code Generator - TypeScript
                vm.SelectedLanguageIndex = 1;   // TypeScript
                vm.RefreshGeneratedCode();
                DoEvents();
                SaveVisual(control, Path.Combine(outputDir, "frame3_typescript.png"));

                // Frame 4: Code Generator - Python
                vm.SelectedLanguageIndex = 2;   // Python
                vm.RefreshGeneratedCode();
                DoEvents();
                SaveVisual(control, Path.Combine(outputDir, "frame4_python.png"));

                // Frame 5: Code Generator - Full Snippet Python
                vm.SelectedCodeModeIndex = 2;   // Full Snippet
                vm.RefreshGeneratedCode();
                DoEvents();
                SaveVisual(control, Path.Combine(outputDir, "frame5_python_snippet.png"));

                // Frame 6: Request Body Editor (POST endpoint)
                vm.SelectedEndpoint = postEp;
                vm.SelectedRequestTabIndex = 3; // Body Tab
                vm.SelectedResponseTabIndex = 0;
                DoEvents();
                SaveVisual(control, Path.Combine(outputDir, "frame6_request_body.png"));

                window.Close();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.IsTrue(File.Exists(Path.Combine(outputDir, "studio_preview.png")));
        }

        private static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(f =>
            {
                ((DispatcherFrame)f).Continue = false;
                return null;
            }), frame);
            Dispatcher.PushFrame(frame);
            Thread.Sleep(150);
        }

        private static void SaveVisual(Visual visual, string filePath)
        {
            var rtb = new RenderTargetBitmap(1280, 800, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var fs = File.Create(filePath);
            encoder.Save(fs);
        }
    }
}
