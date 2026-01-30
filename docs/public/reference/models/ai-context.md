---
description: >-
  Model representing an AI context with resources.
---

# AiContext

Represents a collection of resources (brand voice, guidelines, content) that can be injected into AI operations.

## Namespace

```csharp
using Umbraco.Ai.Core.Contexts;
```

## Definition

{% code title="AiContext" %}
```csharp
public class AiContext : IAiVersionableEntity
{
    public Guid Id { get; internal set; }
    public required string Alias { get; set; }
    public required string Name { get; set; }
    public IList<AiContextResource> Resources { get; set; } = new List<AiContextResource>();

    // Audit properties
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;
    public DateTime DateModified { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; init; }
    public Guid? ModifiedByUserId { get; set; }

    // Versioning
    public int Version { get; internal set; } = 1;
}
```
{% endcode %}

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier (auto-generated) |
| `Alias` | `string` | Unique alias for programmatic lookup (required) |
| `Name` | `string` | Display name (required) |
| `Resources` | `IList<AiContextResource>` | Collection of resources |
| `DateCreated` | `DateTime` | When created (UTC) |
| `DateModified` | `DateTime` | When last modified (UTC) |
| `CreatedByUserId` | `Guid?` | User who created |
| `ModifiedByUserId` | `Guid?` | User who last modified |
| `Version` | `int` | Current version number |

## Example

{% code title="Example" %}
```csharp
var context = new AiContext
{
    Alias = "brand-guidelines",
    Name = "Brand Guidelines",
    Resources = new List<AiContextResource>
    {
        new AiContextResource
        {
            ResourceTypeId = "text",
            Name = "Tone of Voice",
            Description = "Writing style guidelines",
            SortOrder = 0,
            Data = "Always use a friendly, professional tone...",
            InjectionMode = AiContextResourceInjectionMode.Always
        },
        new AiContextResource
        {
            ResourceTypeId = "text",
            Name = "Product Terminology",
            SortOrder = 1,
            Data = "Use these terms when discussing products..."
        }
    }
};
```
{% endcode %}

## Related

* [AiContextResource](#aicontextresource) - Resource model
* [IAiContextService](../services/ai-context-service.md) - Context service

---

# AiContextResource

Represents a single resource within a context.

## Definition

{% code title="AiContextResource" %}
```csharp
public class AiContextResource
{
    public Guid Id { get; internal set; }
    public required string ResourceTypeId { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public object? Data { get; set; }
    public AiContextResourceInjectionMode InjectionMode { get; set; } = AiContextResourceInjectionMode.Always;
}
```
{% endcode %}

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier (auto-generated) |
| `ResourceTypeId` | `string` | Type of resource (required, immutable) |
| `Name` | `string` | Display name (required) |
| `Description` | `string?` | Optional description |
| `SortOrder` | `int` | Order for injection |
| `Data` | `object?` | Resource content (type-specific) |
| `InjectionMode` | `AiContextResourceInjectionMode` | When to inject |

## Resource Types

| Type ID | Data Type | Description |
|---------|-----------|-------------|
| `text` | `string` | Plain text content |
| `document` | `object` | Structured document |
| `url` | `string` | URL reference |

## Injection Modes

{% code title="AiContextResourceInjectionMode" %}
```csharp
public enum AiContextResourceInjectionMode
{
    Always = 0,   // Always inject
    OnDemand = 1  // Only inject when explicitly requested
}
```
{% endcode %}
