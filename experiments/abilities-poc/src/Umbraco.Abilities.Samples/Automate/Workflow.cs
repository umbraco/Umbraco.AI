using Umbraco.Abilities.Entities;

namespace Umbraco.Abilities.Samples.Automate;

/// <summary>
/// An Automate workflow as an entity: one trigger, and a slot of steps whose item types are abilities.
/// Edges between steps are item references, so a graph fits the same structure language as a tree.
/// </summary>
public sealed class WorkflowDefinition(InMemoryDatabase database)
    : InMemoryEntityDefinition(database, "automate.workflow", "name", "/umbraco/section/automate/workflow")
{
    public const string TriggerGroup = "automate.trigger";
    public const string StepGroup = "automate.step";

    /// <summary>Which abilities can be steps. In a real build Automate would pick these from every installed package.</summary>
    public static void RegisterItemTypes(ItemTypeRegistry itemTypes, IEnumerable<IAbility> stepAbilities)
    {
        itemTypes.Add(new ItemTypeDefinition(TriggerGroup, "formSubmitted", "Form submitted", "Runs when a form is submitted.", ElementShape.Of(
            new FieldDefinition("form", "Form name.", FieldType.String, Required: true))));

        itemTypes.Add(new ItemTypeDefinition(TriggerGroup, "schedule", "Schedule", "Runs on a schedule.", ElementShape.Of(
            new FieldDefinition("cron", "Cron expression.", FieldType.String, Required: true))));

        var next = new FieldDefinition("next", "The step to run after this one. Omit for the last step.", FieldType.ItemReference)
        {
            ReferencesItemInSlot = "steps",
        };

        foreach (var ability in stepAbilities)
        {
            itemTypes.Add(AbilityItemTypes.FromAbility(ability, StepGroup, next));
        }
    }

    public override Task<EntityStructure> DescribeAsync(string? subtype, CancellationToken cancellationToken = default)
        => Task.FromResult(new EntityStructure(EntityType, "Workflow", "An automation: a trigger, then steps.", ElementShape
            .Of(
                new FieldDefinition("name", "Workflow name.", FieldType.String, Required: true),
                new FieldDefinition("enabled", "Whether the workflow runs.", FieldType.Boolean))
            .WithSlots(
                new SlotDefinition("trigger", "What starts the workflow.", TriggerGroup) { Min = 1, Max = 1 },
                new SlotDefinition("steps", "The steps. Each step names the next one.", StepGroup) { Min = 1 })));
}
