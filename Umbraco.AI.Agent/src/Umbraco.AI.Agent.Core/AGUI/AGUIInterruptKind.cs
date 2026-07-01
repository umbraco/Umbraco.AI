namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Helpers for building and recognising Umbraco-specific AG-UI interrupt IDs.
/// </summary>
/// <remarks>
/// Encodes the interrupt category as a namespaced prefix in the AG-UI interrupt ID so that
/// the stateless resume path can reconstruct the correct <c>ToolApprovalResponseContent</c>
/// without needing server-side state.
/// </remarks>
internal static class AGUIInterruptKind
{
    private const string ApprovalPrefix = "approval:";

    /// <summary>Returns an interrupt ID for a backend tool approval request.</summary>
    public static string ForApproval(string callId) => $"{ApprovalPrefix}{callId}";

    /// <summary>Returns <c>true</c> when <paramref name="interruptId"/> encodes a tool approval request.</summary>
    public static bool IsApproval(string interruptId) =>
        interruptId.StartsWith(ApprovalPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Extracts the underlying MEAI <c>FunctionCallContent.CallId</c> from an approval interrupt ID,
    /// or <c>null</c> if the ID is not an approval interrupt.
    /// </summary>
    public static string? GetCallId(string interruptId) =>
        IsApproval(interruptId) ? interruptId[ApprovalPrefix.Length..] : null;
}
