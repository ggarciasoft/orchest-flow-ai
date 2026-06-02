using System.Globalization;
using System.Text.RegularExpressions;

namespace OrchestFlowAI.SDK.Helpers;

/// <summary>
/// Shared placeholder resolver for use in node Execute methods.
/// Syntax: {{key}} or {{key|filter}} or {{key|filter:arg}}
///
/// Supported filters:
///   date:FORMAT      — parse value as DateTime, output with .NET format string
///                       e.g. {{startDate|date:yyyy/MM/dd}}
///   upper            — convert to upper case
///   lower            — convert to lower case
///   trim             — trim whitespace
///   default:VALUE    — use VALUE when key is missing or empty
/// </summary>
public static class PlaceholderResolver
{
    // Matches {{key}}, {{key|filter}}, {{key|filter:arg}}
    private static readonly Regex _pattern = new(
        @"\{\{([^}|]+?)(?:\|([^}:]+)(?::([^}]*))?)?\}\}",
        RegexOptions.Compiled);

    /// <summary>Replaces all {{key[|filter[:arg]]}} tokens in <paramref name="template"/> using <paramref name="inputs"/>.</summary>
    public static string Resolve(string? template, IReadOnlyDictionary<string, object?> inputs)
    {
        if (string.IsNullOrEmpty(template)) return template ?? string.Empty;
        return _pattern.Replace(template, m =>
        {
            var key    = m.Groups[1].Value.Trim();
            var filter = m.Groups[2].Success ? m.Groups[2].Value.Trim().ToLowerInvariant() : null;
            var arg    = m.Groups[3].Success ? m.Groups[3].Value : null;

            if (!inputs.TryGetValue(key, out var rawVal))
            {
                // default filter: return arg when key missing
                if (filter == "default" && arg != null) return arg;
                return m.Value; // leave unreplaced
            }

            var value = rawVal?.ToString() ?? string.Empty;

            return filter switch
            {
                "date"    => ApplyDateFormat(value, arg),
                "upper"   => value.ToUpperInvariant(),
                "lower"   => value.ToLowerInvariant(),
                "trim"    => value.Trim(),
                "default" => string.IsNullOrEmpty(value) && arg != null ? arg : value,
                null      => value,
                _         => value, // unknown filter — pass through value
            };
        });
    }

    private static string ApplyDateFormat(string value, string? format)
    {
        if (string.IsNullOrEmpty(format)) return value;
        // Try ISO 8601 first, then common formats
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out var dt))
            return dt.ToString(format, CultureInfo.InvariantCulture);
        return value; // unparseable — return raw
    }
}
