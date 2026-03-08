# Plan: AI Skills Feature for Umbraco.AI

## Context

Both OpenAI and Anthropic now support "Skills" - versioned bundles of instructions, scripts, and resources uploaded as zip files that extend AI capabilities. Skills fill the gap between system prompts (always-on instructions) and tools (atomic functions) by packaging reusable procedures the model can mount and execute.

**Goal**: Add a Skills management section to Umbraco.AI where users can upload zip files, have them pushed to provider APIs, and reference them in chat requests - all without breaking the MEAI/MAF abstraction layer.

---

## Key Design Decisions

### 1. General vs Agent-only?

**General feature in Core, agents are primary consumer.**

Skills live in `Umbraco.AI.Core` because:
- The skill entity, deployment tracking, and upload infrastructure are provider concerns (not agent-specific)
- Any `IChatClient` consumer can reference skills via `ChatOptions.AdditionalProperties`
- Agents are the primary consumer but not the exclusive one

Skills are NOT relevant to Prompts (which are static text templates with no execution capability).

### 2. How to pass skills without breaking MEAI/MAF?

**`ChatOptions.AdditionalProperties` with a well-known key + provider-specific `IChatClient` decorators.**

```
Caller (Agent/Service)
    → Sets ChatOptions.AdditionalProperties["umbraco.ai.skills"] = List<AISkillReference>
    → AIChatService.MergeOptions() preserves AdditionalProperties (already does this)
    → Provider IChatClient receives ChatOptions with skill refs
    → Provider-specific decorator reads the key and translates:
        OpenAI:     Adds shell tool + environment.skills to API request
        Anthropic:  Adds container.skills + code_execution tool to API request
```

The MEAI `IChatClient` interface is never modified. Provider decorators are applied inside `CreateClient()` in each capability, wrapping the base MEAI client before it enters the middleware pipeline.

### 3. How do provider-agnostic skills connect to connections?

**Explicit deployment via skill workspace "Deployments" app.**

Skills are provider-agnostic (just zip + metadata). Users explicitly deploy skills to connections via a "Deployments" workspace app on the skill editor. A separate `AISkillDeployment` table tracks per-connection uploads:

```
AISkill (provider-agnostic)          AISkillDeployment (per-connection)
┌──────────────────────┐            ┌─────────────────────────────────┐
│ Id                   │←───────────│ SkillId                         │
│ Name, Alias          │            │ ConnectionId                    │
│ Description          │            │ ProviderSkillId (from API)      │
│ FileName, FileData   │            │ ProviderSkillVersion            │
│ FileHash (SHA256)    │            │ FileHash (deployed version)     │
│ IsActive             │            │ Status (Pending/Uploaded/Failed)│
└──────────────────────┘            │ StatusMessage                   │
                                    └─────────────────────────────────┘
```

**Deployment UX flow:**
1. **User creates a skill** → uploads zip, sets name/alias/description. Skill is saved locally (no provider upload yet).
2. **"Save and Deploy" button** → Like Umbraco CMS multilingual publish, opens a modal showing all skill-capable connections with checkboxes (all selected by default). User confirms which connections to deploy to. Modal shows real-time progress per connection (uploading → success/failed).
3. **Deployments workspace app** → Shows all deployment statuses. Allows manual deploy/undeploy/retry for individual connections. Shows "Update available" for stale deployments when zip changes.
4. **On skill zip update** → existing deployments become stale (FileHash mismatch). "Save and Deploy" modal pre-selects stale connections. User can also use "Re-deploy All" from Deployments app.
5. **Agent picks skills** → agent editor shows skills filtered to those deployed to the agent's profile's connection. At runtime, if a skill isn't deployed to the needed connection, the request fails with a clear error.

**"Save and Deploy" modal UX** (inspired by CMS multilingual publish):
```
┌──────────────────────────────────────────┐
│  Save and Deploy Skill                   │
│                                          │
│  Select connections to deploy to:        │
│                                          │
│  ☑ My OpenAI Connection      ● Uploaded  │
│  ☑ Anthropic Production      ○ Pending   │
│  ☐ Azure OpenAI (Dev)        — New       │
│  ☐ Google Gemini             ⚠ No skill  │
│                                          │
│  [Cancel]              [Save and Deploy] │
└──────────────────────────────────────────┘

Progress after clicking "Save and Deploy":
┌──────────────────────────────────────────┐
│  Deploying Skill...                      │
│                                          │
│  My OpenAI Connection      ✓ Uploaded    │
│  Anthropic Production      ⟳ Uploading   │
│                                          │
│                              [Close]     │
└──────────────────────────────────────────┘
```

