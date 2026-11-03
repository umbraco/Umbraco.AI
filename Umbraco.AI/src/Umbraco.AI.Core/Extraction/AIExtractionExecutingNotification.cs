using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Published before an inline extraction execution begins (cancelable).
/// </summary>
/// <remarks>
/// Subscribers can inspect the extraction configuration and cancel execution by setting <see cref="CancelableNotification.Cancel"/>.
/// Cancellation reasons should be added to the <see cref="StatefulNotification.Messages"/> collection.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIExtractionExecutingNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIExtractionExecutingNotification"/> class.
    /// </summary>
    /// <param name="extractionId">The deterministic extraction ID.</param>
    /// <param name="alias">The extraction alias.</param>
    /// <param name="name">The extraction display name.</param>
    /// <param name="profileId">The profile ID, if specified.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIExtractionExecutingNotification(
        Guid extractionId,
        string alias,
        string name,
        Guid? profileId,
        EventMessages messages)
        : base(messages)
    {
        ExtractionId = extractionId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
    }

    /// <summary>
    /// Gets the deterministic extraction ID derived from the alias.
    /// </summary>
    public Guid ExtractionId { get; }

    /// <summary>
    /// Gets the extraction alias.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Gets the extraction display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the profile ID, or null if using the default extraction profile.
    /// </summary>
    public Guid? ProfileId { get; }
}
