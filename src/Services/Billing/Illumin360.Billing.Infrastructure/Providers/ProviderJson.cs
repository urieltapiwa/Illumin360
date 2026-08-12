using System.Text.Json;

namespace Illumin360.Billing.Infrastructure.Providers;

/// <summary>Tiny JSON reader for provider responses (top-level or one level of nesting).</summary>
internal static class ProviderJson
{
    /// <summary>Reads a string property, optionally from a nested object (<c>parent</c>).</summary>
    /// <param name="json">The JSON body.</param>
    /// <param name="property">The property name to read.</param>
    /// <param name="parent">Optional parent object to look inside first.</param>
    /// <returns>The string value, or null if absent/unparseable.</returns>
    public static string? ReadString(string json, string property, string? parent = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (parent is not null && root.TryGetProperty(parent, out var nested))
            {
                root = nested;
            }

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out var value))
            {
                return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
            }
        }
        catch (JsonException)
        {
            // Non-JSON / unexpected shape — treat as absent.
        }

        return null;
    }
}
