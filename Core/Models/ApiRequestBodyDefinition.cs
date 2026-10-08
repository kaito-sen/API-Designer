using System.Collections.Generic;
using System.Linq;

namespace API_Integarated.Core.Models
{
    public class ApiRequestBodyDefinition
    {
        public ApiBodyType BodyType { get; set; } = ApiBodyType.None;
        public string RawContent { get; set; } = string.Empty;
        public List<ApiParameter> FormUrlEncodedItems { get; set; } = new();
        public List<ApiParameter> FormDataItems { get; set; } = new();

        public ApiRequestBodyDefinition Clone()
        {
            return new ApiRequestBodyDefinition
            {
                BodyType = this.BodyType,
                RawContent = this.RawContent,
                FormUrlEncodedItems = this.FormUrlEncodedItems.Select(x => x.Clone()).ToList(),
                FormDataItems = this.FormDataItems.Select(x => x.Clone()).ToList()
            };
        }
    }
}
