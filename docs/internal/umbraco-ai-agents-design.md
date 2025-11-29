# Umbraco.Ai.Agents - Design Document

## Executive Summary

Umbraco.Ai.Agents extends Umbraco.Ai with autonomous AI capabilities that can perform actions within Umbraco CMS. While Umbraco.Ai provides foundational AI services (chat, embeddings), Agents add the ability for AI to use **tools** - executing searches, creating content, publishing pages, and more - with human oversight.

### Key Principles

1. **Human-in-the-loop**: All destructive actions require explicit user approval
2. **Native Integration**: Agents feel like a natural part of Umbraco, not bolted-on
3. **Extensible by Design**: Developers can add custom tools for their specific needs
4. **Built on Umbraco.Ai**: Leverages existing Providers, Connections, and Profiles

---

## How Agents Fit Into Umbraco.Ai

```
┌─────────────────────────────────────────────────────────────────┐
│                        Umbraco.Ai                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Providers (OpenAI, Azure, Anthropic, etc.)                    │
│        │                                                        │
│        ▼                                                        │
│   Connections (API keys, endpoints)                             │
│        │                                                        │
│        ▼                                                        │
│   Profiles (model settings, system prompts)                     │
│        │                                                        │
│        ├────────────────┬───────────────────┐                   │
│        ▼                ▼                   ▼                   │
│   Chat Service    Embedding Service    Agent Service ◄── NEW    │
│        │                │                   │                   │
│        │                │                   ▼                   │
│        │                │              Tools ◄── NEW            │
│        │                │               - Content operations    │
│        │                │               - Media operations      │
│        │                │               - Search                │
│        │                │               - Translation           │
│        │                │               - Custom tools          │
│        ▼                ▼                   ▼                   │
│   ┌─────────────────────────────────────────────┐               │
│   │            Your Application                 │               │
│   └─────────────────────────────────────────────┘               │
└─────────────────────────────────────────────────────────────────┘
```

Agents reference existing **Profiles** for their AI model configuration, inheriting connection credentials, model selection, and base settings. This keeps the architecture consistent and avoids duplication.

---

## What is an Agent?

An **Agent** is a configured AI assistant that can:
- Understand natural language requests
- Access tools to gather information or perform actions
- Execute multi-step workflows autonomously
- Always request user approval before making changes

### Agent vs Profile

| Aspect | Profile | Agent |
|--------|---------|-------|
| **Purpose** | Configure AI model settings | Define an AI assistant with capabilities |
| **Contains** | Model, temperature, tokens, system prompt | Profile reference, enabled tools, permissions |
| **Used for** | Direct chat, embeddings | Autonomous task execution |
| **Example** | "Creative Writer" profile with GPT-4 | "Content Assistant" agent that can search and edit content |

### Agent Definition

