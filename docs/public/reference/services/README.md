---
description: >-
  Core service interfaces for AI operations.
---

# Services

Umbraco.Ai provides core services for AI operations. All services are registered with dependency injection and can be injected into controllers, composers, or other services.

## Available Services

| Service | Purpose |
|---------|---------|
| [IAiChatService](ai-chat-service.md) | Chat completions (streaming and non-streaming) |
| [IAiEmbeddingService](ai-embedding-service.md) | Text embedding generation |
| [IAiProfileService](ai-profile-service.md) | Profile CRUD operations |
| [IAiConnectionService](ai-connection-service.md) | Connection management |

## Usage Pattern

All services follow standard dependency injection patterns:

{% code title="Using Services" %}
```csharp
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;

public class MyController : Controller
{
    private readonly IAiChatService _chatService;
    private readonly IAiProfileService _profileService;

    public MyController(IAiChatService chatService, IAiProfileService profileService)
    {
        _chatService = chatService;
        _profileService = profileService;
    }

    public async Task<IActionResult> Chat()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var response = await _chatService.GetChatResponseAsync(messages);
        return Ok(response.Message.Text);
    }
}
```
{% endcode %}

## In This Section

{% content-ref url="ai-chat-service.md" %}
[IAiChatService](ai-chat-service.md)
{% endcontent-ref %}

{% content-ref url="ai-embedding-service.md" %}
[IAiEmbeddingService](ai-embedding-service.md)
{% endcontent-ref %}

{% content-ref url="ai-profile-service.md" %}
[IAiProfileService](ai-profile-service.md)
{% endcontent-ref %}

{% content-ref url="ai-connection-service.md" %}
[IAiConnectionService](ai-connection-service.md)
{% endcontent-ref %}
