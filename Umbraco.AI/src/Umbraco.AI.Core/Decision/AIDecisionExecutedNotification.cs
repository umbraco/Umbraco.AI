using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Published after an inline decision execution completes (not cancelable).
/// </summary>
/// <remarks>
/// Contains execution results including duration and success status for telemetry and logging.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIDecisionExecutedNotification : StatefulNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIDecisionExecutedNotification"/> class.
    /// </summary>
    /// <param name="decisionId">The deterministic decision ID.</param>
    /// <param name="alias">The decision alias.</param>
    /// <param name="name">The decision display name.</param>
    /// <param name="profileId">The profile ID, if specified.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="isSuccess">Whether the execution completed successfully.</param>
    /// <param name="messages">Event messages from the execution.</param>
    public AIDecisionExecutedNotification(
        Guid decisionId,
        string alias,
        string name,
        Guid? profileId,
        TimeSpan duration,
        bool isSuccess,
        EventMessages messages)
    {
        DecisionId = decisionId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
        Duration = duration;
        IsSuccess = isSuccess;
        Messages = messages;
    }

    /// <summary>
    /// Gets the deterministic decision ID derived from the alias.
    /// </summary>
    public Guid DecisionId { get; }

    /// <summary>
    /// Gets the decision alias.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Gets the decision display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the profile ID, or null if using the default decision profile.
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
