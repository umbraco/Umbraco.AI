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
    long? BaseVersion,
    string? Subtype = null);

public sealed class DraftSet
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public List<RecordedCall> Calls { get; } = [];

    /// <summary>The apply steps already done, so a failed apply resumes where it stopped and nothing applies twice.</summary>
    public HashSet<string> AppliedSteps { get; } = [];
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
/// A product's own transaction, such as an Umbraco scope for content or Commerce's unit of work. Products can use
/// their own connection strings, so a draft set never assumes one transaction across products: each product's
/// changes apply all or nothing in its own unit of work, one product after another.
/// </summary>
public interface IUnitOfWorkProvider
{
    /// <summary>Names the product's step when a set is applied, e.g. "Content" or "Commerce".</summary>
    string Name { get; }

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

public sealed record EntityEditTarget(string EntityType, Guid EntityKey, bool Creates, string? Subtype = null);

public sealed record EntityEditContext(IEntityDefinition Definition, EntityStructure Structure, ItemTypeRegistry ItemTypes);

/// <summary>Abilities that manage draft sets themselves are never recorded into one.</summary>
public interface IDraftSetLifecycleAbility : IAbility
{
}

public sealed record EntityProblems(EntityRef Entity, IReadOnlyList<Problem> Problems);

public sealed record DraftSetChange(EntityRef Entity, string Change, int Edits);

/// <summary>One step of applying a set: a product's changes in its own transaction, or one recorded operation.</summary>
public sealed record ApplyStep(string Name, string Description, bool Applied);

public sealed record DraftSetSummary(
    Guid Id,
    string Name,
    IReadOnlyList<DraftSetChange> Entities,
    IReadOnlyList<string> OperationsOnApply,
    IReadOnlyList<ApplyStep> ApplyPlan);
