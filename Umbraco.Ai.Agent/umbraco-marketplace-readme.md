## Umbraco.Ai.Agent

AI agent runtime for Umbraco.Ai - build conversational agents with real-time streaming and tool execution via the AG-UI protocol.

### Features

- **AG-UI Protocol** - Industry-standard protocol for agent-to-UI communication with Server-Sent Events streaming
- **Conversational Agents** - Define agents with system instructions, AI profiles, and context sources
- **Real-Time Streaming** - Stream AI responses token-by-token with lifecycle events (start, content, end)
- **Frontend Tools** - Define tools in your UI that agents can invoke with structured arguments
- **Human-in-the-Loop** - Built-in support for approval workflows and tool execution confirmation
- **Context Injection** - Attach AI contexts for RAG scenarios and brand voice consistency
- **Copilot Sidebar** - Ready-to-use chat interface with entity awareness and tool execution UI
- **Standalone SDK** - Umbraco.Ai.Agui package can be used independently for custom AG-UI endpoints

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.Ai 17.0.0+
- .NET 10.0
