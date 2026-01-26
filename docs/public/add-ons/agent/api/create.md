---
description: >-
  Create a new agent.
---

# Create Agent

Creates a new AI agent.

## Request

```http
POST /umbraco/ai/management/api/v1/agent
```

### Request Body

{% code title="Request" %}
```json
{
  "alias": "content-assistant",
  "name": "Content Assistant",
  "description": "Helps users write and improve content",
  "profileId": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "contextIds": ["e401f2ff-7d65-5c12-a1f7-e812859g1962"],
  "instructions": "You are a helpful content assistant.\n\nYour role is to help users write and improve content.",
  "isActive": true
}
```
{% endcode %}

### Request Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `alias` | string | Yes | Unique alias (URL-safe) |
| `name` | string | Yes | Display name |
| `description` | string | No | Optional description |
| `profileId` | guid | No | Associated AI profile (null uses default) |
| `contextIds` | guid[] | No | AI Contexts to inject |
| `instructions` | string | No | Agent system prompt |
| `isActive` | bool | No | Whether agent is available (default: true) |

## Response

### Success

{% code title="201 Created" %}
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "alias": "content-assistant",
  "name": "Content Assistant",
  "version": 1,
  ...
}
```
{% endcode %}

### Validation Error

{% code title="400 Bad Request" %}
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "alias": ["An agent with this alias already exists"]
  }
}
```
{% endcode %}

## Examples

{% code title="cURL" %}
```bash
curl -X POST "https://your-site.com/umbraco/ai/management/api/v1/agent" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "alias": "content-assistant",
    "name": "Content Assistant",
    "instructions": "You are a helpful content assistant.",
    "isActive": true
  }'
```
{% endcode %}
