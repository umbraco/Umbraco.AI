---
description: >-
  Reference to a specific AI model.
---

# AiModelRef

A lightweight struct that references a specific AI model from a provider.

## Namespace

```csharp
using Umbraco.Ai.Core.Models;
```

## Definition

{% code title="AiModelRef" %}
```csharp
public readonly struct AiModelRef
{
    public AiModelRef(string providerId, string modelId);

    public string ProviderId { get; }
    public string ModelId { get; }

    public override string ToString() => $"{ProviderId}/{ModelId}";
}
```
{% endcode %}

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `ProviderId` | `string` | The provider's unique identifier |
| `ModelId` | `string` | The model's unique identifier |

## Constructor

```csharp
public AiModelRef(string providerId, string modelId)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `providerId` | `string` | Provider ID (required, non-null) |
| `modelId` | `string` | Model ID (required, non-null) |

**Throws**: `ArgumentNullException` if either parameter is null.

## Usage

### Creating a Model Reference

{% code title="Example" %}
```csharp
// OpenAI GPT-4o
var gpt4o = new AiModelRef("openai", "gpt-4o");

// OpenAI GPT-4o mini
var gpt4oMini = new AiModelRef("openai", "gpt-4o-mini");

// OpenAI embedding model
var embedding = new AiModelRef("openai", "text-embedding-3-small");
```
{% endcode %}

### Using with Profiles

{% code title="Example" %}
```csharp
var profile = new AiProfile
{
    Alias = "my-assistant",
    Name = "My Assistant",
    Capability = AiCapability.Chat,
    Model = new AiModelRef("openai", "gpt-4o"),
    ConnectionId = connectionId
};
```
{% endcode %}

### String Representation

{% code title="Example" %}
```csharp
var model = new AiModelRef("openai", "gpt-4o");
Console.WriteLine(model.ToString()); // Output: "openai/gpt-4o"
Console.WriteLine($"Using model: {model}"); // Output: "Using model: openai/gpt-4o"
```
{% endcode %}

## Common Model IDs

### OpenAI Chat Models

| Model ID | Description |
|----------|-------------|
| `gpt-4o` | GPT-4o (Omni) |
| `gpt-4o-mini` | GPT-4o Mini (faster, cheaper) |
| `gpt-4-turbo` | GPT-4 Turbo |
| `gpt-3.5-turbo` | GPT-3.5 Turbo |

### OpenAI Embedding Models

| Model ID | Description |
|----------|-------------|
| `text-embedding-3-small` | Small embedding model (1536 dims) |
| `text-embedding-3-large` | Large embedding model (3072 dims) |
| `text-embedding-ada-002` | Ada v2 (legacy) |

## Notes

- `AiModelRef` is a `readonly struct` for performance
- Both properties are required and validated in the constructor
- The `ToString()` method provides a canonical string format
