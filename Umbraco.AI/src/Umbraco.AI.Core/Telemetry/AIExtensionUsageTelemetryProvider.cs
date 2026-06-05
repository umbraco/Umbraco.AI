using System.Collections;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.Tools;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Telemetry.Interfaces;

namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Contributes anonymous counts of code extension point registrations (tools, context
/// resource types, middleware) to the CMS telemetry report, distinguishing Umbraco-shipped
/// registrations from custom ones.
/// </summary>
/// <remarks>
/// Only counts are reported — extension IDs are developer-authored and can encode business
/// information (e.g. a tool ID like "send-to-acme-erp"), so they are never sent. Middleware
/// pipelines are discovered from the <c>AI{Pipeline}MiddlewareCollection</c> types so new
/// capabilities are reported automatically. Subject to the same gating as
/// <see cref="AIUsageTelemetryProvider"/>: CMS <c>TelemetryLevel.Detailed</c> plus the
/// <c>Umbraco:AI:Telemetry:Enabled</c> kill switch.
/// </remarks>
public sealed class AIExtensionUsageTelemetryProvider : IDetailedTelemetryProvider
{
    /// <summary>
    /// Middleware collection types discovered from the core assembly, keyed by pipeline name
    /// (e.g. "Chat" for <c>AIChatMiddlewareCollection</c>). Cached for the app lifetime —
    /// the set of collection types is fixed at compile time.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<(string Pipeline, Type CollectionType)>> _middlewareCollectionTypes =
        new(() => typeof(AIExtensionUsageTelemetryProvider).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                && t.Name.StartsWith("AI", StringComparison.Ordinal)
                && t.Name.EndsWith("MiddlewareCollection", StringComparison.Ordinal))
            .Select(t => (t.Name["AI".Length..^"MiddlewareCollection".Length], t))
            .ToList());

    private readonly IOptionsMonitor<AIUsageTelemetryOptions> _telemetryOptions;
    private readonly AIToolCollection _tools;
    private readonly AIContextResourceTypeCollection _contextResourceTypes;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIExtensionUsageTelemetryProvider"/> class.
    /// </summary>
    public AIExtensionUsageTelemetryProvider(
        IOptionsMonitor<AIUsageTelemetryOptions> telemetryOptions,
        AIToolCollection tools,
        AIContextResourceTypeCollection contextResourceTypes,
        IServiceProvider serviceProvider)
    {
        _telemetryOptions = telemetryOptions;
        _tools = tools;
        _contextResourceTypes = contextResourceTypes;
        _serviceProvider = serviceProvider;
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
            var result = new List<UsageInformation>
            {
                new(AIUsageTelemetryConstants.ToolCount, _tools.Count()),
                new(AIUsageTelemetryConstants.ToolCustomCount, CountCustom(_tools)),
                new(AIUsageTelemetryConstants.ContextResourceTypeCount, _contextResourceTypes.Count()),
                new(AIUsageTelemetryConstants.ContextResourceTypeCustomCount, CountCustom(_contextResourceTypes)),
            };

            foreach ((var pipeline, Type collectionType) in _middlewareCollectionTypes.Value)
            {
                if (_serviceProvider.GetService(collectionType) is not IEnumerable middleware)
                {
                    continue;
                }

                result.Add(new UsageInformation(
                    AIUsageTelemetryConstants.MiddlewareCustomCount(pipeline),
                    CountCustom(middleware.Cast<object>())));
            }

            return result;
        }
        catch
        {
            // Telemetry is strictly best-effort; never throw into the CMS ReportSiteJob.
            return [];
        }
    }

    private static int CountCustom<T>(IEnumerable<T> registrations)
        where T : notnull
        => registrations.Count(r => !AIUsageTelemetryClassification.IsSystemType(r.GetType()));
}
