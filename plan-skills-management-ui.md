# Skills Management UI — Implementation Plan

## Overview

Add a **Features** extensibility layer to the provider/capability system and implement **Skills** as the first feature type — managed entities in Core representing provider-backed AI operations (web search, code interpreter, image generation, etc.) that users configure and assign to agents via a backoffice management UI.

### Architecture Summary

```
Provider (e.g., OpenAI)
    ├── Capability: Chat
    │   └── Feature: IAISkillsFeature  ← NEW (provider-implemented)
    ├── Capability: Embedding
    └── ...

Skill (managed entity in Core)  ← NEW (stored in DB)
    ├── Name, Alias, Description
    ├── SkillTypeId (provider-defined, e.g. "web-search")
    ├── ConnectionId (link to provider connection)
    ├── Settings (provider-specific config)
    ├── ToolScopeIds (permission grouping)
    └── IsActive flag
```

### Key Design Decisions

1. **Skills in Core** — Skills are a provider-level concept (like connections/profiles), not agent-specific
2. **Features as typed interfaces** — Not metadata flags, but full implementations with provider-specific logic
3. **Same patterns as capabilities** — `TryGetFeature<T>()`, `HasFeature<T>()`, feature pass-through on configured decorator
4. **Provider-implemented** — Core defines `IAISkillsFeature`, OpenAI implements `OpenAISkillsFeature`
5. **Skills are NOT file bundles** — Unlike the earlier SKILL.md manifest concept, skills here are configuration entities that reference provider-defined skill types. Providers handle execution internally.

---

## Phase 1: Features System — Core Infrastructure

Add a general-purpose **Feature** extensibility layer to capabilities. Features are typed interfaces that providers optionally implement on their capabilities, discovered via `TryGetFeature<T>()` / `HasFeature<T>()`.

### 1.1 Feature Marker Interface

**New file:** `Umbraco.AI/src/Umbraco.AI.Core/Providers/Features/IAIFeature.cs`

```csharp
/// <summary>
/// Marker interface for provider features — typed extension points on capabilities.
/// Features represent optional functionality that providers can expose beyond
/// the base capability contract (e.g., skills, vision, structured output).
/// </summary>
public interface IAIFeature
{
    /// <summary>
    /// Unique identifier for this feature type (e.g., "skills", "vision").
    /// </summary>
    string FeatureId { get; }
}
```

### 1.2 Feature Discovery on Capabilities

**Modify:** `Umbraco.AI/src/Umbraco.AI.Core/Providers/IAICapability.cs`

Add feature discovery methods to `IAICapability` interface:

```csharp
IReadOnlyCollection<IAIFeature> GetFeatures();
bool TryGetFeature<TFeature>(out TFeature? feature) where TFeature : class, IAIFeature;
TFeature? GetFeature<TFeature>() where TFeature : class, IAIFeature;
bool HasFeature<TFeature>() where TFeature : class, IAIFeature;
```

**Modify:** `AICapabilityBase` (in same file) — add feature storage and implementation:

```csharp
protected readonly List<IAIFeature> Features = [];

protected void WithFeature<TFeature>() where TFeature : class, IAIFeature, new()
    => Features.Add(new TFeature());

protected void WithFeature(IAIFeature feature)
    => Features.Add(feature);

public IReadOnlyCollection<IAIFeature> GetFeatures() => Features.AsReadOnly();

public bool TryGetFeature<TFeature>(out TFeature? feature) where TFeature : class, IAIFeature
{
    feature = Features.OfType<TFeature>().FirstOrDefault();
    return feature is not null;
}

public TFeature? GetFeature<TFeature>() where TFeature : class, IAIFeature
    => Features.OfType<TFeature>().FirstOrDefault();

public bool HasFeature<TFeature>() where TFeature : class, IAIFeature
    => Features.OfType<TFeature>().Any();
```

### 1.3 Feature Pass-Through on Configured Capabilities

**Modify:** `Umbraco.AI/src/Umbraco.AI.Core/Providers/IAIConfiguredCapability.cs`

Add same feature methods to `IAIConfiguredCapability`.

