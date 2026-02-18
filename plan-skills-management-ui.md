# Skills Management UI - Design Plan

## Context

Both Anthropic (Claude) and OpenAI have converged on an open **Agent Skills** standard: versioned bundles of files anchored by a `SKILL.md` manifest with YAML frontmatter. Skills extend agent capabilities through organized instructions, scripts, and resources that execute in sandboxed container environments.

### How Skills Work at the API Level

| Aspect | Anthropic (Claude) | OpenAI |
|---|---|---|
| **Upload** | `POST /v1/skills` (multipart) | `POST /v1/skills` (multipart) |
| **Manifest** | `SKILL.md` with YAML frontmatter (`name`, `description`) | `SKILL.md` with YAML frontmatter (`name`, `description`) |
| **Attachment** | `container.skills[]` on Messages API | `tools[].environment.skills` on Responses API |
| **Types** | `anthropic` (pre-built: pptx, xlsx, docx, pdf) + `custom` | Hosted + local execution |
| **Versioning** | Epoch timestamps (custom) / dates (built-in), or `"latest"` | Versioned bundles |
| **Limit** | Max 8 skills per request, 8MB upload | Similar constraints |
| **Execution** | Code execution container (no network) | Hosted shell (Debian 12) or local |

Key properties of a skill reference at request time:
```json
{ "type": "anthropic|custom", "skill_id": "skill_01Abc...", "version": "latest" }
```

### What Already Exists in Umbraco.AI

The codebase already has mature infrastructure for **tools** (function-calling):
- `AIAgent.AllowedToolIds` / `AllowedToolScopeIds` — per-agent tool permissions
- `IAIToolScope` / `BuiltInScopes` — scope-based tool categorization
- Frontend permissions UI in agent workspace
- Tool validation at runtime via `IAIAgentService.IsToolAllowedAsync()`

**Skills are distinct from tools.** Tools are function definitions the LLM can call back into user code. Skills are bundles of instructions/scripts uploaded to the provider's execution environment. They complement each other.

---

## Design Goals

1. **Provider-agnostic skill management** — Users manage skills in Umbraco regardless of which provider they use
2. **Provider-specific skill sync** — Each provider plugin handles uploading/syncing skills to its API
3. **Agent-level skill assignment** — Skills are attached to agents (and/or profiles), similar to how tool permissions work today
4. **Consistent UI patterns** — Follow existing workspace/editor patterns from connections, profiles, prompts, and agents
5. **Extensible** — New providers can participate in skills without changes to core

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Umbraco Backoffice UI                     │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────┐ │
│  │ Skill Editor  │  │ Skill Picker │  │ Agent Workspace   │ │
│  │ (CRUD)       │  │ (in Agent)   │  │ + Skills Tab      │ │
│  └──────┬───────┘  └──────┬───────┘  └───────┬───────────┘ │
│         │                 │                   │             │
├─────────┼─────────────────┼───────────────────┼─────────────┤
│         ▼                 ▼                   ▼             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              Management API (Web)                    │    │
│  │  /umbraco/ai/management/api/v1/skill/               │    │
│  └──────────────────────┬──────────────────────────────┘    │
│                         ▼                                    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              AISkill Service (Core)                   │    │
│  │  - CRUD for skill definitions                        │    │
│  │  - Resolves skills for agent at runtime              │    │
│  └──────────┬───────────────────────┬──────────────────┘    │
│             ▼                       ▼                        │
│  ┌──────────────────┐   ┌──────────────────────────────┐    │
│  │ Skill Repository  │   │ IAISkillSyncProvider         │    │
│  │ (Persistence)     │   │ (Provider-level sync)        │    │
│  └──────────────────┘   └──────────────────────────────┘    │
│                                   ▲                          │
│              ┌────────────────────┼────────────────┐         │
│              ▼                    ▼                ▼          │
│  ┌───────────────┐   ┌───────────────┐  ┌──────────────┐   │
│  │ Anthropic      │   │ OpenAI        │  │ Other        │   │
│  │ Skill Sync     │   │ Skill Sync    │  │ Providers    │   │
│  └───────────────┘   └───────────────┘  └──────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## Domain Model

