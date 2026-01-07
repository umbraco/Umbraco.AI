# Umbraco.Ai Documentation Plan

This document outlines the structure and content plan for Umbraco.Ai public documentation, following Umbraco.Docs conventions.

## Documentation Conventions

Following Umbraco.Docs patterns:

- **File naming**: Lowercase with hyphens (e.g., `getting-started.md`)
- **Folder structure**: Each folder has a `README.md` as landing page
- **Frontmatter**: YAML with `description` field for search/cards
- **Code blocks**: Use `{% code title="FileName.cs" %}` syntax with language hints
- **Hints**: Use `{% hint style="info|warning|danger|success" %}` for callouts
- **Images**: Store in `images/` subdirectories, max 800px width
- **Cross-references**: Relative paths (e.g., `../extending/providers/README.md`)

---

## Proposed Structure

```
docs/public/
├── README.md                           # Landing page / Introduction
├── SUMMARY.md                          # Navigation structure
│
├── getting-started/
│   ├── README.md                       # Getting Started overview
│   ├── installation.md                 # NuGet installation
│   ├── configuration.md                # appsettings.json setup
│   ├── first-connection.md             # Creating your first connection
│   └── first-profile.md                # Creating your first profile
│
├── concepts/
│   ├── README.md                       # Core concepts overview
│   ├── providers.md                    # What providers are
│   ├── connections.md                  # What connections are
│   ├── profiles.md                     # What profiles are
│   ├── capabilities.md                 # Chat, Embedding, etc.
│   └── middleware.md                   # Request pipeline concept
│
├── using-the-api/
│   ├── README.md                       # API usage overview
│   ├── chat/
│   │   ├── README.md                   # Chat overview
│   │   ├── basic-chat.md               # Simple chat completion
│   │   ├── streaming.md                # Streaming responses
│   │   ├── system-prompts.md           # Using system prompts
│   │   └── advanced-options.md         # Temperature, max tokens, etc.
│   ├── embeddings/
│   │   ├── README.md                   # Embeddings overview
│   │   ├── generating-embeddings.md    # Basic embedding generation
│   │   └── batch-embeddings.md         # Multiple embeddings
│   └── tools/
│       ├── README.md                   # AI Tools overview
│       └── using-tools.md              # Using tools in chat
│
├── backoffice/
│   ├── README.md                       # Backoffice UI overview
│   ├── managing-connections.md         # Connection CRUD in UI
│   ├── managing-profiles.md            # Profile CRUD in UI
│   └── images/
│       └── (screenshots)
│
├── extending/
│   ├── README.md                       # Extending Umbraco.Ai overview
│   ├── providers/
│   │   ├── README.md                   # Custom providers overview
│   │   ├── creating-a-provider.md      # Step-by-step guide
│   │   ├── provider-settings.md        # AiSettingAttribute usage
│   │   ├── chat-capability.md          # Implementing IAiChatCapability
│   │   └── embedding-capability.md     # Implementing IAiEmbeddingCapability
│   ├── middleware/
│   │   ├── README.md                   # Middleware overview
│   │   ├── chat-middleware.md          # Creating chat middleware
│   │   ├── embedding-middleware.md     # Creating embedding middleware
│   │   └── middleware-ordering.md      # Collection builder ordering
│   └── tools/
│       ├── README.md                   # Custom tools overview
│       └── creating-a-tool.md          # Implementing IAiTool
│
├── management-api/
│   ├── README.md                       # Management API overview
│   ├── authentication.md               # Backoffice auth requirements
│   ├── chat/
│   │   ├── README.md                   # Chat endpoints
│   │   ├── complete.md                 # POST /chat/complete
│   │   └── stream.md                   # POST /chat/stream
│   ├── connections/
│   │   ├── README.md                   # Connection endpoints
│   │   ├── list.md                     # GET /connections
│   │   ├── get.md                      # GET /connections/{idOrAlias}
│   │   ├── create.md                   # POST /connections
│   │   ├── update.md                   # PUT /connections/{id}
│   │   ├── delete.md                   # DELETE /connections/{id}
│   │   ├── test.md                     # POST /connections/{id}/test
│   │   ├── capabilities.md             # GET /connections/capabilities
│   │   └── models.md                   # GET /connections/{id}/models
│   ├── profiles/
│   │   ├── README.md                   # Profile endpoints
│   │   ├── list.md                     # GET /profiles
│   │   ├── get.md                      # GET /profiles/{idOrAlias}
│   │   ├── create.md                   # POST /profiles
│   │   ├── update.md                   # PUT /profiles/{id}
│   │   └── delete.md                   # DELETE /profiles/{id}
│   ├── providers/
│   │   ├── README.md                   # Provider endpoints
│   │   ├── list.md                     # GET /providers
│   │   └── get.md                      # GET /providers/{id}
│   └── embeddings/
│       ├── README.md                   # Embedding endpoints
│       └── generate.md                 # POST /embeddings/generate
│
├── frontend/
│   ├── README.md                       # Frontend integration overview
│   ├── chat-controller.md              # UaiChatController usage
│   ├── chat-repository.md              # UaiChatRepository usage
│   └── types.md                        # TypeScript type exports
│
└── reference/
    ├── README.md                       # API Reference overview
    ├── services/
    │   ├── README.md                   # Services reference
    │   ├── ai-chat-service.md          # IAiChatService
    │   ├── ai-profile-service.md       # IAiProfileService
    │   ├── ai-connection-service.md    # IAiConnectionService
    │   └── ai-embedding-service.md     # IAiEmbeddingService
    ├── models/
    │   ├── README.md                   # Models reference
    │   ├── ai-profile.md               # AiProfile class
    │   ├── ai-connection.md            # AiConnection class
    │   ├── ai-capability.md            # AiCapability enum
    │   └── ai-model-ref.md             # AiModelRef struct
    └── configuration/
        ├── README.md                   # Configuration reference
        └── ai-options.md               # AiOptions class
```

