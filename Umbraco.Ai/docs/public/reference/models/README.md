---
description: >-
  Domain model classes for AI operations.
---

# Models

Core domain models used throughout Umbraco.Ai.

## Available Models

| Model | Description |
|-------|-------------|
| [AiProfile](ai-profile.md) | Configuration for AI model usage |
| [AiConnection](ai-connection.md) | Connection to an AI provider |
| [AiCapability](ai-capability.md) | Type of AI capability |
| [AiModelRef](ai-model-ref.md) | Reference to a specific model |

## Model Relationships

```
AiConnection (credentials)
      │
      └─► AiProfile (settings)
              │
              ├── AiCapability (what it does)
              └── AiModelRef (which model)
```

A profile references:
- One **connection** for authentication
- One **model reference** specifying provider and model ID
- One **capability** type (Chat, Embedding, and so on)

## In This Section

{% content-ref url="ai-profile.md" %}
[AiProfile](ai-profile.md)
{% endcontent-ref %}

{% content-ref url="ai-connection.md" %}
[AiConnection](ai-connection.md)
{% endcontent-ref %}

{% content-ref url="ai-capability.md" %}
[AiCapability](ai-capability.md)
{% endcontent-ref %}

{% content-ref url="ai-model-ref.md" %}
[AiModelRef](ai-model-ref.md)
{% endcontent-ref %}
