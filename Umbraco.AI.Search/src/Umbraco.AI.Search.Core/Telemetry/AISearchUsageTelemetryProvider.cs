using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Telemetry.Interfaces;

namespace Umbraco.AI.Search.Core.Telemetry;

/// <summary>
/// Contributes anonymous, aggregate Umbraco.AI.Search usage information to the CMS telemetry report.
/// </summary>
/// <remarks>
/// Data is only ever sent when the site's telemetry level is set to <c>Detailed</c>, and is
/// suppressed entirely when <c>Umbraco:AI:Telemetry:Enabled</c> is <c>false</c>. Only counts
/// are reported — see <see cref="AISearchUsageTelemetryConstants"/> for the complete whitelist.
/// </remarks>
public sealed class AISearchUsageTelemetryProvider : IDetailedTelemetryProvider
{
    private readonly IOptionsMonitor<AIUsageTelemetryOptions> _telemetryOptions;
    private readonly IAIVectorStore _vectorStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="AISearchUsageTelemetryProvider"/> class.
    /// </summary>
    public AISearchUsageTelemetryProvider(
        IOptionsMonitor<AIUsageTelemetryOptions> telemetryOptions,
        IAIVectorStore vectorStore)
    {
        _telemetryOptions = telemetryOptions;
        _vectorStore = vectorStore;
    }

    /// <inheritdoc />
    public IEnumerable<UsageInformation> GetInformation()
    {
        if (!_telemetryOptions.CurrentValue.Enabled)
        {
            return [];
        }

        try
        {
            var vectorEntryCount = _vectorStore
                .GetDocumentCountAsync(AISearchConstants.IndexAliases.Search)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return
            [
                new UsageInformation(AISearchUsageTelemetryConstants.VectorEntryCount, vectorEntryCount),
            ];
        }
        catch
        {
            // Telemetry is strictly best-effort; never throw into the CMS ReportSiteJob.
            return [];
        }
    }
}
