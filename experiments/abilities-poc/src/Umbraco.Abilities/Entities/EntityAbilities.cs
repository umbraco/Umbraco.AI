using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Abilities.ChangeSets;

namespace Umbraco.Abilities.Entities;

// The generic entity abilities. They work for every registered entity definition, so no package writes
// create_discount, add_discount_rule and so on. Edits only run inside a change set.

public interface IEntityEditInput
{
    string EntityType { get; }

    Guid? Key { get; }

    string? Culture { get; }

    string? Segment { get; }
}

/// <summary>Base for entity edits: recorded into a change set, applied to a working copy on replay.</summary>
public abstract class EntityEditAbilityBase<TInput> : AbilityBase<TInput, object>, IEntityEditAbility
    where TInput : IEntityEditInput
{
    protected virtual bool Creates => false;

    public EntityEditTarget Prepare(JsonObject input)
    {
        FillKeys(input);
        var typed = Read(input);
        if (string.IsNullOrWhiteSpace(typed.EntityType))
        {
            throw new AbilityInputException("entityType is required.", [new Problem("entityType", "Required.")]);
        }

        var key = typed.Key ?? throw new AbilityInputException("key is required.", [new Problem("key", "Required.")]);
        return new EntityEditTarget(typed.EntityType, key, Creates, (typed as CreateEntityInput)?.Subtype);
    }

    public object? Apply(EntityDocument document, JsonObject input, EntityEditContext context) => Apply(document, Read(input), context);

    /// <summary>Writes new keys into the recorded input so replay always produces the same result.</summary>
    protected virtual void FillKeys(JsonObject input)
    {
    }

    protected abstract object? Apply(EntityDocument document, TInput input, EntityEditContext context);

    protected override Task<AbilityResult> ExecuteAsync(TInput input, AbilityContext context, CancellationToken cancellationToken)
        => Task.FromResult(AbilityResult.Fail(
            FailureCategory.Input,
            "Entity edits are recorded in a change set. Call changeSet_create first and run with its id as the active change set."));

    private static TInput Read(JsonObject input)
        => input.Deserialize<TInput>(AbilityJson.Options) ?? throw new AbilityInputException("Input is required.");

    protected static EditTarget Target(EntityDocument document, TInput input, EntityEditContext context)
        => new(document, context.Structure, context.ItemTypes, new VariantId(input.Culture, input.Segment));

    protected static void EnsureKey(JsonObject input, string property)
    {
        if (input[property] is null || string.IsNullOrWhiteSpace(input[property]!.ToString()))
        {
            input[property] = Guid.NewGuid().ToString();
        }
    }
}

public sealed record GetEntitySchemaInput(
    [property: Description("Entity type, e.g. 'commerce.discount'.")] string EntityType,
    [property: Description("Optional item type aliases (from a slot's allowedItemTypes) to get the full schema for. Use group:alias when an alias appears in several groups.")] string[]? ItemTypes = null,
    [property: Description("The subtype, for entity types that have them, e.g. a document's content type alias.")] string? Subtype = null);

[Ability("entity.getSchema", "Get entity schema", Kind = AbilityKind.Read,
    Description = "Returns an entity type's fields (as JSON Schema) and slots with their allowed item types. " +
                  "Call it first. Call again with itemTypes to get the fields and nested slots of the item types you will add.")]
public sealed class GetEntitySchemaAbility(EntityDefinitionRegistry definitions, ItemTypeRegistry itemTypes)
    : AbilityBase<GetEntitySchemaInput, JsonObject>
{
    protected override async Task<AbilityResult> ExecuteAsync(GetEntitySchemaInput input, AbilityContext context, CancellationToken cancellationToken)
    {
        var definition = definitions.Get(input.EntityType);
        var subtypes = await definition.GetSubtypesAsync(cancellationToken);
        if (subtypes.Count > 0 && input.Subtype is null)
        {
            return Success(new JsonObject
            {
                ["entityType"] = input.EntityType,
                ["subtypes"] = new JsonArray(subtypes.Select(s => (JsonNode?)new JsonObject { ["alias"] = s.Alias, ["name"] = s.Name }).ToArray()),
                ["next"] = "Call again with a subtype to get its fields and slots.",
            });
        }

        var structure = await definition.DescribeAsync(input.Subtype, cancellationToken);
        var output = new JsonObject
        {
            ["entityType"] = structure.EntityType,
            ["name"] = structure.Name,
            ["description"] = structure.Description,
            ["variations"] = structure.Variations == Variation.Invariant ? null : structure.Variations.ToString(),
            ["cultures"] = new JsonArray(structure.Cultures.Select(c => (JsonNode?)c).ToArray()),
            ["fields"] = SchemaWriter.Fields(structure.Shape),
            ["slots"] = SchemaWriter.Slots(structure.Shape, itemTypes),
        };

        if (input.ItemTypes is { Length: > 0 })
        {
            var details = new JsonObject();
            foreach (var alias in input.ItemTypes)
            {
                var itemType = itemTypes.Find(alias)
                    ?? throw new AbilityInputException($"Unknown item type '{alias}'.", [new Problem("itemTypes", $"Unknown: {alias}.")]);
                details[alias] = new JsonObject
                {
                    ["name"] = itemType.Name,
                    ["fields"] = SchemaWriter.Fields(itemType.Shape),
                    ["slots"] = SchemaWriter.Slots(itemType.Shape, itemTypes),
                    ["settings"] = itemType.SettingsShape is { } settings ? SchemaWriter.Fields(settings) : null,
                };
            }

            output["itemTypes"] = details;
        }

        return Success(output);
    }
}

