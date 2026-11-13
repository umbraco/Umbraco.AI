using System.ComponentModel;

namespace Umbraco.Abilities.DraftSets;

public sealed record CreateDraftSetInput([property: Description("A name a person will recognise, e.g. 'Black Friday campaign'.")] string Name);

public sealed record DraftSetIdInput([property: Description("The draft set's id.")] Guid DraftSetId);

[Ability("draftSet.create", "Create draft set", Kind = AbilityKind.Write, Reversible = true,
    Description = "Starts a draft set. Writes made with it active are recorded, not saved, until the set is applied.")]
public sealed class CreateDraftSetAbility(DraftSetService draftSets) : AbilityBase<CreateDraftSetInput, object>, IDraftSetLifecycleAbility
{
    protected override Task<AbilityResult> ExecuteAsync(CreateDraftSetInput input, AbilityContext context, CancellationToken cancellationToken)
        => Task.FromResult(Success(new { draftSetId = draftSets.Create(input.Name).Id }));
}

[Ability("draftSet.validate", "Validate draft set", Kind = AbilityKind.Read,
    Description = "Checks every entity in the set. Returns problems by path; fix them with the entity edit abilities.")]
public sealed class ValidateDraftSetAbility(DraftSetService draftSets) : AbilityBase<DraftSetIdInput, object>, IDraftSetLifecycleAbility
{
    protected override async Task<AbilityResult> ExecuteAsync(DraftSetIdInput input, AbilityContext context, CancellationToken cancellationToken)
    {
        var problems = await draftSets.ValidateAsync(input.DraftSetId, cancellationToken);
        return Success(new { valid = problems.Count == 0, entities = problems });
    }
}

[Ability("draftSet.summarize", "Summarize draft set", Kind = AbilityKind.Read,
    Description = "Lists what applying the set would create, update and run. Show it to the person before they apply.")]
public sealed class SummarizeDraftSetAbility(DraftSetService draftSets) : AbilityBase<DraftSetIdInput, DraftSetSummary>, IDraftSetLifecycleAbility
{
    protected override async Task<AbilityResult> ExecuteAsync(DraftSetIdInput input, AbilityContext context, CancellationToken cancellationToken)
        => Success(await draftSets.SummarizeAsync(input.DraftSetId, cancellationToken));
}

[Ability("draftSet.apply", "Apply draft set", Kind = AbilityKind.Write,
    Description = "Saves every entity in the set and runs its recorded operations, all or nothing. This is the only step that changes live data.")]
public sealed class ApplyDraftSetAbility(DraftSetService draftSets) : AbilityBase<DraftSetIdInput, IReadOnlyList<EntityRef>>, IDraftSetLifecycleAbility
{
    protected override Task<AbilityResult> ExecuteAsync(DraftSetIdInput input, AbilityContext context, CancellationToken cancellationToken)
        => draftSets.ApplyAsync(input.DraftSetId, context, cancellationToken);
}

[Ability("draftSet.discard", "Discard draft set", Kind = AbilityKind.Write, Reversible = true,
    Description = "Throws the set away. No live data changes.")]
public sealed class DiscardDraftSetAbility(DraftSetService draftSets) : AbilityBase<DraftSetIdInput, object>, IDraftSetLifecycleAbility
{
    protected override Task<AbilityResult> ExecuteAsync(DraftSetIdInput input, AbilityContext context, CancellationToken cancellationToken)
    {
        draftSets.Discard(input.DraftSetId);
        return Task.FromResult(Success(new { discarded = input.DraftSetId }));
    }
}
