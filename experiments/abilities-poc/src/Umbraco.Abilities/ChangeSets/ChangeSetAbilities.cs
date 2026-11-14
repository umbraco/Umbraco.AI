using System.ComponentModel;

namespace Umbraco.Abilities.ChangeSets;

public sealed record CreateChangeSetInput([property: Description("A name a person will recognise, e.g. 'Black Friday campaign'.")] string Name);

public sealed record ChangeSetIdInput([property: Description("The change set's id.")] Guid ChangeSetId);

[Ability("changeSet.create", "Create change set", Kind = AbilityKind.Write, Reversible = true,
    Description = "Starts a change set. Writes made with it active are recorded, not saved, until the set is applied.")]
public sealed class CreateChangeSetAbility(ChangeSetService changeSets) : AbilityBase<CreateChangeSetInput, object>, IChangeSetLifecycleAbility
{
    protected override Task<AbilityResult> ExecuteAsync(CreateChangeSetInput input, AbilityContext context, CancellationToken cancellationToken)
        => Task.FromResult(Success(new { changeSetId = changeSets.Create(input.Name).Id }));
}

[Ability("changeSet.validate", "Validate change set", Kind = AbilityKind.Read,
    Description = "Checks every entity in the set. Returns problems by path; fix them with the entity edit abilities.")]
public sealed class ValidateChangeSetAbility(ChangeSetService changeSets) : AbilityBase<ChangeSetIdInput, object>, IChangeSetLifecycleAbility
{
    protected override async Task<AbilityResult> ExecuteAsync(ChangeSetIdInput input, AbilityContext context, CancellationToken cancellationToken)
    {
        var problems = await changeSets.ValidateAsync(input.ChangeSetId, cancellationToken);
        return Success(new { valid = problems.Count == 0, entities = problems });
    }
}

[Ability("changeSet.summarize", "Summarize change set", Kind = AbilityKind.Read,
    Description = "Lists what applying the set would create, update and run. Show it to the person before they apply.")]
public sealed class SummarizeChangeSetAbility(ChangeSetService changeSets) : AbilityBase<ChangeSetIdInput, ChangeSetSummary>, IChangeSetLifecycleAbility
{
    protected override async Task<AbilityResult> ExecuteAsync(ChangeSetIdInput input, AbilityContext context, CancellationToken cancellationToken)
        => Success(await changeSets.SummarizeAsync(input.ChangeSetId, cancellationToken));
}

[Ability("changeSet.apply", "Apply change set", Kind = AbilityKind.Write,
    Description = "Saves every entity in the set and runs its recorded operations, all or nothing. This is the only step that changes live data.")]
public sealed class ApplyChangeSetAbility(ChangeSetService changeSets) : AbilityBase<ChangeSetIdInput, IReadOnlyList<EntityRef>>, IChangeSetLifecycleAbility
{
    protected override Task<AbilityResult> ExecuteAsync(ChangeSetIdInput input, AbilityContext context, CancellationToken cancellationToken)
        => changeSets.ApplyAsync(input.ChangeSetId, context, cancellationToken);
}

[Ability("changeSet.discard", "Discard change set", Kind = AbilityKind.Write, Reversible = true,
    Description = "Throws the set away. No live data changes.")]
public sealed class DiscardChangeSetAbility(ChangeSetService changeSets) : AbilityBase<ChangeSetIdInput, object>, IChangeSetLifecycleAbility
{
    protected override Task<AbilityResult> ExecuteAsync(ChangeSetIdInput input, AbilityContext context, CancellationToken cancellationToken)
    {
        changeSets.Discard(input.ChangeSetId);
        return Task.FromResult(Success(new { discarded = input.ChangeSetId }));
    }
}
