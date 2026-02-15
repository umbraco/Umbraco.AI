using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Core.EditableModels;

/// <summary>
/// Represents a field definition for an editable model.
/// </summary>
public class AIEditableModelField
{
    /// <summary>
    /// The unique key identifying the field.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The label for the field.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// The description of the field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The name of the property associated with the field.
    /// </summary>
    [JsonIgnore]
    public string? PropertyName { get; set; }

    /// <summary>
    /// The type of the property associated with the field.
    /// </summary>
    [JsonIgnore]
    public Type? PropertyType { get; set; }

    /// <summary>
    /// The UI alias of the editor used for the field.
    /// </summary>
    public string? EditorUiAlias { get; set; }

    /// <summary>
    /// The configuration for the editor used for the field.
    /// </summary>
    public object? EditorConfig { get; set; }

    /// <summary>
    /// The default value of the field.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// The sort order of the field in the UI.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// The validation rules applied to the field.
    /// </summary>
    public IEnumerable<ValidationAttribute> ValidationRules { get; set; } = Array.Empty<ValidationAttribute>();

    /// <summary>
    /// Indicates whether the field contains sensitive data that should be encrypted at rest.
    /// When true, the field value will be encrypted during persistence and the UI may mask the value.
    /// </summary>
    public bool IsSensitive { get; set; }

    /// <summary>
    /// Optional group identifier for visually grouping related fields in the UI.
    /// Fields with the same group are rendered together in a separate section.
    /// </summary>
    public string? Group { get; set; }
}
