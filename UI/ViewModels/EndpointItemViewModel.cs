using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using API_Integarated.Core.Models;

namespace API_Integarated.UI.ViewModels
{
    public partial class EndpointItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = "New Request";

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private ApiMethod _method = ApiMethod.GET;

        [ObservableProperty]
        private string _url = "https://";

        [ObservableProperty]
        private int _timeoutSeconds = 30;

        [ObservableProperty]
        private bool _ignoreSslErrors = true;

        // Authentication properties
        [ObservableProperty]
        private ApiAuthType _authType = ApiAuthType.None;

        [ObservableProperty]
        private string _bearerToken = string.Empty;

        [ObservableProperty]
        private string _apiKeyName = "X-API-KEY";

        [ObservableProperty]
        private string _apiKeyValue = string.Empty;

        [ObservableProperty]
        private ApiKeyLocation _apiKeyLocation = ApiKeyLocation.Header;

        [ObservableProperty]
        private string _basicUsername = string.Empty;

        [ObservableProperty]
        private string _basicPassword = string.Empty;

        [ObservableProperty]
        private string _customHeaderName = "Authorization";

        [ObservableProperty]
        private string _customHeaderValue = string.Empty;

        // Request Body properties
        [ObservableProperty]
        private ApiBodyType _bodyType = ApiBodyType.None;

        [ObservableProperty]
        private string _rawBody = string.Empty;

        public ObservableCollection<ParameterItemViewModel> QueryParameters { get; } = new();
        public ObservableCollection<ParameterItemViewModel> Headers { get; } = new();
        public ObservableCollection<ParameterItemViewModel> FormUrlEncodedItems { get; } = new();
        public ObservableCollection<ParameterItemViewModel> FormDataItems { get; } = new();
        public ObservableCollection<ExtractionRuleViewModel> ExtractionRules { get; } = new();

        public static EndpointItemViewModel FromDefinition(ApiEndpointDefinition def)
        {
            var vm = new EndpointItemViewModel
            {
                Id = def.Id,
                Name = def.Name,
                Description = def.Description,
                Method = def.Method,
                Url = def.Url,
                TimeoutSeconds = def.TimeoutSeconds,
                IgnoreSslErrors = def.IgnoreSslErrors,

                AuthType = def.Auth.Type,
                BearerToken = def.Auth.BearerToken,
                ApiKeyName = def.Auth.ApiKeyName,
                ApiKeyValue = def.Auth.ApiKeyValue,
                ApiKeyLocation = def.Auth.ApiKeyLocation,
                BasicUsername = def.Auth.BasicUsername,
                BasicPassword = def.Auth.BasicPassword,
                CustomHeaderName = def.Auth.CustomHeaderName,
                CustomHeaderValue = def.Auth.CustomHeaderValue,

                BodyType = def.Body.BodyType,
                RawBody = def.Body.RawContent
            };

            foreach (var q in def.QueryParameters)
                vm.QueryParameters.Add(ParameterItemViewModel.FromModel(q));

            foreach (var h in def.Headers)
                vm.Headers.Add(ParameterItemViewModel.FromModel(h));

            foreach (var f in def.Body.FormUrlEncodedItems)
                vm.FormUrlEncodedItems.Add(ParameterItemViewModel.FromModel(f));

            foreach (var f in def.Body.FormDataItems)
                vm.FormDataItems.Add(ParameterItemViewModel.FromModel(f));

            foreach (var r in def.ExtractionRules)
                vm.ExtractionRules.Add(ExtractionRuleViewModel.FromModel(r));

            return vm;
        }

        public ApiEndpointDefinition ToDefinition()
        {
            return new ApiEndpointDefinition
            {
                Id = this.Id,
                Name = this.Name,
                Description = this.Description,
                Method = this.Method,
                Url = this.Url,
                TimeoutSeconds = this.TimeoutSeconds,
                IgnoreSslErrors = this.IgnoreSslErrors,
                QueryParameters = this.QueryParameters.Select(p => p.ToModel()).ToList(),
                Headers = this.Headers.Select(h => h.ToModel()).ToList(),
                Auth = new ApiAuthDefinition
                {
                    Type = this.AuthType,
                    BearerToken = this.BearerToken,
                    ApiKeyName = this.ApiKeyName,
                    ApiKeyValue = this.ApiKeyValue,
                    ApiKeyLocation = this.ApiKeyLocation,
                    BasicUsername = this.BasicUsername,
                    BasicPassword = this.BasicPassword,
                    CustomHeaderName = this.CustomHeaderName,
                    CustomHeaderValue = this.CustomHeaderValue
                },
                Body = new ApiRequestBodyDefinition
                {
                    BodyType = this.BodyType,
                    RawContent = this.RawBody,
                    FormUrlEncodedItems = this.FormUrlEncodedItems.Select(f => f.ToModel()).ToList(),
                    FormDataItems = this.FormDataItems.Select(f => f.ToModel()).ToList()
                },
                ExtractionRules = this.ExtractionRules.Select(r => r.ToModel()).ToList()
            };
        }
    }
}
