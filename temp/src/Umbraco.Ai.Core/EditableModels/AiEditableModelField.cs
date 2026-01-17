using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Umbraco.Ai.Core.EditableModels;

/// <summary>
/// Represents a field definition for an editable model.
/// </summary>
public class AiEditableModelField
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
}
