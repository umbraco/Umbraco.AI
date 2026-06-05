namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Classifies extension point registrations as system (shipped by Umbraco packages) or
/// custom (site/third-party code) for usage telemetry.
/// </summary>
/// <remarks>
/// Custom extension point IDs are developer-authored and can encode business information
/// (e.g. a tool ID like "send-to-acme-erp"), so only IDs implemented in official
/// Umbraco.AI assemblies are ever reported verbatim; everything else is reported as a
/// count. The match is deliberately strict ("Umbraco.AI" / "Umbraco.AI.*") — a broader
/// "Umbraco.*" prefix would wrongly classify community packages, which conventionally use
/// the "Umbraco.Community.*" naming prefix, as system.
/// </remarks>
internal static class AIUsageTelemetryClassification
{
    private const string SystemAssemblyPrefix = "Umbraco.AI";

    /// <summary>
    /// Determines whether a type ships in an official Umbraco.AI assembly.
    /// </summary>
    internal static bool IsSystemType(Type type)
    {
        var assemblyName = type.Assembly.GetName().Name;

        return assemblyName is not null
            && (assemblyName.Equals(SystemAssemblyPrefix, StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith(SystemAssemblyPrefix + ".", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Builds the set of system-registered IDs (lowercased) from an extension point registry.
    /// </summary>
    internal static HashSet<string> GetSystemIds<T>(IEnumerable<T> registrations, Func<T, string> idSelector)
        where T : notnull
        => registrations
            .Where(r => IsSystemType(r.GetType()))
            .Select(r => idSelector(r).ToLowerInvariant())
            .ToHashSet();

    /// <summary>
    /// Splits a set of in-use IDs into the system IDs (safe to report verbatim) and the
    /// number of distinct custom IDs (reported as a count only). IDs that aren't registered
    /// at all are treated as custom.
    /// </summary>
    internal static (HashSet<string> SystemIds, int CustomCount) ClassifyInUse(
        IEnumerable<string> inUseIds,
        HashSet<string> systemIds)
    {
        var system = new HashSet<string>();
        var custom = new HashSet<string>();

        foreach (var id in inUseIds.Select(id => id.ToLowerInvariant()))
        {
            if (systemIds.Contains(id))
            {
                system.Add(id);
            }
            else
            {
                custom.Add(id);
            }
        }

        return (system, custom.Count);
    }
}
