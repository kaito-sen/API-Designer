using CommunityToolkit.Mvvm.ComponentModel;
using API_Integarated.Core.Models;

namespace API_Integarated.UI.ViewModels
{
    public partial class ParameterItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private string _key = string.Empty;

        [ObservableProperty]
        private string _value = string.Empty;

        [ObservableProperty]
        private string? _description;

        public ParameterItemViewModel() { }

        public ParameterItemViewModel(string key, string value, string? description = null, bool isEnabled = true)
        {
            Key = key;
            Value = value;
            Description = description;
            IsEnabled = isEnabled;
        }

        public static ParameterItemViewModel FromModel(ApiParameter model)
        {
            return new ParameterItemViewModel(model.Key, model.Value, model.Description, model.IsEnabled);
        }

        public ApiParameter ToModel()
        {
            return new ApiParameter(Key, Value, Description, IsEnabled);
        }
    }

    public partial class ExtractionRuleViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private string _variableName = string.Empty;

        [ObservableProperty]
        private string _jsonPath = string.Empty;

        [ObservableProperty]
        private string? _description;

        public static ExtractionRuleViewModel FromModel(VariableExtractionRule model)
        {
            return new ExtractionRuleViewModel
            {
                IsEnabled = model.IsEnabled,
                VariableName = model.VariableName,
                JsonPath = model.JsonPath,
                Description = model.Description
            };
        }

        public VariableExtractionRule ToModel()
        {
            return new VariableExtractionRule
            {
                IsEnabled = IsEnabled,
                VariableName = VariableName,
                JsonPath = JsonPath,
                Description = Description
            };
        }
    }
}
