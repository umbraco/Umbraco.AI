# Table of contents

* [Umbraco.Ai](README.md)

## Getting Started

* [Overview](getting-started/README.md)
  * [Installation](getting-started/installation.md)
  * [Configuration](getting-started/configuration.md)
  * [Your First Connection](getting-started/first-connection.md)
  * [Your First Profile](getting-started/first-profile.md)

## Concepts

* [Core Concepts](concepts/README.md)
  * [Providers](concepts/providers.md)
  * [Connections](concepts/connections.md)
  * [Profiles](concepts/profiles.md)
  * [Capabilities](concepts/capabilities.md)
  * [Middleware](concepts/middleware.md)

## Using the API

* [Overview](using-the-api/README.md)
* [Chat](using-the-api/chat/README.md)
  * [Basic Chat](using-the-api/chat/basic-chat.md)
  * [Streaming](using-the-api/chat/streaming.md)
  * [System Prompts](using-the-api/chat/system-prompts.md)
  * [Advanced Options](using-the-api/chat/advanced-options.md)
* [Embeddings](using-the-api/embeddings/README.md)
  * [Generating Embeddings](using-the-api/embeddings/generating-embeddings.md)
  * [Batch Embeddings](using-the-api/embeddings/batch-embeddings.md)
* [Tools](using-the-api/tools/README.md)
  * [Using Tools](using-the-api/tools/using-tools.md)

## Backoffice

* [Overview](backoffice/README.md)
  * [Managing Connections](backoffice/managing-connections.md)
  * [Managing Profiles](backoffice/managing-profiles.md)

## Extending

* [Overview](extending/README.md)
* [Custom Providers](extending/providers/README.md)
  * [Creating a Provider](extending/providers/creating-a-provider.md)
  * [Provider Settings](extending/providers/provider-settings.md)
  * [Chat Capability](extending/providers/chat-capability.md)
  * [Embedding Capability](extending/providers/embedding-capability.md)
* [Middleware](extending/middleware/README.md)
  * [Chat Middleware](extending/middleware/chat-middleware.md)
  * [Embedding Middleware](extending/middleware/embedding-middleware.md)
  * [Middleware Ordering](extending/middleware/middleware-ordering.md)
* [Custom Tools](extending/tools/README.md)
  * [Creating a Tool](extending/tools/creating-a-tool.md)

## Management API

* [Overview](management-api/README.md)
  * [Authentication](management-api/authentication.md)
* [Chat](management-api/chat/README.md)
  * [Complete](management-api/chat/complete.md)
  * [Stream](management-api/chat/stream.md)
* [Connections](management-api/connections/README.md)
  * [List Connections](management-api/connections/list.md)
  * [Get Connection](management-api/connections/get.md)
  * [Create Connection](management-api/connections/create.md)
  * [Update Connection](management-api/connections/update.md)
  * [Delete Connection](management-api/connections/delete.md)
  * [Test Connection](management-api/connections/test.md)
  * [List Capabilities](management-api/connections/capabilities.md)
  * [Get Models](management-api/connections/models.md)
* [Profiles](management-api/profiles/README.md)
  * [List Profiles](management-api/profiles/list.md)
  * [Get Profile](management-api/profiles/get.md)
  * [Create Profile](management-api/profiles/create.md)
  * [Update Profile](management-api/profiles/update.md)
  * [Delete Profile](management-api/profiles/delete.md)
* [Providers](management-api/providers/README.md)
  * [List Providers](management-api/providers/list.md)
  * [Get Provider](management-api/providers/get.md)
* [Embeddings](management-api/embeddings/README.md)
  * [Generate](management-api/embeddings/generate.md)

## Frontend

* [Overview](frontend/README.md)
  * [Chat Controller](frontend/chat-controller.md)
  * [Chat Repository](frontend/chat-repository.md)
  * [Types](frontend/types.md)

## Reference

* [Overview](reference/README.md)
* [Services](reference/services/README.md)
  * [IAiChatService](reference/services/ai-chat-service.md)
  * [IAiProfileService](reference/services/ai-profile-service.md)
  * [IAiConnectionService](reference/services/ai-connection-service.md)
  * [IAiEmbeddingService](reference/services/ai-embedding-service.md)
* [Models](reference/models/README.md)
  * [AiProfile](reference/models/ai-profile.md)
  * [AiConnection](reference/models/ai-connection.md)
  * [AiCapability](reference/models/ai-capability.md)
  * [AiModelRef](reference/models/ai-model-ref.md)
* [Configuration](reference/configuration/README.md)
  * [AiOptions](reference/configuration/ai-options.md)
