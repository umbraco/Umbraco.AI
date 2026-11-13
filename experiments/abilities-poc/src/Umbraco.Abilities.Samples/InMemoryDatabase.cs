using System.Text.Json.Nodes;

using Umbraco.Abilities.DraftSets;
using Umbraco.Abilities.Entities;

namespace Umbraco.Abilities.Samples;

/// <summary>
/// Stands in for one product's database and transaction. Each sample product has its own, as Commerce, Automate
/// and Umbraco.AI can each use their own connection string.
/// </summary>
public sealed class InMemoryDatabase(string name) : IUnitOfWorkProvider
{
    public string Name { get; } = name;

    private Dictionary<string, Dictionary<Guid, Row>> _tables = [];

    public sealed record Row(JsonObject Root, long Version);

    public Row? Get(string entityType, Guid key)
        => _tables.TryGetValue(entityType, out var table) && table.TryGetValue(key, out var row)
            ? row with { Root = (JsonObject)row.Root.DeepClone() }
            : null;

    public IEnumerable<(Guid Key, Row Row)> All(string entityType)
        => _tables.TryGetValue(entityType, out var table) ? table.Select(kv => (kv.Key, kv.Value)).ToList() : [];

    public long Put(string entityType, Guid key, JsonObject root)
    {
        if (!_tables.TryGetValue(entityType, out var table))
        {
            table = [];
            _tables[entityType] = table;
        }

        var version = table.TryGetValue(key, out var existing) ? existing.Version + 1 : 1;
        table[key] = new Row((JsonObject)root.DeepClone(), version);
        return version;
    }

    public IUnitOfWork Begin() => new Transaction(this, Snapshot());

    private Dictionary<string, Dictionary<Guid, Row>> Snapshot()
        => _tables.ToDictionary(t => t.Key, t => t.Value.ToDictionary(r => r.Key, r => r.Value with { Root = (JsonObject)r.Value.Root.DeepClone() }));

    private sealed class Transaction(InMemoryDatabase database, Dictionary<string, Dictionary<Guid, Row>> snapshot) : IUnitOfWork
    {
        private bool _completed;

        public void Complete() => _completed = true;

        public void Dispose()
        {
            if (!_completed)
            {
                database._tables = snapshot;
            }
        }
    }
}

/// <summary>A minimal entity definition over the in-memory database. A real product maps to its own model and services.</summary>
public abstract class InMemoryEntityDefinition(InMemoryDatabase database, string entityType, string nameField, string editUrlPrefix)
    : IEntityDefinition
{
    protected InMemoryDatabase Database { get; } = database;

    public string EntityType { get; } = entityType;

    public virtual bool CanCreate => true;

    public IUnitOfWorkProvider UnitOfWork => Database;

    public abstract Task<EntityStructure> DescribeAsync(string? subtype, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<EntitySubtype>> GetSubtypesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EntitySubtype>>([]);

    public EntityDocument New(Guid key, string? subtype) => new() { EntityType = EntityType, Key = key };

    public Task<EntityDocument?> LoadAsync(Guid key, CancellationToken cancellationToken = default)
    {
        var row = Database.Get(EntityType, key);
        return Task.FromResult(row is null ? null : new EntityDocument { EntityType = EntityType, Key = key, LiveVersion = row.Version, Root = row.Root });
    }

    public virtual Task<IReadOnlyList<Problem>> ValidateAsync(EntityDocument document, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Problem>>([]);

    public virtual Task<EntityRef> SaveAsync(EntityDocument document, CancellationToken cancellationToken = default)
    {
        Database.Put(EntityType, document.Key, document.Root);
        return Task.FromResult(ToRef(document));
    }

    public Task<IReadOnlyList<EntityRef>> FindAsync(string query, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EntityRef>>(Database.All(EntityType)
            .Select(r => ToRef(new EntityDocument { EntityType = EntityType, Key = r.Key, Root = r.Row.Root }))
            .Where(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList());

    public EntityRef ToRef(EntityDocument document)
        => new(EntityType, document.Key, Element.ReadAny(document.Root, nameField)?.GetValue<string>() ?? "(unnamed)", $"{editUrlPrefix}/{document.Key}");

    protected static string? Text(JsonObject element, string alias) => Element.Fields(element)[alias]?.GetValue<string>();
}