Connections that don't support `IAISkillCapability` (e.g., Google Gemini if no skill API) are shown greyed out with explanation.

**This gives clear user control:**
- User always knows which connections a skill is deployed to
- "Save and Deploy" is the primary workflow (fast, familiar pattern from CMS)
- Deployments app provides granular control for advanced users
- No hidden side effects from saving agents
- Agent editor can filter/warn about undeployed skills

---

## Provider API Summary

| Aspect | OpenAI | Anthropic |
|--------|--------|-----------|
| Upload | `POST /v1/skills` (zip or files) | `POST /v1/skills` (zip or files, beta) |
| Returns | `skill_id` + version | `skill_id` (`skill_01...`) + version |
| Attach to request | Shell tool: `tools[].environment.skills[]` | Messages: `container.skills[]` |
| Reference format | `{type: "skill_reference", skill_id, version}` | `{type: "custom", skill_id, version}` |
| Required tools | Shell tool (`type: "shell"`) | Code execution tool (`type: "code_execution"`) |
| Versioning | Numeric strings ("1", "2") | Timestamps or "latest" |
| Beta headers | None | `skills-2025-10-02`, `code-execution-2025-08-25` |

---

## Architecture Design

### 1. Entity Model: `AISkill` + `AISkillFile` + `AISkillDeployment` (Core)

First-class entities in `Umbraco.AI.Core`, following the Connection/Profile pattern.

`AISkill` is versionable (implements `IAIVersionableEntity`). The zip binary is stored separately in `AISkillFile` (keyed by hash) so version snapshots only serialize lightweight metadata.

```
Umbraco.AI/src/Umbraco.AI.Core/Skills/
    AISkill.cs                  - Domain model (versionable, provider-agnostic)
    AISkillFile.cs              - Zip file storage (keyed by FileHash)
    AISkillDeployment.cs        - Per-connection deployment tracking
    AISkillStatus.cs            - Enum: Pending, Uploaded, Failed
    AISkillReference.cs         - Record for MEAI pass-through
    AISkillUploadResult.cs      - Record returned from provider upload
    IAISkillService.cs          - Service interface (CRUD + deploy orchestration)
    AISkillService.cs           - Service implementation
    IAISkillRepository.cs       - Repository interface
    IAISkillFileRepository.cs   - File repository interface
    IAISkillDeploymentRepository.cs - Deployment repository interface
    AISkillChatOptionsExtensions.cs - Read/write skills from ChatOptions
```

**`AISkill` properties (versionable - serialized to JSON for version snapshots):**

| Property | Type | Purpose |
|----------|------|---------|
| `Id` | `Guid` | Unique identifier |
| `Alias` | `string` | URL-safe identifier |
| `Name` | `string` | Display name |
| `Description` | `string?` | Human-readable description |
| `FileName` | `string` | Original zip file name |
| `FileHash` | `string` | SHA256 of zip (reference to AISkillFile) |
| `IsActive` | `bool` | Whether available for use |
| `DateCreated` | `DateTime` | Audit |
| `DateModified` | `DateTime` | Audit |
| `CreatedByUserId` | `Guid?` | Audit |
| `ModifiedByUserId` | `Guid?` | Audit |
| `Version` | `int` | Optimistic concurrency (starts at 1) |

**`AISkillFile` properties (NOT versioned - content-addressed storage):**

| Property | Type | Purpose |
|----------|------|---------|
| `FileHash` | `string` | PK - SHA256 hash of FileData |
| `FileData` | `byte[]` | Zip contents (DB blob, max 10MB) |
| `FileName` | `string` | Original file name |
| `DateCreated` | `DateTime` | When first stored |

**`AISkillDeployment` properties:**

| Property | Type | Purpose |
|----------|------|---------|
| `Id` | `Guid` | Unique identifier |
| `SkillId` | `Guid` | FK to AISkill |
| `ConnectionId` | `Guid` | The connection deployed to |
| `ProviderSkillId` | `string?` | Provider-returned skill ID |
| `ProviderSkillVersion` | `string?` | Provider-returned version |
| `FileHash` | `string` | Hash of zip that was deployed (detect stale) |
| `Status` | `AISkillStatus` | Pending / Uploaded / Failed |
| `StatusMessage` | `string?` | Error message if failed |
| `DateDeployed` | `DateTime` | When last deployed |