public sealed record FindEntityInput(
    [property: Description("Entity type to search, e.g. 'commerce.collection'.")] string EntityType,
    [property: Description("Words in the entity's name.")] string Query);

[Ability("entity.find", "Find entities", Kind = AbilityKind.Read,
    Description = "Finds entities by name, live and in the active change set. Use it to turn a name the person used into a key.")]
public sealed class FindEntityAbility(EntityDefinitionRegistry definitions, ChangeSetService changeSets)
    : AbilityBase<FindEntityInput, IReadOnlyList<EntityRef>>
{
    protected override async Task<AbilityResult> ExecuteAsync(FindEntityInput input, AbilityContext context, CancellationToken cancellationToken)
    {
        var definition = definitions.Get(input.EntityType);
        var found = (await definition.FindAsync(input.Query, cancellationToken)).ToList();

        if (context.ChangeSetId is { } setId)
        {
            // Entities the set touches are matched on their working state, not their live name.
            var set = changeSets.Get(setId);
            var touched = set.Changes.Where(c => c.EntityType == input.EntityType).Select(c => c.EntityKey!.Value).Distinct().ToList();
            found.RemoveAll(r => touched.Contains(r.Key));

            foreach (var key in touched)
            {
                var document = await changeSets.GetWorkingStateAsync(set, input.EntityType, key, cancellationToken);
                var reference = definition.ToRef(document!);
                if (reference.Name.Contains(input.Query, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(reference);
                }
            }
        }

        return Success(found);
    }
}

public sealed record GetEntityInput(
    [property: Description("Entity type.")] string EntityType,
    [property: Description("Entity key.")] Guid Key);

[Ability("entity.get", "Get entity", Kind = AbilityKind.Read,
    Description = "Returns an entity's fields and slots. Inside a change set this includes the set's edits.")]
public sealed class GetEntityAbility(EntityDefinitionRegistry definitions, ChangeSetService changeSets)
    : AbilityBase<GetEntityInput, JsonObject>
{
    protected override async Task<AbilityResult> ExecuteAsync(GetEntityInput input, AbilityContext context, CancellationToken cancellationToken)
    {
        var document = context.ChangeSetId is { } setId
            ? await changeSets.GetWorkingStateAsync(changeSets.Get(setId), input.EntityType, input.Key, cancellationToken)
            : await definitions.Get(input.EntityType).LoadAsync(input.Key, cancellationToken);

        return document is null
            ? AbilityResult.Fail(FailureCategory.Input, $"No {input.EntityType} with key {input.Key}.", [new Problem("key", "Not found.")])
            : Success(document.ToJson());
    }
}

public sealed record CreateEntityInput(
    [property: Description("Entity type to create.")] string EntityType,
    [property: Description("Optional key for the new entity. Choose one when other entities in the set must refer to it.")] Guid? Key = null,
    [property: Description("Field values, keyed by field alias.")] JsonObject? Fields = null,
    [property: Description("The subtype to create, for entity types that have them, e.g. a document's content type alias.")] string? Subtype = null,
    [property: Description("Culture to edit, e.g. 'en-US'. Needed for values that vary by culture; ignored for invariant values.")] string? Culture = null,
    [property: Description("Segment to edit. Omit for the default segment.")] string? Segment = null) : IEntityEditInput;

[Ability("entity.create", "Create entity", Kind = AbilityKind.Write, Reversible = true,
    Description = "Creates an entity in the active change set. Nothing goes live until the set is applied. " +
                  "Returns the new key. Add slot items afterwards with entity.addItem.")]
public sealed class CreateEntityAbility : EntityEditAbilityBase<CreateEntityInput>
{
    protected override bool Creates => true;

    protected override void FillKeys(JsonObject input) => EnsureKey(input, "key");

    protected override object? Apply(EntityDocument document, CreateEntityInput input, EntityEditContext context)
    {
        var target = Target(document, input, context);
        EntityEditor.SetFields(target, EntityEditor.ResolveElement(target, string.Empty), input.Fields);
        return context.Definition.ToRef(document);
    }
}

public sealed record SetEntityValueInput(
    [property: Description("Entity type.")] string EntityType,
    [property: Description("Entity key.")] Guid? Key,
    [property: Description("Path of the element that holds the field: \"\" for the entity itself, \"slot/itemKey\" for an item, or \"slot/itemKey/@settings\" or \"@layout\".")] string? ElementPath,
    [property: Description("Field alias.")] string Field,
    [property: Description("The new value, matching the field's schema. null clears it.")] JsonNode? Value,
    [property: Description("Culture to edit, e.g. 'en-US'. Needed for values that vary by culture; ignored for invariant values.")] string? Culture = null,
    [property: Description("Segment to edit. Omit for the default segment.")] string? Segment = null) : IEntityEditInput;

[Ability("entity.setValue", "Set entity value", Kind = AbilityKind.Write, Reversible = true,
    Description = "Sets one field on the entity, on an item, or on an item's @settings or @layout, in the active change set. " +
                  "Give a culture for values that vary by culture. Problem paths from changeSet.validate have the same form: elementPath plus field.")]
public sealed class SetEntityValueAbility : EntityEditAbilityBase<SetEntityValueInput>
{
    protected override object? Apply(EntityDocument document, SetEntityValueInput input, EntityEditContext context)
    {
        var target = Target(document, input, context);
        var element = EntityEditor.ResolveElement(target, input.ElementPath);
        EntityEditor.SetField(target, element, input.Field, input.Value);
        return new { path = EntityEditor.Join(element.Path, input.Field) };
    }
}

public sealed record AddEntityItemInput(
    [property: Description("Entity type.")] string EntityType,
    [property: Description("Entity key.")] Guid? Key,
    [property: Description("Path of the slot, e.g. \"rules\" or \"blocks/{itemKey}/cards\" for a slot inside an item.")] string SlotPath,
    [property: Description("Item type alias, from the slot's allowedItemTypes.")] string ItemType,
    [property: Description("Optional key for the new item. Choose one when another item must refer to it.")] Guid? ItemKey = null,
    [property: Description("Field values for the new item, keyed by field alias.")] JsonObject? Fields = null,
    [property: Description("Optional zero-based position. Omit to append.")] int? Position = null,
    [property: Description("Culture to edit, e.g. 'en-US'. Needed for values that vary by culture; ignored for invariant values.")] string? Culture = null,
    [property: Description("Segment to edit. Omit for the default segment.")] string? Segment = null) : IEntityEditInput;

[Ability("entity.addItem", "Add entity item", Kind = AbilityKind.Write, Reversible = true,
    Description = "Adds a typed item to a slot, in the active change set. Returns the item's path; use it as the " +
                  "start of a slot path to fill nested slots, or as elementPath to set its fields.")]
public sealed class AddEntityItemAbility : EntityEditAbilityBase<AddEntityItemInput>
{
    protected override void FillKeys(JsonObject input) => EnsureKey(input, "itemKey");

    protected override object? Apply(EntityDocument document, AddEntityItemInput input, EntityEditContext context)
    {
        var target = Target(document, input, context);
        var item = EntityEditor.AddItem(target, input.SlotPath, input.ItemType, input.ItemKey!.Value, input.Position);
        EntityEditor.SetFields(target, item, input.Fields);
        return new { itemKey = input.ItemKey, path = item.Path };
    }
}

public sealed record RemoveEntityItemInput(
    [property: Description("Entity type.")] string EntityType,
    [property: Description("Entity key.")] Guid? Key,
    [property: Description("Path of the slot.")] string SlotPath,
    [property: Description("Key of the item to remove.")] Guid ItemKey,
    [property: Description("Culture to edit, e.g. 'en-US'. Needed for values that vary by culture; ignored for invariant values.")] string? Culture = null,
    [property: Description("Segment to edit. Omit for the default segment.")] string? Segment = null) : IEntityEditInput;

[Ability("entity.removeItem", "Remove entity item", Kind = AbilityKind.Write, Reversible = true,
    Description = "Removes an item from a slot, in the active change set.")]
public sealed class RemoveEntityItemAbility : EntityEditAbilityBase<RemoveEntityItemInput>
{
    protected override object? Apply(EntityDocument document, RemoveEntityItemInput input, EntityEditContext context)
    {
        EntityEditor.RemoveItem(Target(document, input, context), input.SlotPath, input.ItemKey);
        return new { removed = input.ItemKey };
    }
}

public sealed record MoveEntityItemInput(
    [property: Description("Entity type.")] string EntityType,
    [property: Description("Entity key.")] Guid? Key,
    [property: Description("Path of the slot.")] string SlotPath,
    [property: Description("Key of the item to move.")] Guid ItemKey,
    [property: Description("New zero-based position.")] int Position,
    [property: Description("Culture to edit, e.g. 'en-US'. Needed for values that vary by culture; ignored for invariant values.")] string? Culture = null,
    [property: Description("Segment to edit. Omit for the default segment.")] string? Segment = null) : IEntityEditInput;

[Ability("entity.moveItem", "Move entity item", Kind = AbilityKind.Write, Reversible = true,
    Description = "Moves an item within its slot, in the active change set.")]
public sealed class MoveEntityItemAbility : EntityEditAbilityBase<MoveEntityItemInput>
{
    protected override object? Apply(EntityDocument document, MoveEntityItemInput input, EntityEditContext context)
    {
        EntityEditor.MoveItem(Target(document, input, context), input.SlotPath, input.ItemKey, input.Position);
        return new { moved = input.ItemKey, position = input.Position };
    }
}
