# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Agent.Copilot.Workspace add-on package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Package Overview

Copilot Workspace is a full backoffice **section** (not a sidebar panel) providing a broad-scope, system-wide AI chat with **persisted conversations** and **projects** (named containers grouping conversations plus reusable context attachments). It complements the contextual `Umbraco.AI.Agent.Copilot` sidebar under a shared Copilot brand — Workspace is unscoped (not tied to a section/entity), while the sidebar Copilot is contextual to whatever the editor is working on.

Its durable conversation/project persistence is deliberately split into reusable, host-agnostic `Umbraco.AI.Agent.Conversations.*` sub-packages (see Project Structure below) so another product could host the same CRUD layer under a different section/policy in future — the Conversations assemblies carry no knowledge of which product hosts them.

### Build Commands

```bash
# Build the .NET solution
dotnet build Umbraco.AI.Agent.Copilot.Workspace/Umbraco.AI.Agent.Copilot.Workspace.slnx

# Build frontend assets (from repository root)
npm run build:copilot-workspace

# Watch frontend during development
npm run watch:copilot-workspace

# Run frontend tests
npm run test:copilot-workspace
```

### Testing

```bash
# Run all .NET tests
dotnet test Umbraco.AI.Agent.Copilot.Workspace/Umbraco.AI.Agent.Copilot.Workspace.slnx

# Run specific test project
dotnet test Umbraco.AI.Agent.Copilot.Workspace/tests/Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit/Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.csproj
```

## Project Structure

Two product families live in this one package directory:

| Project                                               | Purpose                                                                 |
| ------------------------------------------------------ | ------------------------------------------------------------------------ |
| `Umbraco.AI.Agent.Conversations.Core`                 | Host-agnostic conversation/project domain models and services          |
| `Umbraco.AI.Agent.Conversations.Persistence`          | EF Core DbContext, entities, and repository implementations            |
| `Umbraco.AI.Agent.Conversations.Persistence.SqlServer`| SQL Server migrations                                                  |
| `Umbraco.AI.Agent.Conversations.Persistence.Sqlite`   | SQLite migrations                                                      |
| `Umbraco.AI.Agent.Conversations.Web`                  | Host-agnostic Conversations/Projects CRUD controllers (no `[MapToApi]`, no policy — see below) |
| `Umbraco.AI.Agent.Copilot.Workspace.Core`             | Workspace-specific domain: section alias, authorization policy, the Workspace `AIAgentSurface`, content migrations |
| `Umbraco.AI.Agent.Copilot.Workspace.Web`              | Workspace-specific Management API: AG-UI stream controller, file endpoints, and the application-model convention that binds the host-agnostic Conversations controllers into this product's OpenAPI document and section policy |
| `Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets` | TypeScript/Lit frontend (the backoffice section itself)                |
| `Umbraco.AI.Agent.Copilot.Workspace.Startup`          | Umbraco Composer for auto-discovery and DI registration                |
| `Umbraco.AI.Agent.Copilot.Workspace`                  | Meta-package that bundles all components                               |

**Why Conversations is split out from Copilot.Workspace:** `ConversationsManagementControllerBase` and its controllers carry no `[MapToApi]`, no named JSON options, and no section `[Authorize]` of their own — the hosting product (Copilot Workspace today) binds all of that at runtime via an `IApplicationModelConvention`. This is what lets the Conversations web layer be reused under a different host later without touching its code.

## Key Concepts

### Conversations and Projects

- **`AIConversation`** — a persisted conversation (also the AG-UI `threadId`). Carries an optional `ProjectId`, the owning user's key, an optional `AgentIdOrAlias` (null uses "Auto"), an optional profile override, and conversation-scoped `ContextIds`/resources that stack on top of the owning project's.
- **`AIProject`** — a named container grouping conversations plus reusable context attachments (`ContextIds` referencing existing `AIContext` entities, and its own directly-attached `Resources`). Optional custom `Instructions` are injected into every chat in the project. Projects are private per user (MVP).
- Persistence is **M.E.AI-agnostic**: messages are stored with serialized string content; `ConversationChatHistoryProvider` maps between stored messages and M.E.AI `ChatMessage`s at runtime.

### The Workspace Agent Surface

`CopilotWorkspaceAgentSurface` (`Umbraco.AI.Agent.Copilot.Workspace.Core.Surfaces`) registers the `copilot-workspace` surface id with `SupportedScopeDimensions = []` — deliberately empty, since the Workspace is unscoped. Agents opt in to appearing in Workspace purely via their `SurfaceIds`, not via any section/entity scope.

### Authorization

