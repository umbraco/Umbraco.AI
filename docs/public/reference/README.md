---
description: >-
  API reference documentation for Umbraco.Ai.
---

# Reference

This section provides detailed API reference documentation for Umbraco.Ai services, models, and configuration.

## Services

Core services for AI operations:

| Service | Description |
|---------|-------------|
| [IAiChatService](services/ai-chat-service.md) | Chat completion operations |
| [IAiEmbeddingService](services/ai-embedding-service.md) | Embedding generation |
| [IAiProfileService](services/ai-profile-service.md) | Profile management |
| [IAiConnectionService](services/ai-connection-service.md) | Connection management |

## Models

Domain models used throughout the API:

| Model | Description |
|-------|-------------|
| [AiProfile](models/ai-profile.md) | Profile configuration |
| [AiConnection](models/ai-connection.md) | Provider connection |
| [AiCapability](models/ai-capability.md) | Capability enumeration |
| [AiModelRef](models/ai-model-ref.md) | Model reference struct |

## Configuration

Application configuration options:

| Class | Description |
|-------|-------------|
| [AiOptions](configuration/ai-options.md) | Global AI settings |

## In This Section

{% content-ref url="services/README.md" %}
[Services](services/README.md)
{% endcontent-ref %}

{% content-ref url="models/README.md" %}
[Models](models/README.md)
{% endcontent-ref %}

{% content-ref url="configuration/README.md" %}
[Configuration](configuration/README.md)
{% endcontent-ref %}
