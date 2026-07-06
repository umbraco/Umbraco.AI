using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Describes a trackable AI operation. Supplied to <see cref="IAIOperationTracker"/> before the
/// operation runs (audit start needs the prompt + metadata up front).
/// </summary>
internal sealed class AIOperationDescriptor
{
    /// <summary>The capability being tracked (drives context extraction).</summary>
    public required AICapability Capability { get; init; }

    /// <summary>Prompt/input descriptor captured for the audit entry.</summary>
    public object? PromptData { get; init; }

    /// <summary>Optional audit metadata (LogKeys), pre-extracted by the caller from its own source.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// When true, a usage record is written even if no <c>UsageDetails</c> are available
    /// (duration/status only). Chat/Embedding = false; SpeechToText/ImageGeneration = true.
    /// </summary>
    public bool RecordUsageWhenEmpty { get; init; }
}
