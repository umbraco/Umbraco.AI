---
description: >-
  Model representing an entity version history record.
---

# AiEntityVersion

Represents a version history record for a versioned AI entity.

## Namespace

```csharp
using Umbraco.Ai.Core.Versioning;
```

## Definition

{% code title="AiEntityVersion" %}
```csharp
public class AiEntityVersion
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Snapshot { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? ChangeDescription { get; set; }
}
```
{% endcode %}

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Version record identifier |
| `EntityId` | `Guid` | ID of the versioned entity |
| `EntityType` | `string` | Type discriminator |
| `Version` | `int` | Sequential version number |
| `Snapshot` | `string` | JSON serialization of entity state |
| `DateCreated` | `DateTime` | When this version was created |
| `CreatedByUserId` | `Guid?` | User who created this version |
| `ChangeDescription` | `string?` | Optional description of changes |

## Entity Types

| Type String | Entity | Package |
|-------------|--------|---------|
| `"connection"` | `AiConnection` | Umbraco.Ai |
| `"profile"` | `AiProfile` | Umbraco.Ai |
| `"context"` | `AiContext` | Umbraco.Ai |
| `"prompt"` | `AiPrompt` | Umbraco.Ai.Prompt |
| `"agent"` | `AiAgent` | Umbraco.Ai.Agent |

## Example

{% code title="Example" %}
```csharp
// Getting version history
var (versions, total) = await _versionService.GetVersionHistoryAsync(
    profileId,
    "profile");

foreach (var version in versions)
{
    Console.WriteLine($"v{version.Version} - {version.DateCreated}");
    if (!string.IsNullOrEmpty(version.ChangeDescription))
    {
        Console.WriteLine($"  {version.ChangeDescription}");
    }
}
```
{% endcode %}

---

# AiVersionComparison

Result of comparing two entity versions.

{% code title="AiVersionComparison" %}
```csharp
public class AiVersionComparison
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int FromVersion { get; set; }
    public int ToVersion { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public IReadOnlyList<AiVersionChange> Changes { get; set; } = Array.Empty<AiVersionChange>();
    public string? FromSnapshot { get; set; }
    public string? ToSnapshot { get; set; }
}
```
{% endcode %}

---

# AiVersionChange

Represents a single change between two versions.

{% code title="AiVersionChange" %}
```csharp
public class AiVersionChange
{
    public string Path { get; set; } = string.Empty;
    public AiVersionChangeType ChangeType { get; set; }
    public string? FromValue { get; set; }
    public string? ToValue { get; set; }
}

public enum AiVersionChangeType
{
    Added = 0,
    Removed = 1,
    Modified = 2
}
```
{% endcode %}

---

# AiVersionCleanupResult

Result of version cleanup operation.

{% code title="AiVersionCleanupResult" %}
```csharp
public class AiVersionCleanupResult
{
    public int DeletedCount { get; set; }
    public DateTime? OldestRetained { get; set; }
}
```
{% endcode %}

---

# IAiVersionableEntity

Interface implemented by entities that support versioning.

{% code title="IAiVersionableEntity" %}
```csharp
public interface IAiVersionableEntity : IAiAuditableEntity
{
    int Version { get; }
}
```
{% endcode %}

## Related

* [IAiEntityVersionService](../services/ai-entity-version-service.md) - Version service
* [Version History Concept](../../concepts/versioning.md) - Versioning concepts