**Design decisions:**
- **Provider-agnostic skill entity**: The zip is stored once. Deployments track per-connection uploads.
- **Separate file storage**: Zip binary in `AISkillFile` (content-addressed by SHA256 hash). `AISkill` references by `FileHash`. Version snapshots only serialize lightweight metadata + hash. Old file versions are preserved (can be garbage collected later).
- **Zip stored in DB** as blob. Skills are small (KB-range, max 10MB enforced). Can migrate to blob storage later.
- **Entity versioning**: `AISkill` implements `IAIVersionableEntity`. Each save increments `Version`. Snapshots capture metadata + FileHash (not binary). Any version's zip can be retrieved from `AISkillFile` by its hash.
- **Provider versioning with pinning**: When a skill zip changes (`FileHash` differs), re-deploy creates a new version on the provider. Each deployment pins to the specific `ProviderSkillVersion` returned by the upload. This prevents cross-environment contamination when environments share connections.
- **Stale detection**: Compare `AISkill.FileHash` with `AISkillDeployment.FileHash` to know if re-upload is needed.

### 2. Provider Skill Capability: `IAISkillCapability`

New optional capability interface that providers can implement to support skills.

```
Umbraco.AI/src/Umbraco.AI.Core/Providers/IAICapability.cs  (extend existing file)
    IAISkillCapability              - Upload/delete skills on provider
    IAIConfiguredSkillCapability    - Same with resolved settings
```

```csharp
public interface IAISkillCapability : IAICapability
{
    /// Upload a new skill or new version of existing skill.
    /// If providerSkillId is null, creates new. Otherwise uploads new version.
    Task<AISkillUploadResult> UploadSkillAsync(
        object? settings, byte[] zipData, string name, string description,
        string? providerSkillId, CancellationToken ct);

    Task DeleteSkillAsync(
        object? settings, string providerSkillId, CancellationToken ct);
}

public interface IAIConfiguredSkillCapability : IAIConfiguredCapability
{
    Task<AISkillUploadResult> UploadSkillAsync(
        byte[] zipData, string name, string description,
        string? providerSkillId, CancellationToken ct);

    Task DeleteSkillAsync(string providerSkillId, CancellationToken ct);
}
```

```csharp
public record AISkillUploadResult(string ProviderSkillId, string Version);
```

Provider implementations:
- `Umbraco.AI.OpenAI/OpenAISkillCapability.cs` - Calls OpenAI `POST /v1/skills`
- `Umbraco.AI.Anthropic/AnthropicSkillCapability.cs` - Calls Anthropic `POST /v1/skills`

### 3. MEAI Pass-through

**Well-known key**: `"umbraco.ai.skills"` in `ChatOptions.AdditionalProperties`
**Value**: `List<AISkillReference>`

```csharp
public record AISkillReference(string ProviderSkillId, string Version);
```

**Extension methods** (in `Umbraco.AI.Extensions` namespace):
```csharp
public static class AISkillChatOptionsExtensions
{
    public static void SetSkills(this ChatOptions options, IEnumerable<AISkillReference> skills);
    public static IReadOnlyList<AISkillReference>? GetSkills(this ChatOptions options);
}
```

**Provider-side decorators:**
Each provider wraps their `IChatClient` with a skill-aware decorator applied in `CreateClient()`:

```csharp
// In OpenAIChatCapability.CreateClient()
protected override IChatClient CreateClient(OpenAIProviderSettings settings, string? modelId)
{
    var baseClient = OpenAIProvider.CreateOpenAIClient(settings)
        .GetResponsesClient(modelId ?? DefaultChatModel)
        .AsIChatClient();
    return new OpenAISkillInjectingChatClient(baseClient);
}
```

The decorator reads `ChatOptions.GetSkills()` and translates to provider-specific format:
- **OpenAI**: Adds/merges shell tool with `environment.skills` containing `skill_reference` entries
- **Anthropic**: Adds `container.skills` with `custom` entries + ensures `code_execution` tool is present

### 4. Agent Integration

Add `SkillIds` to `AIAgent`. Agent factory resolves skills and attaches them to `ChatOptions`.

**Changes:**
- `AIAgent.SkillIds` - `IReadOnlyList<Guid>` (references `AISkill.Id`)
- `AIAgentFactory.CreateAgentAsync()`:
  1. Resolves agent's skills via `IAISkillService`
  2. Looks up deployments for the agent's profile's connection
  3. If any skill lacks a valid deployment → throw with clear error message ("Skill 'X' is not deployed to connection 'Y'")
  4. Adds `AISkillReference` list to `ChatOptions` via extension method

