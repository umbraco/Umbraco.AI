## Umbraco.AI.Agent

AI agent management and runtime for Umbraco.AI - store, manage, and execute conversational agents with real-time streaming via the AG-UI protocol.

### Features

- **Agent Management** - Store and manage AI agent definitions with instructions and configuration
- **AG-UI Protocol** - Industry-standard protocol for agent-to-UI communication with Server-Sent Events streaming
- **Real-Time Streaming** - Stream AI responses token-by-token with lifecycle events (start, content, end)
- **Profile Integration** - Link agents to Umbraco.AI profiles for model configuration
- **Context Injection** - Attach AI contexts for RAG scenarios and brand voice consistency
- **Management API** - RESTful API for agent CRUD operations and execution
- **Backoffice UI** - Agent management interface integrated into Umbraco
- **Standalone SDK** - Umbraco.AI.Agui package can be used independently for custom AG-UI endpoints

> For the Copilot chat sidebar with tool execution and HITL approval UI, install [Umbraco.AI.Agent.Copilot](https://www.nuget.org/packages/Umbraco.AI.Agent.Copilot) alongside this package.

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
