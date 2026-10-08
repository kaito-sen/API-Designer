namespace API_Integarated.Core.Models
{
    public class ApiAuthDefinition
    {
        public ApiAuthType Type { get; set; } = ApiAuthType.None;

        // Bearer Token
        public string BearerToken { get; set; } = string.Empty;

        // API Key
        public string ApiKeyName { get; set; } = "X-API-KEY";
        public string ApiKeyValue { get; set; } = string.Empty;
        public ApiKeyLocation ApiKeyLocation { get; set; } = ApiKeyLocation.Header;

        // Basic Authentication
        public string BasicUsername { get; set; } = string.Empty;
        public string BasicPassword { get; set; } = string.Empty;

        // Custom Header
        public string CustomHeaderName { get; set; } = "Authorization";
        public string CustomHeaderValue { get; set; } = string.Empty;

        public ApiAuthDefinition Clone()
        {
            return new ApiAuthDefinition
            {
                Type = this.Type,
                BearerToken = this.BearerToken,
                ApiKeyName = this.ApiKeyName,
                ApiKeyValue = this.ApiKeyValue,
                ApiKeyLocation = this.ApiKeyLocation,
                BasicUsername = this.BasicUsername,
                BasicPassword = this.BasicPassword,
                CustomHeaderName = this.CustomHeaderName,
                CustomHeaderValue = this.CustomHeaderValue
            };
        }
    }
}
