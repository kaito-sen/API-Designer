using System;
using System.Collections.Generic;
using System.Linq;

namespace API_Integarated.Core.Models
{
    public class ApiCollection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Default Collection";
        public string? Description { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public List<ApiEndpointDefinition> Endpoints { get; set; } = new();
        public List<ApiParameter> EnvironmentVariables { get; set; } = new();

        public ApiCollection Clone()
        {
            return new ApiCollection
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{this.Name} (Copy)",
                Description = this.Description,
                BaseUrl = this.BaseUrl,
                Endpoints = this.Endpoints.Select(e => e.Clone()).ToList(),
                EnvironmentVariables = this.EnvironmentVariables.Select(v => v.Clone()).ToList()
            };
        }
    }
}
