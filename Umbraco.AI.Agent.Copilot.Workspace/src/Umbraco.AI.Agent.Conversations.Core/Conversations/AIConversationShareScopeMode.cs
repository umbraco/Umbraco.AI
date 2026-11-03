namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>How much of a shared conversation stays visible to the recipient.</summary>
public enum AIConversationShareScopeMode
{
    /// <summary>Only messages up to the share's <see cref="AIConversationShare.CutoffMessageId"/>.</summary>
    Snapshot = 0,

    /// <summary>All messages, including ones added after the share was created.</summary>
    Live = 1,
}