### New Entity: `AISkill` (Core)

```csharp
public class AISkill
{
    public Guid Id { get; set; }
    public required string Alias { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }

    // The skill content
    public required AISkillContent Content { get; set; }

    // Provider sync state (per-connection)
    // Tracked separately — see AISkillProviderState below

    // Audit
    public bool IsActive { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid ModifiedByUserId { get; set; }
    public int Version { get; set; }
}

public class AISkillContent
{
    // The SKILL.md manifest content (markdown with YAML frontmatter)
    public required string Manifest { get; set; }

    // Additional files bundled with the skill
    // Stored as a dictionary of relative-path → content
    public Dictionary<string, AISkillFile> Files { get; set; } = new();
}

public class AISkillFile
{
    public required string Path { get; set; }
    public required byte[] Content { get; set; }
    public required string MimeType { get; set; }
}
```

### Provider Sync State: `AISkillProviderState`

Tracks the mapping between an Umbraco skill and its provider-side counterpart per connection:

```csharp
public class AISkillProviderState
{
    public Guid SkillId { get; set; }
    public Guid ConnectionId { get; set; }

    // The provider's remote skill ID (e.g., "skill_01AbCd..." for Anthropic)
    public string? RemoteSkillId { get; set; }

    // The provider's remote version identifier
    public string? RemoteVersion { get; set; }

    // Sync status
    public AISkillSyncStatus Status { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? SyncError { get; set; }
}

public enum AISkillSyncStatus
{
    NotSynced,      // Never uploaded to this provider
    Synced,         // In sync with provider
    OutOfSync,      // Local changes not yet uploaded
    SyncFailed,     // Last sync attempt failed
    Unsupported     // Provider doesn't support skills
}
```

### Agent Integration

Extend `AIAgent` with a skill assignment list (mirrors the existing `AllowedToolIds` pattern):

```csharp
// Added to AIAgent
public IReadOnlyList<Guid> SkillIds { get; set; } = [];
```

### Alternative: Profile-Level Skills

Skills could also attach at the **profile** level rather than (or in addition to) agent level. This would allow non-agent chat scenarios to use skills too. However, since skills are most relevant to agentic use cases, agent-level assignment is the primary design. Profile-level could be a future enhancement.

---

## Provider Integration

### New Capability: `IAISkillSyncProvider`

Each provider that supports skills implements this interface:

```csharp
public interface IAISkillSyncProvider
{
    /// Whether this provider supports skills at all
    bool SupportsSkills { get; }

    /// Upload or update a skill to the provider
    Task<AISkillSyncResult> SyncSkillAsync(
        AISkill skill,
        AIConnection connection,
        AISkillProviderState? existingState,
        CancellationToken ct);

    /// Remove a skill from the provider
    Task DeleteSkillAsync(
        AISkillProviderState state,
        AIConnection connection,
        CancellationToken ct);

    /// List pre-built/vendor skills available from this provider
    Task<IEnumerable<AIProviderSkillInfo>> GetProviderSkillsAsync(
        AIConnection connection,
        CancellationToken ct);
}
```

### Provider-Specific Implementations

**Anthropic Provider (`Umbraco.AI.Anthropic`)**:
- `SyncSkillAsync` → calls `POST /v1/skills` (create) or `POST /v1/skills/{id}/versions` (update)
- Maps `AISkillContent.Manifest` + `Files` into multipart upload
- Also exposes built-in Anthropic skills (pptx, xlsx, docx, pdf) via `GetProviderSkillsAsync`
- At chat time, adds `container.skills[]` entries to the Messages API request

