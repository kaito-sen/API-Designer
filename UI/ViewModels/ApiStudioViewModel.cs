using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using API_Integarated.Core.Models;
using API_Integarated.Core.Services;

namespace API_Integarated.UI.ViewModels
{
    public partial class ApiStudioViewModel : ObservableObject
    {
        private readonly IApiExecutionEngine _engine;
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private string _collectionName = "REST API Collection";

        [ObservableProperty]
        private string _collectionDescription = "Collection of designed endpoints";

        [ObservableProperty]
        private EndpointItemViewModel? _selectedEndpoint;

        [ObservableProperty]
        private ApiResponseData? _lastResponse;

        [ObservableProperty]
        private bool _isExecuting;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private string _formattedResponseBody = string.Empty;

        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        [ObservableProperty]
        private string _generatedCSharpCode = "// Send a request with JSON response to auto-generate code.";

        [ObservableProperty]
        private string _rootModelClassName = "ResponseModel";

        [ObservableProperty]
        private string _modelNamespace = "YourProject.Models";

        [ObservableProperty]
        private int _selectedCodeModeIndex = 0; // 0: Response Model, 1: Request Model, 2: Full Integration Snippet

        [ObservableProperty]
        private int _selectedLanguageIndex = 0; // 0: C#, 1: TypeScript, 2: Python

        [ObservableProperty]
        private int _selectedRequestTabIndex = 0; // 0: Params, 1: Auth, 2: Headers, 3: Body, 4: Extraction, 5: Settings

        [ObservableProperty]
        private int _selectedResponseTabIndex = 0; // 0: Pretty JSON, 1: Raw Body, 2: Headers, 3: Code Generator

        public string CurrentSyntaxLanguage => SelectedLanguageIndex switch
        {
            1 => "JavaScript",
            2 => "Python",
            _ => "C#"
        };

        public string SelectedLanguageName => SelectedLanguageIndex switch
        {
            1 => "TypeScript",
            2 => "Python",
            _ => "C#"
        };

        public string CopyButtonText => $"Copy {SelectedLanguageName}";
        public string ExportButtonText => $"Export .{GetFileExtension()} File";
        public string GenerateModelButtonText => $"Gen {SelectedLanguageName} Model";

        public string GetFileExtension() => SelectedLanguageIndex switch
        {
            1 => "ts",
            2 => "py",
            _ => "cs"
        };

        public ObservableCollection<EndpointItemViewModel> Endpoints { get; } = new();
        public ObservableCollection<ParameterItemViewModel> EnvironmentVariables { get; } = new();

        public ApiStudioViewModel() : this(new HttpExecutionEngine())
        {
        }

        public ApiStudioViewModel(IApiExecutionEngine engine)
        {
            _engine = engine;
            _engine.OnVariableExtracted += HandleVariableExtracted;

            // Load default sample collection so the UI is immediately ready to test
            LoadSampleCollection();
        }

