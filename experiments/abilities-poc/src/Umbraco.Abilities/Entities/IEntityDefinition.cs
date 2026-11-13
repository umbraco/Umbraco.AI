namespace Umbraco.Abilities.Entities;

/// <summary>
/// What a package implements once per entity type. The framework derives the generic entity
/// abilities (schema, create, edit, find, validate) from it.
/// </summary>
public interface IEntityDefinition
{
    string EntityType { get; }

    /// <summary>False for entities that can be found and referenced but not created here.</summary>
    bool CanCreate { get; }

    /// <summary>May depend on what is installed, so it is resolved at run time.</summary>
    Task<EntityStructure> DescribeAsync(CancellationToken cancellationToken = default);

    EntityDocument New(Guid key);

    Task<EntityDocument?> LoadAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>Domain rules beyond the structure (ranges, dates in order, uniqueness). Problems by path.</summary>
    Task<IReadOnlyList<Problem>> ValidateAsync(EntityDocument document, CancellationToken cancellationToken = default);

    Task<EntityRef> SaveAsync(EntityDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EntityRef>> FindAsync(string query, CancellationToken cancellationToken = default);

    EntityRef ToRef(EntityDocument document);
}

public sealed class EntityDefinitionRegistry
{
    private readonly Dictionary<string, IEntityDefinition> _definitions = new(StringComparer.Ordinal);

    public EntityDefinitionRegistry Add(IEntityDefinition definition)
    {
        _definitions.Add(definition.EntityType, definition);
        return this;
    }

    public IEntityDefinition? Find(string entityType) => _definitions.GetValueOrDefault(entityType);

    public IEntityDefinition Get(string entityType)
        => Find(entityType) ?? throw new AbilityInputException(
            $"Unknown entity type '{entityType}'.",
            [new Problem("entityType", $"Known types: {string.Join(", ", _definitions.Keys)}.")]);

    public IReadOnlyCollection<IEntityDefinition> All => _definitions.Values;
}