An agent consists of:
- **Name & Description**: Human-readable identification
- **Profile Reference**: Which AI profile to use (inherits model, connection, settings)
- **System Prompt**: Agent-specific instructions (extends profile's prompt)
- **Enabled Tools**: Which tools this agent can access
- **Permissions**: Which user groups can use this agent

---

## Tools

Tools are the actions agents can perform. Each tool is a discrete capability that the agent can invoke when needed.

### Tool Categories

| Category | Description | Examples |
|----------|-------------|----------|
| **Content** | Work with Umbraco content | Search, create, update, publish, delete |
| **Media** | Work with media library | Search, upload, organize |
| **Navigation** | Understand site structure | Get tree, find children, get ancestors |
| **Search** | Find information | Full-text search, semantic search |
| **Translation** | Language operations | Translate text, translate content properties |
| **Users** | Member/user information | Search members, get user details |
| **System** | Schema information | Get content types, property structures |

### Built-in Umbraco Tools

**Content Tools**
- `content.search` - Search content by text, filter by type
- `content.get` - Get content by ID or path
- `content.create` - Create new content (requires approval)
- `content.update` - Update content properties (requires approval)
- `content.publish` - Publish content (requires approval)
- `content.unpublish` - Unpublish content (requires approval)
- `content.delete` - Move to recycle bin (requires approval)

**Media Tools**
- `media.search` - Search media by name or type
- `media.get` - Get media details
- `media.upload` - Upload new media (requires approval)
- `media.organize` - Move/rename media (requires approval)

**Navigation Tools**
- `navigation.tree` - Get site structure
- `navigation.children` - Get children of a node
- `navigation.ancestors` - Get breadcrumb path
- `navigation.siblings` - Get items at same level

**Search Tools**
- `search.fulltext` - Lucene/Examine full-text search
- `search.semantic` - AI-powered semantic search (using embeddings)
- `search.similar` - Find content similar to a given item

**Generation Tools**
- `generate.text` - Generate text content
- `generate.summary` - Summarize existing content
- `generate.translate` - Translate text to another language

### Custom Tools

Developers can create custom tools for domain-specific needs:
- E-commerce: Check inventory, create orders
- Marketing: Schedule campaigns, analyze engagement
- Integration: Query external APIs, sync data

Tools are registered via attributes and discovered automatically, following the same pattern as Umbraco.Ai providers.

---

## Approval Workflow

All tools that modify content require explicit user approval. This ensures humans remain in control.

### How Approval Works

```
┌─────────────────────────────────────────────────────────────────┐
│ User: "Update the homepage hero text to mention our summer sale" │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Agent: I'll update the hero text on the Home page.               │
│                                                                  │
│ ┌──────────────────────────────────────────────────────────────┐ │
│ │ PROPOSED ACTION: Update Content                               │ │
│ │                                                               │ │
│ │ Page: Home                                                    │ │
│ │ Property: heroText                                            │ │
│ │                                                               │ │
│ │ Current:                                                      │ │
│ │   "Welcome to our website"                                    │ │
│ │                                                               │ │
│ │ New:                                                          │ │
│ │   "Summer Sale! Up to 50% off - Welcome to our website"       │ │
│ │                                                               │ │
│ │ [Approve]  [Reject]  [Modify]                                 │ │
│ └──────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Approval States

1. **Read-only tools** (search, get, navigate): Execute immediately, no approval needed
2. **Modifying tools** (create, update, publish, delete): Show preview, wait for approval
3. **Approved**: Action executes, agent continues
4. **Rejected**: Agent acknowledges and offers alternatives

---

## Backoffice Integration

Agents integrate into the Umbraco backoffice through multiple touchpoints, making AI assistance available wherever users need it.

### Entry Points

#### 1. Global AI Assistant (Header App)

A persistent AI button in the top navigation bar provides access to agents from anywhere in the backoffice.

```
┌─────────────────────────────────────────────────────────────────┐
│ [Logo]  Content  Media  Settings  Members           [AI] [User] │
└─────────────────────────────────────────────────────────────────┘
                                                        ▲
                                                        │
                                          Click to open AI sidebar
```

**Clicking the AI button opens a sidebar:**

```
┌────────────────────────────────────────┐
│ AI Assistant              [Agent ▼] X │
├────────────────────────────────────────┤
│ Working with: Home Page                │ ◄── Context awareness
├────────────────────────────────────────┤
│                                        │
│ [User bubble]                          │
│ Help me write a compelling intro       │
│ paragraph for this page                │
│                                        │
│ [Agent bubble]                         │
│ Here's a draft introduction for your   │
│ Home Page:                             │
│                                        │
│ "Welcome to [Company], where we..."    │
│                                        │
│ Would you like me to update the        │
│ page with this text?                   │
│                                        │
├────────────────────────────────────────┤
│ [Type your message...          ] [Send]│
└────────────────────────────────────────┘
```

**Key Features:**
- **Agent Selector**: Switch between configured agents (Content Assistant, Translator, etc.)
- **Context Banner**: Shows what content/workspace is currently active
- **Chat Interface**: Natural conversation with the agent
- **Action Previews**: Inline approval widgets for proposed changes

#### 2. Content Entity Actions

Right-click menu actions on content items in the tree provide quick AI operations.

```
Content
├── Home
│   ├── About Us        [Right-click]
│   │                   ┌─────────────────────────┐
│   │                   │ Edit                    │
│   │                   │ Create                  │
│   │                   │ Sort                    │
│   │                   ├─────────────────────────┤
│   │                   │ ✨ Generate Summary     │ ◄── AI Actions
│   │                   │ ✨ Translate            │
│   │                   │ ✨ AI Suggestions       │
│   │                   └─────────────────────────┘
│   ├── Products
│   └── Contact
```

**Available Actions:**
- **Generate Summary**: Create a summary of the content
- **Translate**: Translate content to another language
- **AI Suggestions**: Get improvement suggestions

Each action opens a focused modal for that specific task.

#### 3. Inline Property Actions

AI assistance buttons appear directly in property editors for contextual help.

```
┌────────────────────────────────────────────────────────────────┐
│ Main Content                                          [✨ AI] │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Lorem ipsum dolor sit amet, consectetur adipiscing elit.       │
│ Sed do eiusmod tempor incididunt ut labore et dolore magna     │
│ aliqua.                                                        │
│                                                                │
└────────────────────────────────────────────────────────────────┘
                                                          │
                                                          ▼
                                              ┌────────────────────┐
                                              │ ✨ Write with AI   │
                                              │ ✨ Improve         │
                                              │ ✨ Translate       │
                                              │ ✨ Summarize       │
                                              └────────────────────┘
```

**Inline Actions:**
- **Write with AI**: Generate content from a prompt
- **Improve**: Enhance existing text (clarity, engagement, SEO)
- **Translate**: Translate to another language
- **Summarize**: Create a shorter version

#### 4. Agent Management Section (Future)

A dedicated backoffice section for administrators to configure and manage agents.

```
┌─────────────────────────────────────────────────────────────────┐
│ AI Agents                                        [+ New Agent]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ ✨ Content Assistant                              [Active]   │ │
│ │ Profile: Content Writer | 8 tools enabled                   │ │
│ │ Groups: Editors, Writers                   [Edit] [Disable] │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 🌍 Translation Assistant                         [Active]   │ │
│ │ Profile: Translator | 3 tools enabled                       │ │
│ │ Groups: All users                          [Edit] [Disable] │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 📊 Analytics Assistant                          [Disabled]  │ │
│ │ Profile: Analyst | 5 tools enabled                          │ │
│ │ Groups: Administrators                        [Edit] [Enable]│ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Session Memory

Agent conversations persist during the user's session but are cleared on logout. This provides continuity while working but ensures a fresh start each login.

### Memory Scope

- **Within session**: Agent remembers context from earlier in the conversation
- **Cross-workspace**: Memory persists when switching between content items
- **Session boundary**: Memory cleared on logout or session timeout

### Context Awareness

The agent automatically understands what the user is working on:
- Current content item being edited
- Content type and available properties
- User's permissions
- Current culture/language

This context is injected into conversations so users don't need to repeatedly explain what they're working on.

---

## Frontend Integration (Website)

Beyond the backoffice, agents can power frontend experiences:

### AI-Powered Search

```csharp
// Example: Semantic search endpoint
app.MapGet("/api/search", async (string query, IAiAgentService agentService) =>
{
    var results = await agentService.ExecuteToolAsync("search.semantic", new { query });
    return results;
});
```

### Chatbots and Virtual Assistants

Agents can be exposed via API to power:
- Website chatbots for visitor support
- Customer service automation
- Interactive content recommendations
- FAQ answering systems

### Content Personalization

Use agent capabilities to:
- Generate personalized content variants
- Create dynamic summaries
- Translate content on-demand
- Answer user questions about site content

---

## Security and Permissions

### User Group Permissions

Agents respect Umbraco's permission system:
- Agents can be restricted to specific user groups
- Tool actions respect content permissions (can't edit content user can't access)
- Audit trail of all agent actions

### Tool Authorization

Each tool can specify permission requirements:
- Read-only tools: Generally available to all users
- Content modification: Requires appropriate content permissions
- Publishing: Requires publish permissions
- System tools: May require administrator access

### Rate Limiting

Protect against excessive usage:
- Configurable requests per minute per user
- Maximum tool calls per agent execution
- Concurrent execution limits

---

## Architecture Overview

### Backend Components

```
Umbraco.Ai.Agents (new project)
├── Models/
│   ├── AiAgent                    # Agent definition
│   ├── AgentSession               # Conversation state
│   └── ToolInvocation             # Tool call record
├── Tools/
│   ├── IAiTool                    # Tool interface
│   ├── AiToolAttribute            # Discovery attribute
│   └── Built-in tools...
├── Services/
│   ├── IAiAgentService            # Agent CRUD
│   ├── IAiAgentExecutor           # Execution engine
│   └── IAgentSessionStore         # Session memory
└── Configuration/
    └── UmbracoBuilderExtensions   # DI registration
```

### Frontend Components

```
Umbraco.Ai.Web.StaticAssets/Client/src/
├── sidebar/
│   ├── ai-sidebar.element.ts       # Main chat sidebar
│   ├── ai-message.element.ts       # Chat message bubble
│   └── ai-tool-preview.element.ts  # Action approval widget
├── header-apps/
│   └── ai-header-app.element.ts    # Header AI button
├── entity-actions/
│   └── ai-actions.ts               # Context menu actions
├── property-actions/
│   └── ai-property-actions.ts      # Inline property buttons
└── modals/
    └── ai-generation-modal.ts      # Focused task modals
```

### API Endpoints

```
POST   /umbraco/ai/api/v1/agents/{id}/chat          # Send message
POST   /umbraco/ai/api/v1/agents/{id}/chat/stream   # Streaming chat
POST   /umbraco/ai/api/v1/agents/{id}/approve/{callId}  # Approve action
GET    /umbraco/ai/api/v1/agents                    # List agents
GET    /umbraco/ai/api/v1/tools                     # List available tools
```

---

## User Interaction Flows

### Flow 1: Content Generation via Sidebar

```
1. User clicks AI button in header
2. Sidebar opens, showing current context ("Working with: Home Page")
3. User types: "Write an engaging introduction about our company"
4. Agent generates text and displays it
5. User types: "Add this to the main content property"
6. Agent shows approval widget with preview
7. User clicks "Approve"
8. Content is updated
9. Agent confirms: "Done! The introduction has been added."
```

### Flow 2: Quick Translation via Entity Action

```
1. User right-clicks "About Us" page in content tree
2. Selects "Translate" from context menu
3. Modal opens with language selector
4. User selects "French" and clicks "Translate"
5. Agent translates all text properties
6. Preview shows before/after for each property
7. User reviews and clicks "Apply All"
8. French variant is created/updated
```

### Flow 3: Inline Text Improvement

```
1. User is editing a product description
2. Clicks AI button on the textarea property
3. Selects "Improve" from the popover
4. Modal shows current text and options (tone, length, etc.)
5. Agent suggests improved version
6. User clicks "Apply" or "Regenerate"
7. Property value is updated with improved text
```

---

## Future Considerations

### Persistent Memory (Future Enhancement)
- Store conversation history in database
- Agents remember past interactions across sessions
- Build knowledge over time

### Scheduled Agent Tasks
- Agents that run automatically on schedules
- Content audits, optimization suggestions
- Automated translation of new content

### Multi-Agent Orchestration
- Multiple agents working together
- Specialized agents for different tasks
- Agent handoffs (e.g., content agent to translation agent)

### Learning and Adaptation
- Agents learn from user feedback
- Custom training on organization's content
- Improved suggestions over time

---

## Summary

Umbraco.Ai.Agents brings autonomous AI capabilities to Umbraco CMS while maintaining human control. By integrating at multiple touchpoints (sidebar, entity actions, inline buttons) and requiring approval for all modifications, agents become powerful assistants that enhance productivity without compromising content governance.

The extension builds naturally on Umbraco.Ai's foundation, reusing Providers, Connections, and Profiles while adding the new concepts of Agents and Tools. This consistent architecture makes it familiar to developers already using Umbraco.Ai.

Key differentiators:
- **Human-in-the-loop**: Every change requires approval
- **Context-aware**: Agents understand what you're working on
- **Extensible**: Add custom tools for your specific needs
- **Native feel**: Integrates seamlessly into the Umbraco experience