        private void HandleVariableExtracted(string varName, string varValue)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var existing = EnvironmentVariables.FirstOrDefault(v => v.Key.Equals(varName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Value = varValue;
                }
                else
                {
                    EnvironmentVariables.Add(new ParameterItemViewModel(varName, varValue, "Extracted from API response"));
                }
                StatusMessage = $"Variable '{{{{{varName}}}}}' set to: {varValue}";
            });
        }

        public void LoadSampleCollection()
        {
            var sample = ApiStorageService.CreateDefaultSampleCollection();
            LoadFromModel(sample);
        }

        public void LoadFromModel(ApiCollection collection)
        {
            CollectionName = collection.Name;
            CollectionDescription = collection.Description ?? string.Empty;

            Endpoints.Clear();
            foreach (var ep in collection.Endpoints)
            {
                Endpoints.Add(EndpointItemViewModel.FromDefinition(ep));
            }

            EnvironmentVariables.Clear();
            foreach (var env in collection.EnvironmentVariables)
            {
                EnvironmentVariables.Add(ParameterItemViewModel.FromModel(env));
            }

            SelectedEndpoint = Endpoints.FirstOrDefault();
        }

        public ApiCollection ToModel()
        {
            var coll = new ApiCollection
            {
                Name = CollectionName,
                Description = CollectionDescription,
                Endpoints = Endpoints.Select(e => e.ToDefinition()).ToList(),
                EnvironmentVariables = EnvironmentVariables.Select(e => e.ToModel()).ToList()
            };
            return coll;
        }

        [RelayCommand]
        private async Task SendRequestAsync()
        {
            if (SelectedEndpoint == null) return;

            IsExecuting = true;
            StatusMessage = $"Sending {SelectedEndpoint.Method} {SelectedEndpoint.Url}...";
            LastResponse = null;
            FormattedResponseBody = string.Empty;

            _cts = new CancellationTokenSource();

            try
            {
                var variables = GetVariablesDictionary();
                var endpointDef = SelectedEndpoint.ToDefinition();

                var response = await _engine.ExecuteAsync(endpointDef, variables, _cts.Token);
                LastResponse = response;

                // Format response body if JSON
                if (!string.IsNullOrEmpty(response.ResponseBody))
                {
                    FormattedResponseBody = JsonHelper.FormatJson(response.ResponseBody);

                    // Auto-generate C# Model
                    var suggestedName = !string.IsNullOrWhiteSpace(SelectedEndpoint?.Name)
                        ? CSharpModelGenerator.ToPascalCase(SelectedEndpoint.Name)
                        : "ResponseModel";
                    RootModelClassName = suggestedName;
                    RefreshGeneratedCode();
                }

                StatusMessage = response.IsSuccess
                    ? $"Completed in {response.ElapsedMilliseconds} ms (Status: {response.StatusCode} {response.StatusDescription})"
                    : $"Failed (Status: {response.StatusCode} {response.StatusDescription}) - {response.ErrorMessage}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                LastResponse = ApiResponseData.CreateError(ex.Message);
            }
            finally
            {
                IsExecuting = false;
                _cts = null;
            }
        }

        [RelayCommand]
        private void CancelRequest()
        {
            _cts?.Cancel();
            StatusMessage = "Request cancelled by user.";
        }

        [RelayCommand]
        private void AddEndpoint()
        {
            var newEp = new EndpointItemViewModel
            {
                Name = $"New Request {Endpoints.Count + 1}",
                Method = ApiMethod.GET,
                Url = "{{baseUrl}}/"
            };
            Endpoints.Add(newEp);
            SelectedEndpoint = newEp;
        }

        [RelayCommand]
        private void CloneEndpoint()
        {
            if (SelectedEndpoint == null) return;
            var cloneDef = SelectedEndpoint.ToDefinition().Clone();
            var cloneVm = EndpointItemViewModel.FromDefinition(cloneDef);
            Endpoints.Add(cloneVm);
            SelectedEndpoint = cloneVm;
        }

        [RelayCommand]
        private void DeleteEndpoint()
        {
            if (SelectedEndpoint == null) return;
            var index = Endpoints.IndexOf(SelectedEndpoint);
            Endpoints.Remove(SelectedEndpoint);

            if (Endpoints.Count > 0)
            {
                SelectedEndpoint = Endpoints[Math.Clamp(index, 0, Endpoints.Count - 1)];
            }
            else
            {
                SelectedEndpoint = null;
            }
        }

        [RelayCommand]
        private void FormatJsonBody()
        {
            if (SelectedEndpoint == null || string.IsNullOrWhiteSpace(SelectedEndpoint.RawBody)) return;
            SelectedEndpoint.RawBody = JsonHelper.FormatJson(SelectedEndpoint.RawBody);
        }

        // --- Parameter Management Commands ---
        [RelayCommand]
        private void AddQueryParam()
        {
            SelectedEndpoint?.QueryParameters.Add(new ParameterItemViewModel("", ""));
        }

        [RelayCommand]
        private void RemoveQueryParam(ParameterItemViewModel? item)
        {
            if (item != null) SelectedEndpoint?.QueryParameters.Remove(item);
        }

        [RelayCommand]
        private void AddHeader()
        {
            SelectedEndpoint?.Headers.Add(new ParameterItemViewModel("", ""));
        }

        [RelayCommand]
        private void RemoveHeader(ParameterItemViewModel? item)
        {
            if (item != null) SelectedEndpoint?.Headers.Remove(item);
        }

        [RelayCommand]
        private void AddFormUrlItem()
        {
            SelectedEndpoint?.FormUrlEncodedItems.Add(new ParameterItemViewModel("", ""));
        }

        [RelayCommand]
        private void RemoveFormUrlItem(ParameterItemViewModel? item)
        {
            if (item != null) SelectedEndpoint?.FormUrlEncodedItems.Remove(item);
        }

        [RelayCommand]
        private void AddFormDataItem()
        {
            SelectedEndpoint?.FormDataItems.Add(new ParameterItemViewModel("", ""));
        }

        [RelayCommand]
        private void RemoveFormDataItem(ParameterItemViewModel? item)
        {
            if (item != null) SelectedEndpoint?.FormDataItems.Remove(item);
        }

        [RelayCommand]
        private void AddExtractionRule()
        {
            SelectedEndpoint?.ExtractionRules.Add(new ExtractionRuleViewModel { VariableName = "token", JsonPath = "$.token" });
        }

        [RelayCommand]
        private void RemoveExtractionRule(ExtractionRuleViewModel? item)
        {
            if (item != null) SelectedEndpoint?.ExtractionRules.Remove(item);
        }

        [RelayCommand]
        private void AddEnvVar()
        {
            EnvironmentVariables.Add(new ParameterItemViewModel("newVar", "value"));
        }

        [RelayCommand]
        private void RemoveEnvVar(ParameterItemViewModel? item)
        {
            if (item != null) EnvironmentVariables.Remove(item);
        }

        // --- Save and Load Collection ---
        [RelayCommand]
        private async Task SaveCollectionAsync()
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "JSON API Collection (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = $"{CollectionName.Replace(' ', '_')}.json"
                };

                if (sfd.ShowDialog() == true)
                {
                    var model = ToModel();
                    await ApiStorageService.SaveToFileAsync(model, sfd.FileName);
                    CurrentFilePath = sfd.FileName;
                    StatusMessage = $"Saved collection to {sfd.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task LoadCollectionAsync()
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "JSON API Collection (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".json"
                };

                if (ofd.ShowDialog() == true)
                {
                    var model = await ApiStorageService.LoadFromFileAsync(ofd.FileName);
                    if (model != null)
                    {
                        LoadFromModel(model);
                        CurrentFilePath = ofd.FileName;
                        StatusMessage = $"Loaded collection from {ofd.FileName}";
                    }
                    else
                    {
                        StatusMessage = "Failed to load collection (file empty or invalid format).";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Load error: {ex.Message}";
            }
        }

        partial void OnSelectedCodeModeIndexChanged(int value)
        {
            RefreshGeneratedCode();
        }

        partial void OnSelectedLanguageIndexChanged(int value)
        {
            OnPropertyChanged(nameof(CurrentSyntaxLanguage));
            OnPropertyChanged(nameof(SelectedLanguageName));
            OnPropertyChanged(nameof(CopyButtonText));
            OnPropertyChanged(nameof(ExportButtonText));
            OnPropertyChanged(nameof(GenerateModelButtonText));
            RefreshGeneratedCode();
        }

        partial void OnSelectedEndpointChanged(EndpointItemViewModel? value)
        {
            if (value != null && !string.IsNullOrWhiteSpace(value.Name))
            {
                RootModelClassName = CSharpModelGenerator.ToPascalCase(value.Name);
            }
            RefreshGeneratedCode();
        }

        partial void OnRootModelClassNameChanged(string value)
        {
            RefreshGeneratedCode();
        }

        public void RefreshGeneratedCode()
        {
            if (SelectedEndpoint == null) return;

            var endpointDef = SelectedEndpoint.ToDefinition();
            var respJson = LastResponse?.ResponseBody;

            switch (SelectedLanguageIndex)
            {
                case 1: // TypeScript
                    RefreshTypeScriptCode(endpointDef, respJson);
                    break;
                case 2: // Python
                    RefreshPythonCode(endpointDef, respJson);
                    break;
                case 0: // C#
                default:
                    RefreshCSharpCode(endpointDef, respJson);
                    break;
            }
        }

        private void RefreshCSharpCode(ApiEndpointDefinition endpointDef, string? respJson)
        {
            switch (SelectedCodeModeIndex)
            {
                case 1: // Request Model
                    GeneratedCSharpCode = CSharpModelGenerator.GenerateFromEndpointRequest(
                        endpointDef,
                        GetRequestTypeName(),
                        ModelNamespace);
                    break;

                case 2: // Full Snippet
                    GeneratedCSharpCode = CSharpModelGenerator.GenerateFullIntegrationSnippet(
                        endpointDef,
                        respJson,
                        ModelNamespace);
                    break;

                case 0: // Response Model
                default:
                    if (!string.IsNullOrWhiteSpace(respJson))
                    {
                        GeneratedCSharpCode = CSharpModelGenerator.Generate(
                            respJson,
                            RootModelClassName,
                            ModelNamespace);
                    }
                    else
                    {
                        GeneratedCSharpCode = "// No response received yet. Click SEND to execute API and generate Response Model.";
                    }
                    break;
            }
        }

        private void RefreshTypeScriptCode(ApiEndpointDefinition endpointDef, string? respJson)
        {
            switch (SelectedCodeModeIndex)
            {
                case 1: // Request Model
                    GeneratedCSharpCode = TypeScriptCodeGenerator.GenerateFromEndpointRequest(
                        endpointDef,
                        GetRequestTypeName());
                    break;

                case 2: // Full Snippet
                    GeneratedCSharpCode = TypeScriptCodeGenerator.GenerateFullIntegrationSnippet(
                        endpointDef,
                        respJson,
                        RootModelClassName);
                    break;

                case 0: // Response Model
                default:
                    if (!string.IsNullOrWhiteSpace(respJson))
                    {
                        GeneratedCSharpCode = TypeScriptCodeGenerator.Generate(
                            respJson,
                            RootModelClassName);
                    }
                    else
                    {
                        GeneratedCSharpCode = "// No response received yet. Click SEND to execute API and generate TypeScript types.";
                    }
                    break;
            }
        }

        private void RefreshPythonCode(ApiEndpointDefinition endpointDef, string? respJson)
        {
            switch (SelectedCodeModeIndex)
            {
                case 1: // Request Model
                    GeneratedCSharpCode = PythonCodeGenerator.GenerateFromEndpointRequest(
                        endpointDef,
                        GetRequestTypeName());
                    break;

                case 2: // Full Snippet
                    GeneratedCSharpCode = PythonCodeGenerator.GenerateFullIntegrationSnippet(
                        endpointDef,
                        respJson,
                        RootModelClassName);
                    break;

                case 0: // Response Model
                default:
                    if (!string.IsNullOrWhiteSpace(respJson))
                    {
                        GeneratedCSharpCode = PythonCodeGenerator.Generate(
                            respJson,
                            RootModelClassName);
                    }
                    else
                    {
                        GeneratedCSharpCode = "# No response received yet. Click SEND to execute API and generate Python Pydantic models.";
                    }
                    break;
            }
        }

        private string GetRequestTypeName()
        {
            return RootModelClassName.EndsWith("Response", StringComparison.OrdinalIgnoreCase)
                ? RootModelClassName[..^8] + "Request"
                : (RootModelClassName.EndsWith("Model", StringComparison.OrdinalIgnoreCase)
                    ? RootModelClassName[..^5] + "Request"
                    : $"{RootModelClassName}Request");
        }

        [RelayCommand]
        public void GenerateCSharpModel()
        {
            RefreshGeneratedCode();
            StatusMessage = $"Updated {SelectedLanguageName} Code ({GetModeName()})";
        }

        [RelayCommand]
        public void GenerateRequestCSharpModel()
        {
            SelectedCodeModeIndex = 1; // Switch to Request Model
            RefreshGeneratedCode();
            if (!string.IsNullOrEmpty(GeneratedCSharpCode))
            {
                Clipboard.SetText(GeneratedCSharpCode);
                StatusMessage = $"Request {SelectedLanguageName} Model generated and copied to clipboard!";
            }
        }

        private string GetModeName() => SelectedCodeModeIndex switch
        {
            1 => "Request Model",
            2 => "Full Integration Code",
            _ => "Response Model"
        };

        [RelayCommand]
        public void CopyCSharpModel()
        {
            if (!string.IsNullOrEmpty(GeneratedCSharpCode))
            {
                Clipboard.SetText(GeneratedCSharpCode);
                StatusMessage = $"{SelectedLanguageName} code copied to clipboard!";
            }
        }

        [RelayCommand]
        public async Task ExportCSharpModelAsync()
        {
            try
            {
                var ext = GetFileExtension();
                var lang = SelectedLanguageName;
                var filter = ext switch
                {
                    "ts" => "TypeScript Source File (*.ts)|*.ts|All Files (*.*)|*.*",
                    "py" => "Python Source File (*.py)|*.py|All Files (*.*)|*.*",
                    _ => "C# Source File (*.cs)|*.cs|All Files (*.*)|*.*"
                };
                var defaultFileName = ext switch
                {
                    "py" => $"{PythonCodeGenerator.ToSnakeCase(RootModelClassName)}.py",
                    _ => $"{CSharpModelGenerator.ToPascalCase(RootModelClassName)}.{ext}"
                };

                var sfd = new SaveFileDialog
                {
                    Filter = filter,
                    DefaultExt = $".{ext}",
                    FileName = defaultFileName
                };

                if (sfd.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllTextAsync(sfd.FileName, GeneratedCSharpCode);
                    StatusMessage = $"Exported {lang} code to {sfd.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }

        private Dictionary<string, string> GetVariablesDictionary()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var env in EnvironmentVariables.Where(v => v.IsEnabled && !string.IsNullOrWhiteSpace(v.Key)))
            {
                dict[env.Key.Trim()] = env.Value;
            }
            return dict;
        }
    }
}
