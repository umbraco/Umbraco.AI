using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Startup.Configuration;

/// <summary>
/// Composer to add Umbraco AI services to the Umbraco builder.
/// </summary>
public class UmbracoAIComposer : IComposer
{
    /// <summary>
    /// Composes the Umbraco AI services into the Umbraco builder.
    /// </summary>
    /// <param name="builder"></param>
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddUmbracoAI();
    }
}