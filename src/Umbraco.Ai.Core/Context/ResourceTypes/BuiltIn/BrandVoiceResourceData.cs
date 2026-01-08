using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Context.ResourceTypes.BuiltIn;

/// <summary>
/// Data model for the Brand Voice resource type.
/// </summary>
public sealed class BrandVoiceResourceData
{
    /// <summary>
    /// Description of the tone to use (e.g., "Professional but approachable").
    /// </summary>
    [AiField(EditorUiAlias = "Umb.PropertyEditorUi.TextArea", EditorConfig = "[{ \"alias\": \"rows\", \"value\": 10 }]", SortOrder = 10)]
    public string? ToneDescription { get; set; }

    /// <summary>
    /// Description of the target audience (e.g., "B2B tech decision makers").
    /// </summary>
    [AiField(EditorUiAlias = "Umb.PropertyEditorUi.TextArea", SortOrder = 20)]
    public string? TargetAudience { get; set; }

    /// <summary>
    /// Style guidelines to follow (e.g., "Use active voice, be concise").
    /// </summary>
    [AiField(EditorUiAlias = "Umb.PropertyEditorUi.TextArea", SortOrder = 30)]
    public string? StyleGuidelines { get; set; }

    /// <summary>
    /// Patterns and phrases to avoid (e.g., "Jargon, exclamation marks").
    /// </summary>
    [AiField(EditorUiAlias = "Umb.PropertyEditorUi.TextArea", SortOrder = 40)]
    public string? AvoidPatterns { get; set; }
}
