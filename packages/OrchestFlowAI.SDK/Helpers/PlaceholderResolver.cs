using System.Globalization;
using System.Text.Json;
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
///
/// Dot-notation nested access: {{root.child.grandchild}} or {{items.0.name}}
///   — If the full dotted key is not found as a flat key, the resolver splits at
///     the first dot, resolves the root, then traverses the remainder. Intermediate
///     values may be JsonElement objects or JSON strings. Array segments must be
///     non-negative integer indices.
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

            object? rawVal;

            // Flat key lookup takes priority over nested resolution.
            if (!inputs.TryGetValue(key, out rawVal))
            {
                // Attempt dot-notation nested resolution only when the full key is absent.
                if (key.Contains('.'))
                {
                    rawVal = TryResolveNestedPath(inputs, key);
                }

                if (rawVal is null)
                {
                    if (filter == "default" && arg != null) return arg;
                    return m.Value; // leave unreplaced
                }
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

    /// <summary>
    /// Attempts to resolve a dot-separated path against <paramref name="inputs"/>.
    /// Returns the resolved value, or <c>null</c> when any segment cannot be traversed.
    /// </summary>
    private static object? TryResolveNestedPath(IReadOnlyDictionary<string, object?> inputs, string dottedKey)
    {
        var dotIndex = dottedKey.IndexOf('.');
        if (dotIndex < 0) return null;

        var root     = dottedKey[..dotIndex];
        var restPath = dottedKey[(dotIndex + 1)..];

        if (!inputs.TryGetValue(root, out var rootVal) || rootVal is null)
            return null;

        // Resolve rootVal into a JsonElement for uniform traversal.
        JsonElement element;
        if (rootVal is JsonElement je)
        {
            element = je;
        }
        else if (rootVal is string json)
        {
            try
            {
                // JsonDocument is disposed after we clone the root element.
                using var doc = JsonDocument.Parse(json);
                element = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }
        else
        {
            // Attempt to round-trip arbitrary objects through JSON serialisation.
            try
            {
                var serialised = JsonSerializer.Serialize(rootVal);
                using var doc  = JsonDocument.Parse(serialised);
                element        = doc.RootElement.Clone();
            }
            catch
            {
                return null;
            }
        }

        return TraverseJsonElement(element, restPath);
    }

    /// <summary>Traverses <paramref name="element"/> along the dot-separated <paramref name="path"/>.</summary>
    private static object? TraverseJsonElement(JsonElement element, string path)
    {
        var segments = path.Split('.');
        var current  = element;

        foreach (var segment in segments)
        {
            if (current.ValueKind == JsonValueKind.Object)
            {
                if (!current.TryGetProperty(segment, out current))
                    return null;
            }
            else if (current.ValueKind == JsonValueKind.Array)
            {
                if (!int.TryParse(segment, out var index) || index < 0 || index >= current.GetArrayLength())
                    return null;

                current = current[index];
            }
            else
            {
                return null;
            }
        }

        // Return the most natural CLR representation so the caller can call ToString() on it.
        return current.ValueKind switch
        {
            JsonValueKind.String  => current.GetString(),
            JsonValueKind.Number  => current.GetRawText(),
            JsonValueKind.True    => "true",
            JsonValueKind.False   => "false",
            JsonValueKind.Null    => string.Empty,
            _                     => current.GetRawText(),
        };
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
