using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Shared logic for turning a dangling <see cref="ToolApprovalRequestContent"/> into a synthesized denial
/// message, so the model sees a resolved turn instead of an orphaned request. Used by both the AG-UI
/// streaming path (a browser reload before Approve/Deny was clicked) and the headless persisted-run path
/// (an earlier run of the same conversation that never resolved its own request) — they discover
/// "dangling" requests differently (the AG-UI path also excludes whatever the current request's own Resume
/// entries cover; the persisted path has no Resume concept, so everything found is dangling), but both
/// converge on appending the same shape of denial once found.
/// </summary>
internal static class AIApprovalDenialHelper
{
    /// <summary>
    /// Appends a denial for each request in <paramref name="staleRequests"/> to <paramref name="chatMessages"/>
    /// and logs one line per denial.
    /// </summary>
    public static void AppendDenials(
        List<ChatMessage> chatMessages,
        IReadOnlyList<ToolApprovalRequestContent> staleRequests,
        string reason,
        ILogger logger)
    {
        foreach (var stale in staleRequests)
        {
            chatMessages.Add(new ChatMessage(ChatRole.User, [stale.CreateResponse(false, reason)]));
            logger.LogInformation(
                "Auto-denying stale approval request for callId {CallId} -- {Reason}",
                stale.ToolCall.CallId,
                reason);
        }
    }
}
