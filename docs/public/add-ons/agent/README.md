---
description: >-
  Agent Runtime add-on for configuring and running AI agents with streaming responses.
---

# Agent Runtime

The Agent Runtime add-on (`Umbraco.Ai.Agent`) enables you to configure and run AI agents that can interact with users through streaming responses and execute frontend tools.

## Installation

{% code title="Package Manager Console" %}
```powershell
Install-Package Umbraco.Ai.Agent
```
{% endcode %}

Or via .NET CLI:

{% code title="Terminal" %}
```bash
dotnet add package Umbraco.Ai.Agent
```
{% endcode %}

## Features

- **Agent Definitions** - Configure reusable AI agents with instructions
- **AG-UI Protocol** - Stream responses using the AG-UI event protocol
- **Profile Association** - Link agents to specific AI profiles
- **Context Injection** - Include AI Contexts for brand voice
- **Version History** - Track changes with full rollback support
- **Backoffice Management** - Full UI for managing agents
- **Management API** - RESTful API for agent operations

{% hint style="info" %}
For the **Copilot chat sidebar** with frontend tools and HITL approval, install the [Agent Copilot](../agent-copilot/README.md) add-on alongside this package.
{% endhint %}

## Quick Start

### 1. Create an Agent

In the backoffice, navigate to **Settings** > **AI** > **Agents** and create a new agent:

| Field | Value |
|-------|-------|
| Alias | `content-assistant` |
| Name | Content Assistant |
| Instructions | `You are a helpful content assistant. Help users write and improve content.` |
| Profile | (select your chat profile) |

### 2. Run the Agent

{% code title="Example.cs" %}
```csharp
public class AgentRunner
{
    private readonly IAiAgentService _agentService;

    public AgentRunner(IAiAgentService agentService)
    {
        _agentService = agentService;
    }

    public async Task RunAsync(HttpResponse response)
    {
        var agent = await _agentService.GetAgentByAliasAsync("content-assistant");

        response.ContentType = "text/event-stream";

        await foreach (var evt in _agentService.StreamAgentAsync(
            agent!.Id,
            new AiAgentRunRequest
            {
                Messages = new[]
                {
                    new AiAgentMessage { Role = "user", Content = "Help me write a blog post about AI" }
                }
            }))
        {
            // Write SSE events
            await response.WriteAsync($"event: {evt.Type}\n");
            await response.WriteAsync($"data: {JsonSerializer.Serialize(evt)}\n\n");
            await response.Body.FlushAsync();
        }
    }
}
```
{% endcode %}

### 3. Consume in Frontend

{% code title="Frontend.ts" %}
```typescript
const eventSource = new EventSource('/api/agent/content-assistant/run');

eventSource.addEventListener('text_message_content', (e) => {
  const data = JSON.parse(e.data);
  console.log('Content:', data.content);
});

eventSource.addEventListener('run_finished', () => {
  eventSource.close();
});
```
{% endcode %}

## AG-UI Protocol

The Agent Runtime uses the AG-UI (Agent UI) protocol for streaming responses. This protocol defines event types for:

- **Lifecycle events** - `run_started`, `run_finished`, `run_error`
- **Text streaming** - `text_message_start`, `text_message_content`, `text_message_end`
- **Tool calls** - `tool_call_start`, `tool_call_args`, `tool_call_end`
- **State updates** - `state_snapshot`, `state_delta`

## Documentation

| Section | Description |
|---------|-------------|
| [Concepts](concepts.md) | Agent architecture and AG-UI protocol |
| [Getting Started](getting-started.md) | Step-by-step setup guide |
| [Instructions](instructions.md) | Agent instruction configuration |
| [Streaming](streaming.md) | SSE streaming and event handling |
| [API Reference](api/README.md) | Management API endpoints |
| [Service Reference](reference/ai-agent-service.md) | IAiAgentService |

For Copilot-specific features:

| Section | Description |
|---------|-------------|
| [Copilot Overview](../agent-copilot/README.md) | Chat sidebar and tool execution |
| [Frontend Tools](../agent-copilot/frontend-tools.md) | Browser-executable tools |
| [Copilot Usage](../agent-copilot/copilot.md) | Using the chat interface |

## Related

* [Add-ons Overview](../README.md) - All add-on packages
* [AI Contexts](../../concepts/contexts.md) - Brand voice and guidelines
* [Profiles](../../concepts/profiles.md) - AI configuration
