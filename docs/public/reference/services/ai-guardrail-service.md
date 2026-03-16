---
description: >-
    Service for managing AI guardrails.
---

# IAIGuardrailService

Service for guardrail CRUD operations and lookups. Guardrails define rules that evaluate AI inputs and responses for safety, compliance, and quality.

## Namespace

```csharp
using Umbraco.AI.Core.Guardrails;
```

## Interface

{% code title="IAIGuardrailService" %}

```csharp
public interface IAIGuardrailService
{
    Task<AIGuardrail?> GetGuardrailAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AIGuardrail?> GetGuardrailByAliasAsync(string alias, CancellationToken cancellationToken = default);

    Task<IEnumerable<AIGuardrail>> GetAllGuardrailsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<AIGuardrail>> GetGuardrailsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<AIGuardrail> CreateGuardrailAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default);

    Task<AIGuardrail> UpdateGuardrailAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default);

    Task DeleteGuardrailAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AIGuardrail> SaveGuardrailAsync(AIGuardrail guardrail, CancellationToken cancellationToken = default);

    Task<bool> GuardrailAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
```

{% endcode %}

## Methods

### GetGuardrailAsync

Gets a guardrail by its unique identifier.

| Parameter           | Type                | Description          |
| ------------------- | ------------------- | -------------------- |
| `id`                | `Guid`              | The guardrail ID     |
| `cancellationToken` | `CancellationToken` | Cancellation token   |

**Returns**: The guardrail if found, otherwise `null`.

{% code title="Example" %}

```csharp
var guardrail = await _guardrailService.GetGuardrailAsync(guardrailId);
if (guardrail != null)
{
    Console.WriteLine($"Guardrail: {guardrail.Name}");
    Console.WriteLine($"Rules: {guardrail.Rules.Count}");
}
```

{% endcode %}

### GetGuardrailByAliasAsync

Gets a guardrail by its alias.

| Parameter           | Type                | Description          |
| ------------------- | ------------------- | -------------------- |
| `alias`             | `string`            | The guardrail alias  |
| `cancellationToken` | `CancellationToken` | Cancellation token   |

**Returns**: The guardrail if found, otherwise `null`.

{% code title="Example" %}

```csharp
var safetyGuardrail = await _guardrailService.GetGuardrailByAliasAsync("content-safety");
```

{% endcode %}

### GetAllGuardrailsAsync

Gets all guardrails.

**Returns**: All guardrails in the system.

{% code title="Example" %}

```csharp
var allGuardrails = await _guardrailService.GetAllGuardrailsAsync();
foreach (var gr in allGuardrails)
{
    Console.WriteLine($"{gr.Alias}: {gr.Name} ({gr.Rules.Count} rules)");
}
```

{% endcode %}

### GetGuardrailsByIdsAsync

Gets multiple guardrails by their IDs.

| Parameter           | Type                 | Description                  |
| ------------------- | -------------------- | ---------------------------- |
| `ids`               | `IEnumerable<Guid>`  | The guardrail IDs to look up |
| `cancellationToken` | `CancellationToken`  | Cancellation token           |

**Returns**: The guardrails that were found.

{% code title="Example" %}

```csharp
var guardrails = await _guardrailService.GetGuardrailsByIdsAsync(profile.Settings.GuardrailIds);
```

{% endcode %}

### CreateGuardrailAsync

Creates a new guardrail.

| Parameter           | Type                | Description             |
| ------------------- | ------------------- | ----------------------- |
| `guardrail`         | `AIGuardrail`       | The guardrail to create |
| `cancellationToken` | `CancellationToken` | Cancellation token      |

**Returns**: The created guardrail with ID assigned.

{% code title="Example" %}

```csharp
var guardrail = new AIGuardrail
{
    Alias = "content-safety",
    Name = "Content Safety Policy",
    Rules =
    [
        new AIGuardrailRule
        {
            EvaluatorId = "pii",
            Name = "Block PII",
            Phase = AIGuardrailPhase.PreGenerate,
            Action = AIGuardrailAction.Block,
            SortOrder = 0
        }
    ]
};

var created = await _guardrailService.CreateGuardrailAsync(guardrail);
```

{% endcode %}

### UpdateGuardrailAsync

Updates an existing guardrail. A new version is created automatically.

| Parameter           | Type                | Description             |
| ------------------- | ------------------- | ----------------------- |
| `guardrail`         | `AIGuardrail`       | The guardrail to update |
| `cancellationToken` | `CancellationToken` | Cancellation token      |

**Returns**: The updated guardrail with version incremented.

{% code title="Example" %}

```csharp
guardrail.Name = "Updated Safety Policy";
guardrail.Rules.Add(new AIGuardrailRule
{
    EvaluatorId = "toxicity",
    Name = "Block toxic content",
    Phase = AIGuardrailPhase.PostGenerate,
    Action = AIGuardrailAction.Block,
    SortOrder = 1
});

var updated = await _guardrailService.UpdateGuardrailAsync(guardrail);
Console.WriteLine($"Updated to version {updated.Version}");
```

{% endcode %}

### DeleteGuardrailAsync

Deletes a guardrail by ID.

| Parameter           | Type                | Description          |
| ------------------- | ------------------- | -------------------- |
| `id`                | `Guid`              | The guardrail ID     |
| `cancellationToken` | `CancellationToken` | Cancellation token   |

{% code title="Example" %}

```csharp
await _guardrailService.DeleteGuardrailAsync(guardrailId);
```

{% endcode %}

### SaveGuardrailAsync

Creates or updates a guardrail (upsert). Used internally by deploy connectors.

| Parameter           | Type                | Description           |
| ------------------- | ------------------- | --------------------- |
| `guardrail`         | `AIGuardrail`       | The guardrail to save |
| `cancellationToken` | `CancellationToken` | Cancellation token    |

**Returns**: The saved guardrail.

### GuardrailAliasExistsAsync

Checks if an alias is already in use.

| Parameter           | Type                | Description                          |
| ------------------- | ------------------- | ------------------------------------ |
| `alias`             | `string`            | The alias to check                   |
| `excludeId`         | `Guid?`             | Optional ID to exclude (for updates) |
| `cancellationToken` | `CancellationToken` | Cancellation token                   |

**Returns**: `true` if alias exists, `false` otherwise.

{% code title="Example" %}

```csharp
if (await _guardrailService.GuardrailAliasExistsAsync("content-safety"))
{
    Console.WriteLine("Alias already in use");
}
```

{% endcode %}

## Related

- [AIGuardrail](../models/ai-guardrail.md) - The guardrail model
- [Guardrails Concept](../../concepts/guardrails.md) - Guardrail concepts
