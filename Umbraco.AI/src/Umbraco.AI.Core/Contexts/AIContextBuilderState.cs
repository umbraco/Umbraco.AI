using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Shared context-selection state for the inline builders. Unlike guardrails, the replace-mode list is
/// nullable so that <c>SetContexts(Array.Empty&lt;Guid&gt;())</c> can signal "explicitly use no contexts"
/// (empty override) distinct from "not set".
/// </summary>
internal sealed class AIContextBuilderState
{
    private IReadOnlyList<Guid>? _ids;
    private IReadOnlyList<string>? _aliases;
    private IReadOnlyList<Guid> _additionalIds = [];
    private IReadOnlyList<string>? _additionalAliases;

    public IReadOnlyList<Guid>? Ids => _ids;
    public IReadOnlyList<string>? Aliases => _aliases;
    public IReadOnlyList<Guid> AdditionalIds => _additionalIds;
    public IReadOnlyList<string>? AdditionalAliases => _additionalAliases;

    public void Set(Guid[] ids)
    {
        _ids = ids;
        _aliases = null;
    }

    public void SetByAlias(string[] aliases)
    {
        _aliases = aliases;
        _ids = null;
    }

    public void With(Guid[] ids)
    {
        _additionalIds = ids;
        _additionalAliases = null;
    }

    public void WithByAlias(string[] aliases)
    {
        _additionalAliases = aliases;
        _additionalIds = [];
    }

    public void SetResolvedIds(IReadOnlyList<Guid> ids) => _ids = ids;

    public void SetResolvedAdditionalIds(IReadOnlyList<Guid> ids) => _additionalIds = ids;

    /// <summary>
    /// Writes the replace (override) and additive lists onto the runtime context under the core context
    /// override / additional keys. Override is emitted whenever <see cref="Set"/> has been called — even
    /// with an empty array — so that the override can explicitly suppress profile contexts.
    /// </summary>
    public void WriteToContext(AIRuntimeContext context)
    {
        if (_ids is not null)
        {
            context.SetValue(Constants.ContextKeys.ContextIdsOverride, _ids);
        }

        if (_additionalIds.Count > 0)
        {
            context.SetValue(Constants.ContextKeys.AdditionalContextIds, _additionalIds);
        }
    }

    /// <summary>
    /// Writes the replace (override) and additive lists into an additional-properties dictionary
    /// (used by the agent service, which emits them when creating the MAF agent instead of populating
    /// a runtime context directly).
    /// </summary>
    public void WriteTo(IDictionary<string, object?> properties)
    {
        if (_ids is not null)
        {
            properties[Constants.ContextKeys.ContextIdsOverride] = _ids;
        }

        if (_additionalIds.Count > 0)
        {
            properties[Constants.ContextKeys.AdditionalContextIds] = _additionalIds;
        }
    }
}
