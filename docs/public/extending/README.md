---
description: >-
  Extend Umbraco.AI with custom providers, middleware, and tools.
---

# Extending Umbraco.AI

Umbraco.AI is designed to be extensible. You can add support for new AI providers, customize the request pipeline with middleware, and create custom tools for AI agents.

## Extension Points

<table data-view="cards">
<thead>
<tr>
<th></th>
<th></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Custom Providers</strong></td>
<td>Add support for AI services not included out of the box</td>
</tr>
<tr>
<td><strong>Middleware</strong></td>
<td>Add logging, caching, rate limiting, and custom behavior</td>
</tr>
<tr>
<td><strong>Custom Tools</strong></td>
<td>Create tools that AI agents can use to perform actions</td>
</tr>
</tbody>
</table>

## When to Extend

### Create a Custom Provider When

* You need to connect to an AI service without an existing provider
* You want to use a self-hosted AI model
* You need custom authentication or API handling

### Create Middleware When

* You want to log all AI requests and responses
* You need to cache responses for identical requests
* You want to add rate limiting or retry logic
* You need to modify requests or responses globally

### Create Custom Tools When

* You want AI agents to interact with your systems
* You need to expose business logic to AI
* You want to enable AI to query databases or APIs

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        Your Code                            │
│         IAIChatService / IAIEmbeddingService                │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Middleware Pipeline                     │
│    [Your Middleware] → [Logging] → [Caching] → ...          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        Provider                             │
│         OpenAI / Azure / [Your Provider]                    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                        AI Service API
```

## In This Section

{% content-ref url="providers/README.md" %}
[Custom Providers](providers/README.md)
{% endcontent-ref %}

{% content-ref url="middleware/README.md" %}
[Middleware](middleware/README.md)
{% endcontent-ref %}

{% content-ref url="tools/README.md" %}
[Custom Tools](tools/README.md)
{% endcontent-ref %}
