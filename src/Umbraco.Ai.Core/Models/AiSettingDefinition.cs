using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Represents the definition of an AI setting.
/// </summary>
public class AiSettingDefinition
{
    /// <summary>
    /// The unique key identifying the setting.
    /// </summary>
    public required string Key { get; set; }
    
    /// <summary>
    /// The label for the setting.
    /// </summary>
    public required string Label { get; set; }
    
    /// <summary>
    /// The description of the setting.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The name of the property associated with the setting.
    /// </summary>
    [JsonIgnore]
    public string? PropertyName { get; set; }

    /// <summary>
    /// The type of the property associated with the setting.
    /// </summary>
    [JsonIgnore]
    public Type? PropertyType { get; set; }
    
    // /// <summary>
    // /// The alias of the editor used for the setting.
    // /// </summary>
    // public string? EditorAlias { get; set; }
    
    /// <summary>
    /// The UI alias of the editor used for the setting.
    /// </summary>
    public string? EditorUiAlias { get; set; }
    
    // /// <summary>
    // /// The element name of the editor used for the setting.
    // /// </summary>
    // public string? EditorElementName { get; set; }
    
    /// <summary>
    /// The configuration for the editor used for the setting.
    /// </summary>
    public object? EditorConfig { get; set; }
    
    /// <summary>
    /// The default value of the setting.
    /// </summary>
    public object? DefaultValue { get; set; }
    
    /// <summary>
    /// The sort order of the setting in the UI.
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// The validation rules applied to the setting.
    /// </summary>
    public IEnumerable<ValidationAttribute> ValidationRules { get; set; } = Array.Empty<ValidationAttribute>();
}