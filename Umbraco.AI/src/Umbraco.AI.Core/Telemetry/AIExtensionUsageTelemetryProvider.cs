using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.Tools;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Telemetry.Interfaces;

namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Contributes anonymous counts of code extension point registrations to the CMS telemetry
/// report, distinguishing Umbraco-shipped registrations from custom ones.
/// </summary>
/// <remarks>
/// Extension collections are discovered by sweeping all loaded Umbraco.AI assemblies for
/// <see cref="BuilderCollectionBase{TItem}"/> implementations (e.g.
/// <c>AIToolCollection</c> → "UmbracoAIToolCustomCount"), so new extension points — including
/// those added by add-on packages and future capabilities — are reported without changes
/// here. Only counts are reported — extension IDs are developer-authored and can encode
/// business information (e.g. a tool ID like "send-to-acme-erp"), so they are never sent.
/// Collections whose in-use IDs are already reported with system/custom classification by an
/// entity-level provider are excluded to avoid double reporting. Subject to the same gating
/// as <see cref="AIUsageTelemetryProvider"/>: CMS <c>TelemetryLevel.Detailed</c> plus the
/// <c>Umbraco:AI:Telemetry:Enabled</c> kill switch.
/// </remarks>
public sealed class AIExtensionUsageTelemetryProvider : IDetailedTelemetryProvider
{
    /// <summary>
    /// Collections excluded from the sweep because their usage is already reported with
    /// system/custom classification elsewhere (providers, evaluators, test features/graders,
    /// agent surfaces).
    /// </summary>
    private static readonly HashSet<string> _excludedCollections =
    [
        "AIProviderCollection",
        "AIGuardrailEvaluatorCollection",
        "AITestFeatureCollection",
        "AITestGraderCollection",
        "AIAgentSurfaceCollection",
    ];

    private readonly IOptionsMonitor<AIUsageTelemetryOptions> _telemetryOptions;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIExtensionUsageTelemetryProvider"/> class.
    /// </summary>
    public AIExtensionUsageTelemetryProvider(
        IOptionsMonitor<AIUsageTelemetryOptions> telemetryOptions,
        IServiceProvider serviceProvider)
    {
        _telemetryOptions = telemetryOptions;
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
            var result = new List<UsageInformation>();

            // Total registration counts for the extension points where adoption depth matters
            if (_serviceProvider.GetService<AIToolCollection>() is { } tools)
            {
                result.Add(new UsageInformation(AIUsageTelemetryConstants.ToolCount, tools.Count()));
            }

            if (_serviceProvider.GetService<AIContextResourceTypeCollection>() is { } resourceTypes)
            {
                result.Add(new UsageInformation(AIUsageTelemetryConstants.ContextResourceTypeCount, resourceTypes.Count()));
            }

            // Custom registration counts for every discovered extension collection
            foreach ((var name, Type collectionType) in GetExtensionCollectionTypes())
            {
                if (_serviceProvider.GetService(collectionType) is not IEnumerable registrations)
                {
                    continue;
                }

                var customCount = registrations
                    .Cast<object>()
                    .Count(r => !AIUsageTelemetryClassification.IsSystemType(r.GetType()));

                result.Add(new UsageInformation(AIUsageTelemetryConstants.ExtensionCustomCount(name), customCount));
            }

            return result;
        }
        catch
        {
            // Telemetry is strictly best-effort; never throw into the CMS ReportSiteJob.
            return [];
        }
    }

    /// <summary>
    /// Discovers extension collection types across all loaded Umbraco.AI assemblies, keyed by
    /// name (e.g. "Tool" for <c>AIToolCollection</c>). Evaluated per call — assemblies are all
    /// loaded by the time the telemetry job first runs, but this stays robust if invoked earlier.
    /// </summary>
    private static IEnumerable<(string Name, Type CollectionType)> GetExtensionCollectionTypes()
        => AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name is { } name
                && (name.Equals("Umbraco.AI", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("Umbraco.AI.", StringComparison.OrdinalIgnoreCase)))
            .SelectMany(GetLoadableTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                && t.Name.StartsWith("AI", StringComparison.Ordinal)
                && t.Name.EndsWith("Collection", StringComparison.Ordinal)
                && !_excludedCollections.Contains(t.Name)
                && IsBuilderCollection(t))
            .Select(t => (t.Name["AI".Length..^"Collection".Length], t));

    private static Type[] GetLoadableTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>().ToArray();
        }
    }

    private static bool IsBuilderCollection(Type type)
    {
        for (Type? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(BuilderCollectionBase<>))
            {
                return true;
            }
        }

        return false;
    }
}
