using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace API_Integarated.Core.Services
{
    public class VariableResolver
    {
        private static readonly Regex VariableRegex = new(@"\{\{([a-zA-Z0-9_\-\$\.]+)\}\}", RegexOptions.Compiled);

        public static string Resolve(string? input, IDictionary<string, string>? variables)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return VariableRegex.Replace(input, match =>
            {
                var key = match.Groups[1].Value.Trim();

                // Dynamic built-in variables
                if (key.Equals("$guid", StringComparison.OrdinalIgnoreCase))
                    return Guid.NewGuid().ToString();

                if (key.Equals("$timestamp", StringComparison.OrdinalIgnoreCase))
                    return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                if (key.Equals("$isoTimestamp", StringComparison.OrdinalIgnoreCase))
                    return DateTime.UtcNow.ToString("o");

                if (key.Equals("$randomInt", StringComparison.OrdinalIgnoreCase))
                    return Random.Shared.Next(1, 100000).ToString();

                // Custom environment variables
                if (variables != null && variables.TryGetValue(key, out var val))
                    return val ?? string.Empty;

                // Return original if not found
                return match.Value;
            });
        }
    }
}