**Modify:** `Umbraco.AI/src/Umbraco.AI.Core/Providers/AIConfiguredCapability.cs`

In both `AIConfiguredChatCapability` and `AIConfiguredEmbeddingCapability`, delegate to the inner capability:

```csharp
public IReadOnlyCollection<IAIFeature> GetFeatures() => _inner.GetFeatures();
public bool TryGetFeature<TFeature>(out TFeature? feature) where TFeature : class, IAIFeature
    => _inner.TryGetFeature(out feature);
// ... etc
```

Features don't need the configured/settings-baked-in pattern since they're metadata and factory interfaces — the configured capability simply delegates.

### 1.4 Feature Aggregation on Providers

**Modify:** `Umbraco.AI/src/Umbraco.AI.Core/Providers/IAIProvider.cs` — add:

```csharp
IReadOnlyCollection<IAIFeature> GetFeatures();
bool HasFeature<TFeature>() where TFeature : class, IAIFeature;
```

**Modify:** `Umbraco.AI/src/Umbraco.AI.Core/Providers/AIProviderBase.cs` — implement by scanning all capabilities:

```csharp
public IReadOnlyCollection<IAIFeature> GetFeatures()
    => Capabilities.SelectMany(c => c.GetFeatures()).Distinct().ToList().AsReadOnly();

public bool HasFeature<TFeature>() where TFeature : class, IAIFeature
    => Capabilities.Any(c => c.HasFeature<TFeature>());
```

**Modify:** `IAIConfiguredProvider` / `AIConfiguredProvider` — same aggregation on configured side.

### 1.5 Management API — Feature Exposure

**Modify:** `ProviderResponseModel` — add features to provider detail response:

```csharp
public IEnumerable<string> Features { get; set; } = [];  // Feature IDs
```

**Modify:** `AllProviderController` / `ByIdProviderController` mapping — populate features from `provider.GetFeatures()`.

---

## Phase 2: Skills Feature Interface

Define the concrete **Skills Feature** — the first feature type.

### 2.1 Skill Descriptor

**New file:** `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkillDescriptor.cs`

```csharp
/// <summary>
/// Describes a skill type that a provider offers (e.g., "web-search", "code-interpreter").
/// Returned by IAISkillsFeature.GetAvailableSkillsAsync.
/// </summary>
public class AISkillDescriptor
{
    /// <summary>Provider-defined skill type ID (e.g., "web-search").</summary>
    public required string SkillTypeId { get; init; }

    /// <summary>Human-readable name.</summary>
    public required string Name { get; init; }

    /// <summary>Description of what this skill does.</summary>
    public string? Description { get; init; }

    /// <summary>Icon for UI display.</summary>
    public string? Icon { get; init; }
}
```

### 2.2 Skills Feature Interface

**New file:** `Umbraco.AI/src/Umbraco.AI.Core/Skills/IAISkillsFeature.cs`

```csharp
/// <summary>
/// Feature interface indicating a capability supports skill execution.
/// Providers implement this on their capabilities to expose available
/// skill types and their configuration schemas.
/// </summary>
public interface IAISkillsFeature : IAIFeature
{
    /// <summary>
    /// Returns descriptors for all skill types this capability supports.
    /// </summary>
    Task<IReadOnlyList<AISkillDescriptor>> GetAvailableSkillsAsync(
        object? settings, CancellationToken ct);

    /// <summary>
    /// Returns the settings schema for a specific skill type,
    /// used to render the configuration form in the backoffice.
    /// </summary>
    AIEditableModelSchema? GetSkillSettingsSchema(string skillTypeId);
}
```

The `settings` parameter receives the provider connection settings (same pattern as capability methods). When called through a configured provider, the caller passes the resolved connection settings.

---

## Phase 3: Skills Domain Model

### 3.1 Skill Entity

**New file:** `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkill.cs`

