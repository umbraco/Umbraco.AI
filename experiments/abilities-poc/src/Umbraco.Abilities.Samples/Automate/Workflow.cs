using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples.Automate.Plugins;

namespace Umbraco.Abilities.Samples.Automate;

/// <summary>
/// An Automate workflow as an entity: one trigger, and steps whose item types are abilities and Automate's actions.
/// Edges between steps are item references, so a graph fits the same structure language as a tree.
/// </summary>
public sealed class WorkflowDefinition(InMemoryDatabase database)
    : InMemoryEntityDefinition(database, "umbraco-automate-workflow", "name", "/umbraco/section/automate/workflow")
{
    public const string TriggerGroup = "automate.trigger";
    public const string StepGroup = "automate.step";

    /// <summary>
    /// Triggers and actions come from Automate's plugin collections, through one source each. Steps can also be any
    /// package's abilities. Neither needs anything written per trigger or action.
    /// </summary>
    public static void RegisterItemTypes(
        ItemTypeRegistry itemTypes,
        IEnumerable<IAbility> stepAbilities,
        AutomatePluginCollection triggers,
        AutomatePluginCollection actions,
        Func<Guid, string, bool> hasSection)
    {
        var next = new FieldDefinition("next", "The step to run after this one. Omit for the last step.", FieldType.ItemReference)
        {
            ReferencesItemIn = "steps",
        };

        itemTypes.AddSource(new AutomatePluginItemTypes(TriggerGroup, triggers, hasSection));
        itemTypes.AddSource(new AutomatePluginItemTypes(StepGroup, actions, hasSection, next));

        foreach (var ability in stepAbilities)
        {
            itemTypes.Add(AbilityItemTypes.FromAbility(ability, StepGroup, next));
        }
    }

    public override Task<EntityStructure> DescribeAsync(string? subtype, CancellationToken cancellationToken = default)
        => Task.FromResult(new EntityStructure(EntityType, "Workflow", "An automation: a trigger, then steps.", ElementShape
            .Of(
                new FieldDefinition("name", "Workflow name.", FieldType.String, Required: true),
                new FieldDefinition("enabled", "Whether the workflow runs.", FieldType.Boolean),
                FieldDefinition.ItemsOf("trigger", "What starts the workflow.", TriggerGroup) with { Min = 1, Max = 1 },
                FieldDefinition.ItemsOf("steps", "The steps. Each step names the next one.", StepGroup) with { Min = 1 })));
}

/// <summary>
/// The item types of one of Automate's plugin seams (triggers or actions), read from its collection whenever they're
/// needed. A plugin's required sections decide who is offered it. Settings that support bindings accept one.
/// </summary>
public sealed class AutomatePluginItemTypes(string group, AutomatePluginCollection plugins, Func<Guid, string, bool> hasSection, params FieldDefinition[] extraFields) : IItemTypeSource
{
    /// <summary>Automate's table from an editor UI alias to what it picks. None yet, so pickers stay plain values.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> Editors = new Dictionary<string, string[]>();

    public string Group => group;

    public IEnumerable<ItemTypeDefinition> GetItemTypes() => plugins.Plugins.Select(plugin =>
    {
        var fields = plugin.Fields
            .Select(f => ItemTypeFields.For(new SettingDescriptor(f.Key, f.Field.Label, f.Field.Description, f.PropertyType, f.Rules, f.Field.EditorUiAlias, f.Field.SortOrder, f.Field.SupportsBindings), Editors))
            .Concat(extraFields)
            .ToList();
        return new ItemTypeDefinition(group, plugin.Alias, plugin.Name, plugin.Description ?? plugin.Name, new ElementShape(fields))
        {
            AvailableTo = user => plugin.RequiredSections.All(section => hasSection(user, section)),
        };
    });
}
