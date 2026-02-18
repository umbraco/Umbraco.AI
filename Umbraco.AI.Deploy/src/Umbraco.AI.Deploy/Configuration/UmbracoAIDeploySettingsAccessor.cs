using Microsoft.Extensions.Options;

namespace Umbraco.AI.Deploy.Configuration;

/// <summary>
/// Provides access to the current Umbraco.AI Deploy settings.
/// </summary>
public class UmbracoAIDeploySettingsAccessor(IOptionsMonitor<UmbracoAIDeploySettings> optionsMonitor)
{
    /// <summary>
    /// Gets the current Umbraco.AI Deploy settings.
    /// </summary>
    public UmbracoAIDeploySettings Settings => optionsMonitor.CurrentValue;
}