`CopilotWorkspaceAuthorizationPolicies.SectionAccessCopilotWorkspace` lives in `*.Core` (a plain string constant) specifically so both the Workspace stream/file controllers *and* the reused Conversations CRUD controllers can reference the same policy — section access gates the stored corpus, not just the UI.

## Management API

Root path: `/umbraco/ai/management/api` (shared OpenAPI document convention — one document per product, named via `CopilotWorkspaceConstants.ManagementApi.ApiName = "ai-copilot-workspace-management"`).

**Conversations** (`/v1/conversations`, route segments owned by `ConversationsManagementApiConstants`):

| Method | Endpoint                                     | Description                                  |
| ------ | --------------------------------------------- | --------------------------------------------- |
| GET    | `/conversations`                              | List conversations                           |
| GET    | `/conversations/{id}`                         | Get conversation by id                       |
| POST   | `/conversations`                              | Create conversation                          |
| PUT    | `/conversations/{id}`                         | Update conversation                          |
| DELETE | `/conversations/{id}`                         | Delete conversation                          |
| GET    | `/conversations/{id}/messages`                | Get conversation messages                    |
| DELETE | `/conversations/{id}/messages/after-last-user`| Truncate messages after the last user turn   |
| POST   | `/conversations/{id}/stream-agui`             | Stream an AG-UI run for this conversation (Workspace-specific, not part of the reusable Conversations assembly) |

**Projects** (`/v1/projects`):

| Method | Endpoint             | Description          |
| ------ | --------------------- | --------------------- |
| GET    | `/projects`           | List projects        |
| GET    | `/projects/{id}`      | Get project by id     |
| POST   | `/projects`           | Create project        |
| PUT    | `/projects/{id}`      | Update project        |
| DELETE | `/projects/{id}`      | Delete project        |

## Database Migrations

Two independent EF Core migration sets:

- **Conversations/Projects data** (`Umbraco.AI.Agent.Conversations.Persistence.{SqlServer,Sqlite}`) — prefix `UmbracoAIConversations_`.
- **Umbraco content migrations** (`Umbraco.AI.Agent.Copilot.Workspace.Core/Migrations/CopilotWorkspaceMigrationPlan.cs`) — e.g. `AddCopilotWorkspaceSectionToAdminGroup`, run via Umbraco's own migration runner (not EF Core) to grant section access on install.

```bash
# SQL Server
dotnet ef migrations add UmbracoAIConversations_<MigrationName> \
  -p Umbraco.AI.Agent.Copilot.Workspace/src/Umbraco.AI.Agent.Conversations.Persistence.SqlServer \
  -c UmbracoAIConversationsDbContext \
  --output-dir Migrations

# SQLite
dotnet ef migrations add UmbracoAIConversations_<MigrationName> \
  -p Umbraco.AI.Agent.Copilot.Workspace/src/Umbraco.AI.Agent.Conversations.Persistence.Sqlite \
  -c UmbracoAIConversationsDbContext \
  --output-dir Migrations
```

## Frontend Architecture

Located in `src/Umbraco.AI.Agent.Copilot.Workspace.Web.StaticAssets/Client/`, npm package `@umbraco-ai/agent-copilot-workspace`:

```
src/
├── api/              # Generated OpenAPI client + hand-written core helpers
├── chat/             # Chat run/streaming integration
├── conversation/      # Conversation entity, entity-actions, modal, repository, workspace view
├── project/           # Project entity-actions, repository, workspace view
├── section/shell/     # The Copilot Workspace backoffice section shell
├── sidebar/menu/       # Section-local navigation (conversations/projects tree)
└── lang/               # Localization
```

Follows the same barrel-export registration pattern as every other Umbraco.AI frontend package — see [../.claude/memory/frontend-entry-points.md](../.claude/memory/frontend-entry-points.md).

## Key Namespaces

- `Umbraco.AI.Agent.Conversations.Core.Conversations` / `.Projects` — domain models and services (`AIConversation`, `AIProject`, `IAIConversationService`, `IAIProjectService`)
- `Umbraco.AI.Agent.Conversations.Web.Api.Management` — host-agnostic Conversations/Projects CRUD controllers
- `Umbraco.AI.Agent.Copilot.Workspace.Core.Authorization` — `CopilotWorkspaceAuthorizationPolicies`
- `Umbraco.AI.Agent.Copilot.Workspace.Core.Surfaces` — `CopilotWorkspaceAgentSurface`
- `Umbraco.AI.Agent.Copilot.Workspace.Web.Api.Management.Stream` — AG-UI stream controller + runtime context builders

## Target Framework

- .NET 11.0 (`net11.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Dependencies

- Umbraco CMS 19.x
- Umbraco.AI 19.x
- Umbraco.AI.Agent 19.x
- Entity Framework Core 11.x
