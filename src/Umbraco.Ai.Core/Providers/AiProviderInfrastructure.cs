using Umbraco.Ai.Core.Settings;

namespace Umbraco.Ai.Core.Providers;

internal sealed class AiProviderInfrastructure(
    IAiCapabilityFactory capabilityFactory,
    IAiSettingDefinitionBuilder settingDefinitionBuilder)
    : IAiProviderInfrastructure
{
    public IAiCapabilityFactory CapabilityFactory { get; } = capabilityFactory;
    
    public IAiSettingDefinitionBuilder SettingDefinitionBuilder { get; } = settingDefinitionBuilder;
}