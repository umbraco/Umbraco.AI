using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Provider.Models;

/// <summary>
/// Represents a setting definition for a provider.
/// </summary>
public class SettingDefinitionModel
{
    /// <summary>
    /// The unique key identifying the setting.
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The label for the setting.
    /// </summary>
    [Required]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The description of the setting.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The UI alias of the editor used for the setting.
    /// </summary>
    public string? EditorUiAlias { get; set; }

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
    /// Whether this setting is required.
    /// </summary>
    public bool IsRequired { get; set; }
}
