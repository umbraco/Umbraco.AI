using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Search.Core;
using Umbraco.AI.Search.Core.Chunking;
using Umbraco.AI.Search.Core.Configuration;
using Umbraco.AI.Search.Extensions;
using Umbraco.Cms.Core;
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
        builder.Services.AddSingleton<IAITokenCounter, WordBasedAITokenCounter>();
        builder.Services.AddSingleton<IAITextChunker, RecursiveAITextChunker>();

        builder.Services.AddTransient<AIVectorIndexer>();
        builder.Services.AddTransient<AIVectorSearcher>();

        // Register the correct persistence provider based on the configured database.
        // Umbraco stores the provider name at ConnectionStrings:umbracoDbDSN_ProviderName.
        var providerName = builder.Config.GetSection("ConnectionStrings:umbracoDbDSN_ProviderName").Value;

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
            options.RegisterIndex<AIVectorIndexer, AIVectorSearcher>("VectorContentIndex"));
    }
}
