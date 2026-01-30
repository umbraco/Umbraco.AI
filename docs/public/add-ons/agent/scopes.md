---
description: >-
  Categorize agents with scopes for filtering and organization.
---

# Agent Scopes

Scopes allow you to categorize agents for specific purposes. Add-on packages can register their own scopes, and agents can be assigned to one or more scopes to indicate their intended use.

## What are Scopes?

Scopes are categorization tags that:

- **Group agents** by their intended context (e.g., "copilot", "content-editing")
- **Enable filtering** via the API to find agents for specific purposes
- **Allow extensibility** - any add-on package can define new scopes
- **Support multiple assignments** - agents can belong to several scopes

{% hint style="info" %}
An agent with no scopes will appear in general listings but will not be returned when filtering by a specific scope.
{% endhint %}

## Built-in Scopes

The **Agent Copilot** add-on registers the `copilot` scope, which indicates agents that should appear in the copilot chat sidebar.

| Scope ID | Package | Icon | Description |
|----------|---------|------|-------------|
| `copilot` | Umbraco.Ai.Agent.Copilot | `icon-chat` | Agents available in the copilot chat sidebar |

## Assigning Scopes to Agents

### Via Backoffice

When creating or editing an agent in the backoffice, you can assign scopes in the **Scopes** section. Available scopes are populated from all registered scope providers.

### Via API

Include `scopeIds` when creating or updating an agent:

{% code title="Request" %}
```json
{
  "alias": "content-assistant",
  "name": "Content Assistant",
  "scopeIds": ["copilot"],
  "instructions": "You are a helpful content assistant."
}
```
{% endcode %}

### Via Code

```csharp
var agent = new AiAgent
{
    Alias = "content-assistant",
    Name = "Content Assistant",
    ScopeIds = ["copilot", "content-editing"],
    Instructions = "You are a helpful content assistant."
};

await _agentService.SaveAgentAsync(agent);
```

## Querying Agents by Scope

### List Agents by Scope

Use the `scopeId` query parameter to filter agents:

```http
GET /umbraco/ai/management/api/v1/agent?scopeId=copilot
```

### Get All Registered Scopes

Retrieve all scopes registered in the system:

```http
GET /umbraco/ai/management/api/v1/agent/scopes
```

{% code title="200 OK" %}
```json
[
  {
    "id": "copilot",
    "icon": "icon-chat"
  }
]
```
{% endcode %}

### Via Service

```csharp
// Get agents by scope
var copilotAgents = await _agentService.GetAgentsByScopeAsync("copilot");

// Or use paged query with scope filter
var pagedResult = await _agentService.GetAgentsPagedAsync(
    skip: 0,
    take: 10,
    scopeId: "copilot"
);
```

## Creating Custom Scopes

Add-on packages can register their own scopes to categorize agents for their specific features.

### 1. Define the Scope Class

Create a class that derives from `AiAgentScopeBase` and decorate it with the `[AiAgentScope]` attribute:

{% code title="MyFeatureScope.cs" %}
```csharp
using Umbraco.Ai.Agent.Core.Scopes;

namespace MyPackage.Scopes;

[AiAgentScope("my-feature", Icon = "icon-settings")]
public class MyFeatureScope : AiAgentScopeBase
{
    /// <summary>
    /// Constant for referencing this scope ID in code.
    /// </summary>
    public const string ScopeId = "my-feature";
}
```
{% endcode %}

### 2. Automatic Registration

Scopes are automatically discovered and registered during application startup. The framework scans for all types with the `[AiAgentScope]` attribute that implement `IAiAgentScope`.

### 3. Manual Registration (Optional)

For more control, you can manually register scopes in a composer:

{% code title="MyComposer.cs" %}
```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Ai.Agent.Core.Configuration;

namespace MyPackage;

public class MyComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AiAgentScopes()
            .Add<MyFeatureScope>();
    }
}
```
{% endcode %}

### 4. Query Agents by Your Scope

```csharp
public class MyFeatureService
{
    private readonly IAiAgentService _agentService;

    public MyFeatureService(IAiAgentService agentService)
    {
        _agentService = agentService;
    }

    public async Task<IEnumerable<AiAgent>> GetMyFeatureAgentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _agentService.GetAgentsByScopeAsync(
            MyFeatureScope.ScopeId,
            cancellationToken);
    }
}
```

## Scope Interface

The `IAiAgentScope` interface defines the contract for scopes:

```csharp
public interface IAiAgentScope
{
    /// <summary>
    /// Gets the unique identifier for this scope.
    /// Should be a simple, URL-safe string like "copilot" or "content-editing".
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the icon to display for this scope.
    /// Uses Umbraco icon names (e.g., "icon-chat", "icon-document").
    /// </summary>
    string Icon { get; }
}
```

## Frontend Localization

Scope names and descriptions are localized on the frontend using a naming convention:

| Key Pattern | Purpose |
|-------------|---------|
| `uaiAgentScope_{scopeId}Label` | Display name for the scope |
| `uaiAgentScope_{scopeId}Description` | Description shown in UI |

**Example for a custom "content-editing" scope:**

{% code title="en.ts" %}
```typescript
export default {
  uaiAgentScope_contentEditingLabel: "Content Editing",
  uaiAgentScope_contentEditingDescription: "Agents for inline content editing"
};
```
{% endcode %}

## Scope Collection

The `AiAgentScopeCollection` provides methods for working with registered scopes:

```csharp
public class MyScopeHelper
{
    private readonly AiAgentScopeCollection _scopes;

    public MyScopeHelper(AiAgentScopeCollection scopes)
    {
        _scopes = scopes;
    }

    public void CheckScopes()
    {
        // Check if a scope exists
        if (_scopes.Exists("my-feature"))
        {
            // Get scope by ID
            var scope = _scopes.GetById("my-feature");

            // Get multiple scopes
            var selectedScopes = _scopes.GetByIds(["copilot", "my-feature"]);
        }
    }
}
```

## Best Practices

### Scope Naming

- Use lowercase, URL-safe identifiers (e.g., `content-editing`, not `Content Editing`)
- Keep names short but descriptive
- Use hyphens to separate words

### Scope Design

- **Single purpose** - each scope should represent one clear use case
- **Document your scopes** - provide localization keys for UI display
- **Use constants** - define a `ScopeId` constant for code references

### Multi-Scope Agents

Agents can belong to multiple scopes when they serve multiple purposes:

```csharp
var versatileAgent = new AiAgent
{
    Alias = "universal-assistant",
    Name = "Universal Assistant",
    ScopeIds = ["copilot", "content-editing", "my-feature"],
    Instructions = "You can help with various tasks..."
};
```

## Related

* [Agent Concepts](concepts.md) - Agent overview
* [API: List Agents](api/list.md) - List endpoint with scope filtering
* [Agent Copilot](../agent-copilot/README.md) - Copilot scope usage
