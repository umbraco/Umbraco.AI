using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

using Umbraco.Abilities.ChangeSets;

namespace Umbraco.Abilities.Entities.Models;

/// <summary>
/// The base for an entity definition whose entities are C# classes. A package implements loading and saving its own
/// model; the framework works out the structure, maps the model to and from entity documents, and runs the model's
/// DataAnnotations. Everything else (schemas, generic edit abilities, change sets, rebasing) comes for free.
/// </summary>
/// <typeparam name="TModel">The model class, marked with <see cref="EntityAttribute"/> and a <c>[Key]</c> Guid property.</typeparam>
public abstract class EntityDefinitionBase<TModel> : IEntityDefinition
    where TModel : class
{
    private readonly (string EntityType, string Name, string Description) _entity;
    private readonly ModelInfo _info;

    protected EntityDefinitionBase(ItemTypeRegistry itemTypes, IUnitOfWorkProvider unitOfWork)
    {
        ItemTypes = itemTypes;
        UnitOfWork = unitOfWork;
        _info = ModelShapes.Info(typeof(TModel));
        _entity = _info.Entity
            ?? throw new InvalidOperationException($"{typeof(TModel).Name} is not an entity: add [Entity] or configure it with IsEntity.");
        if (_info.Key is null || _info.Key.PropertyType != typeof(Guid))
        {
            throw new InvalidOperationException($"{typeof(TModel).Name} needs a Guid key: add [Key] or configure HasKey.");
        }
    }

    public string EntityType => _entity.EntityType;

    public virtual bool CanCreate => true;

    public IUnitOfWorkProvider UnitOfWork { get; }

    protected ItemTypeRegistry ItemTypes { get; }

    // ── What a package implements ──────────────────────────────────────────────────────────────────

    /// <summary>Loads the model, or null if there is none with this key. Set its [EntityVersion] if it has one.</summary>
    public abstract Task<TModel?> LoadModelAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>Saves the model, creating it when it is new. Throw to fail the save; the product's transaction rolls back.</summary>
    public abstract Task SaveModelAsync(TModel model, CancellationToken cancellationToken = default);

    /// <summary>Finds models by name. Optional.</summary>
    protected virtual Task<IReadOnlyList<TModel>> FindModelsAsync(string query, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TModel>>([]);

    /// <summary>Domain rules the attributes can't express, e.g. dates in order or unique codes. Optional.</summary>
    protected virtual Task<IReadOnlyList<Problem>> ValidateModelAsync(TModel model, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Problem>>([]);

    /// <summary>The cultures the entity can have, when it has values that vary by culture.</summary>
    protected virtual Task<IReadOnlyList<string>> GetCulturesAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([]);

    protected virtual string? GetEditUrl(Guid key) => null;

    // ── Helpers for problems ───────────────────────────────────────────────────────────────────────

    /// <summary>A problem on a property of the model, e.g. <c>ProblemAt(d => d.EndDate, "...")</c>.</summary>
    protected static Problem ProblemAt<TValue>(Expression<Func<TModel, TValue>> property, string message)
        => new(AliasOf(property), message);

    /// <summary>A problem on a property of an item in the model, e.g. <c>ProblemAt(reward, r => r.Percent, "...")</c>.</summary>
    protected static Problem ProblemAt<TItem, TValue>(TItem item, Expression<Func<TItem, TValue>> property, string message)
        where TItem : class
        => new(EntityEditor.Join(ModelShapes.PathOf(item), AliasOf(property)), message);

    // ── IEntityDefinition, done by the framework ───────────────────────────────────────────────────

    public Task<IReadOnlyList<EntitySubtype>> GetSubtypesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EntitySubtype>>([]);

    public async Task<EntityStructure> DescribeAsync(string? subtype, CancellationToken cancellationToken = default)
        => new(EntityType, _entity.Name, _entity.Description, _info.Shape)
        {
            Variations = _info.Variations,
            Cultures = await GetCulturesAsync(cancellationToken),
        };

    public EntityDocument New(Guid key, string? subtype) => new() { EntityType = EntityType, Key = key };

    public async Task<EntityDocument?> LoadAsync(Guid key, CancellationToken cancellationToken = default)
        => await LoadModelAsync(key, cancellationToken) is { } model ? ToDocument(model) : null;

    public async Task<IReadOnlyList<Problem>> ValidateAsync(EntityDocument document, CancellationToken cancellationToken = default)
    {
        var model = ToModel(document);
        return [.. ModelShapes.Validate(model), .. await ValidateModelAsync(model, cancellationToken)];
    }

    public async Task<EntityRef> SaveAsync(EntityDocument document, CancellationToken cancellationToken = default)
    {
        await SaveModelAsync(ToModel(document), cancellationToken);
        return ToRef(document);
    }

    public async Task<IReadOnlyList<EntityRef>> FindAsync(string query, CancellationToken cancellationToken = default)
        => (await FindModelsAsync(query, cancellationToken)).Select(m => ToRef(ToDocument(m))).ToList();

    public EntityRef ToRef(EntityDocument document)
    {
        var name = _info.Name is { } nameProperty ? document.Root.ReadAny(ModelShapes.AliasOf(nameProperty))?.GetValue<string>() : null;
        return new EntityRef(EntityType, document.Key, name ?? "(unnamed)", GetEditUrl(document.Key));
    }

    /// <summary>The model a document describes.</summary>
    public TModel ToModel(EntityDocument document)
    {
        var model = (TModel)ModelShapes.Read(typeof(TModel), document.Root, ItemTypes, string.Empty);
        _info.Key!.SetValue(model, document.Key);
        _info.Version?.SetValue(model, document.LiveVersion);
        return model;
    }

    /// <summary>The document for a model.</summary>
    public EntityDocument ToDocument(TModel model)
    {
        var document = new EntityDocument
        {
            EntityType = EntityType,
            Key = (Guid)_info.Key!.GetValue(model)!,
            LiveVersion = _info.Version?.GetValue(model) is { } version ? Convert.ToInt64(version) : null,
        };
        ModelShapes.Write(model, document.Root);
        return document;
    }

    private static string AliasOf(LambdaExpression property) => ModelShapes.AliasOf(ModelBuilder<TModel>.PropertyOf(property));
}

/// <summary>
/// A product ability that changes one entity, written against its model: <see cref="Edit"/> changes the model. Run
/// directly, it loads, edits and saves the entity; inside a change set it is recorded as a change to the entity, shown
/// in its working state and saved with the entity's product.
/// </summary>
public abstract class ModelEditAbilityBase<TInput, TModel>(EntityDefinitionBase<TModel> definition)
    : AbilityBase<TInput, EntityRef>, IEntityEditAbility
    where TModel : class
{
    /// <summary>The key of the entity the input edits.</summary>
    protected abstract Guid KeyOf(TInput input);

    /// <summary>Changes the model. Throw <see cref="AbilityInputException"/> for a correctable problem.</summary>
    protected abstract void Edit(TModel model, TInput input);

    public EntityEditTarget Prepare(JsonObject input)
        => new(definition.EntityType, KeyOf(Read(input)), Creates: false);

    public object? Apply(EntityDocument document, JsonObject input, EntityEditContext context)
    {
        var model = definition.ToModel(document);
        Edit(model, Read(input));
        document.Root = definition.ToDocument(model).Root;
        return definition.ToRef(document);
    }

    protected override async Task<AbilityResult> ExecuteAsync(TInput input, AbilityContext context, CancellationToken cancellationToken)
    {
        var key = KeyOf(input);
        var model = await definition.LoadModelAsync(key, cancellationToken)
            ?? throw new AbilityInputException($"No {definition.EntityType} with key {key}.", [new Problem("key", "Not found.")]);
        var before = definition.ToDocument(model);
        Edit(model, input);

        // Run directly, the ability saves: the user must be allowed to change the entity, as inside a change set.
        var after = definition.ToDocument(model);
        if (await EntityAccess.CheckAsync(definition, new EntityAccessRequest(context.UserKey, EntityPermission.Update, key, after, EntityAccess.CulturesChanged(before, after)), cancellationToken) is { } denied)
        {
            return EntityAccess.Denied(denied);
        }

        await definition.SaveModelAsync(model, cancellationToken);
        return Success(definition.ToRef(definition.ToDocument(model)));
    }

    private static TInput Read(JsonObject input)
        => System.Text.Json.JsonSerializer.Deserialize<TInput>(input, AbilityJson.Options) ?? throw new AbilityInputException("Input is required.");
}
