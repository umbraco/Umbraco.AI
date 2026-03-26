using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.AI.Search.Db;
using Umbraco.AI.Search.Db.VectorStore;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Search.Db.SqlServer.VectorStore;

/// <summary>
/// SQL Server implementation of the vector store.
/// </summary>
/// <remarks>
/// Extends <see cref="EFCoreAIVectorStore"/> with native <c>VECTOR_DISTANCE()</c> support
/// on SQL Server 2025+. Falls back to brute-force cosine similarity in .NET on older versions.
/// </remarks>
internal sealed class SqlServerAIVectorStore : EFCoreAIVectorStore
{
    private readonly ILogger<SqlServerAIVectorStore> _logger;
    private bool? _supportsNativeVectors;
    private bool _loggedDimensionWarning;
    private bool _loggedVersionWarning;

    public SqlServerAIVectorStore(
        IEFCoreScopeProvider<UmbracoAISearchDbContext> scopeProvider,
        ILogger<SqlServerAIVectorStore> logger)
        : base(scopeProvider)
    {
        _logger = logger;
    }

    /// <summary>
    /// SQL Server vector type supports a maximum of 1998 dimensions.
    /// </summary>
    private const int MaxNativeVectorDimensions = 1998;

    /// <inheritdoc />
    public override async Task<IReadOnlyList<AIVectorSearchResult>> SearchAsync(string indexName, ReadOnlyMemory<float> queryVector, string? culture = null, int topK = 10, CancellationToken cancellationToken = default)
    {
        if (queryVector.Length > MaxNativeVectorDimensions)
        {
            if (!_loggedDimensionWarning)
            {
                _logger.LogWarning(
                    "Vector search is using brute-force because the embedding model produces {Dimensions} dimensions, " +
                    "which exceeds the SQL Server vector type maximum of {MaxDimensions}. " +
                    "To enable native VECTOR_DISTANCE, configure the embedding profile's Dimensions setting to {MaxDimensions} or lower " +
                    "(e.g. text-embedding-3-small at 1536, or text-embedding-3-large with dimensions reduced to 1998).",
                    queryVector.Length,
                    MaxNativeVectorDimensions,
                    MaxNativeVectorDimensions);
                _loggedDimensionWarning = true;
            }

            return await base.SearchAsync(indexName, queryVector, culture, topK, cancellationToken);
        }

        if (!await CheckNativeVectorSupportAsync(cancellationToken))
        {
            if (!_loggedVersionWarning)
            {
                _logger.LogWarning(
                    "Vector search is using brute-force because the SQL Server instance does not support VECTOR_DISTANCE. " +
                    "Native vector search requires SQL Server 2025 or later.");
                _loggedVersionWarning = true;
            }

            return await base.SearchAsync(indexName, queryVector, culture, topK, cancellationToken);
        }

        using IEfCoreScope<UmbracoAISearchDbContext> scope = ScopeProvider.CreateScope();

        IReadOnlyList<AIVectorSearchResult> results = await scope.ExecuteWithContextAsync(
            db => SearchNativeAsync(db, indexName, queryVector, culture, topK, cancellationToken));

        scope.Complete();
        return results;
    }

    // ── Native search ───────────────────────────────────────────────────

    /// <summary>
    /// SQL Server 2025+ path: uses <c>VECTOR_DISTANCE('cosine', CAST(Vector AS vector(N)), ...)</c>
    /// for server-side similarity ranking. The dimension N is derived from the query vector length.
    /// Works because the Vector column stores JSON arrays and nvarchar-to-vector CAST is supported.
    /// </summary>
    private static async Task<IReadOnlyList<AIVectorSearchResult>> SearchNativeAsync(
        UmbracoAISearchDbContext db,
        string indexName,
        ReadOnlyMemory<float> queryVector,
        string? culture,
        int topK,
        CancellationToken cancellationToken)
    {
        string queryJson = VectorToJson(queryVector);
        int dimensions = queryVector.Length;

        // VECTOR_DISTANCE returns distance (lower = more similar), so 1 - distance = similarity.
        // CAST from nvarchar to vector at query time — supported per SQL Server docs.
        // Uses SqlQueryRaw because vector(N) and FETCH NEXT N require literal values in SQL —
        // EF Core's interpolated SQL would parameterize them as @p0 which is invalid syntax.
        // Only queryJson, indexName, and culture are parameterized ({0}, {1}, {2}).
#pragma warning disable EF1003 // dimensions and topK are int — no SQL injection risk
        var baseSql =
            $"SELECT DocumentId, " +
            $"1.0 - VECTOR_DISTANCE('cosine', CAST(Vector AS vector({dimensions})), CAST({{0}} AS vector({dimensions}))) AS Score, " +
            $"Metadata FROM umbracoAISearchVectorEntry WHERE IndexName = {{1}}";

        List<VectorSearchRow> rows = culture is not null
            ? await db.Database
                .SqlQueryRaw<VectorSearchRow>(
                    baseSql + $" AND (Culture = {{2}} OR Culture IS NULL) ORDER BY Score DESC OFFSET 0 ROWS FETCH NEXT {topK} ROWS ONLY",
                    queryJson, indexName, culture)
                .ToListAsync(cancellationToken)
            : await db.Database
                .SqlQueryRaw<VectorSearchRow>(
                    baseSql + $" AND Culture IS NULL ORDER BY Score DESC OFFSET 0 ROWS FETCH NEXT {topK} ROWS ONLY",
                    queryJson, indexName)
                .ToListAsync(cancellationToken);
#pragma warning restore EF1003

        return rows
            .Select(r => new AIVectorSearchResult(r.DocumentId, r.Score, DeserializeMetadata(r.Metadata)))
            .ToList();
    }

    // ── Version detection ───────────────────────────────────────────────

    /// <summary>
    /// Checks whether the SQL Server instance supports native vector operations.
    /// Opens its own scope for the probe query. Result is cached for the lifetime of this instance.
    /// </summary>
    private async Task<bool> CheckNativeVectorSupportAsync(CancellationToken cancellationToken)
    {
        if (_supportsNativeVectors.HasValue)
        {
            return _supportsNativeVectors.Value;
        }

        try
        {
            using IEfCoreScope<UmbracoAISearchDbContext> scope = ScopeProvider.CreateScope();

            await scope.ExecuteWithContextAsync(async db =>
            {
                await db.Database
                    .SqlQueryRaw<double>("SELECT VECTOR_DISTANCE('cosine', CAST('[1.0]' AS vector(1)), CAST('[1.0]' AS vector(1))) AS [Value]")
                    .SingleAsync(cancellationToken);
                return true;
            });

            scope.Complete();
            _supportsNativeVectors = true;
        }
        catch
        {
            _supportsNativeVectors = false;
        }

        return _supportsNativeVectors.Value;
    }

    private sealed class VectorSearchRow
    {
        public string DocumentId { get; set; } = string.Empty;
        public double Score { get; set; }
        public string? Metadata { get; set; }
    }
}
