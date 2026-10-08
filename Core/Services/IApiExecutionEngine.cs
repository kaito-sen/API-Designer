using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API_Integarated.Core.Models;

namespace API_Integarated.Core.Services
{
    public interface IApiExecutionEngine
    {
        event Action<string, string>? OnVariableExtracted;

        Task<ApiResponseData> ExecuteAsync(
            ApiEndpointDefinition endpoint,
            IDictionary<string, string>? variables = null,
            CancellationToken cancellationToken = default);
    }
}
