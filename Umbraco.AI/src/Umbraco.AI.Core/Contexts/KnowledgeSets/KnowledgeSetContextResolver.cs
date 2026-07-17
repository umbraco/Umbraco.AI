using Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;
using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Utilities;

namespace Umbraco.AI.Core.Contexts.KnowledgeSets;

/// <summary>
/// Resolves context from installed <see cref="IAIKnowledgeSet"/>s.
/// </summary>
/// <remarks>
/// <para>
/// In v1 every discovered knowledge set is auto-active: this resolver iterates all of them, asks each
/// for its items, and maps each item into a sealed <see cref="AIContextResource"/> shape carrying the
/// built-in <c>text</c> resource type and <see cref="AIContextResourceInjectionMode.OnDemand"/>. It is
/// the sole constructor of the real resource shape, so everything downstream — the resolution service,
/// <see cref="AIContextProcessor"/>, and the context tools — works unchanged.
/// </para>
/// <para>
/// Each item is given a deterministic, namespaced GUID derived from its knowledge set id and item name,
/// so dedup, caching, and <c>get_context_resource</c> keep working and identifiers stay stable across
/// restarts while never colliding with user-authored context resource GUIDs.
/// </para>
/// </remarks>
internal sealed class KnowledgeSetContextResolver : IAIContextResolver
{
    // Fixed namespace for knowledge-set resource identifiers. Combined with "{setId}\0{itemName}" via
    // DeterministicGuid (UUIDv5), this guarantees stable, collision-free GUIDs distinct from user context
    // resources.
    private static readonly Guid KnowledgeSetNamespace = new("7d4f2c6e-9b1a-4c8e-8f3d-2a5b6c7d8e9f");

    private readonly AIKnowledgeSetCollection _knowledgeSets;
    private readonly IAIEditableModelResolver _modelResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeSetContextResolver"/> class.
    /// </summary>
    /// <param name="knowledgeSets">The collection of discovered knowledge sets.</param>
    /// <param name="modelResolver">The editable model resolver, used to escape literal content.</param>
    public KnowledgeSetContextResolver(
        AIKnowledgeSetCollection knowledgeSets,
        IAIEditableModelResolver modelResolver)
    {
        _knowledgeSets = knowledgeSets;
        _modelResolver = modelResolver;
    }

    /// <inheritdoc />
    public async Task<AIContextResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var resources = new List<AIContextResolverResource>();
        var sources = new List<AIContextResolverSource>();

        foreach (var knowledgeSet in _knowledgeSets)
        {
            var items = await knowledgeSet.GetItemsAsync(cancellationToken);
            if (items.Count == 0)
            {
                continue;
            }

            sources.Add(new AIContextResolverSource(null, knowledgeSet.Name));

            foreach (var item in items)
            {
                resources.Add(new AIContextResolverResource
                {
                    Id = CreateResourceId(knowledgeSet.Id, item.Name),
                    ResourceTypeId = "text",
                    Name = item.Name,
                    Description = item.Description,
                    // Content is always a literal (never a config reference), but routing it through the
                    // text resource type runs AIEditableModelResolver, which would treat a leading '$' as a
                    // config reference. Escape it so it survives the round-trip verbatim.
                    Settings = new TextResourceSettings { Content = _modelResolver.EscapeLiteral(item.Content) },
                    InjectionMode = AIContextResourceInjectionMode.OnDemand,
                    ContextName = knowledgeSet.Name,
                    ContextDescription = knowledgeSet.Description
                });
            }
        }

        if (resources.Count == 0)
        {
            return AIContextResolverResult.Empty;
        }

        return new AIContextResolverResult
        {
            Resources = resources,
            Sources = sources
        };
    }

    // Deterministic, namespaced resource id for a knowledge-set item. Keyed on "{setId}\0{itemName}"
    // so ids stay stable across restarts, differ per set + item, and never collide with user-authored
    // context resources. internal so the resolver tests can pin their expectations to this derivation.
    internal static Guid CreateResourceId(string knowledgeSetId, string itemName)
        => DeterministicGuid.Create(KnowledgeSetNamespace, $"{knowledgeSetId}\0{itemName}");
}
