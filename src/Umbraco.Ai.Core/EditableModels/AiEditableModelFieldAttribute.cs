namespace Umbraco.Ai.Core.EditableModels;

/// <summary>
/// Attribute to decorate properties with metadata for UI rendering.
/// Used for both provider settings and data models.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AiEditableModelFieldAttribute : Attribute
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
    /// Default value for the field.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Sort order for displaying fields in UI.
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Short alias for <see cref="AiEditableModelFieldAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AiFieldAttribute : AiEditableModelFieldAttribute;
