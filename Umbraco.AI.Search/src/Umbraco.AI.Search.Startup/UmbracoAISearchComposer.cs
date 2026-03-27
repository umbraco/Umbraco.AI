using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Search.Core;
using Umbraco.AI.Search.Core.Chunking;
using Umbraco.AI.Search.Core.Configuration;
using Umbraco.AI.Search.Core.Search;
using Umbraco.AI.Search.Extensions;
using Umbraco.AI.Search.Core.Notifications;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

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
/// Search core registration (<c>AddSearchCore()</c>) is called conditionally — only if no
/// other package (e.g., Examine) has already registered <c>ISearcherResolver</c>.
/// </para>
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
        // Supports an optional umbracoAiDbDSN override for a separate AI database,
        // falling back to the default umbracoDbDSN connection string.
        var providerName =
            builder.Config.GetSection("ConnectionStrings:umbracoAiDbDSN_ProviderName").Value
            ?? builder.Config.GetSection("ConnectionStrings:umbracoDbDSN_ProviderName").Value;

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

        // Workaround: IPublishedContentChangeStrategy does not handle media changes.
        // Listen to cache refresher + rebuild notifications to index media directly.
        // See: https://github.com/umbraco/Umbraco.Cms.Search/issues/108
        // TODO: Remove when the CMS Search framework supports media via RegisterContentIndex.
        builder.AddNotificationHandler<MediaCacheRefresherNotification, MediaIndexingNotificationHandler>();
        builder.AddNotificationAsyncHandler<IndexRebuildCompletedNotification, MediaIndexingNotificationHandler>();

        // Register search core services if no other package (e.g., Examine) has already done so.
        // AddSearchCore() is not idempotent (registers transients and notification handlers),
        // so we check for ISearcherResolver as a sentinel before calling it.
        // TODO: Remove guard when Umbraco.Cms.Search makes AddSearchCore() idempotent.
        // See: https://github.com/umbraco/Umbraco.Cms.Search/pull/109
        if (builder.Services.All(s => s.ServiceType != typeof(ISearcherResolver)))
        {
            builder.AddSearchCore();
        }
    }
}
