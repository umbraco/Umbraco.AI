using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI file thread lifecycle provider collection
/// configuration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI file thread lifecycle provider collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI file thread lifecycle provider collection builder.</returns>
    /// <remarks>
    /// Register a provider here when a longer-lived record elsewhere in the system owns a file-store
    /// thread id, so the retention sweep never ages out its attachments just because it's been quiet a
    /// while. Example:
    /// <code>
    /// builder.AIFileThreadLifecycleProviders()
    ///     .Append&lt;ConversationFileThreadLifecycleProvider&gt;();
    /// </code>
    /// </remarks>
    public static AIFileThreadLifecycleProviderCollectionBuilder AIFileThreadLifecycleProviders(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIFileThreadLifecycleProviderCollectionBuilder>();
}
