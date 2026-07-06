namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Controls how destructive backend tools are gated for human-in-the-loop (HITL) approval
/// during an agent run. Backend approval relies on the AG-UI interrupt/resume mechanism, which
/// only exists on the interactive streaming path; non-interactive callers must choose a policy
/// that resolves deterministically without a human, so a destructive tool can never leave a run
/// stalled waiting for an approval that will never arrive.
/// </summary>
public enum AIApprovalPolicy
{
    /// <summary>
    /// Pause the run and emit a <c>human_approval</c> interrupt for each destructive tool call,
    /// resuming (executing or skipping) once the user responds. Requires an interactive surface
    /// that can resume — i.e. the AG-UI streaming path. This is the only policy that actually
    /// asks a human.
    /// </summary>
    Interactive,

    /// <summary>
    /// Skip destructive tools without executing them and tell the model the action was denied,
    /// allowing the run to complete. Safe default for non-interactive callers (programmatic
    /// <c>RunAgentAsync</c>/<c>StreamAgentAsync</c>, Automate) where no human is present to approve.
    /// </summary>
    DenyAll,

    /// <summary>
    /// Execute destructive tools without prompting for approval. The action is still captured by
    /// the existing audit-log middleware. Use only behind an explicit per-agent/per-automation
    /// opt-in: it bypasses the approval gate entirely.
    /// </summary>
    AllowAll,
}