```csharp
public class AISkill : IAIVersionableEntity
{
    public Guid Id { get; internal set; }
    public required string Alias { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The provider-defined skill type (e.g., "web-search", "code-interpreter").
    /// Immutable after creation.
    /// </summary>
    public required string SkillTypeId { get; init; }

    /// <summary>
    /// The connection providing this skill. Determines which provider
    /// and credentials are used for skill execution.
    /// </summary>
    public required Guid ConnectionId { get; set; }

    /// <summary>
    /// Provider-specific configuration for this skill instance.
    /// Schema driven by IAISkillsFeature.GetSkillSettingsSchema.
    /// </summary>
    public object? Settings { get; set; }

    /// <summary>
    /// Tool scopes this skill operates in, for permission grouping.
    /// Allows agents to include/exclude skills via scope permissions.
    /// </summary>
    public IReadOnlyList<string> ToolScopeIds { get; set; } = [];

    // Standard entity metadata
    public int Version { get; internal set; }
    public DateTime DateCreated { get; init; }
    public DateTime DateModified { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ModifiedByUserId { get; set; }
}
```

### 3.2 Skill Service

**New file:** `Umbraco.AI/src/Umbraco.AI.Core/Skills/IAISkillService.cs`

```csharp
public interface IAISkillService
{
    Task<AISkill?> GetSkillAsync(Guid id, CancellationToken ct);
    Task<AISkill?> GetSkillByAliasAsync(string alias, CancellationToken ct);
    Task<IEnumerable<AISkill>> GetSkillsAsync(CancellationToken ct);
    Task<(IEnumerable<AISkill> Items, int Total)> GetSkillsPagedAsync(
        string? filter = null, int skip = 0, int take = 100,
        CancellationToken ct = default);
    Task<AISkill> SaveSkillAsync(AISkill skill, CancellationToken ct);
    Task<bool> DeleteSkillAsync(Guid id, CancellationToken ct);
    Task<bool> SkillAliasExistsAsync(string alias, Guid? excludeId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns available skill types across all connections that support the skills feature.
    /// </summary>
    Task<IReadOnlyList<AISkillDescriptor>> GetAvailableSkillTypesAsync(CancellationToken ct);

    /// <summary>
    /// Returns available skill types for a specific connection.
    /// </summary>
    Task<IReadOnlyList<AISkillDescriptor>> GetAvailableSkillTypesAsync(
        Guid connectionId, CancellationToken ct);

    /// <summary>
    /// Returns the settings schema for a skill type on a specific connection.
    /// </summary>
    Task<AIEditableModelSchema?> GetSkillSettingsSchemaAsync(
        Guid connectionId, string skillTypeId, CancellationToken ct);
}
```

**New file:** `Umbraco.AI/src/Umbraco.AI.Core/Skills/AISkillService.cs`

Implementation:
- Injects `IAISkillRepository`, `IAIConnectionService`, `IBackOfficeSecurityAccessor`
- `SaveSkillAsync`: validates connection exists, validates connection's provider supports skills feature, validates skillTypeId is in the provider's available skills
- `GetAvailableSkillTypesAsync()`: iterates active connections → `GetConfiguredProviderAsync` → checks for `IAISkillsFeature` on each capability → aggregates descriptors
- `GetAvailableSkillTypesAsync(connectionId)`: single connection variant
- `GetSkillSettingsSchemaAsync`: resolves connection → gets configured provider → finds skills feature → calls `GetSkillSettingsSchema`

### 3.3 Skill Repository Interface

**New file:** `Umbraco.AI/src/Umbraco.AI.Core/Skills/IAISkillRepository.cs`

```csharp
public interface IAISkillRepository
{
    Task<AISkill?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<AISkill?> GetByAliasAsync(string alias, CancellationToken ct);
    Task<IEnumerable<AISkill>> GetAllAsync(CancellationToken ct);
    Task<(IEnumerable<AISkill> Items, int Total)> GetPagedAsync(
        string? filter, int skip, int take, CancellationToken ct);
    Task<AISkill> SaveAsync(AISkill skill, Guid? userId = null,
        CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task<bool> AliasExistsAsync(string alias, Guid? excludeId, CancellationToken ct);
}
```

---

## Phase 4: Skills Persistence

### 4.1 Database Entity

**New file:** `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillEntity.cs`

