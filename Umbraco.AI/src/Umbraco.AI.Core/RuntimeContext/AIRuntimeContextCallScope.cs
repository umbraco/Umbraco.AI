using Umbraco.AI.Core.Observability;

namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Opens the runtime context an AI call runs in.
/// </summary>
/// <remarks>
/// <para>
/// A call made with no runtime context gets a new one. A call made in a context its caller set up for it
/// (e.g. the prompt or agent service) runs in that context.
/// </para>
/// <para>
/// A call made inside another AI call while that call's own context is still current (a guardrail judge, a
/// tool that calls an AI service) gets its own context instead. It is built from the same request and keeps
/// who the running call belongs to (feature, entity, log values), so a pass-through call is still recorded
/// under it, but none of its settings (options override, guardrail and context overrides, system prompt).
/// What it writes also stays out of the running call's context.
/// </para>
/// <para>
/// The current context is held per async flow, so AI calls run in parallel from the same call (e.g.
/// concurrent tool calls) each get their own context and never see each other's.
/// </para>
/// </remarks>
internal static class AIRuntimeContextCallScope
{
    /// <summary>
    /// Opens the runtime context for one AI call.
    /// </summary>
    /// <returns>The scope this call created, to dispose when the call is done; <c>null</c> when the call runs
    /// in its caller's context.</returns>
    public static IAIRuntimeContextScope? Begin(
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors,
        IEnumerable<AIRequestContextItem>? contextItems)
    {
        var current = contextAccessor.Context;
        if (current is not null && !BelongsToRunningCall(current))
        {
            return null;
        }

        var items = current is null
            ? contextItems ?? []
            : current.RequestContextItems.Concat(contextItems ?? []);

        var scope = scopeProvider.CreateScope(items);
        contributors.Populate(scope.Context);

        if (current is not null)
        {
            CopyIdentity(current, scope.Context);
        }

        return scope;
    }

    /// <summary>
    /// Carries over who the running call belongs to, but none of its settings.
    /// </summary>
    /// <remarks>
    /// A pass-through call (e.g. a search embedding run by an agent's tool) is recorded under the running
    /// call's feature and entity, with its log values; a call with its own feature (e.g. a guardrail judge)
    /// replaces the feature keys when it populates its context.
    /// </remarks>
    private static void CopyIdentity(AIRuntimeContext from, AIRuntimeContext to)
    {
        foreach (var key in IdentityKeys)
        {
            if (from.Data.TryGetValue(key, out var value))
            {
                to.SetValue(key, value);
            }
        }

        if (from.TryGetValue<string[]>(Constants.ContextKeys.LogKeys, out var logKeys))
        {
            foreach (var key in logKeys)
            {
                if (from.Data.TryGetValue(key, out var value))
                {
                    to.SetValue(key, value);
                }
            }
        }
    }

    private static readonly string[] IdentityKeys =
    [
        Constants.ContextKeys.FeatureType,
        Constants.ContextKeys.FeatureId,
        Constants.ContextKeys.FeatureAlias,
        Constants.ContextKeys.FeatureVersion,
        Constants.ContextKeys.EntityId,
        Constants.ContextKeys.EntityType,
        Constants.ContextKeys.LogKeys,

        // A guardrail judge must not run guardrails on itself.
        Constants.ContextKeys.IsGuardrailEvaluation,
    ];

    private static bool BelongsToRunningCall(AIRuntimeContext context)
        => AIOperationScope.Current is { HasEnded: false } running
           && ReferenceEquals(running.RuntimeContext, context);
}
