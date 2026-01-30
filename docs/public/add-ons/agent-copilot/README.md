---
description: >-
  Copilot chat UI add-on for AI agents with sidebar, tool execution, and HITL support.
---

# Agent Copilot

The Agent Copilot add-on (`Umbraco.Ai.Agent.Copilot`) provides an interactive AI assistant sidebar in the Umbraco backoffice. It requires the Agent Runtime (`Umbraco.Ai.Agent`) for backend functionality.

## Installation

Install both the Agent Runtime and Copilot packages:

{% code title="Package Manager Console" %}
```powershell
Install-Package Umbraco.Ai.Agent
Install-Package Umbraco.Ai.Agent.Copilot
```
{% endcode %}

Or via .NET CLI:

{% code title="Terminal" %}
```bash
dotnet add package Umbraco.Ai.Agent
dotnet add package Umbraco.Ai.Agent.Copilot
```
{% endcode %}

{% hint style="info" %}
The `Umbraco.Ai.Agent.Copilot` package depends on `Umbraco.Ai.Agent` for agent definitions and streaming APIs.
{% endhint %}

## Features

- **Sidebar Chat UI** - Conversational interface in the backoffice
- **Content Awareness** - Understands current editing context
- **Tool Execution** - Frontend tools execute in the browser
- **HITL Approval** - Human-in-the-loop confirmation for actions
- **AG-UI Integration** - Real-time streaming responses
- **Entity Selector** - Target specific content items

## Quick Start

### 1. Install Both Packages

```bash
dotnet add package Umbraco.Ai.Agent
dotnet add package Umbraco.Ai.Agent.Copilot
```

### 2. Create an Agent

In the backoffice, navigate to **Settings** > **AI** > **Agents** and create an agent configured for Copilot use.

### 3. Configure Default Copilot Agent

{% code title="Program.cs" %}
```csharp
services.Configure<AiAgentOptions>(options =>
{
    options.DefaultCopilotAgentAlias = "content-assistant";
});
```
{% endcode %}

### 4. Access the Copilot

The Copilot sidebar appears in the Content and Media sections. Click the **AI Assistant** button in the header to open it.

## Package Architecture

```
┌───────────────────────────────────────────────────┐
│                 Umbraco.Ai.Agent                   │
│  (Backend APIs, Agent Definitions, Streaming)     │
└───────────────────────────────────────────────────┘
                        │
                        │ uses
                        ▼
┌───────────────────────────────────────────────────┐
│            Umbraco.Ai.Agent.Copilot               │
│  (Chat UI, Tool System, HITL Approval)            │
└───────────────────────────────────────────────────┘
```

The Agent package provides:
- Agent CRUD operations
- AG-UI streaming endpoints
- Backend tool execution
- Management API

The Copilot package provides:
- Sidebar chat interface
- Frontend tool framework
- HITL approval elements
- Content context injection

## Documentation

| Section | Description |
|---------|-------------|
| [Copilot Usage](copilot.md) | Using the chat interface |
| [Frontend Tools](frontend-tools.md) | Creating browser-executable tools |

## Related

* [Agent Runtime](../agent/README.md) - Backend agent functionality
* [Add-ons Overview](../README.md) - All add-on packages
* [AI Contexts](../../concepts/contexts.md) - Brand voice and guidelines