```csharp
internal class AISkillEntity
{
    public Guid Id { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string SkillTypeId { get; set; } = string.Empty;
    public Guid ConnectionId { get; set; }
    public string? Settings { get; set; }         // JSON string
    public string? ToolScopeIds { get; set; }     // Comma-delimited
    public int Version { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ModifiedByUserId { get; set; }
}
```

### 4.2 DbContext Update

**Modify:** `Umbraco.AI/src/Umbraco.AI.Persistence/UmbracoAIDbContext.cs`

```csharp
internal DbSet<AISkillEntity> Skills { get; set; } = null!;

// In OnModelCreating:
modelBuilder.Entity<AISkillEntity>(entity =>
{
    entity.ToTable("umbracoAISkill");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Alias).HasMaxLength(100).IsRequired();
    entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
    entity.Property(e => e.Description).HasMaxLength(2000);
    entity.Property(e => e.SkillTypeId).HasMaxLength(100).IsRequired();
    entity.HasIndex(e => e.Alias).IsUnique();
    entity.HasOne<AIConnectionEntity>()
        .WithMany()
        .HasForeignKey(e => e.ConnectionId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

### 4.3 Entity Factory

**New file:** `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/AISkillFactory.cs`

Static factory with three methods following the established pattern:
- `BuildDomain(AISkillEntity entity) → AISkill`
- `BuildEntity(AISkill skill) → AISkillEntity`
- `UpdateEntity(AISkillEntity entity, AISkill skill)`

Settings serialized/deserialized as JSON. ToolScopeIds stored as comma-delimited string.

### 4.4 Repository Implementation

**New file:** `Umbraco.AI/src/Umbraco.AI.Persistence/Skills/EfCoreAISkillRepository.cs`

Follows `EfCoreAIProfileRepository` patterns exactly:
- Uses `IEFCoreScopeProvider<UmbracoAIDbContext>`
- Scope-based query execution
- `SaveAsync` handles insert/update with version increment
- `GetPagedAsync` with filter on Name/Alias/Description
- Case-insensitive alias lookup

### 4.5 EF Core Migrations

Generate migrations in both persistence projects:

```bash
# SQL Server
dotnet ef migrations add UmbracoAI_AddSkills \
  --project Umbraco.AI/src/Umbraco.AI.Persistence.SqlServer \
  --startup-project Umbraco.AI/src/Umbraco.AI.Persistence.SqlServer \
  --context UmbracoAIDbContext

# SQLite
dotnet ef migrations add UmbracoAI_AddSkills \
  --project Umbraco.AI/src/Umbraco.AI.Persistence.Sqlite \
  --startup-project Umbraco.AI/src/Umbraco.AI.Persistence.Sqlite \
  --context UmbracoAIDbContext
```

### 4.6 DI Registration

**Modify:** `Umbraco.AI/src/Umbraco.AI.Persistence/Configuration/UmbracoBuilderExtensions.cs`

```csharp
builder.Services.AddSingleton<IAISkillRepository, EfCoreAISkillRepository>();
```

**Modify:** `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs`

```csharp
builder.Services.AddSingleton<IAISkillService, AISkillService>();
```

---

## Phase 5: Skills Management API

### 5.1 Controller Structure

```
Umbraco.AI.Web/Api/Management/Skill/
├── Controllers/
│   ├── SkillControllerBase.cs                       [route: "skill"]
│   ├── AllSkillController.cs                        [GET    /v1/skill]
│   ├── ByIdOrAliasSkillController.cs               [GET    /v1/skill/{idOrAlias}]
│   ├── CreateSkillController.cs                     [POST   /v1/skill]
│   ├── UpdateSkillController.cs                     [PUT    /v1/skill/{id}]
│   ├── DeleteSkillController.cs                     [DELETE /v1/skill/{id}]
│   ├── AliasExistsSkillController.cs               [GET    /v1/skill/{alias}/exists]
│   ├── AvailableSkillTypesController.cs            [GET    /v1/skill/available-types]
│   └── SkillTypeSettingsSchemaController.cs         [GET    /v1/skill/settings-schema]
├── Models/
│   ├── CreateSkillRequestModel.cs
│   ├── UpdateSkillRequestModel.cs
│   ├── SkillResponseModel.cs
│   ├── SkillItemResponseModel.cs
│   └── SkillTypeResponseModel.cs
└── Mapping/
    └── SkillMapDefinition.cs
