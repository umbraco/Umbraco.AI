using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Search.Core;
using Umbraco.AI.Search.Core.Chunking;
using Umbraco.AI.Search.Core.Configuration;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.AI.Search.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
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

        // Register both providers — the correct IAIVectorStore is resolved at runtime
        builder.AddUmbracoAISearchSqlServer();
        builder.AddUmbracoAISearchSqlite();

        // Resolve IAIVectorStore based on configured database provider
        builder.Services.AddSingleton<IAIVectorStore>(sp =>
        {
            var connectionStrings = sp.GetRequiredService<IOptions<ConnectionStrings>>().Value;

            return connectionStrings.ProviderName switch
            {
                Umbraco.Cms.Core.Constants.ProviderNames.SQLServer =>
                    sp.GetRequiredService<SqlServer.VectorStore.SqlServerAIVectorStore>(),

                Umbraco.Cms.Core.Constants.ProviderNames.SQLLite or "Microsoft.Data.SQLite" =>
                    sp.GetRequiredService<Sqlite.VectorStore.SqliteAIVectorStore>(),

                _ => throw new InvalidOperationException(
                    $"Unsupported database provider '{connectionStrings.ProviderName}' for Umbraco.AI.Search. " +
                    "Supported: SQL Server, SQLite."),
            };
        });

        builder.Services.AddOptions<AIVectorSearchOptions>()
            .BindConfiguration("Umbraco:AI:Search");

        builder.Services.Configure<IndexOptions>(options =>
            options.RegisterIndex<AIVectorIndexer, AIVectorSearcher>("VectorContentIndex"));
    }
}
