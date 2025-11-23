using Umbraco.Ai.Core.Settings;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Defines the infrastructure components required by AI providers.
/// </summary>
public interface IAiProviderInfrastructure
{
    /// <summary>
    /// Factory for creating AI capability instances.
    /// </summary>
    IAiCapabilityFactory CapabilityFactory { get; }
    
    /// <summary>
    /// Builder for AI setting definitions.
    /// </summary>
    IAiSettingDefinitionBuilder SettingDefinitionBuilder { get; }
}