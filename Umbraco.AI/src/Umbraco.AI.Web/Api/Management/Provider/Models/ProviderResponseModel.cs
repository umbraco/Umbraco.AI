using System.ComponentModel.DataAnnotations;

using Umbraco.AI.Web.Api.Management.Common.Models;

namespace Umbraco.AI.Web.Api.Management.Provider.Models;

/// <summary>
/// Full response model for a provider including settings schema.
/// </summary>
public class ProviderResponseModel
{
    /// <summary>
    /// The unique identifier of the provider.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the provider.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The capabilities supported by this provider.
    /// </summary>
    [Required]
    public IEnumerable<string> Capabilities { get; set; } = [];

    /// <summary>
    /// The settings schema for this provider.
    /// </summary>
    public EditableModelSchemaModel? SettingsSchema { get; set; }

    /// <summary>
    /// The provider-declared profile-level settings schemas (e.g. reasoning effort), keyed by
    /// capability name (e.g. "Chat"). Only capabilities that declare extra profile settings appear.
    /// Used by the UI to render additional fields on the profile editor.
    /// </summary>
    [Required]
    public IReadOnlyDictionary<string, EditableModelSchemaModel> ProfileSettingsSchemas { get; set; }
        = new Dictionary<string, EditableModelSchemaModel>();
}
