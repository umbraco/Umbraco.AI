using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Abilities.Entities;

namespace Umbraco.Abilities.ChangeSets;

/// <summary>
/// Records Write ability calls instead of running them, works out an entity's state inside a set, and applies
/// the set. Unlike the CMS Draft Sets PoC, the working state is computed in memory from
/// <see cref="IEntityDefinition.LoadAsync"/> plus the recorded edits, so it works for any entity type.
/// <para>
/// Applying is atomic per product, not across products: each product's changes save in that product's own
/// transaction, one product after another, and side effects (such as sending an email) run last.
/// If a step fails, the steps before it stay applied, the set is kept, and applying again resumes from the
/// failed step.
/// </para>
/// </summary>
public sealed class ChangeSetService(
    IChangeSetStore store,
    AbilityRegistry abilities,
    EntityDefinitionRegistry definitions,
    ItemTypeRegistry itemTypes)
{
    public ChangeSet Create(string name)
    {
        var set = new ChangeSet { Name = name };
        store.Save(set);
        return set;
    }

    public ChangeSet Get(Guid id)
        => store.Get(id) ?? throw new AbilityInputException($"No change set {id}.", [new Problem("changeSetId", "Not found.")]);

    public void Discard(Guid id) => store.Remove(Get(id).Id);

    public async Task<AbilityResult> RecordAsync(Guid setId, IAbility ability, JsonObject input, CancellationToken cancellationToken = default)
    {
        try
        {
            var set = Get(setId);
            var recorded = (JsonObject)input.DeepClone();

            if (ability is not IEntityEditAbility edit)
            {
                // A side effect (send an email, call another system): not a change, so it runs after the set is applied.
                set.AfterApply.Add(new RecordedCall(ability.Descriptor.Id, recorded, null, null, false, null));
                store.Save(set);
                return AbilityResult.Ok(new
                {
                    recorded = true,
                    runsAfterApply = true,
                    note = "This is a side effect, not a change: it runs once every change in the set is applied, and can't be undone.",
                });
            }

            var target = edit.Prepare(recorded);
            var definition = definitions.Get(target.EntityType);
            if (set.AppliedSteps.Contains(definition.UnitOfWork.Name))
            {
                throw new AbilityInputException(
                    $"{definition.UnitOfWork.Name} changes in this set are already applied. Start a new change set to change them again.",
                    [new Problem("entityType", "Already applied.")]);
            }

            var working = await GetWorkingStateAsync(set, target.EntityType, target.EntityKey, cancellationToken);

            if (target.Creates)
            {
                if (!definition.CanCreate)
                {
                    throw new AbilityInputException($"{target.EntityType} cannot be created here.", [new Problem("entityType", "Not creatable.")]);
                }

                if (working is not null)
                {
                    throw new AbilityInputException($"{target.EntityType} {target.EntityKey} already exists.", [new Problem("key", "Already exists.")]);
                }

                var subtypes = await definition.GetSubtypesAsync(cancellationToken);
                if (subtypes.Count > 0 && !subtypes.Any(s => s.Alias == target.Subtype))
                {
                    throw new AbilityInputException(
                        $"{target.EntityType} needs a subtype. Subtypes: {string.Join(", ", subtypes.Select(s => s.Alias))}.",
                        [new Problem("subtype", "Required, and must be one of the listed subtypes.")]);
                }

                working = definition.New(target.EntityKey, target.Subtype);
            }
            else if (working is null)
            {
                throw new AbilityInputException($"No {target.EntityType} with key {target.EntityKey}.", [new Problem("key", "Not found.")]);
            }

            var structure = await definition.DescribeAsync(working.Subtype, cancellationToken);
            var output = edit.Apply(working, recorded, new EntityEditContext(definition, structure, itemTypes));

            set.Changes.Add(new RecordedCall(ability.Descriptor.Id, recorded, target.EntityType, target.EntityKey, target.Creates, working.LiveVersion, working.Subtype));
            store.Save(set);
            return AbilityResult.Ok(output);
        }
        catch (AbilityInputException ex)
        {
            return AbilityResult.Fail(FailureCategory.Input, ex.Message, ex.Problems);
        }
        catch (JsonException ex)
        {
            return AbilityResult.Fail(FailureCategory.Input, $"The input does not match the schema: {ex.Message}");
        }
    }

    /// <summary>The entity as it would be if the set were applied now, or null if it exists neither live nor in the set.</summary>
    public async Task<EntityDocument?> GetWorkingStateAsync(ChangeSet set, string entityType, Guid key, CancellationToken cancellationToken = default)
    {
        var definition = definitions.Get(entityType);
        var calls = set.Changes.Where(c => c.EntityType == entityType && c.EntityKey == key).ToList();

        var document = calls.FirstOrDefault() is { Creates: true } create
            ? definition.New(key, create.Subtype)
            : (await definition.LoadAsync(key, cancellationToken))?.Clone();

        if (document is null || calls.Count == 0)
        {
            return document;
        }

        var context = new EntityEditContext(definition, await definition.DescribeAsync(document.Subtype, cancellationToken), itemTypes);
        foreach (var call in calls)
        {
            ((IEntityEditAbility)abilities.Find(call.AbilityId)!).Apply(document, call.Input, context);
        }

        return document;
    }

    public async Task<IReadOnlyList<EntityProblems>> ValidateAsync(Guid setId, CancellationToken cancellationToken = default)
    {
        var set = Get(setId);
        var result = new List<EntityProblems>();

        foreach (var (entityType, key) in PendingEntities(set))
        {
            var definition = definitions.Get(entityType);
            var document = (await GetWorkingStateAsync(set, entityType, key, cancellationToken))!;
            var structure = await definition.DescribeAsync(document.Subtype, cancellationToken);

            var problems = new List<Problem>();
            problems.AddRange(await StructureValidator.ValidateAsync(document, structure, itemTypes, (type, k) => ExistsAsync(set, type, k, cancellationToken)));
            problems.AddRange(await definition.ValidateAsync(document, cancellationToken));

            // The generic structure check and the product's own validation often find the same problem.
            var distinct = problems.DistinctBy(p => (p.Path, p.Culture, p.Segment)).ToList();
            if (distinct.Count > 0)
            {
                result.Add(new EntityProblems(definition.ToRef(document), distinct));
            }
        }

        return result;
    }

    public async Task<ChangeSetSummary> SummarizeAsync(Guid setId, CancellationToken cancellationToken = default)
    {
        var set = Get(setId);
        var changes = new List<ChangeSetChange>();
        foreach (var (entityType, key) in TouchedEntities(set))
        {
            var document = (await GetWorkingStateAsync(set, entityType, key, cancellationToken))!;
            var calls = set.Changes.Where(c => c.EntityType == entityType && c.EntityKey == key).ToList();
            changes.Add(new ChangeSetChange(definitions.Get(entityType).ToRef(document), calls[0].Creates ? "created" : "updated", calls.Count));
        }

        var afterApply = set.AfterApply.Select(c => c.AbilityId).ToList();
        var plan = Plan(set).Select(s => new ApplyStep(s.Name, s.Description, set.AppliedSteps.Contains(s.Key))).ToList();
        return new ChangeSetSummary(set.Id, set.Name, changes, afterApply, plan);
    }

    /// <summary>
    /// Validates the set and checks nothing changed underneath it, then applies it one step at a time: each
    /// product's entities in that product's own transaction, then each recorded operation. A failed step rolls back
    /// only itself; earlier steps stay applied and are skipped when the set is applied again.
    /// </summary>
    public async Task<AbilityResult> ApplyAsync(Guid setId, AbilityContext context, CancellationToken cancellationToken = default)
    {
        ChangeSet set;
        try
        {
            set = Get(setId);
        }
        catch (AbilityInputException ex)
        {
            return AbilityResult.Fail(FailureCategory.Input, ex.Message, ex.Problems);
        }

        var invalid = await ValidateAsync(setId, cancellationToken);
        if (invalid.Count > 0)
        {
            var problems = invalid.SelectMany(e => e.Problems.Select(p => p with { Path = $"{e.Entity.EntityType}:{e.Entity.Key}/{p.Path}" })).ToList();
            return AbilityResult.Fail(FailureCategory.Input, "The change set has problems. Fix them and apply again.", problems);
        }

        var conflicts = new List<Problem>();
        foreach (var (entityType, key) in PendingEntities(set))
        {
            var first = set.Changes.First(c => c.EntityType == entityType && c.EntityKey == key);
            if (first.Creates)
            {
                continue;
            }

            var live = await definitions.Get(entityType).LoadAsync(key, cancellationToken);
            if (live?.LiveVersion != first.BaseVersion)
            {
                conflicts.Add(new Problem($"{entityType}:{key}", $"Changed since it was edited in this set (version {first.BaseVersion} then, {live?.LiveVersion} now)."));
            }
        }

        if (conflicts.Count > 0)
        {
            return AbilityResult.Fail(FailureCategory.Runtime, "Live data changed underneath the change set.", conflicts);
        }

        var saved = new List<EntityRef>();
        var steps = new List<Problem>();
        string? failure = null;
        var runContext = context with { ChangeSetId = null };

        foreach (var step in Plan(set))
        {
            if (set.AppliedSteps.Contains(step.Key))
            {
                steps.Add(new Problem(step.Name, "Already applied."));
                continue;
            }

            if (failure is not null)
            {
                steps.Add(new Problem(step.Name, "Not applied yet."));
                continue;
            }

            try
            {
                if (step.UnitOfWork is { } unitOfWork)
                {
                    using var transaction = unitOfWork.Begin();
                    foreach (var (entityType, key) in step.Entities)
                    {
                        var document = (await GetWorkingStateAsync(set, entityType, key, cancellationToken))!;
                        saved.Add(await definitions.Get(entityType).SaveAsync(document, cancellationToken));
                    }

                    transaction.Complete();
                }
                else
                {
                    var result = await abilities.Find(step.Call!.AbilityId)!.InvokeAsync(step.Call.Input, runContext, cancellationToken);
                    if (!result.Success)
                    {
                        throw new ChangeSetApplyException(result.Failure!.Message);
                    }
                }

                set.AppliedSteps.Add(step.Key);
                store.Save(set);
                steps.Add(new Problem(step.Name, "Applied."));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A product's changes roll back with its transaction; a side effect has nothing to roll back.
                var outcome = step.UnitOfWork is not null ? "failed and was rolled back" : "failed";
                failure = $"{step.Name} {outcome}: {ex.Message}";
                steps.Add(new Problem(step.Name, $"{char.ToUpperInvariant(outcome[0])}{outcome[1..]}: {ex.Message}"));
            }
        }

        if (failure is not null)
        {
            var appliedSome = set.AppliedSteps.Count > 0;
            return AbilityResult.Fail(
                FailureCategory.Runtime,
                appliedSome
                    ? $"Partly applied. {failure} Earlier steps stay applied. Fix the cause and apply again to apply the rest; nothing is applied twice."
                    : $"Nothing was applied. {failure}",
                steps);
        }

        store.Remove(set.Id);
        return AbilityResult.Ok(saved);
    }

    /// <summary>
    /// The steps of applying a set: one per product transaction, in the order the set first touched them, then each
    /// side effect in the order it was recorded, once every change is saved.
    /// </summary>
    private IReadOnlyList<PlannedStep> Plan(ChangeSet set)
    {
        var steps = TouchedEntities(set)
            .GroupBy(e => definitions.Get(e.EntityType).UnitOfWork.Name)
            .Select(g => new PlannedStep(
                g.Key,
                g.Key,
                $"{g.Count()} {(g.Count() == 1 ? "entity" : "entities")}, saved together: all or nothing.",
                definitions.Get(g.First().EntityType).UnitOfWork,
                g.ToList(),
                null))
            .ToList();

        steps.AddRange(set.AfterApply.Select((call, index) => new PlannedStep(
            $"after:{index}", call.AbilityId, "Side effect: runs after every change is applied, and can't be undone.", null, [], call)));
        return steps;
    }

    private sealed record PlannedStep(
        string Key,
        string Name,
        string Description,
        IUnitOfWorkProvider? UnitOfWork,
        IReadOnlyList<(string EntityType, Guid Key)> Entities,
        RecordedCall? Call);

    /// <summary>Entities whose product's changes are not applied yet.</summary>
    private IEnumerable<(string EntityType, Guid Key)> PendingEntities(ChangeSet set)
        => TouchedEntities(set).Where(e => !set.AppliedSteps.Contains(definitions.Get(e.EntityType).UnitOfWork.Name));

    private async Task<bool> ExistsAsync(ChangeSet set, string entityType, Guid key, CancellationToken cancellationToken)
        => definitions.Find(entityType) is not null && await GetWorkingStateAsync(set, entityType, key, cancellationToken) is not null;

    private static IEnumerable<(string EntityType, Guid Key)> TouchedEntities(ChangeSet set)
        => set.Changes.Select(c => (c.EntityType!, c.EntityKey!.Value)).Distinct();

    private sealed class ChangeSetApplyException(string message) : Exception(message);
}

/// <summary>
/// The single entry point products call. A Write ability called with an active change set is recorded instead of run.
/// </summary>
public sealed class AbilityInvoker(AbilityRegistry abilities, ChangeSetService changeSets)
{
    public async Task<AbilityResult> InvokeAsync(string abilityId, JsonObject input, AbilityContext context, CancellationToken cancellationToken = default)
    {
        var ability = abilities.Find(abilityId);
        if (ability is null)
        {
            return AbilityResult.Fail(FailureCategory.Input, $"Unknown ability '{abilityId}'.");
        }

        if (context.ChangeSetId is { } setId
            && ability.Descriptor.Kind == AbilityKind.Write
            && ability is not IChangeSetLifecycleAbility)
        {
            return await changeSets.RecordAsync(setId, ability, input, cancellationToken);
        }

        return await ability.InvokeAsync(input, context, cancellationToken);
    }
}
