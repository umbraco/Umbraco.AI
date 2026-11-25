namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// The outcome statuses of resolving an <c>onBehalfOf</c> value into user group keys.
/// </summary>
public enum AIAgentUserGroupResolutionStatus
{
    /// <summary>The groups were resolved.</summary>
    Success,

    /// <summary>The value is not in the <c>kind:value</c> form, or its kind or value is empty.</summary>
    Malformed,

    /// <summary>No resolver claims the kind.</summary>
    NotHandled,

    /// <summary>The resolver refuses: the caller may not act for this value.</summary>
    Forbidden,

    /// <summary>The value has no user to take groups from (for example, a workspace with no service account).</summary>
    NoUser,
}