---

## Section Details

### 1. Getting Started

**Purpose**: Quick onboarding for new users

| Article | Content |
|---------|---------|
| `installation.md` | NuGet package installation, provider packages (OpenAI), prerequisites (.NET 10, Umbraco 17+) |
| `configuration.md` | appsettings.json structure, `Umbraco:Ai` section, default profile aliases |
| `first-connection.md` | Backoffice walkthrough: create connection, enter API key, configuration references, multiple connections |
| `first-profile.md` | Create a chat profile, select model, configure settings, use in code |

### 2. Concepts

**Purpose**: Explain the mental model before diving into code

| Article | Content |
|---------|---------|
| `providers.md` | Provider = installable plugin (NuGet), discovery via `[AiProvider]`, examples (OpenAI) |
| `connections.md` | Connection = credentials + provider, stored in DB, can have multiple per provider |
| `profiles.md` | Profile = connection + model + settings, use-case specific (e.g., "content-assistant") |
| `capabilities.md` | Chat, Embedding, future (Media, Moderation), M.E.AI types (`IChatClient`, `IEmbeddingGenerator`) |
| `middleware.md` | Pipeline concept, wrapping clients, logging/caching/rate-limiting examples |

### 3. Using the API

**Purpose**: Developer guide for consuming AI services

**Chat Section**:
- Basic `IAiChatService.GetResponseAsync()` usage
- Streaming with `GetStreamingResponseAsync()`
- System prompts via profile settings
- Options: temperature, max tokens, stop sequences

**Embeddings Section**:
- Basic `IAiEmbeddingService.GenerateEmbeddingAsync()` usage
- Batch operations with `GenerateEmbeddingsAsync()`

**Tools Section**:
- What AI tools are
- Using tools with chat completions
- Tool result handling

### 4. Backoffice

**Purpose**: End-user guide for UI management

| Article | Content |
|---------|---------|
| `managing-connections.md` | Screenshots + steps: navigate to AI section, create/edit/delete connections, test button |
| `managing-profiles.md` | Screenshots + steps: create profile, select capability, choose connection/model, configure settings |

### 5. Extending

**Purpose**: Developer guide for adding custom functionality

**Providers Section**:
| Article | Content |
|---------|---------|
| `creating-a-provider.md` | Full walkthrough: create project, `[AiProvider]` attribute, `AiProviderBase<TSettings>`, register capabilities |
| `provider-settings.md` | `[AiSetting]` attribute, editor UI aliases, config resolution (`$ConfigKey`) |
| `chat-capability.md` | `AiChatCapabilityBase<TSettings>`, `CreateChatClient()` implementation |
| `embedding-capability.md` | `AiEmbeddingCapabilityBase<TSettings>`, `CreateEmbeddingGenerator()` implementation |

