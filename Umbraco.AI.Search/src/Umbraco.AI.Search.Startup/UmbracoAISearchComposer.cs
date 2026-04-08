using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Configuration;
using Umbraco.AI.Search.Core;
using Umbraco.AI.Search.Core.Chunking;
using Umbraco.AI.Search.Core.Configuration;
using Umbraco.AI.Search.Core.Search;
using Umbraco.AI.Search.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.AI.Search.Startup;

/// <summary>
/// Registers AI vector search services with the Umbraco DI container.
/// </summary>
/// <remarks>
/// This Composer registers the <see cref="AIVectorIndexer"/> and <see cref="AIVectorSearcher"/>
/// as concrete types only — not as <c>IIndexer</c>/<c>ISearcher</c> — to avoid becoming the
/// default search provider and overriding Examine or other providers.
/// </remarks>
[ComposeAfter(typeof(AI.Startup.Configuration.UmbracoAIComposer))]
public sealed class UmbracoAISearchComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IAITokenCounter, WordBasedAITokenCounter>();
        builder.Services.AddSingleton<IAITextChunker, RecursiveAITextChunker>();

        builder.Services.AddTransient<AIVectorIndexer>();
        builder.Services.AddTransient<AIVectorSearcher>();

        // Register the correct persistence provider based on the configured database.
        // Uses AIConnectionStringResolver which checks umbracoAIDbDSN first, then falls back to umbracoDbDSN.
        var (_, providerName) = AIConnectionStringResolver.Resolve(builder.Config);

        if (providerName == Constants.ProviderNames.SQLServer)
        {
            builder.AddUmbracoAISearchSqlServer();
        }
        else
        {
            builder.AddUmbracoAISearchSqlite();
        }

        builder.Services.AddOptions<AIVectorSearchOptions>()
            .BindConfiguration("Umbraco:AI:Search");

        builder.Services.Configure<IndexOptions>(options =>
            options.RegisterContentIndex<AIVectorIndexer, AIVectorSearcher, IPublishedContentChangeStrategy>(AISearchConstants.IndexAliases.Search,
                UmbracoObjectTypes.Document, UmbracoObjectTypes.Media));

        builder.AddSearchCore();
    }
}
