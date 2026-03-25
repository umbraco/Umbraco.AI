using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Search.Core;
using Umbraco.AI.Search.Core.Configuration;
using Umbraco.AI.Search.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Configuration;

namespace Umbraco.AI.Search.Startup;

/// <summary>
/// Registers AI vector search services with the Umbraco DI container.
/// </summary>
/// <remarks>
/// <para>
/// This Composer registers the <see cref="AIVectorIndexer"/> and <see cref="AIVectorSearcher"/>
/// as concrete types only — not as <c>IIndexer</c>/<c>ISearcher</c> — to avoid becoming the
/// default search provider and overriding Examine or other providers.
/// </para>
/// <para>
/// Prerequisite: <c>AddSearchCore()</c> must have been called by the implementor's Composer
/// before this Composer runs. Use <c>[ComposeAfter]</c> in your Composer if needed.
/// </para>
/// </remarks>
[ComposeAfter(typeof(AI.Startup.Configuration.UmbracoAIComposer))]
public sealed class UmbracoAISearchComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register concrete types only — NOT as IIndexer/ISearcher.
        // This keeps them available in DI for index registration to resolve,
        // without polluting the default IIndexer/ISearcher resolution.
        builder.Services.AddTransient<AIVectorIndexer>();
        builder.Services.AddTransient<AIVectorSearcher>();

        // Register EF Core persistence for the vector store (replaces in-memory default)
        builder.AddUmbracoAISearchPersistence();

        // Register configuration options
        builder.Services.AddOptions<AIVectorSearchOptions>()
            .BindConfiguration("Umbraco:AI:Search");

        // Register as a named index so the search framework routes requests
        // for "VectorContentIndex" to our AIVectorIndexer/AIVectorSearcher.
        builder.Services.Configure<IndexOptions>(options =>
            options.RegisterIndex<AIVectorIndexer, AIVectorSearcher>("VectorContentIndex"));
    }
}
