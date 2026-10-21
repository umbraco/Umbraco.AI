using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Published after an inline extraction execution completes (not cancelable).
/// </summary>
/// <remarks>
/// Contains execution results including duration and success status for telemetry and logging.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIExtractionExecutedNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIExtractionExecutedNotification"/> class.
    /// </summary>
    /// <param name="extractionId">The deterministic extraction ID.</param>
    /// <param name="alias">The extraction alias.</param>
    /// <param name="name">The extraction display name.</param>
    /// <param name="profileId">The profile ID, if specified.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="isSuccess">Whether the execution completed successfully.</param>
    /// <param name="messages">Event messages from the execution.</param>
    public AIExtractionExecutedNotification(
        Guid extractionId,
        string alias,
        string name,
        Guid? profileId,
        TimeSpan duration,
        bool isSuccess,
        EventMessages messages)
    {
        ExtractionId = extractionId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
        Duration = duration;
        IsSuccess = isSuccess;
        Messages = messages;
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

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets whether the execution completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the event messages.
    /// </summary>
    public EventMessages Messages { get; }
}
