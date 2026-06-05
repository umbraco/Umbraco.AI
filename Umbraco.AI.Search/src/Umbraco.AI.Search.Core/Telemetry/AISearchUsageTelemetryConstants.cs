namespace Umbraco.AI.Search.Core.Telemetry;

/// <summary>
/// Key names for the usage telemetry data Umbraco.AI.Search contributes to the CMS telemetry report.
/// </summary>
/// <remarks>
/// This is the complete safelist of data Umbraco.AI.Search reports. Values are always counts —
/// never indexed content, document IDs, or user identities.
/// </remarks>
public static class AISearchUsageTelemetryConstants
{
    /// <summary>The number of vector entries stored in the AI search index.</summary>
    public const string VectorEntryCount = "UmbracoAISearchVectorEntryCount";
}
