namespace Umbraco.AI.Core.EditableModels;

/// <summary>
/// Attribute to decorate properties with metadata for UI rendering.
/// Used for both provider settings and data models.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AIEditableModelFieldAttribute : Attribute
{
    /// <summary>
    /// Display label for the field.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Description text for the field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Umbraco editor UI alias for rendering the field.
    /// </summary>
    public string? EditorUiAlias { get; set; }

    /// <summary>
    /// The configuration for the editor used for the field.
    /// </summary>
    public string? EditorConfig { get; set; }

    /// <summary>
    /// Sort order for displaying fields in UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Indicates whether the field holds a secret (an API key, client secret, connection string).
    /// </summary>
    /// <remarks>
    /// Marking a field sensitive has four effects: the value is encrypted at rest, it is masked in
    /// version-history diffs, it becomes the only kind of field allowed to reference a configuration
    /// key under <c>Umbraco:AI:Secrets</c>, and it renders with a masked editor (with a reveal
    /// toggle) instead of a plain text box.
    ///
    /// Set <see cref="EditorUiAlias"/> to opt out of the masked editor — masking a multi-line field,
    /// for instance, would make it unusable. Sensitive values should be strings; the persistence
    /// layer only encrypts string values.
    /// </remarks>
    public bool IsSensitive { get; set; }

    /// <summary>
    /// Optional group name used to visually group related fields in the UI.
    /// Fields with the same group name are rendered together in a separate section.
    /// The value should be a simple PascalCase identifier (e.g., "Features", "Advanced").
    /// The frontend constructs the localization key by convention: <c>#uaiGroups_{camelCase(group)}Label</c>.
    /// </summary>
    public string? Group { get; set; }
}

/// <summary>
/// Short alias for <see cref="AIEditableModelFieldAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AIFieldAttribute : AIEditableModelFieldAttribute;