**Agent editor validation:**
- When selecting skills in the agent editor, the UI shows deployment status per skill for the agent's selected profile's connection
- Skills without a valid deployment are shown with a warning badge
- Saving an agent with undeployed skills is allowed (profile might change later) but shows a warning

### 5. Deployment Orchestration (in `AISkillService`)

```csharp
public interface IAISkillService
{
    // Skill CRUD
    Task<AISkill?> GetSkillAsync(Guid id, CancellationToken ct);
    Task<AISkill?> GetSkillByAliasAsync(string alias, CancellationToken ct);
    Task<IReadOnlyList<AISkill>> GetAllSkillsAsync(CancellationToken ct);
    Task<AISkill> CreateSkillAsync(AISkill skill, CancellationToken ct);
    Task<AISkill> UpdateSkillAsync(AISkill skill, CancellationToken ct);
    Task DeleteSkillAsync(Guid id, CancellationToken ct);

    // Deployment (user-initiated)
    Task<AISkillDeployment> DeployToConnectionAsync(Guid skillId, Guid connectionId, CancellationToken ct);
    Task<IReadOnlyList<AISkillDeployment>> GetDeploymentsAsync(Guid skillId, CancellationToken ct);
    Task<AISkillDeployment?> GetDeploymentAsync(Guid skillId, Guid connectionId, CancellationToken ct);
    Task RedeployAsync(Guid skillId, Guid connectionId, CancellationToken ct);
    Task RedeployAllAsync(Guid skillId, CancellationToken ct);
    Task UndeployAsync(Guid skillId, Guid connectionId, CancellationToken ct);

    // Convenience: resolve skill refs for a set of skill IDs + connection
    Task<IReadOnlyList<AISkillReference>> ResolveSkillReferencesAsync(
        IEnumerable<Guid> skillIds, Guid connectionId, CancellationToken ct);
}
```

**`DeployToConnectionAsync` logic:**
1. Validate the connection exists and its provider supports `IAISkillCapability`
2. Look up existing deployment for (skillId, connectionId)
3. If exists and `FileHash` matches → return existing (already up to date)
4. If exists but hash differs → re-upload (new version), update deployment record
5. If not exists → upload new skill to provider, create deployment record
6. On success → set Status=Uploaded, store ProviderSkillId + Version
7. On failure → set Status=Failed, StatusMessage with error details

**`RedeployAsync` logic:**
- Same as deploy but forces re-upload regardless of FileHash (for retry scenarios)

### 6. Management API

CRUD endpoints for skills + deployment status.

```
Umbraco.AI/src/Umbraco.AI.Web/Api/Management/Skill/
    Controllers/
        SkillController.cs              - GET, POST, PUT, DELETE
        SkillDeploymentController.cs    - GET deployments, POST retry
    Models/
        CreateSkillRequestModel.cs      - Name, alias, description, zip file (multipart)
        UpdateSkillRequestModel.cs      - Name, description, zip file (optional re-upload)
        SkillResponseModel.cs           - Skill + deployment statuses
        SkillDeploymentResponseModel.cs
    Mapping/
        SkillMappingDefinition.cs
```

**Endpoints:**
- `POST   /umbraco/ai/management/api/v1/skill` - Create skill (multipart zip upload)
- `GET    /umbraco/ai/management/api/v1/skill` - List all skills
- `GET    /umbraco/ai/management/api/v1/skill/{id}` - Get by ID (includes deployments)
- `PUT    /umbraco/ai/management/api/v1/skill/{id}` - Update (optionally re-upload zip)
- `DELETE /umbraco/ai/management/api/v1/skill/{id}` - Delete (+ delete from all providers)
- `GET    /umbraco/ai/management/api/v1/skill/{id}/deployment` - List deployments for skill
- `POST   /umbraco/ai/management/api/v1/skill/{id}/deployment/{connectionId}` - Deploy skill to connection
- `POST   /umbraco/ai/management/api/v1/skill/{id}/deployment/{connectionId}/redeploy` - Re-deploy (force re-upload)
- `DELETE /umbraco/ai/management/api/v1/skill/{id}/deployment/{connectionId}` - Undeploy from connection
- `POST   /umbraco/ai/management/api/v1/skill/{id}/deployment/redeploy-all` - Re-deploy to all connections

### 7. Persistence

