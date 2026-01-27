using Umbraco.Ai.DevUI.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.DevUI;

/// <summary>
/// Composer for registering DevUI services and configuration.
/// </summary>
public class DevUIComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddUmbracoAiDevUI();
    }
}