```

### 5.2 Controller Base

```csharp
[UmbracoAIVersionedManagementApiRoute("skill")]
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public abstract class SkillControllerBase : ManagementApiControllerBase { }
```

### 5.3 Request/Response Models

**CreateSkillRequestModel:**
```csharp
public class CreateSkillRequestModel
{
    [Required] public required string Alias { get; init; }
    [Required] public required string Name { get; init; }
    public string? Description { get; init; }
    [Required] public required string SkillTypeId { get; init; }
    [Required] public required Guid ConnectionId { get; init; }
    public object? Settings { get; init; }
    public IReadOnlyList<string> ToolScopeIds { get; init; } = [];
    public bool IsActive { get; init; } = true;
}
```

**UpdateSkillRequestModel:**
```csharp
public class UpdateSkillRequestModel
{
    [Required] public required string Alias { get; init; }
    [Required] public required string Name { get; init; }
    public string? Description { get; init; }
    [Required] public required Guid ConnectionId { get; init; }
    public object? Settings { get; init; }
    public IReadOnlyList<string> ToolScopeIds { get; init; } = [];
    public bool IsActive { get; init; }
}
```

Note: `SkillTypeId` is not in update — it's immutable after creation.

**SkillResponseModel:**
```csharp
public class SkillResponseModel
{
    public Guid Id { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SkillTypeId { get; set; } = string.Empty;
    public Guid ConnectionId { get; set; }
    public object? Settings { get; set; }
    public IReadOnlyList<string> ToolScopeIds { get; set; } = [];
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
}
```

**SkillItemResponseModel** (for collection/list):
```csharp
public class SkillItemResponseModel
{
    public Guid Id { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SkillTypeId { get; set; } = string.Empty;
    public Guid ConnectionId { get; set; }
    public bool IsActive { get; set; }
    public DateTime DateModified { get; set; }
}
```

**SkillTypeResponseModel** (for available skill types):
```csharp
public class SkillTypeResponseModel
{
    public string SkillTypeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
}
```

### 5.4 Key Endpoints

**GET /v1/skill/available-types?connectionId={optional}**
- Without `connectionId`: aggregates skill types from all connections with skills feature
- With `connectionId`: returns skill types from that specific connection
- Returns `IEnumerable<SkillTypeResponseModel>`

**GET /v1/skill/settings-schema?connectionId={required}&skillTypeId={required}**
- Returns the `EditableModelSchemaModel` for the skill type's configuration
- Used by the frontend to render the dynamic settings form

---

## Phase 6: Skills Management UI (Frontend)

### 6.1 Directory Structure

```
Client/src/skill/
├── constants.ts
├── types.ts
├── type-mapper.ts
├── paths.ts
├── manifests.ts
├── collection/
│   ├── manifests.ts
│   ├── skill-collection.element.ts
│   ├── views/table/
│   │   └── skill-table-collection-view.element.ts
│   ├── action/
│   │   └── manifests.ts
│   └── bulk-action/
│       └── manifests.ts
├── workspace/
│   ├── manifests.ts
│   ├── skill/
│   │   ├── manifests.ts
│   │   ├── skill-workspace.context.ts
│   │   ├── skill-workspace-editor.element.ts
│   │   └── views/
│   │       ├── skill-details-workspace-view.element.ts
│   │       └── skill-info-workspace-view.element.ts
│   └── skill-root/
│       └── manifests.ts
├── repository/
│   ├── manifests.ts
│   ├── collection/
│   │   └── skill-collection.server-data-source.ts
│   ├── detail/
│   │   └── skill-detail.server-data-source.ts
│   └── skill-types/
│       └── skill-types.server-data-source.ts
├── entity-actions/
│   └── manifests.ts
├── menu/
│   └── manifests.ts
└── modals/
    └── skill-create-options/
        ├── skill-create-options-modal.element.ts
        └── manifests.ts
```

### 6.2 Constants

```typescript
export const UAI_SKILL_ENTITY_TYPE = "uai-skill";
export const UAI_SKILL_ROOT_ENTITY_TYPE = "uai-skill-root";
export const UAI_SKILL_WORKSPACE_ALIAS = "UmbracoAI.Workspace.Skill";
export const UAI_SKILL_ROOT_WORKSPACE_ALIAS = "UmbracoAI.Workspace.Skill.Root";
export const UAI_SKILL_COLLECTION_ALIAS = "UmbracoAI.Collection.Skill";
export const UAI_SKILL_ICON = "icon-wand";
```

### 6.3 Frontend Models

```typescript
export interface UaiSkillDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    skillTypeId: string;
    connectionId: string;
    settings: Record<string, unknown> | null;
    toolScopeIds: string[];
    isActive: boolean;
    version: number;
    dateCreated: string | null;
    dateModified: string | null;
}

export interface UaiSkillItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    skillTypeId: string;
    connectionId: string;
    isActive: boolean;
    dateModified: string | null;
}

