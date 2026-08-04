using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Core.Contexts.Resolvers;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Copilot.Workspace.Web.Api.Management.Stream;

/// <summary>
/// Maps a conversation's <em>own</em> attached contexts/resources into runtime-context properties, so
/// they inject alongside the project's grounding. Unlike <see cref="ProjectRuntimeContextBuilder"/>
/// there is no framing or instructions synthesis — a conversation only contributes its referenced
/// <c>AIContext</c> ids and its directly-attached resources, riding the same
/// resolve→format→inject pipeline (<see cref="CoreConstants.ContextKeys.AdditionalContextIds"/> /
/// <see cref="CoreConstants.ContextKeys.AdditionalResources"/>). Per-resource
/// <c>InjectionMode</c> remains the single source of truth for always-in-prompt vs. tool-fetched.
/// </summary>
internal static class ConversationRuntimeContextBuilder
{
    /// <summary>
    /// Builds the runtime-context properties for <paramref name="conversation"/>, or <c>null</c> when
    /// the conversation contributes nothing of its own to inject.
    /// </summary>
    public static IReadOnlyDictionary<string, object?>? Build(AIConversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        var properties = new Dictionary<string, object?>();

        if (conversation.ContextIds.Count > 0)
        {
            properties[CoreConstants.ContextKeys.AdditionalContextIds] = conversation.ContextIds.ToList();
        }

        if (conversation.Resources.Count > 0)
        {
            var contextName = string.IsNullOrWhiteSpace(conversation.Title) ? "Conversation" : conversation.Title.Trim();

            properties[CoreConstants.ContextKeys.AdditionalResources] = conversation.Resources
                .OrderBy(r => r.SortOrder)
                .Select(r => new AIContextResolverResource
                {
                    Id = r.Id,
                    ResourceTypeId = r.ResourceTypeId,
                    Name = r.Name ?? string.Empty,
                    Description = r.Description,
                    Settings = r.Settings,
                    InjectionMode = r.InjectionMode,
                    ContextName = contextName,
                })
                .ToList();
        }

        return properties.Count > 0 ? properties : null;
    }
}
