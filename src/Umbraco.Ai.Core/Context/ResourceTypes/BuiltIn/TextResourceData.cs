namespace Umbraco.Ai.Core.Context.ResourceTypes.BuiltIn;

/// <summary>
/// Data model for the Text resource type.
/// </summary>
public sealed class TextResourceData
{
    /// <summary>
    /// The text content (can be plain text or markdown).
    /// </summary>
    public string? Content { get; set; }
}