```
Umbraco.AI/src/Umbraco.AI.Persistence/Skills/
    AISkillEntity.cs
    AISkillFileEntity.cs
    AISkillDeploymentEntity.cs
    AISkillRepository.cs
    AISkillFileRepository.cs
    AISkillDeploymentRepository.cs
    AISkillEntityConfiguration.cs
    AISkillFileEntityConfiguration.cs
    AISkillDeploymentEntityConfiguration.cs
```

**Table: `UmbracoAISkill`** (versionable metadata)

| Column | Type | Notes |
|--------|------|-------|
| Id | uniqueidentifier | PK |
| Alias | nvarchar(255) | Unique |
| Name | nvarchar(255) | |
| Description | nvarchar(max) | Nullable |
| FileName | nvarchar(255) | Original file name |
| FileHash | nvarchar(64) | FK → UmbracoAISkillFile |
| IsActive | bit | |
| DateCreated | datetime2 | |
| DateModified | datetime2 | |
| CreatedByUserId | uniqueidentifier | Nullable |
| ModifiedByUserId | uniqueidentifier | Nullable |
| Version | int | Optimistic concurrency |

**Table: `UmbracoAISkillFile`** (content-addressed blob storage)

| Column | Type | Notes |
|--------|------|-------|
| FileHash | nvarchar(64) | PK (SHA256) |
| FileData | varbinary(max) | Zip blob (max 10MB) |
| FileName | nvarchar(255) | Original file name |
| DateCreated | datetime2 | When first stored |

**Table: `UmbracoAISkillDeployment`**

| Column | Type | Notes |
|--------|------|-------|
| Id | uniqueidentifier | PK |
| SkillId | uniqueidentifier | FK → UmbracoAISkill, cascade delete |
| ConnectionId | uniqueidentifier | FK to connection |
| ProviderSkillId | nvarchar(255) | Nullable |
| ProviderSkillVersion | nvarchar(100) | Nullable |
| FileHash | nvarchar(64) | Hash of deployed zip |
| Status | int | 0=Pending, 1=Uploaded, 2=Failed |
| StatusMessage | nvarchar(max) | Nullable |
| DateDeployed | datetime2 | |

Unique constraint: `(SkillId, ConnectionId)` - one deployment per skill per connection.

### 8. Agent Persistence Changes

Add `SkillIds` JSON column to agent entity.

```
Umbraco.AI.Agent/src/Umbraco.AI.Agent.Persistence/Agents/
    AIAgentEntity.cs  - Add SkillIds property (JSON serialized list of GUIDs)

Migration: UmbracoAIAgent_AddSkillIds.cs
```

---

## Implementation Order

### Phase 1: Core Skill Entity & Service
1. `AISkill` domain model (versionable) in `Umbraco.AI.Core/Skills/`
2. `AISkillFile` domain model (content-addressed zip storage)
3. `AISkillDeployment` domain model
3b. `AISkillStatus` enum
4. `AISkillReference` record + `AISkillUploadResult` record
5. `IAISkillRepository` + `IAISkillFileRepository` + `IAISkillDeploymentRepository` interfaces
6. `IAISkillService` interface with CRUD + deployment methods
7. `AISkillService` implementation with deployment orchestration
8. `AISkillChatOptionsExtensions` (SetSkills/GetSkills on ChatOptions)

### Phase 2: Provider Skill Capability
9. `IAISkillCapability` + `IAIConfiguredSkillCapability` interfaces (in IAICapability.cs)
10. `AISkillCapabilityBase<TSettings>` abstract base
11. `OpenAISkillCapability` - upload/delete via OpenAI API
12. `AnthropicSkillCapability` - upload/delete via Anthropic API
13. Register capabilities in provider constructors

### Phase 3: Provider Skill Injection (MEAI Integration)
14. `OpenAISkillInjectingChatClient` - reads skills from ChatOptions, injects shell tool + environment
15. `AnthropicSkillInjectingChatClient` - reads skills from ChatOptions, injects container + code_execution
16. Wire into `CreateClient()` in each provider's chat capability

### Phase 4: Persistence
17. `AISkillEntity` + `AISkillEntityConfiguration`
18. `AISkillFileEntity` + `AISkillFileEntityConfiguration`
19. `AISkillDeploymentEntity` + `AISkillDeploymentEntityConfiguration`
20. `AISkillRepository` + `AISkillFileRepository` + `AISkillDeploymentRepository`
21. EF Core migrations (SqlServer + Sqlite) for all three tables
21. DI registration in Startup composer

### Phase 5: Management API
22. Request/response models
23. `SkillController` with CRUD endpoints
24. `SkillDeploymentController` with deployment listing + retry
25. Mapping definitions
26. OpenAPI spec generation