export interface UaiSkillTypeModel {
    skillTypeId: string;
    name: string;
    description: string | null;
    icon: string | null;
}
```

### 6.4 Collection View (List)

**skill-table-collection-view.element.ts** — table columns:

| Column | Content |
|--------|---------|
| Name | Link to edit workspace |
| Type | Skill type name (from descriptor) |
| Connection | Resolved via `uaiWithConnection` directive |
| Status | Active/Inactive `<uui-tag>` |
| Modified | Formatted date |

### 6.5 Workspace Editor

**skill-workspace-editor.element.ts** — header:
- Back button to skill collection
- Name input with alias lock (auto-generate alias from name)
- Active/Inactive status selector

**skill-details-workspace-view.element.ts** — details tab:
- **Description** — `<uui-textarea>` for skill description
- **Connection** — Connection picker, filtered to connections whose providers have the skills feature. On change: reloads available skill types and clears settings.
- **Skill Type** — Dropdown populated from `GET /v1/skill/available-types?connectionId=...`. Disabled until connection is selected. On change: loads settings schema and resets settings.
- **Settings** — `<uai-model-editor>` driven by schema from `GET /v1/skill/settings-schema?connectionId=...&skillTypeId=...`. Empty/hidden if skill type has no configurable settings.
- **Tool Scopes** — `<uai-tool-scope-picker>` multi-select for permission grouping

**skill-info-workspace-view.element.ts** — info tab (read-only):
- Version history (`uai-version-history`)
- Id (with "Unsaved" tag if new)
- Date created, date modified
- Connection name
- Skill type name

### 6.6 Create Flow

1. User clicks "Create" on skill collection
2. **Skill create options modal** opens:
   - Step 1: Select a connection (only connections with skills feature shown)
   - Step 2: Select a skill type (loaded from selected connection)
3. User selects → navigated to create workspace with `connectionId` and `skillTypeId` pre-populated
4. User fills name, alias, description, settings
5. Save creates the skill

This mirrors the connection create flow (which opens a provider picker modal first).

### 6.7 Menu Registration

```typescript
{
    type: "menuItem",
    kind: "entityContainer",
    alias: "UmbracoAI.MenuItem.Skills",
    weight: 70,  // Below Connections (90), Profiles (80)
    meta: {
        label: "Skills",
        icon: UAI_SKILL_ICON,
        entityType: UAI_SKILL_ROOT_ENTITY_TYPE,
        childEntityTypes: [UAI_SKILL_ENTITY_TYPE],
        menus: [UAI_CORE_MENU_ALIAS],
    },
}
```

### 6.8 Manifest Aggregation

**Modify:** `Client/src/manifests.ts` — add `...skillManifests`
**Modify:** `Client/src/index.ts` — add skill re-exports
**Modify:** `Client/src/exports.ts` — add public skill type exports

---

## Phase 7: OpenAPI Client Regeneration

After the Management API is built and the demo site runs:

1. Start demo site: `/demo-site-management start`
2. Generate client: `npm run generate-client`
3. This produces `SkillsService` in `api/sdk.gen.ts`

---

## Phase 8: OpenAI Provider — Skills Feature Implementation

### 8.1 Skills Feature

**New file:** `Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/Skills/OpenAISkillsFeature.cs`

```csharp
public class OpenAISkillsFeature : IAISkillsFeature
{
    public string FeatureId => "skills";

