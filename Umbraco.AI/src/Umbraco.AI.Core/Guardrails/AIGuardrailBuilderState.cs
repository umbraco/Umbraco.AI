using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Shared guardrail-selection state for the inline builders (chat / agent / speech-to-text / embedding).
/// Holds the replace-mode and additive-mode lists (IDs and aliases) and knows how to emit the matching
/// runtime-context keys so each builder exposes the same <c>WithGuardrails</c> / <c>SetGuardrails</c>
/// semantics without duplicating the field pairs and logic.
/// </summary>
internal sealed class AIGuardrailBuilderState
{
    private IReadOnlyList<Guid> _ids = [];
    private IReadOnlyList<string>? _aliases;
    private IReadOnlyList<Guid> _additionalIds = [];
    private IReadOnlyList<string>? _additionalAliases;

    public IReadOnlyList<Guid> Ids => _ids;
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
        _ids = [];
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
    /// Writes the replace (override) and additive lists onto the runtime context under the core guardrail
    /// override / additional keys. Empty lists are skipped so source resolvers behave normally.
    /// </summary>
    public void WriteToContext(AIRuntimeContext context)
    {
        if (_ids.Count > 0)
        {
            context.SetValue(Constants.ContextKeys.GuardrailIdsOverride, _ids);
        }

        if (_additionalIds.Count > 0)
        {
            context.SetValue(Constants.ContextKeys.AdditionalGuardrailIds, _additionalIds);
        }
    }

    /// <summary>
    /// Writes the replace (override) and additive lists into an additional-properties dictionary
    /// (used by the agent service, which emits them when creating the MAF agent instead of populating
    /// a runtime context directly).
    /// </summary>
    public void WriteTo(IDictionary<string, object?> properties)
    {
        if (_ids.Count > 0)
        {
            properties[Constants.ContextKeys.GuardrailIdsOverride] = _ids;
        }

        if (_additionalIds.Count > 0)
        {
            properties[Constants.ContextKeys.AdditionalGuardrailIds] = _additionalIds;
        }
    }
}
