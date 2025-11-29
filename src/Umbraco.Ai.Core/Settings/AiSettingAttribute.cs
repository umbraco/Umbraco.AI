namespace Umbraco.Ai.Core.Settings;

/// <summary>
/// Attribute to decorate provider setting properties with metadata for UI rendering.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AiSettingAttribute : Attribute
{
    /// <summary>
    /// Display label for the setting.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Description text for the setting.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Umbraco editor UI alias for rendering the setting.
    /// </summary>
    public string? EditorUiAlias { get; set; }

    /// <summary>
    /// Default value for the setting.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Sort order for displaying settings in UI.
    /// </summary>
    public int SortOrder { get; set; }
}
