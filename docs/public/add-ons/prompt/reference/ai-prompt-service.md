---
description: >-
  Service for managing and executing prompts.
---

# IAiPromptService

Service for prompt CRUD operations and execution.

## Namespace

```csharp
using Umbraco.Ai.Prompt.Core.Prompts;
```

## Interface

{% code title="IAiPromptService" %}
```csharp
public interface IAiPromptService
{
    Task<AiPrompt?> GetPromptAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AiPrompt?> GetPromptByAliasAsync(string alias, CancellationToken cancellationToken = default);

    Task<IEnumerable<AiPrompt>> GetPromptsAsync(CancellationToken cancellationToken = default);

    Task<PagedModel<AiPrompt>> GetPromptsPagedAsync(
        int skip = 0,
        int take = 100,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default);

    Task<AiPrompt> SavePromptAsync(AiPrompt prompt, CancellationToken cancellationToken = default);

    Task<bool> DeletePromptAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> PromptAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<AiPromptExecutionResult> ExecutePromptAsync(
        Guid promptId,
        AiPromptExecutionRequest request,
        CancellationToken cancellationToken = default);
}
```
{% endcode %}

## Methods

### GetPromptAsync

Gets a prompt by its unique identifier.

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | `Guid` | The prompt ID |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The prompt if found, otherwise `null`.

### GetPromptByAliasAsync

Gets a prompt by its alias.

| Parameter | Type | Description |
|-----------|------|-------------|
| `alias` | `string` | The prompt alias |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The prompt if found, otherwise `null`.

### GetPromptsAsync

Gets all prompts.

**Returns**: All prompts in the system.

### GetPromptsPagedAsync

Gets prompts with pagination and filtering.

| Parameter | Type | Description |
|-----------|------|-------------|
| `skip` | `int` | Items to skip |
| `take` | `int` | Items to take |
| `filter` | `string?` | Filter by name |
| `profileId` | `Guid?` | Filter by profile |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: Paged result with items and total count.

### SavePromptAsync

Creates or updates a prompt.

| Parameter | Type | Description |
|-----------|------|-------------|
| `prompt` | `AiPrompt` | The prompt to save |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: The saved prompt with ID and version.

### DeletePromptAsync

Deletes a prompt by ID.

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | `Guid` | The prompt ID |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: `true` if deleted, `false` if not found.

### PromptAliasExistsAsync

Checks if an alias is in use.

| Parameter | Type | Description |
|-----------|------|-------------|
| `alias` | `string` | The alias to check |
| `excludeId` | `Guid?` | Optional ID to exclude |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: `true` if alias exists.

### ExecutePromptAsync

Executes a prompt with variables.

| Parameter | Type | Description |
|-----------|------|-------------|
| `promptId` | `Guid` | The prompt ID |
| `request` | `AiPromptExecutionRequest` | Execution parameters |
| `cancellationToken` | `CancellationToken` | Cancellation token |

**Returns**: Execution result with response.

{% code title="Example" %}
```csharp
var result = await _promptService.ExecutePromptAsync(
    promptId,
    new AiPromptExecutionRequest
    {
        Variables = new Dictionary<string, string>
        {
            ["title"] = "Article Title",
            ["content"] = "Article content..."
        }
    });

Console.WriteLine($"Response: {result.Response}");
Console.WriteLine($"Tokens used: {result.Usage.TotalTokens}");
```
{% endcode %}

## Related Models

### AiPromptExecutionRequest

{% code title="AiPromptExecutionRequest" %}
```csharp
public class AiPromptExecutionRequest
{
    public IDictionary<string, string> Variables { get; set; }
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityContext { get; set; }
}
```
{% endcode %}

### AiPromptExecutionResult

{% code title="AiPromptExecutionResult" %}
```csharp
public class AiPromptExecutionResult
{
    public string Response { get; set; }
    public Guid PromptId { get; set; }
    public int PromptVersion { get; set; }
    public Guid ProfileId { get; set; }
    public AiModelRef Model { get; set; }
    public AiTokenUsage Usage { get; set; }
    public Guid AuditLogId { get; set; }
}
```
{% endcode %}

## Related

* [AiPrompt Model](ai-prompt.md) - Prompt model reference
* [Prompt Concepts](../concepts.md) - Concepts overview