### Phase 6: Agent Integration
27. Add `SkillIds` to `AIAgent` entity
28. Agent persistence migration (add SkillIds JSON column)
29. `AIAgentFactory` changes: resolve skills → lookup deployments → fail if missing → attach to ChatOptions
30. Agent Management API updates (include skillIds in create/update models)

### Phase 7: Frontend (Full Stack)
31. Generate OpenAPI client for skill endpoints
32. Skill section in backoffice (Lit web components):
    - Skill collection view (table with name, deployment count, status summary)
    - Skill workspace with two apps:
      a. **Details app** - Name, alias, description, zip file upload/replace
      b. **Deployments app** - List of connection deployments with status badges, deploy/undeploy/retry actions, stale version indicators
    - **"Save and Deploy" modal** (like CMS multilingual publish):
      - Shows all skill-capable connections with checkboxes (default: all selected)
      - Greyed-out entries for connections without skill support
      - Real-time progress indicators per connection during deployment
      - Stale connections pre-selected when zip has changed
    - Delete confirmation dialog
33. Agent editor UI updates:
    - Skill picker (multi-select from available skills)
    - Show deployment status badges per skill for the agent's profile's connection
    - Warning for skills not deployed to the agent's connection

### Phase 8: Deploy Support
34. `Umbraco.AI.Deploy` - Add skill artifact support using **companion file pattern**:
    - **Transferred**: `AISkill` metadata (`.uda` artifact) + zip binary (companion `.zip` file alongside the `.uda`)
    - **NOT transferred**: `AISkillDeployment` records (environment-specific, re-created on target)
    - On target environment: skill appears with no deployments → user uses "Save and Deploy" to upload to target's connections → gets fresh `ProviderSkillId` + pinned version

    **Companion File Pattern** (similar to how CMS Deploy handles media files):

    ```
    ~/data/UmbracoAI/
        AISkill__abc123def456.uda          ← JSON artifact (metadata: name, alias, hash, etc.)
        AISkill__abc123def456.zip          ← Companion binary (the skill zip file)
    ```

    **How it works:**

    1. **On save (source environment):**
       - `AISkillSavedDeployRefresherNotificationAsyncHandler` triggers
       - Service connector creates `AISkillArtifact` (JSON metadata including `FileHash`, `FileName`)
       - After writing the `.uda` artifact, the handler also writes the companion `.zip` file to the same directory using the same naming convention (`{entityType}__{guid}.zip`)
       - The zip binary is read from `IAISkillFileRepository` using the skill's `FileHash`

    2. **On deploy (target environment):**
       - `UmbracoAISkillServiceConnector.ProcessAsync()` reads the `.uda` artifact
       - Detects companion `.zip` file at `{artifactPath}.zip` (same path, different extension)
       - Reads the zip binary, computes SHA256 hash, stores in `AISkillFile` table
       - Creates/updates `AISkill` entity with metadata + `FileHash` reference
       - No deployments created — user must explicitly deploy to target connections

    3. **On delete (source environment):**
       - `AISkillDeletedDeployRefresherNotificationAsyncHandler` triggers
       - Deletes both the `.uda` artifact AND the companion `.zip` file

    **New files for Deploy support:**

    ```
    Umbraco.AI.Deploy/src/Umbraco.AI.Deploy/
        Artifacts/
            AISkillArtifact.cs              - Skill metadata artifact (no binary data)
        Connectors/ServiceConnectors/
            UmbracoAISkillServiceConnector.cs - Handles .uda + companion .zip read/write
        NotificationHandlers/
            AISkillSavedDeployRefresherNotificationAsyncHandler.cs   - Writes .uda + .zip
            AISkillDeletedDeployRefresherNotificationAsyncHandler.cs - Deletes .uda + .zip
    ```

    **`AISkillArtifact` properties** (JSON in `.uda` file):

    | Property | Type | Notes |
    |----------|------|-------|
    | `Alias` | `string` | URL-safe identifier |
    | `Name` | `string` | Display name |
    | `Description` | `string?` | Optional |
    | `FileName` | `string` | Original zip file name |
    | `FileHash` | `string` | SHA256 of zip (used to detect changes) |
    | `IsActive` | `bool` | Whether available |

    **Key design decisions:**
    - **No binary in JSON**: The `.uda` file stays pure JSON (no base64-encoded blobs). Binary lives in the companion file.
    - **Hash-based change detection**: On import, if the companion `.zip` hash matches an existing `AISkillFile` in the DB, the binary isn't re-stored (content-addressed deduplication).
    - **Git-friendly**: Both `.uda` and `.zip` are committed to the Deploy data folder. The `.zip` is a binary file tracked by git (small files, KB-range, max 10MB).
    - **Notification handler owns companion file lifecycle**: The saved/deleted handlers manage both files atomically. The service connector reads them during import.