**OpenAI Provider (`Umbraco.AI.OpenAI`)**:
- `SyncSkillAsync` → calls `POST /v1/skills` (OpenAI's equivalent endpoint)
- At chat time, mounts skills via `tools[].environment.skills` on Responses API

**Providers without skills support** (Google, Amazon, etc.):
- Return `SupportsSkills = false`
- UI shows appropriate messaging

### Chat Client Integration

A new middleware or extension to the chat client factory resolves skills at runtime:

```
Agent.SkillIds
  → Resolve AISkill entities
    → Look up AISkillProviderState for the agent's connection
      → Provider maps to API-specific skill references
        → Injected into chat request (container.skills / environment.skills)
```

This would be implemented as a new `IAISkillChatMiddleware` that intercepts `CompleteAsync` calls and enriches the request options with skill references. The exact mechanism depends on whether M.E.AI's `ChatOptions` supports extensibility for provider-specific data (it does via `AdditionalProperties`).

---

## Management API

### Endpoints

```
GET    /umbraco/ai/management/api/v1/skill              # List skills (paged)
GET    /umbraco/ai/management/api/v1/skill/{id}          # Get skill by ID
POST   /umbraco/ai/management/api/v1/skill               # Create skill
PUT    /umbraco/ai/management/api/v1/skill/{id}           # Update skill
DELETE /umbraco/ai/management/api/v1/skill/{id}           # Delete skill

# Sync operations
POST   /umbraco/ai/management/api/v1/skill/{id}/sync/{connectionId}   # Sync to provider
GET    /umbraco/ai/management/api/v1/skill/{id}/sync                   # Get sync states

# Provider built-in skills
GET    /umbraco/ai/management/api/v1/skill/provider/{connectionId}     # List provider skills
```

### Request/Response Models

```
CreateSkillRequestModel:
  - alias: string
  - name: string
  - description: string?
  - icon: string?
  - manifest: string           # SKILL.md content
  - files: FileUpload[]?       # Additional skill files
  - isActive: boolean

SkillResponseModel:
  - id: guid
  - alias: string
  - name: string
  - description: string?
  - icon: string?
  - manifest: string
  - fileCount: int
  - isActive: boolean
  - syncStates: SkillSyncStateResponseModel[]
  - dateCreated, dateModified, version

SkillSyncStateResponseModel:
  - connectionId: guid
  - connectionName: string
  - status: string (NotSynced|Synced|OutOfSync|SyncFailed|Unsupported)
  - remoteSkillId: string?
  - lastSyncedAt: datetime?
  - syncError: string?
```

---

## Frontend UI

### Skill Section in AI Settings

Skills appear as a new section within the existing AI settings area, alongside Connections, Profiles, Prompts, and Agents:

```
AI Settings
├── Connections
├── Profiles
├── Prompts
├── Agents
└── Skills        ← NEW
```

### Skill List View

A table/list view following the same patterns as existing entity lists:

| Column | Description |
|---|---|
| Name | Skill name with icon |
| Alias | URL-safe alias |
| Description | Short description |
| Sync Status | Badges showing sync state per connection |
| Active | Toggle |

### Skill Editor (Workspace)

The skill editor workspace follows the existing pattern with these views:

**Info View (default):**
- Name / Alias inputs (same header pattern as agent editor)
- Description textarea
- Icon picker
- Active toggle

**Content View:**
- SKILL.md editor — a code/markdown editor for the manifest
  - Syntax highlighting for YAML frontmatter + markdown body
  - Could use a simple `<textarea>` initially, or integrate a code editor component
- File manager — list of additional skill files with upload/remove
  - Shows file path, size, mime type
  - Drag-and-drop upload support

**Sync View:**
- Table of connections with sync status per connection
- "Sync" button per connection (or "Sync All")
- Shows: connection name, provider, status badge, last synced, error message
- Status badges: `Not Synced` (gray), `Synced` (green), `Out of Sync` (amber), `Failed` (red), `Unsupported` (gray, disabled)

### Agent Workspace — Skills Tab

Add a new tab/view to the existing agent workspace editor (alongside Info, Details, Permissions, Availability):

**Skills View:**
- Skill picker component (similar to tool picker pattern in permissions view)
- Shows assigned skills with name, description, sync status indicator
- Add/remove skills from agent
- Informational note: "Skills are synced to providers via the Skills section. Only synced skills will be available at runtime."

### Provider Skills (Built-in)

For providers that offer built-in skills (e.g., Anthropic's pptx/xlsx/docx/pdf):
- Show in skill list with a "Provider" badge (distinct from custom skills)
- Not editable (read-only detail view showing name + description)
- Can be assigned to agents same as custom skills
- Don't require sync (they're already on the provider side)

---

## Data Flow: Runtime Skill Resolution

```
1. User sends message to Agent (via Copilot UI or API)
2. Agent resolved → AIAgent entity loaded
3. Agent.SkillIds resolved → List<AISkill> loaded
4. Agent.ProfileId → AIProfile → AIConnection → Provider determined
5. For each skill:
   a. Look up AISkillProviderState for (skill.Id, connection.Id)
   b. If synced → map to provider skill reference
   c. If provider skill (built-in) → map to provider skill reference
   d. If not synced → skip (or warn)
6. Skill references added to chat request via middleware:
   - Anthropic: container.skills[{ type, skill_id, version }]
   - OpenAI: tools[].environment.skills[{ id, version }]
7. Chat response may include skill execution results
```

---

## Database Schema

### New Tables

**`UmbracoAISkill`** (in a new `Umbraco.AI.Skill` product, or within Core):

| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| Alias | nvarchar(255) | Unique |
| Name | nvarchar(255) | |
| Description | nvarchar(2000) | Nullable |
| Icon | nvarchar(255) | Nullable |
| Manifest | nvarchar(max) | SKILL.md content |
| IsActive | bit | |
| DateCreated | datetime | |
| DateModified | datetime | |
| CreatedByUserId | uniqueidentifier | |
| ModifiedByUserId | uniqueidentifier | |
| Version | int | Optimistic concurrency |

**`UmbracoAISkillFile`**:

| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| SkillId | uniqueidentifier | FK → UmbracoAISkill |
| Path | nvarchar(500) | Relative path |
| Content | varbinary(max) | File bytes |
| MimeType | nvarchar(255) | |

**`UmbracoAISkillProviderState`**:

| Column | Type | Notes |
|---|---|---|
| SkillId | uniqueidentifier | PK, FK → UmbracoAISkill |
| ConnectionId | uniqueidentifier | PK, FK → UmbracoAIConnection |
| RemoteSkillId | nvarchar(500) | Nullable |
| RemoteVersion | nvarchar(255) | Nullable |
| Status | int | Enum |
| LastSyncedAt | datetime | Nullable |
| SyncError | nvarchar(2000) | Nullable |

**`UmbracoAIAgentSkill`** (junction table):

| Column | Type | Notes |
|---|---|---|
| AgentId | uniqueidentifier | PK, FK → UmbracoAIAgent |
| SkillId | uniqueidentifier | PK, FK → UmbracoAISkill |
| SortOrder | int | |

Migration prefix: `UmbracoAI_` (if in Core) or `UmbracoAISkill_` (if separate product).

---

## Product Placement Decision

### Option A: New Product (`Umbraco.AI.Skill`)

Following the pattern of `Umbraco.AI.Prompt` and `Umbraco.AI.Agent` — a standalone add-on:

```
Umbraco.AI.Skill/
├── src/
│   ├── Umbraco.AI.Skill.Core/
│   ├── Umbraco.AI.Skill.Web/
│   ├── Umbraco.AI.Skill.Web.StaticAssets/
│   ├── Umbraco.AI.Skill.Persistence/
│   ├── Umbraco.AI.Skill.Persistence.SqlServer/
│   ├── Umbraco.AI.Skill.Persistence.Sqlite/
│   ├── Umbraco.AI.Skill.Startup/
│   └── Umbraco.AI.Skill/
├── tests/
└── Umbraco.AI.Skill.sln
```

**Pros**: Clean separation, independent versioning, optional install
**Cons**: Skills are tightly coupled to agents — feels like it should be part of agent or core

### Option B: Extend Core (`Umbraco.AI`)

Add skill management to the core package since skills are a provider-level concept (not agent-specific).

**Pros**: Skills are fundamentally about provider capabilities, matches where connections/profiles live
**Cons**: Core grows larger, skills may not be needed without agents

### Option C: Extend Agent (`Umbraco.AI.Agent`)

Add skills within the agent package since that's the primary consumer.

**Pros**: Skills are most useful with agents, keeps it contained
**Cons**: Limits reuse if profiles need skills too

### Recommendation: Option A (New Product)

A new `Umbraco.AI.Skill` product is most consistent with the monorepo pattern. The core `IAISkillSyncProvider` interface lives in `Umbraco.AI.Core` (like `IAIProvider`), while the entity management, persistence, and UI live in the add-on. The Agent package would take an optional dependency on Skill for the agent-skill assignment feature.

---

## Chat Client Integration Detail

### M.E.AI Extension Point

Microsoft.Extensions.AI's `ChatOptions` has an `AdditionalProperties` dictionary that can carry provider-specific data. The flow:

1. **Core defines** a well-known key (e.g., `"umbraco-ai-skills"`) and a `ChatOptionsSkillsExtension` model
2. **Skill middleware** populates `ChatOptions.AdditionalProperties["umbraco-ai-skills"]` with resolved skill references
3. **Provider plugins** read this in their `IChatClient` implementation and map to provider-specific API parameters

```csharp
// In middleware (Core or Skill package)
public class AISkillChatMiddleware : IAIChatMiddleware
{
    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> messages,
        ChatOptions? options,
        IChatClient innerClient,
        CancellationToken ct)
    {
        // Resolve skills from runtime context (agent → skills → sync states)
        var skills = await ResolveSkillsAsync(ct);
        if (skills.Any())
        {
            options ??= new ChatOptions();
            options.AdditionalProperties ??= new();
            options.AdditionalProperties["umbraco-ai-skills"] = skills;
        }
        return await innerClient.CompleteAsync(messages, options, ct);
    }
}

// In Anthropic provider
// When building the Messages API request, check for skill references
// and add to container.skills[]
```

### Alternative: Dedicated Capability

Instead of middleware + AdditionalProperties, skills could be a new capability type alongside Chat and Embedding. However, skills augment chat rather than being a standalone capability, so the middleware approach is more natural.

---

## Open Questions

1. **File storage**: Should skill files be stored in the database (as proposed) or on disk/blob storage? Database is simpler but has size limits. For the 8MB max that providers impose, database storage is likely fine.

2. **Skill versioning in Umbraco**: Should Umbraco track skill versions locally (like content versioning) or just track the current state? The plan above takes the simpler "current state only" approach — the provider handles versioning.

3. **Provider skills catalog**: Should Umbraco surface a browsable catalog of provider built-in skills, or just let users reference them by ID? A catalog UI is nicer but requires each provider to implement `GetProviderSkillsAsync`.

4. **Sync strategy**: Should sync be manual (user clicks "Sync") or automatic (on save)? Manual gives more control and avoids accidental production changes. Auto-sync could be a per-connection setting.

5. **Multi-agent skill sharing**: If multiple agents use the same skill with the same connection, the skill only needs to be synced once. The current design handles this via the `AISkillProviderState` keyed by (skillId, connectionId).

6. **Scope for initial implementation**: Should the first version support file uploads, or just the SKILL.md manifest? Starting with manifest-only reduces complexity significantly while still being useful.
