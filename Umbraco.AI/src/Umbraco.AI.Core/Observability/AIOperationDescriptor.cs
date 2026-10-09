using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// Describes a trackable AI operation. Supplied to <see cref="IAIOperationTracker"/> before the
/// operation runs, so recorders have the prompt up front.
/// </summary>
internal sealed class AIOperationDescriptor
{
    /// <summary>The capability being tracked (drives context extraction).</summary>
    public required AICapability Capability { get; init; }

    /// <summary>What the call was given (messages, inputs, options), for recorders that keep it.</summary>
    public object? PromptData { get; init; }
}