### Multi-Environment Safety (Version Pinning)

**Problem**: If dev and prod share the same provider connection (API key), uploading a new skill version on dev could affect prod.

**Solution**: Each `AISkillDeployment` pins to a specific `ProviderSkillVersion`. At runtime, the pinned version (not "latest") is passed to the provider.

```
Environment A (Dev):
  Skill "Code Review" → Connection "OpenAI Shared"
  Deployment: ProviderSkillId=sk_abc, Version="2"  ← dev uploaded v2

Environment B (Prod):
  Skill "Code Review" → Connection "OpenAI Shared"  (same API key!)
  Deployment: ProviderSkillId=sk_abc, Version="1"  ← prod still on v1

→ Prod safely uses v1 until explicitly re-deployed
→ Dev uses v2 independently
```

When `Umbraco.AI.Deploy` transfers skills (companion file pattern):
- Target gets the `.uda` (metadata) + companion `.zip` (binary) via git
- Service connector reads both files during import, stores zip in `AISkillFile` table
- Target has NO deployments → user must explicitly deploy via "Save and Deploy" modal
- Even if target uses the same connection/API key, it creates its own deployment with its own pinned version

---

## Key Files to Modify

| File | Change |
|------|--------|
| `Umbraco.AI/src/Umbraco.AI.Core/Providers/IAICapability.cs` | Add `IAISkillCapability` interface |
| `Umbraco.AI/src/Umbraco.AI.Core/Providers/IAIConfiguredCapability.cs` | Add `IAIConfiguredSkillCapability` |
| `Umbraco.AI/src/Umbraco.AI.Core/Providers/AIConfiguredCapability.cs` | Add configured skill capability impl |
| `Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/OpenAIProvider.cs` | Register `OpenAISkillCapability` |
| `Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/OpenAIChatCapability.cs` | Wrap client with skill injector |
| `Umbraco.AI.Anthropic/src/Umbraco.AI.Anthropic/AnthropicProvider.cs` | Register `AnthropicSkillCapability` |
| `Umbraco.AI.Anthropic/src/Umbraco.AI.Anthropic/AnthropicChatCapability.cs` | Wrap client with skill injector |
| `Umbraco.AI/src/Umbraco.AI.Persistence/UmbracoAIDbContext.cs` | Add DbSets for skill + deployment |
| `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Agents/AIAgent.cs` | Add `SkillIds` property |
| `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Chat/AIAgentFactory.cs` | Resolve and attach skills |

## New Files to Create

