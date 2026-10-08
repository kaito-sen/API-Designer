using System;
using System.Collections.Generic;
using System.Linq;

namespace API_Integarated.Core.Models
{
    public class VariableExtractionRule
    {
        public bool IsEnabled { get; set; } = true;
        public string VariableName { get; set; } = string.Empty;
        public string JsonPath { get; set; } = string.Empty; // e.g. "data.token" or "$.token"
        public string? Description { get; set; }

        public VariableExtractionRule Clone()
        {
            return new VariableExtractionRule
            {
                IsEnabled = this.IsEnabled,
                VariableName = this.VariableName,
                JsonPath = this.JsonPath,
                Description = this.Description
            };
        }
    }

    public class ApiEndpointDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "New Request";
        public string? Description { get; set; }
        public ApiMethod Method { get; set; } = ApiMethod.GET;
        public string Url { get; set; } = "https://";
        public List<ApiParameter> QueryParameters { get; set; } = new();
        public List<ApiParameter> Headers { get; set; } = new();
        public ApiAuthDefinition Auth { get; set; } = new();
        public ApiRequestBodyDefinition Body { get; set; } = new();
        public List<VariableExtractionRule> ExtractionRules { get; set; } = new();
        public int TimeoutSeconds { get; set; } = 30;
        public bool IgnoreSslErrors { get; set; } = true;

        public ApiEndpointDefinition Clone()
        {
            return new ApiEndpointDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{this.Name} (Copy)",
                Description = this.Description,
                Method = this.Method,
                Url = this.Url,
                QueryParameters = this.QueryParameters.Select(p => p.Clone()).ToList(),
                Headers = this.Headers.Select(h => h.Clone()).ToList(),
                Auth = this.Auth.Clone(),
                Body = this.Body.Clone(),
                ExtractionRules = this.ExtractionRules.Select(r => r.Clone()).ToList(),
                TimeoutSeconds = this.TimeoutSeconds,
                IgnoreSslErrors = this.IgnoreSslErrors
            };
        }
    }
}
