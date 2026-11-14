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

    public async Task DiscardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var set = Get(id);
        foreach (var (step, draftId) in set.Drafts.Where(d => !set.AppliedSteps.Contains(d.Key)))
        {
            await DraftingFor(step).DiscardDraftAsync(draftId, cancellationToken);
        }

        store.Remove(set.Id);
    }

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
            var before = working?.Clone();

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

            // Remember the version the set starts editing from, to rebase onto newer live versions later.
            // (Only for entities that exist live: one the set creates has nothing to rebase onto.)
            // (Products with their own drafts keep their own base.)
            if (definition is not IDraftingEntityDefinition && !target.Creates && working.LiveVersion is { } version && !set.Bases.ContainsKey(ChangeSet.EntityKey(target.EntityType, target.EntityKey)))
            {
                if (definition is IVersionedEntityDefinition)
                {
                    set.BaseVersions.TryAdd(ChangeSet.EntityKey(target.EntityType, target.EntityKey), version);
                }
                else
                {
                    set.Bases.TryAdd(ChangeSet.EntityKey(target.EntityType, target.EntityKey), working.Clone());
                }
            }

            var structure = await definition.DescribeAsync(working.Subtype, cancellationToken);
            var output = edit.Apply(working, recorded, new EntityEditContext(definition, structure, itemTypes));
            if (definition is IDraftingEntityDefinition drafting)
            {
                // The product's draft records the change; the set only remembers that it touched the entity.
                await drafting.StageAsync(await EnsureDraftAsync(set, drafting, cancellationToken), target.Creates ? null : before, working, cancellationToken);
            }

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

    /// <summary>
    /// The entity as it would be if the set were applied now, or null if it exists neither live nor in the set. The set's
    /// edits are replayed onto the current live version, which rebases them onto anything someone else saved since.
    /// An edit to something they removed is skipped (it shows as a conflict), and conflicts resolved as "theirs" take
    /// the live value.
    /// </summary>
    public async Task<EntityDocument?> GetWorkingStateAsync(ChangeSet set, string entityType, Guid key, CancellationToken cancellationToken = default)
    {
        var definition = definitions.Get(entityType);
        if (definition is IDraftingEntityDefinition drafting)
        {
            return set.Drafts.TryGetValue(definition.UnitOfWork.Name, out var draftId)
                ? await drafting.LoadDraftAsync(draftId, key, cancellationToken)
                : (await definition.LoadAsync(key, cancellationToken))?.Clone();
        }

        var calls = set.Changes.Where(c => c.EntityType == entityType && c.EntityKey == key).ToList();

        if (calls.FirstOrDefault() is { Creates: true } create)
        {
            return await ReplayAsync(definition, definition.New(key, create.Subtype), calls, skipUnder: [], cancellationToken);
        }

        var live = await definition.LoadAsync(key, cancellationToken);
        if (live is null || calls.Count == 0)
        {
            return live?.Clone();
        }

        var start = await GetStartAsync(set, definition, key, cancellationToken);
        var theirs = start is not null && start.LiveVersion != live.LiveVersion
            ? EntityDiff.Compare(start.Root, live.Root)
            : [];

        var document = await ReplayAsync(definition, live.Clone(), calls, theirs.Where(t => t.Structural).ToList(), cancellationToken);

        // Conflicts resolved as "theirs" take the live value: a field, an item, or a slot's order.
        var prefix = $"{ChangeSet.EntityKey(entityType, key)}|";
        foreach (var (resolutionKey, side) in set.Resolutions)
        {
            if (side == ConflictSide.Theirs && resolutionKey.StartsWith(prefix, StringComparison.Ordinal))
            {
                var parts = resolutionKey[prefix.Length..].Split('|');
                EntityDiff.CopyPath(live.Root, document.Root, new ChangedPath(parts[0], parts[1], Structural: false));
            }
        }

        return document;
    }

    /// <summary>
    /// Paths both the set and someone else changed, differently, since the set started editing each entity, with how
    /// they were resolved. Keyed by entity; an entity someone else deleted has one conflict at the empty path.
    /// </summary>
    public async Task<IReadOnlyList<RebasedEntity>> GetRebasedAsync(ChangeSet set, CancellationToken cancellationToken = default)
    {
        var result = new List<RebasedEntity>();
        foreach (var (step, draftId) in set.Drafts.Where(d => !set.AppliedSteps.Contains(d.Key)))
        {
            result.AddRange(await DraftingFor(step).GetRebasedAsync(draftId, cancellationToken));
        }

        foreach (var (entityType, key) in PendingEntities(set))
        {
            var definition = definitions.Get(entityType);
            if (definition is IDraftingEntityDefinition)
            {
                continue; // reported by the product's draft, above
            }

            var start = await GetStartAsync(set, definition, key, cancellationToken);
            var live = await definition.LoadAsync(key, cancellationToken);
            if (start is null)
            {
                // The product's history no longer has the version the set started from (e.g. after a version clean-up).
                if (set.BaseVersions.TryGetValue(ChangeSet.EntityKey(entityType, key), out var lost) && live is not null && live.LiveVersion != lost)
                {
                    result.Add(new RebasedEntity(definition.ToRef(live), ["(unknown: the starting version is no longer in the history)"],
                        [new ChangeSetConflict(definition.ToRef(live), string.Empty, null, null, null, null, null, null)]));
                }

                continue;
            }

            if (live is null)
            {
                result.Add(new RebasedEntity(definition.ToRef(start), ["(deleted)"],
                    [new ChangeSetConflict(definition.ToRef(start), string.Empty, null, null, null, null, null, null)]));
                continue;
            }

            if (live.LiveVersion == start.LiveVersion)
            {
                continue;
            }

            var calls = set.Changes.Where(c => c.EntityType == entityType && c.EntityKey == key).ToList();
            var yoursDocument = await ReplayAsync(definition, start.Clone(), calls, skipUnder: [], cancellationToken);
            var theirs = EntityDiff.Compare(start.Root, live.Root);
            var yours = EntityDiff.Compare(start.Root, yoursDocument.Root);

            var conflicts = new List<ChangeSetConflict>();
            foreach (var (mine, other) in yours.SelectMany(y => theirs.Where(t => EntityDiff.Overlaps(y, t)).Select(t => (y, t))))
            {
                // Report at the item someone added or removed, else at the value itself.
                var at = other.Structural ? other : mine.Structural ? mine : mine;
                var yourValue = EntityDiff.ValueAt(yoursDocument.Root, at);
                var theirValue = EntityDiff.ValueAt(live.Root, at);
                if (JsonNode.DeepEquals(yourValue, theirValue) || conflicts.Any(c => c.Path == at.Path && c.Culture == at.Variant.Culture && c.Segment == at.Variant.Segment))
                {
                    continue; // Both made the same change, or it's already listed.
                }

                conflicts.Add(new ChangeSetConflict(
                    definition.ToRef(live),
                    at.Path,
                    at.Variant.Culture,
                    at.Variant.Segment,
                    EntityDiff.ValueAt(start.Root, at),
                    yourValue,
                    theirValue,
                    set.Resolutions.TryGetValue(ChangeSet.ResolutionKey(entityType, key, at), out var side) ? side : null));
            }

            result.Add(new RebasedEntity(definition.ToRef(live), theirs.Select(t => Describe(t)).Distinct().ToList(), conflicts));
        }

        return result;
    }

    /// <summary>Keeps the set's value or the live one for one conflict.</summary>
    public async Task ResolveConflictAsync(Guid setId, string entityType, Guid key, string path, VariantId variant, ConflictSide keep, CancellationToken cancellationToken = default)
    {
        var set = Get(setId);
        if (definitions.Get(entityType) is IDraftingEntityDefinition drafting
            && set.Drafts.TryGetValue(drafting.UnitOfWork.Name, out var draftId))
        {
            await drafting.ResolveConflictAsync(draftId, key, path, variant, keep, cancellationToken);
            return;
        }

        var conflict = (await GetRebasedAsync(set, cancellationToken))
            .Where(r => r.Entity.EntityType == entityType && r.Entity.Key == key)
            .SelectMany(r => r.Conflicts)
            .FirstOrDefault(c => c.Path == path && c.Culture == variant.Culture && c.Segment == variant.Segment)
            ?? throw new AbilityInputException($"No conflict at '{path}' on {entityType} {key}.", [new Problem("path", "No conflict here. List conflicts with changeSet.summarize.")]);

        if (conflict.Path.Length == 0)
        {
            throw new AbilityInputException(
                "This entity can't be rebased: someone else deleted it, or the version the set started from is gone. Discard the set's changes to it and make them again.",
                [new Problem("path", "Can't be rebased.")]);
        }

        if (keep == ConflictSide.Yours && conflict.Theirs is null && conflict.Yours is not null && conflict.Path.Split('/')[^1] is var last && Guid.TryParse(last, out _))
        {
            throw new AbilityInputException("Someone else removed this item, so your edits to it can't be kept. Keep theirs, or add the item again.", [new Problem(path, "Removed by someone else.")]);
        }

        set.Resolutions[ChangeSet.ResolutionKey(entityType, key, new ChangedPath(path, variant.Key, Structural: false))] = keep;
        store.Save(set);
    }

    /// <summary>
    /// The version the set started editing an entity from: its own snapshot, or, when the product keeps version
    /// history, that version loaded from the history.
    /// </summary>
    private static async Task<EntityDocument?> GetStartAsync(ChangeSet set, IEntityDefinition definition, Guid key, CancellationToken cancellationToken)
    {
        var entityKey = ChangeSet.EntityKey(definition.EntityType, key);
        if (set.Bases.TryGetValue(entityKey, out var snapshot))
        {
            return snapshot;
        }

        if (set.BaseVersions.TryGetValue(entityKey, out var version) && definition is IVersionedEntityDefinition versioned)
        {
            return await versioned.LoadVersionAsync(key, version, cancellationToken);
        }

        return null;
    }

    private async Task<EntityDocument> ReplayAsync(IEntityDefinition definition, EntityDocument document, IReadOnlyList<RecordedCall> calls, IReadOnlyList<ChangedPath> skipUnder, CancellationToken cancellationToken)
    {
        if (calls.Count == 0)
        {
            return document;
        }

        var context = new EntityEditContext(definition, await definition.DescribeAsync(document.Subtype, cancellationToken), itemTypes);
        foreach (var call in calls)
        {
            try
            {
                ((IEntityEditAbility)abilities.Find(call.AbilityId)!).Apply(document, call.Input, context);
            }
            catch (AbilityInputException ex) when (ex.Problems.Any(p => skipUnder.Any(t => p.Path == t.Path || p.Path.StartsWith(t.Path + "/", StringComparison.Ordinal))))
            {
                // The edit was to something someone else removed since; it shows as a conflict instead.
            }
        }

        return document;
    }

    private static string Describe(ChangedPath change) => change.Variant.Culture is { } culture ? $"{change.Path} ({culture})" : change.Path;

    public async Task<IReadOnlyList<EntityProblems>> ValidateAsync(Guid setId, CancellationToken cancellationToken = default)
    {
        var set = Get(setId);
        var result = new List<EntityProblems>();

        var rebased = await GetRebasedAsync(set, cancellationToken);
        foreach (var (entityType, key) in PendingEntities(set))
        {
            var definition = definitions.Get(entityType);
            var conflicts = rebased.Where(r => r.Entity.EntityType == entityType && r.Entity.Key == key).SelectMany(r => r.Conflicts).Where(c => c.Resolution is null).ToList();
            var document = await GetWorkingStateAsync(set, entityType, key, cancellationToken);
            if (document is null)
            {
                result.Add(new EntityProblems(rebased.First(r => r.Entity.Key == key).Entity, [new Problem(string.Empty, "Someone else deleted this since the set started editing it.")]));
                continue;
            }

            var structure = await definition.DescribeAsync(document.Subtype, cancellationToken);

            var problems = new List<Problem>();
            problems.AddRange(conflicts.Select(c => new Problem(
                c.Path,
                c.Path.Length == 0
                    ? "Someone else changed this since the set started editing it, and what they changed can't be worked out: the version the set started from is gone. Discard the set's changes to it and make them again."
                    : $"Someone else changed this since the set started editing it. Yours: {c.Yours?.ToJsonString() ?? "(removed)"}; theirs: {c.Theirs?.ToJsonString() ?? "(removed)"}. Keep one with changeSet.resolveConflict.")
            {
                Culture = c.Culture,
                Segment = c.Segment,
            }));
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
            var document = await GetWorkingStateAsync(set, entityType, key, cancellationToken)
                ?? (await GetStartAsync(set, definitions.Get(entityType), key, cancellationToken))!;
            var calls = set.Changes.Where(c => c.EntityType == entityType && c.EntityKey == key).ToList();
            changes.Add(new ChangeSetChange(definitions.Get(entityType).ToRef(document), calls[0].Creates ? "created" : "updated", calls.Count));
        }

        var afterApply = set.AfterApply.Select(c => c.AbilityId).ToList();
        var plan = Plan(set).Select(s => new ApplyStep(s.Name, s.Description, set.AppliedSteps.Contains(s.Key))).ToList();
        return new ChangeSetSummary(set.Id, set.Name, changes, afterApply, plan, await GetRebasedAsync(set, cancellationToken));
    }

    /// <summary>
    /// Validates the set, including conflicts with anything saved since that are not resolved yet, then applies it one step at a time: each
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
                    if (set.Drafts.TryGetValue(step.Key, out var draftId))
                    {
                        // The product applies its own draft, all or nothing.
                        var drafting = DraftingFor(step.Key);
                        foreach (var (_, key) in step.Entities)
                        {
                            saved.Add(drafting.ToRef((await drafting.LoadDraftAsync(draftId, key, cancellationToken))!));
                        }

                        await drafting.ApplyDraftAsync(draftId, cancellationToken);
                    }
                    else
                    {
                        foreach (var (entityType, key) in step.Entities)
                        {
                            var document = (await GetWorkingStateAsync(set, entityType, key, cancellationToken))!;
                            saved.Add(await definitions.Get(entityType).SaveAsync(document, cancellationToken));
                        }
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

    private async Task<Guid> EnsureDraftAsync(ChangeSet set, IDraftingEntityDefinition drafting, CancellationToken cancellationToken)
    {
        if (!set.Drafts.TryGetValue(drafting.UnitOfWork.Name, out var draftId))
        {
            draftId = await drafting.CreateDraftAsync(set.Name, cancellationToken);
            set.Drafts[drafting.UnitOfWork.Name] = draftId;
        }

        return draftId;
    }

    private IDraftingEntityDefinition DraftingFor(string step)
        => definitions.All.OfType<IDraftingEntityDefinition>().First(d => d.UnitOfWork.Name == step);

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