| File | Purpose |
|------|---------|
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkill.cs` | Domain model (versionable, provider-agnostic) |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkillFile.cs` | Content-addressed zip storage |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkillDeployment.cs` | Per-connection deployment tracking |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkillStatus.cs` | Status enum |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkillReference.cs` | MEAI pass-through record |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkillUploadResult.cs` | Upload result record |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/IAISkillService.cs` | Service interface |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkillService.cs` | Service implementation |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/IAISkillRepository.cs` | Skill repository interface |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/IAISkillFileRepository.cs` | File repository interface |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/IAISkillDeploymentRepository.cs` | Deployment repository interface |
| `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkillChatOptionsExtensions.cs` | ChatOptions extensions |
| `Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/OpenAISkillCapability.cs` | OpenAI skill upload |
| `Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/OpenAISkillInjectingChatClient.cs` | Skill injection decorator |
| `Umbraco.AI.Anthropic/src/Umbraco.AI.Anthropic/AnthropicSkillCapability.cs` | Anthropic skill upload |
| `Umbraco.AI.Anthropic/src/Umbraco.AI.Anthropic/AnthropicSkillInjectingChatClient.cs` | Skill injection decorator |
| `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillEntity.cs` | EF Core entity |
| `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillFileEntity.cs` | EF Core entity (content-addressed) |
| `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillDeploymentEntity.cs` | EF Core entity |
| `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillRepository.cs` | Repository impl |
| `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillFileRepository.cs` | Repository impl |
| `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillDeploymentRepository.cs` | Repository impl |
| `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillEntityConfiguration.cs` | EF Core config |
| `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillFileEntityConfiguration.cs` | EF Core config |
| `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillDeploymentEntityConfiguration.cs` | EF Core config |
| `Umbraco.AI/src/Umbraco.AI.Web/Api/Management/Skill/Controllers/SkillController.cs` | CRUD API |
| `Umbraco.AI/src/Umbraco.AI.Web/Api/Management/Skill/Controllers/SkillDeploymentController.cs` | Deployment API |
| `Umbraco.AI/src/Umbraco.AI.Web/Api/Management/Skill/Models/*.cs` | DTOs |
| `Umbraco.AI/src/Umbraco.AI.Web/Api/Management/Skill/Mapping/SkillMappingDefinition.cs` | Mapping |
| `Umbraco.AI.Deploy/src/Umbraco.AI.Deploy/Artifacts/AISkillArtifact.cs` | Skill deploy artifact (metadata only, no binary) |
| `Umbraco.AI.Deploy/src/Umbraco.AI.Deploy/Connectors/ServiceConnectors/UmbracoAISkillServiceConnector.cs` | Skill service connector (reads .uda + companion .zip) |
| `Umbraco.AI.Deploy/src/Umbraco.AI.Deploy/NotificationHandlers/AISkillSavedDeployRefresherNotificationAsyncHandler.cs` | Writes .uda + companion .zip on save |
| `Umbraco.AI.Deploy/src/Umbraco.AI.Deploy/NotificationHandlers/AISkillDeletedDeployRefresherNotificationAsyncHandler.cs` | Deletes .uda + companion .zip on delete |

---

## Resolved Decisions

1. **General vs Agent-only**: General feature in Core. Agents are primary consumer. Not relevant to Prompts (which are static text).
2. **MEAI integration**: `ChatOptions.AdditionalProperties` with well-known key `"umbraco.ai.skills"`. Provider decorators translate to API-specific format.
3. **Skill-to-Connection mapping**: Provider-agnostic skills with `AISkillDeployment` table tracking per-connection uploads.
4. **Deployment UX**: "Save and Deploy" button opens a connection selection modal (like CMS multilingual publish). Deployments workspace app provides granular control. At runtime, missing deployments produce clear errors.
5. **Skill versioning**: New version created on provider with each zip update. **Version-pinned per deployment** (NOT "latest"). Each `AISkillDeployment` records the specific `ProviderSkillVersion` used at runtime. This prevents cross-environment contamination when environments share the same connection/API key. Re-deploying on a specific environment updates that environment's pinned version.
6. **Frontend scope**: Full stack - includes skill workspace (details + deployments apps), collection view, "Save and Deploy" modal, and agent skill picker.
7. **File size limit**: 10MB maximum zip file size enforced on upload.
8. **Deploy support**: Companion file pattern — `.uda` artifact (JSON metadata) + companion `.zip` file saved alongside in the same directory (`~/data/UmbracoAI/`). Both committed to git. On import, service connector reads both files. Deployments are environment-specific and NOT transferred — re-created on target via "Save and Deploy".
9. **Zip storage**: Separate `AISkillFile` table (content-addressed by SHA256 hash) stores zip blobs. `AISkill` references by hash. Version snapshots only serialize metadata + hash, not binary data. Old file versions preserved for rollback.

## Skill Removal / Cleanup Scenarios

| Scenario | Behavior |
|----------|----------|
| **Skill deleted from Umbraco** | Delete all `AISkillDeployment` records (DB cascade). Call `DeleteSkillAsync()` on each deployment's provider to remove from external API. Remove skill ID from any agents referencing it. |
| **Skill removed from an agent** | Remove ID from `AIAgent.SkillIds`. Deployment record remains (other agents may use same skill+connection). |
| **Connection deleted** | Cascade-delete deployments for that connection. Skill entity remains (provider-agnostic). |
| **Skill zip updated** | `FileHash` changes. Existing deployments shown as "stale" in Deployments app. User re-deploys explicitly (or uses "Re-deploy All" action). New provider version created on re-deploy. |
| **Failed deployment** | Remains with `Status=Failed` for diagnostics. User can retry via API/UI. |

Provider-side deletion is best-effort (fire and forget with logging). If the provider API call fails during skill deletion, we still delete the local records since the remote skill is orphaned but harmless.

## Remaining Open Questions

None - all questions resolved.

---

## References

- [OpenAI Skills Guide](https://developers.openai.com/api/docs/guides/tools-skills/)
- [Anthropic Skills Guide](https://platform.claude.com/docs/en/build-with-claude/skills-guide)
- [OpenAI Skills Cookbook](https://developers.openai.com/cookbook/examples/skills_in_api/)