**Middleware Section**:
| Article | Content |
|---------|---------|
| `chat-middleware.md` | `IAiChatMiddleware` interface, `Apply()` method, delegating pattern |
| `embedding-middleware.md` | `IAiEmbeddingMiddleware` interface |
| `middleware-ordering.md` | `builder.AiChatMiddleware().Append<T>().InsertBefore<T, U>()` |

**Tools Section**:
| Article | Content |
|---------|---------|
| `creating-a-tool.md` | `[AiTool]` attribute, `AiToolBase<TArgs>`, `ExecuteAsync()`, registration |

### 6. Management API

**Purpose**: REST API reference for integrations

Each endpoint article includes:
- HTTP method and path
- Request/response models (JSON)
- Authentication requirements
- Example cURL/fetch calls
- Error responses

**Key patterns to document**:
- `IdOrAlias` - endpoints accept GUID or string alias
- Pagination - `skip`, `take`, `total` pattern
- Filtering - `filter`, `providerId`, `capability` query params

### 7. Frontend

**Purpose**: TypeScript/JavaScript integration guide

| Article | Content |
|---------|---------|
| `chat-controller.md` | `UaiChatController` class, methods, usage in custom elements |
| `chat-repository.md` | `UaiChatRepository` for direct API calls |
| `types.md` | Exported types: `UaiChatMessage`, `UaiChatResult`, `UaiChatStreamChunk`, etc. |

### 8. Reference

**Purpose**: API reference documentation (comprehensive but concise)

Each service reference includes:
- Interface signature
- Method descriptions
- Parameter/return type details
- Code example

---

## Content Priorities

### Phase 1 - Essential (MVP)
1. `README.md` (landing page)
2. `getting-started/` (all articles)
3. `concepts/` (all articles)
4. `using-the-api/chat/` (basic-chat.md, streaming.md)

### Phase 2 - Core Features
1. `backoffice/` (all articles with screenshots)
2. `using-the-api/embeddings/`
3. `management-api/chat/` and `management-api/connections/`

### Phase 3 - Extension Points
1. `extending/providers/` (all articles)
2. `extending/middleware/` (all articles)
3. `management-api/profiles/` and `management-api/providers/`

### Phase 4 - Complete Coverage
1. `extending/tools/`
2. `frontend/`
3. `reference/` (all articles)
4. `management-api/embeddings/`

---

## Example Article Templates

### Concept Article Template

```markdown
---
description: >-
  Brief description for search results and cards.
---

# Title

Introduction paragraph explaining the concept.

## What is [Concept]

Explanation with context.

## How [Concept] Works

Technical details.

{% hint style="info" %}
Helpful tip or note.
{% endhint %}

## Example

{% code title="Example.cs" %}
```csharp
// Code example
```
{% endcode %}

## Related

* [Related Topic](../path/to/related.md)
```

### API Endpoint Template

```markdown
---
description: >-
  Brief description of what this endpoint does.
---

# Endpoint Name

`METHOD /path/to/endpoint`

## Request

### Headers

| Header | Value |
|--------|-------|
| Authorization | Bearer {token} |
| Content-Type | application/json |

### Body

{% code title="Request Body" %}
```json
{
  "property": "value"
}
```
{% endcode %}

## Response

### Success (200)

{% code title="Response" %}
```json
{
  "property": "value"
}
```
{% endcode %}

### Errors

| Status | Description |
|--------|-------------|
| 400 | Bad request |
| 401 | Unauthorized |
| 404 | Not found |

## Example

{% code title="cURL" %}
```bash
curl -X POST https://example.com/umbraco/ai/management/api/v1/chat/complete \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"messages": [{"role": "user", "content": "Hello"}]}'
```
{% endcode %}
```

---

## Image Requirements

Screenshots needed:
- Backoffice AI section navigation
- Connection list view
- Connection create/edit form
- Connection test result
- Profile list view
- Profile create/edit form
- Model selection dropdown

All images:
- Max 800px width
- PNG format
- Stored in relevant `images/` subdirectory
- Descriptive filenames (e.g., `connection-create-form.png`)

---

## SUMMARY.md Structure

```markdown
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
```

---

## Next Steps

1. Review and approve this plan
2. Create folder structure
3. Write Phase 1 articles (Getting Started + Concepts)
4. Capture backoffice screenshots
5. Continue through remaining phases
