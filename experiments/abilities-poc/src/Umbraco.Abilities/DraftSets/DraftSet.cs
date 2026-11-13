using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;

namespace Umbraco.Abilities.DraftSets;

/// <summary>
/// One recorded Write ability call. This replaces the Draft Sets PoC's content-only
/// <c>DraftSetOperation { Kind, ContentKey, ParentKey, PayloadJson }</c>.
/// </summary>
/// <param name="EntityType">Set for entity edits; null for operations run when the set is applied.</param>
/// <param name="Creates">The call creates the entity, so its working state starts from <see cref="IEntityDefinition.New"/>.</param>
/// <param name="BaseVersion">The live version the edit was made against, for detecting changes underneath the set.</param>
public sealed record RecordedCall(
    string AbilityId,
    JsonObject Input,
    string? EntityType,
    Guid? EntityKey,
    bool Creates,
    long? BaseVersion);

public sealed class DraftSet
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public List<RecordedCall> Calls { get; } = [];
}

public interface IDraftSetStore
{
    DraftSet? Get(Guid id);

    void Save(DraftSet set);

    void Remove(Guid id);
}

public sealed class InMemoryDraftSetStore : IDraftSetStore
{
    private readonly Dictionary<Guid, DraftSet> _sets = [];

    public DraftSet? Get(Guid id) => _sets.GetValueOrDefault(id);

    public void Save(DraftSet set) => _sets[set.Id] = set;

    public void Remove(Guid id) => _sets.Remove(id);
}

/// <summary>
/// The transaction applies run in. Products whose writes share it get all-or-nothing apply across
/// entity types; in Umbraco this would be an <c>ICoreScope</c>.
/// </summary>
public interface IUnitOfWorkProvider
{
    IUnitOfWork Begin();
}

public interface IUnitOfWork : IDisposable
{
    void Complete();
}

/// <summary>An entity edit: it says which entity it edits, and how its recorded input changes a working copy.</summary>
public interface IEntityEditAbility : IAbility
{
    /// <summary>
    /// Identifies the edited entity and writes into <paramref name="input"/> anything replay must not decide
    /// again, such as new keys. Keys chosen here are what lets other entities in the set refer to new items.
    /// </summary>
    EntityEditTarget Prepare(JsonObject input);

    /// <summary>Applies a recorded input to a working copy. Throws <see cref="AbilityInputException"/> when invalid.</summary>
    object? Apply(EntityDocument document, JsonObject input, EntityEditContext context);
}

public sealed record EntityEditTarget(string EntityType, Guid EntityKey, bool Creates);

public sealed record EntityEditContext(IEntityDefinition Definition, EntityStructure Structure, ItemTypeRegistry ItemTypes);

/// <summary>Abilities that manage draft sets themselves are never recorded into one.</summary>
public interface IDraftSetLifecycleAbility : IAbility
{
}

public sealed record EntityProblems(EntityRef Entity, IReadOnlyList<Problem> Problems);

public sealed record DraftSetChange(EntityRef Entity, string Change, int Edits);

public sealed record DraftSetSummary(Guid Id, string Name, IReadOnlyList<DraftSetChange> Entities, IReadOnlyList<string> OperationsOnApply);
