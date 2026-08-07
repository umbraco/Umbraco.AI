using System.Text.Json;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Helpers for reading values out of AG-UI metadata dictionaries.
/// </summary>
/// <remarks>
/// Metadata is typed as <c>IReadOnlyDictionary&lt;string, object?&gt;</c>, so values that came in
/// over the wire are <see cref="JsonElement"/> instances rather than the CLR types they represent.
/// A plain <c>as string</c> cast silently returns <c>null</c> for those, so always read through here.
/// </remarks>
internal static class AGUIMetadata
{
    /// <summary>
    /// Gets a non-empty string value from the metadata, or <c>null</c> when absent or not a string.
    /// </summary>
    public static string? GetString(IReadOnlyDictionary<string, object?>? metadata, string key)
    {
        if (metadata is null || !metadata.TryGetValue(key, out var raw))
        {
            return null;
        }

        var value = raw switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null,
        };

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