    public Task<IReadOnlyList<AISkillDescriptor>> GetAvailableSkillsAsync(
        object? settings, CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<AISkillDescriptor>>(new List<AISkillDescriptor>
        {
            new()
            {
                SkillTypeId = "web-search",
                Name = "Web Search",
                Description = "Search the web for current information",
                Icon = "icon-globe",
            },
            new()
            {
                SkillTypeId = "code-interpreter",
                Name = "Code Interpreter",
                Description = "Execute Python code in a sandboxed environment",
                Icon = "icon-code",
            },
            new()
            {
                SkillTypeId = "image-generation",
                Name = "Image Generation",
                Description = "Generate images using DALL-E",
                Icon = "icon-picture",
            },
        });
    }

    public AIEditableModelSchema? GetSkillSettingsSchema(string skillTypeId)
    {
        return skillTypeId switch
        {
            "web-search" => BuildWebSearchSchema(),
            _ => null,
        };
    }

    private static AIEditableModelSchema BuildWebSearchSchema()
    {
        // Return schema with fields like "searchContextSize" (low/medium/high)
        // and "userLocation" for geographic relevance
        // Details TBD based on OpenAI's actual API parameters
    }
}
```

### 8.2 Register on Chat Capability

**Modify:** `Umbraco.AI.OpenAI/src/Umbraco.AI.OpenAI/OpenAIChatCapability.cs`

```csharp
public class OpenAIChatCapability : AIChatCapabilityBase<OpenAIProviderSettings>
{
    public OpenAIChatCapability(OpenAIProvider provider) : base(provider)
    {
        WithFeature(new OpenAISkillsFeature());
    }
    // ... existing code unchanged
}
```

---

## Implementation Order

| Step | Phase | Description | Depends On |
|------|-------|-------------|------------|
| 1 | 1.1-1.2 | Feature marker interface + capability base class changes | — |
| 2 | 1.3 | Configured capability feature pass-through | 1 |
| 3 | 1.4-1.5 | Provider-level feature aggregation + API exposure | 1, 2 |
| 4 | 2 | Skills feature interface + descriptor | 1 |
| 5 | 3 | Skill domain model + service interface + implementation | 4 |
| 6 | 4.1-4.4 | Persistence entity, factory, repository | 5 |
| 7 | 4.5 | EF Core migrations (both SqlServer + Sqlite) | 6 |
| 8 | 4.6 | DI registration | 5, 6 |
| 9 | 5 | Management API controllers + models + mapping | 5, 8 |
| 10 | 7 | OpenAPI client generation | 9 |
| 11 | 6.1-6.3 | Frontend: constants, types, type-mapper | 10 |
| 12 | 6.4 | Frontend: repository + data sources | 11 |
| 13 | 6.5 | Frontend: collection (list view) | 12 |
| 14 | 6.6-6.7 | Frontend: workspace (create/edit) | 12 |
| 15 | 6.8-6.9 | Frontend: menu + manifest registration | 13, 14 |
| 16 | 8 | OpenAI provider skills feature | 4 |

Steps 1-3 can be a single commit. Steps 5-8 can be combined. Step 16 is independent after step 4.

---

## Files Summary

### Modified (Existing)

| File | Changes |
|------|---------|
| `Umbraco.AI.Core/Providers/IAICapability.cs` | Add feature methods to interface + base classes |
| `Umbraco.AI.Core/Providers/IAIConfiguredCapability.cs` | Add feature methods |
| `Umbraco.AI.Core/Providers/AIConfiguredCapability.cs` | Delegate feature methods to inner |
| `Umbraco.AI.Core/Providers/IAIProvider.cs` | Add feature aggregation methods |
| `Umbraco.AI.Core/Providers/AIProviderBase.cs` | Implement feature aggregation |
| `Umbraco.AI.Core/Providers/IAIConfiguredProvider.cs` | Add feature methods |
| `Umbraco.AI.Core/Providers/AIConfiguredProvider.cs` | Implement feature delegation |
| `Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs` | Register `AISkillService` |
| `Umbraco.AI.Persistence/UmbracoAIDbContext.cs` | Add `Skills` DbSet + configuration |
| `Umbraco.AI.Persistence/Configuration/UmbracoBuilderExtensions.cs` | Register `EfCoreAISkillRepository` |
| `Umbraco.AI.Web/Api/Management/Provider/Models/ProviderResponseModel.cs` | Add `Features` property |
| `Umbraco.AI.Web/Api/Management/Provider/Controllers/*.cs` | Map features |
| `Umbraco.AI.OpenAI/OpenAIChatCapability.cs` | Register skills feature |
| `Client/src/manifests.ts` | Add skill manifests |
| `Client/src/index.ts` | Add skill re-exports |
| `Client/src/exports.ts` | Add public skill exports |

### Created (New)

| File | Purpose |
|------|---------|
| `Umbraco.AI.Core/Providers/Features/IAIFeature.cs` | Feature marker interface |
| `Umbraco.AI.Core/Skills/AISkillDescriptor.cs` | Skill type descriptor |
| `Umbraco.AI.Core/Skills/IAISkillsFeature.cs` | Skills feature interface |
| `Umbraco.AI.Core/Skills/AISkill.cs` | Domain model |
| `Umbraco.AI.Core/Skills/IAISkillService.cs` | Service interface |
| `Umbraco.AI.Core/Skills/AISkillService.cs` | Service implementation |
| `Umbraco.AI.Core/Skills/IAISkillRepository.cs` | Repository interface |
| `Umbraco.AI.Persistence/Skills/AISkillEntity.cs` | DB entity |
| `Umbraco.AI.Persistence/Skills/AISkillFactory.cs` | Entity-domain mapper |
| `Umbraco.AI.Persistence/Skills/EfCoreAISkillRepository.cs` | Repository impl |
| Migrations (SqlServer + Sqlite) | `umbracoAISkill` table |
| `Umbraco.AI.Web/Api/Management/Skill/Controllers/*.cs` | ~8 controller files |
| `Umbraco.AI.Web/Api/Management/Skill/Models/*.cs` | ~5 model files |
| `Umbraco.AI.Web/Api/Management/Skill/Mapping/SkillMapDefinition.cs` | Mapping |
| `Client/src/skill/**/*` | ~25 frontend files |
| `Umbraco.AI.OpenAI/Skills/OpenAISkillsFeature.cs` | OpenAI implementation |

---

## Commit Strategy

Following conventional commits:

1. `feat(core): Add features extensibility layer to provider system`
2. `feat(core): Add skills feature interface and domain model`
3. `feat(core): Add skills persistence layer with migrations`
4. `feat(core): Add skills management API`
5. `feat(frontend): Add skills management UI`
6. `feat(openai): Add skills feature to OpenAI chat capability`

---

## Testing Strategy

- **Unit tests** for `AISkillService` (validation, CRUD logic, feature resolution)
- **Unit tests** for feature discovery on capabilities/providers (TryGetFeature, HasFeature)
- **Integration tests** for persistence (save/load/delete round-trips)
- **Manual testing** via demo site for full UI flow (create, edit, list, delete skills)

---

## Not In Scope (Future Work)

- **Agent skill assignment** — Adding `SkillIds` to `AIAgent` and a skills tab in the agent workspace. This is a natural next step but separate from the core skills management feature.
- **Runtime skill injection** — Chat middleware that injects skill references into provider requests at execution time.
- **Anthropic/Google/Amazon provider implementations** — Only OpenAI is implemented initially.
- **Skill file bundles** — The earlier SKILL.md manifest + file upload concept. Skills here are configuration entities, not file bundles.
- **Skill versioning** — Local version tracking. Currently just tracks `Version` for optimistic concurrency.
