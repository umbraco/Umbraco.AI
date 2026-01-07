---
description: >-
  REST API for managing AI connections, profiles, and performing AI operations.
---

# Management API

The Umbraco.Ai Management API provides REST endpoints for managing connections and profiles, as well as performing chat completions and embedding generation.

## Base URL

All endpoints are prefixed with:

```
/umbraco/ai/management/api/v1
```

## API Versioning

The API uses URL-based versioning. The current version is `v1`.

## Available Endpoints

### Chat Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/chat/complete` | Get a chat completion (default profile) |
| POST | `/chat/{profileIdOrAlias}/complete` | Get a chat completion (specific profile) |
| POST | `/chat/stream` | Stream a chat completion (default profile) |
| POST | `/chat/{profileIdOrAlias}/stream` | Stream a chat completion (specific profile) |

### Connection Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/connections` | List all connections |
| GET | `/connections/{idOrAlias}` | Get a specific connection |
| POST | `/connections` | Create a connection |
| PUT | `/connections/{id}` | Update a connection |
| DELETE | `/connections/{id}` | Delete a connection |
| POST | `/connections/{idOrAlias}/test` | Test a connection |
| GET | `/connections/capabilities` | List available capabilities |
| GET | `/connections/capabilities/{capability}` | Get connections by capability |
| GET | `/connections/{idOrAlias}/models` | Get available models |

### Profile Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/profiles` | List all profiles |
| GET | `/profiles/{idOrAlias}` | Get a specific profile |
| POST | `/profiles` | Create a profile |
| PUT | `/profiles/{id}` | Update a profile |
| DELETE | `/profiles/{id}` | Delete a profile |

### Provider Information

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/providers` | List all providers |
| GET | `/providers/{id}` | Get a specific provider |

### Embedding Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/embeddings/generate` | Generate embeddings (default profile) |
| POST | `/embeddings/{profileIdOrAlias}/generate` | Generate embeddings (specific profile) |

## IdOrAlias Pattern

Many endpoints accept an `IdOrAlias` parameter, which can be either:

* A GUID (for example, `3fa85f64-5717-4562-b3fc-2c963f66afa6`)
* A string alias (for example, `my-chat-profile`)

This provides flexibility in how you reference resources.

## Response Format

All responses use standard HTTP status codes and return JSON:

### Success Response

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "alias": "my-profile",
  "name": "My Profile"
}
```

### Error Response

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Connection not found"
}
```

## Pagination

List endpoints support pagination:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `skip` | int | 0 | Number of items to skip |
| `take` | int | 100 | Number of items to return |
| `filter` | string | null | Filter by name (contains) |

Response includes total count:

```json
{
  "items": [...],
  "total": 42
}
```

## In This Section

{% content-ref url="authentication.md" %}
[Authentication](authentication.md)
{% endcontent-ref %}

{% content-ref url="chat/README.md" %}
[Chat](chat/README.md)
{% endcontent-ref %}

{% content-ref url="connections/README.md" %}
[Connections](connections/README.md)
{% endcontent-ref %}

{% content-ref url="profiles/README.md" %}
[Profiles](profiles/README.md)
{% endcontent-ref %}

{% content-ref url="providers/README.md" %}
[Providers](providers/README.md)
{% endcontent-ref %}

{% content-ref url="embeddings/README.md" %}
[Embeddings](embeddings/README.md)
{% endcontent-ref %}
