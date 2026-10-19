using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Published before an inline decision execution begins (cancelable).
/// </summary>
/// <remarks>
/// Subscribers can inspect the decision configuration and cancel execution by setting <see cref="CancelableNotification.Cancel"/>.
/// Cancellation reasons should be added to the <see cref="StatefulNotification.Messages"/> collection.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIDecisionExecutingNotification : CancelableNotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIDecisionExecutingNotification"/> class.
    /// </summary>
    /// <param name="decisionId">The deterministic decision ID.</param>
    /// <param name="alias">The decision alias.</param>
    /// <param name="name">The decision display name.</param>
    /// <param name="profileId">The profile ID, if specified.</param>
    /// <param name="messages">Event messages for cancellation reasons.</param>
    public AIDecisionExecutingNotification(
        Guid decisionId,
        string alias,
        string name,
        Guid? profileId,
        EventMessages messages)
        : base(messages)
    {
        DecisionId = decisionId;
        Alias = alias;
        Name = name;
        ProfileId = profileId;
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
}
