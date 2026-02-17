using Microsoft.Extensions.Options;

namespace Umbraco.AI.Deploy.Configuration;

public class UmbracoAIDeploySettingsAccessor(IOptionsMonitor<UmbracoAIDeploySettings> optionsMonitor)
{
    public UmbracoAIDeploySettings Settings => optionsMonitor.CurrentValue;
}
