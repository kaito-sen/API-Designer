namespace API_Integarated.Core.Models
{
    public class ApiParameter
    {
        public bool IsEnabled { get; set; } = true;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ApiParameter() { }

        public ApiParameter(string key, string value, string? description = null, bool isEnabled = true)
        {
            Key = key;
            Value = value;
            Description = description;
            IsEnabled = isEnabled;
        }

        public ApiParameter Clone()
        {
            return new ApiParameter(Key, Value, Description, IsEnabled);
        }
    }
}
