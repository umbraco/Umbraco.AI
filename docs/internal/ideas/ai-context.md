# AI Context - Future Consideration

## Status: Under Consideration

This document explores **AI Context**, a system for attaching contextual resources (brand voice, documents, reference materials) that automatically enrich all AI operations. Inspired by [Perplex AI ContentBuddy](https://marketplace.umbraco.com/package/perplex.ai.contentbuddy)'s centralized tone of voice feature.

---

## The Idea

AI Context provides a centralized place to define *how* AI should behave when generating content. Instead of repeating brand guidelines in every prompt, you define them once and they're automatically injected into all AI operations.

**Core Concept**: Define once, apply everywhere.

```
AI Context (standalone, reusable)
├── Name
├── Alias                                    ← For referencing
└── Resources[]                              ← Generic, extensible
    ├── BrandVoiceResource                   ← Structured tone/audience/style
    ├── DocumentResource                     ← Attached files (style guides, etc.)
    ├── ExternalLinkResource                 ← URLs with cached content
    └── TextResource                         ← Free-form instructions

Context can be assigned to:
├── Content nodes (via property editor, inherits down tree)
├── Profiles (default baseline for the profile)
├── Prompts (task-specific guidance)
└── Agents (specialized expertise)
```

---

## Why AI Context?

Without AI Context, every AI operation needs to include brand guidelines:

```
❌ Without Context:
"Write a meta description. Be professional but approachable.
Target B2B decision makers. Avoid jargon. Here's our style guide..."

Every. Single. Time.
```

With AI Context, the guidelines are injected automatically:

```
✅ With Context:
"Write a meta description for: {content}"

→ System automatically adds brand voice, audience, and reference materials
```

---

## Key Design Decisions

### 1. Contexts are Standalone, Reusable Entities

AI Contexts are defined independently and can be assigned to multiple things. They don't "own" content - instead, content (and profiles, prompts, agents) reference them.

```
AI Contexts (defined in settings):
├── Corporate Brand Voice
├── Consumer Brand Voice
├── SEO Guidelines
├── Legal Compliance
└── Accessibility Standards
```

### 2. Multi-Level Context Assignment

Context can be assigned at four levels, each serving a different purpose:

| Level | How Assigned | Purpose | Example |
|-------|--------------|---------|---------|
| **Content** | Property editor on content nodes | Site/section brand voice | "Corporate Site uses Corporate Brand Voice" |
| **Profile** | UI picker in profile settings | Default baseline for profile | "Content Writing profile includes Brand Basics" |
| **Prompt** | UI picker in prompt settings | Task-specific guidance | "SEO Meta Description prompt uses SEO Guidelines" |
| **Agent** | UI picker in agent settings | Specialized expertise | "Legal Reviewer agent uses Legal Compliance" |

### 3. Content Context Inheritance

Content nodes inherit context from ancestors, allowing section-level overrides:

```
Content Tree:
├── Corporate Site                    ← Corporate Brand Voice (assigned)
│   ├── Products                      ← (inherits Corporate)
│   ├── Blog                          ← Casual Blog Voice (override)
│   │   ├── Post 1                    ← (inherits Casual Blog)
│   │   └── Post 2                    ← (inherits Casual Blog)
│   └── Legal                         ← Legal Tone (override)
├── Consumer Brand                    ← Consumer Brand Voice (assigned)
│   └── ...
└── Partner Portal                    ← (no assignment, uses global default)
```

**Resolution**: Walk up the content tree until a node with assigned context is found, or fall back to global default.

### 4. Multiple Contexts per Assignment

Profiles, Prompts, and Agents can have multiple contexts assigned, allowing composition:

```
Agent: "Content Optimizer"
Assigned contexts:
├── SEO Guidelines           ← How to optimize for search
├── Accessibility Standards  ← How to ensure accessibility
└── Performance Tips         ← How to write efficient content
```

### 5. Context Merge Order

When multiple contexts apply, they're merged in order from broadest to most specific:

```
Merge order (later entries override for conflicts):
1. Profile context(s)     ← Default baseline
2. Agent context(s)       ← Specialized expertise
3. Prompt context(s)      ← Task-specific guidance
4. Content context        ← Brand voice (final authority)
```

All resources are collected and injected. For conflicting guidance (e.g., two different tone descriptions), the more specific level wins.

### 6. Pluggable Resource Type System

Resource types are **pluggable** - developers can define custom resource types beyond the built-in ones. Each resource type consists of:

1. **Resource Type Definition** - Metadata, schema, validation
2. **Resource Formatter** - Converts resource data to text for AI injection
3. **UI Editor** - Frontend component for editing the resource

```
Resource Type Plugin:
├── Definition (C#)
│   ├── Alias ("brand-voice")
│   ├── Name ("Brand Voice")
│   ├── Description
│   ├── Icon
│   └── Data schema (JSON Schema or C# type)
├── Formatter (C#)
│   └── Format(data) → string for AI injection
└── UI Editor (TypeScript/Lit)
    └── Custom editor component for backoffice
```

**Built-in Resource Types:**
```
├── brand-voice     → Structured tone, audience, style, avoid patterns
├── document        → Attached files (style guides, brand books)
├── external-link   → URLs with periodically cached content
└── text            → Free-form additional instructions
```

**Custom Resource Types (examples):**
```
├── rag-collection  → Reference to vector store for retrieval
├── mcp-server      → MCP server connection for dynamic context
├── glossary        → Term definitions for consistent language
└── competitor-info → Competitor details to differentiate against
```

### 7. Automatic Injection

AI Context is automatically injected into all AI operations - Prompts, Workflows, and Agents - without developers needing to handle it explicitly.

### 8. Property Constraints Are Schema Concerns

Property-specific hints (e.g., "max 160 chars for meta descriptions") are **not** part of AI Context. These are schema-level concerns that belong with content type/property editor configuration.

AI Context provides *brand and editorial guidance* (tone, voice, reference materials), not *field validation rules*. Property constraints can be inferred from existing Umbraco property editor configuration (max length, validation regex, JSON schema) - that's a separate concern.

---

## Data Model

### Core Context Model

```csharp
public sealed class AiContext
{
    public Guid Id { get; internal set; }
    public required string Alias { get; init; }         // "corporate-brand-voice" (immutable)
    public required string Name { get; set; }           // "Corporate Brand Voice"
    public bool IsGlobalDefault { get; set; }           // True for the fallback context
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    // Resources (generic, extensible)
    public IList<AiContextResource> Resources { get; set; } = [];
}

public sealed class AiContextResource
{
    public Guid Id { get; internal set; }
    public required string ResourceType { get; init; }  // "brand-voice", "document", "text" (immutable)
    public required string Name { get; set; }           // "Brand Guidelines", "Style Guide"
    public int SortOrder { get; set; }                  // Controls injection order
    public required string Data { get; set; }           // JSON blob for type-specific data
}
```

### Context Assignments

Context can be assigned to content, profiles, prompts, and agents:

```csharp
// Content → Context (stored separately, used by property editor)
public class AiContentContextAssignment
{
    public Guid ContentId { get; set; }                 // Umbraco content node
    public Guid ContextId { get; set; }                 // Assigned context
}

// Profile → Context(s) (on AiProfile entity)
public class AiProfile
{
    // ... existing profile properties ...
    public IList<Guid> ContextIds { get; set; } = [];   // Multiple contexts allowed
}

// Prompt → Context(s) (on AiPrompt entity in Umbraco.Ai.Prompt)
public class AiPrompt
{
    // ... existing prompt properties ...
    public IList<Guid> ContextIds { get; set; } = [];   // Multiple contexts allowed
}

// Agent → Context(s) (on AiAgent entity in Umbraco.Ai.Agent)
public class AiAgent
{
    // ... existing agent properties ...
    public IList<Guid> ContextIds { get; set; } = [];   // Multiple contexts allowed
}
```

### Resource Type Schemas

Each resource type has its own JSON schema for the `Data` field:

```csharp
// brand-voice
public class BrandVoiceResourceData
{
    public string? ToneDescription { get; set; }        // "Professional but approachable"
    public string? TargetAudience { get; set; }         // "B2B tech decision makers"
    public string? StyleGuidelines { get; set; }        // "Use active voice, be concise"
    public string? AvoidPatterns { get; set; }          // "Jargon, exclamation marks"
}

// document
public class DocumentResourceData
{
    public Guid? MediaId { get; set; }                  // Reference to Umbraco media item
    public string? Content { get; set; }                // Extracted/uploaded text content
    public string? MimeType { get; set; }               // "application/pdf", "text/plain"
    public string? Description { get; set; }            // What this document is about
}

// external-link
public class ExternalLinkResourceData
{
    public string Url { get; set; }                     // "https://brand.example.com/guidelines"
    public string? CachedContent { get; set; }          // Periodically fetched content
    public DateTime? LastFetched { get; set; }          // When cache was last updated
    public string? Description { get; set; }            // What this link provides
}

// text
public class TextResourceData
{
    public string Content { get; set; }                 // Free-form instructions
}
```

### Resource Type Plugin System

Resource types are discovered and registered using Umbraco's collection builder pattern, following the same approach used for providers and tools:

```csharp
// Resource type definition attribute (mirrors AiProviderAttribute, AiToolAttribute)
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AiContextResourceTypeAttribute : Attribute
{
    public string Alias { get; }
    public string Name { get; }

    public AiContextResourceTypeAttribute(string alias, string name)
    {
        Alias = alias;
        Name = name;
    }
}

// Infrastructure for resource types (mirrors IAiProviderInfrastructure)
public interface IAiContextResourceTypeInfrastructure
{
    IJsonSerializer JsonSerializer { get; }
    ILogger Logger { get; }
}

// Interface for the collection
public interface IAiContextResourceType
{
    string Alias { get; }
    string Name { get; }
    string Description { get; }
    string Icon { get; }
    Type DataType { get; }
    string Format(object data);
    ValidationResult Validate(object data);
}

// Base class for resource type definitions (mirrors AiProviderBase pattern)
public abstract class AiContextResourceTypeBase<TData> : IAiContextResourceType
    where TData : class
{
    protected readonly IAiContextResourceTypeInfrastructure Infrastructure;

    protected AiContextResourceTypeBase(IAiContextResourceTypeInfrastructure infrastructure)
    {
        Infrastructure = infrastructure;

        var attribute = GetType().GetCustomAttribute<AiContextResourceTypeAttribute>(inherit: false);
        if (attribute == null)
            throw new InvalidOperationException(
                $"Resource type '{GetType().FullName}' is missing AiContextResourceTypeAttribute.");

        Alias = attribute.Alias;
        Name = attribute.Name;
    }

    public string Alias { get; }
    public string Name { get; }
    public abstract string Description { get; }
    public abstract string Icon { get; }              // Umbraco icon alias
    public Type DataType => typeof(TData);

    // Formatting for AI injection
    public abstract string Format(TData data);

    // Validation
    public virtual ValidationResult Validate(TData data) => ValidationResult.Success;

    // Interface implementation with safe casting
    string IAiContextResourceType.Format(object data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Format((TData)data);
    }

    ValidationResult IAiContextResourceType.Validate(object data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Validate((TData)data);
    }
}
```

**Built-in Resource Type Example:**

```csharp
[AiContextResourceType("brand-voice", "Brand Voice")]
public class BrandVoiceResourceType : AiContextResourceTypeBase<BrandVoiceResourceData>
{
    public BrandVoiceResourceType(IAiContextResourceTypeInfrastructure infrastructure)
        : base(infrastructure) { }

    public override string Description => "Define tone, audience, style guidelines, and patterns to avoid";
    public override string Icon => "icon-voice";

    public override string Format(BrandVoiceResourceData data)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(data.ToneDescription))
            sb.AppendLine($"Tone: {data.ToneDescription}");
        if (!string.IsNullOrEmpty(data.TargetAudience))
            sb.AppendLine($"Audience: {data.TargetAudience}");
        if (!string.IsNullOrEmpty(data.StyleGuidelines))
            sb.AppendLine($"Style: {data.StyleGuidelines}");
        if (!string.IsNullOrEmpty(data.AvoidPatterns))
            sb.AppendLine($"Avoid: {data.AvoidPatterns}");

        return sb.ToString();
    }
}
```

**Custom Resource Type Example:**

```csharp
// Custom resource type for glossary terms
[AiContextResourceType("glossary", "Glossary")]
public class GlossaryResourceType : AiContextResourceTypeBase<GlossaryResourceData>
{
    public GlossaryResourceType(IAiContextResourceTypeInfrastructure infrastructure)
        : base(infrastructure) { }

    public override string Description => "Define terms and their approved definitions for consistent language";
    public override string Icon => "icon-book";

    public override string Format(GlossaryResourceData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Glossary - Use these terms consistently:");
        foreach (var term in data.Terms)
        {
            sb.AppendLine($"- {term.Term}: {term.Definition}");
        }
        return sb.ToString();
    }
}

public class GlossaryResourceData
{
    public IList<GlossaryTerm> Terms { get; set; } = [];
}

public class GlossaryTerm
{
    public required string Term { get; set; }
    public required string Definition { get; set; }
}
```

**Collection and Collection Builder:**

```csharp
// Collection (injected via DI for runtime access)
public class AiContextResourceTypeCollection : BuilderCollectionBase<IAiContextResourceType>
{
    public AiContextResourceTypeCollection(Func<IEnumerable<IAiContextResourceType>> items)
        : base(items) { }

    public IAiContextResourceType? GetByAlias(string alias)
        => this.FirstOrDefault(t => t.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
}

// Collection Builder (used in Composers for registration)
public class AiContextResourceTypeCollectionBuilder
    : LazyCollectionBuilderBase<AiContextResourceTypeCollectionBuilder, AiContextResourceTypeCollection, IAiContextResourceType>
{
    protected override AiContextResourceTypeCollectionBuilder This => this;
}
```

**Auto-Discovery Registration (in UmbracoBuilderExtensions.cs):**

```csharp
public static IUmbracoBuilder AddUmbracoAi(this IUmbracoBuilder builder)
{
    // ... existing registrations ...

    // Auto-discover resource types via attribute (mirrors provider/tool discovery)
    builder.AiContextResourceTypes()
        .Add(() => builder.TypeLoader.GetTypesWithAttribute<IAiContextResourceType, AiContextResourceTypeAttribute>(cache: true));

    return builder;
}

// Extension method for collection builder access
public static AiContextResourceTypeCollectionBuilder AiContextResourceTypes(this IUmbracoBuilder builder)
    => builder.WithCollectionBuilder<AiContextResourceTypeCollectionBuilder>();
```

**Manual Registration via Composer (for custom types):**

```csharp
public class MyAiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Add custom resource types (built-in types are auto-discovered)
        builder.AiContextResourceTypes()
            .Add<GlossaryResourceType>()
            .Add<CompetitorInfoResourceType>();

        // Or exclude unwanted built-in types
        builder.AiContextResourceTypes()
            .Exclude<ExternalLinkResourceType>();
    }
}
```

### Frontend UI Registration

Each resource type needs a corresponding UI editor in the backoffice. This uses Umbraco's manifest system:

```typescript
// Resource type UI manifest (umbraco-package.json)
{
  "name": "My AI Resources",
  "extensions": [
    {
      "type": "aiContextResourceEditor",
      "alias": "My.AiContextResourceEditor.Glossary",
      "name": "Glossary Resource Editor",
      "meta": {
        "resourceType": "glossary",
        "label": "Glossary",
        "description": "Define terms for consistent language",
        "icon": "icon-book"
      },
      "element": "./editors/glossary-resource-editor.js"
    }
  ]
}
```

```typescript
// glossary-resource-editor.ts
import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';

@customElement('glossary-resource-editor')
export class GlossaryResourceEditor extends LitElement {
    @property({ type: Object })
    data: GlossaryResourceData = { terms: [] };

    render() {
        return html`
            <div class="glossary-editor">
                <h4>Glossary Terms</h4>
                ${this.data.terms.map((term, index) => html`
                    <div class="term-row">
                        <input
                            .value=${term.term}
                            @input=${(e) => this.updateTerm(index, 'term', e.target.value)}
                            placeholder="Term"
                        />
                        <input
                            .value=${term.definition}
                            @input=${(e) => this.updateTerm(index, 'definition', e.target.value)}
                            placeholder="Definition"
                        />
                        <button @click=${() => this.removeTerm(index)}>×</button>
                    </div>
                `)}
                <button @click=${this.addTerm}>+ Add Term</button>
            </div>
        `;
    }

    private updateTerm(index: number, field: string, value: string) {
        this.data.terms[index][field] = value;
        this.dispatchEvent(new CustomEvent('change', { detail: this.data }));
    }

    private addTerm() {
        this.data.terms = [...this.data.terms, { term: '', definition: '' }];
        this.requestUpdate();
    }

    private removeTerm(index: number) {
        this.data.terms = this.data.terms.filter((_, i) => i !== index);
        this.dispatchEvent(new CustomEvent('change', { detail: this.data }));
    }
}
```

### Context Formatter

The formatter uses the resource type collection to convert resolved resources to AI-injectable text:

```csharp
public interface IAiContextFormatter
{
    string Format(ResolvedAiContext context);
}

// Implementation uses the collection (injected via DI)
public class AiContextFormatter : IAiContextFormatter
{
    private readonly AiContextResourceTypeCollection _resourceTypes;

    public AiContextFormatter(AiContextResourceTypeCollection resourceTypes)
    {
        _resourceTypes = resourceTypes;
    }

    public string Format(ResolvedAiContext context)
    {
        if (context.Resources.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("--- Context ---");

        foreach (var resource in context.Resources)
        {
            var resourceType = _resourceTypes.GetByAlias(resource.ResourceType);
            if (resourceType is not null)
            {
                var formatted = resourceType.Format(resource.Data);
                if (!string.IsNullOrEmpty(formatted))
                {
                    sb.AppendLine($"[{resource.Name}]");
                    sb.AppendLine(formatted);
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
    }
}
```

---

## Context Resolution

When an AI operation executes, the system resolves and merges contexts from all applicable levels:

```csharp
public class AiContextResolver
{
    public async Task<ResolvedAiContext> ResolveAsync(
        AiContextResolutionRequest request,
        CancellationToken ct = default)
    {
        var allResources = new List<ResolvedResource>();
        var sourceContexts = new List<ContextSource>();

        // 1. Profile context(s) - broadest baseline
        if (request.ProfileId.HasValue)
        {
            var profile = await _profileRepository.GetByIdAsync(request.ProfileId.Value, ct);
            if (profile?.ContextIds.Any() == true)
            {
                foreach (var contextId in profile.ContextIds)
                {
                    var context = await _contextRepository.GetByIdAsync(contextId, ct);
                    if (context != null)
                    {
                        allResources.AddRange(ResolveResources(context));
                        sourceContexts.Add(new("Profile", profile.Name, context.Name));
                    }
                }
            }
        }

        // 2. Agent context(s) - specialized expertise
        if (request.AgentContextIds?.Any() == true)
        {
            foreach (var contextId in request.AgentContextIds)
            {
                var context = await _contextRepository.GetByIdAsync(contextId, ct);
                if (context != null)
                {
                    allResources.AddRange(ResolveResources(context));
                    sourceContexts.Add(new("Agent", request.AgentName, context.Name));
                }
            }
        }

        // 3. Prompt context(s) - task-specific guidance
        if (request.PromptContextIds?.Any() == true)
        {
            foreach (var contextId in request.PromptContextIds)
            {
                var context = await _contextRepository.GetByIdAsync(contextId, ct);
                if (context != null)
                {
                    allResources.AddRange(ResolveResources(context));
                    sourceContexts.Add(new("Prompt", request.PromptName, context.Name));
                }
            }
        }

        // 4. Content context - walk up tree, most specific (final authority)
        if (request.ContentId.HasValue)
        {
            var contentContext = await ResolveContentContextAsync(request.ContentId.Value, ct);
            if (contentContext != null)
            {
                allResources.AddRange(ResolveResources(contentContext));
                sourceContexts.Add(new("Content", request.ContentPath, contentContext.Name));
            }
        }

        // 5. Global default if nothing found
        if (allResources.Count == 0)
        {
            var global = await _contextRepository.GetGlobalDefaultAsync(ct);
            if (global != null)
            {
                allResources.AddRange(ResolveResources(global));
                sourceContexts.Add(new("Global", null, global.Name));
            }
        }

        return new ResolvedAiContext
        {
            Resources = allResources,
            Sources = sourceContexts
        };
    }

    private async Task<AiContext?> ResolveContentContextAsync(Guid contentId, CancellationToken ct)
    {
        // Walk up the content tree looking for assigned context
        var currentId = contentId;
        while (currentId != Guid.Empty)
        {
            var assignment = await _assignmentRepository.GetByContentIdAsync(currentId, ct);
            if (assignment != null)
            {
                return await _contextRepository.GetByIdAsync(assignment.ContextId, ct);
            }

            // Move to parent
            var content = await _contentService.GetByIdAsync(currentId, ct);
            currentId = content?.ParentId ?? Guid.Empty;
        }

        return null;
    }
}

public class AiContextResolutionRequest
{
    public Guid? ContentId { get; set; }
    public string? ContentPath { get; set; }
    public Guid? ProfileId { get; set; }
    public IEnumerable<Guid>? PromptContextIds { get; set; }
    public string? PromptName { get; set; }
    public IEnumerable<Guid>? AgentContextIds { get; set; }
    public string? AgentName { get; set; }
}
```

### Resolved Context Model

```csharp
public class ResolvedAiContext
{
    public IReadOnlyList<ResolvedResource> Resources { get; set; } = [];
    public IReadOnlyList<ContextSource> Sources { get; set; } = [];  // For debugging/UI

    public static ResolvedAiContext Empty => new();

    // Convenience accessors
    public BrandVoiceResourceData? BrandVoice =>
        Resources.FirstOrDefault(r => r.ResourceType == "brand-voice")?.Data as BrandVoiceResourceData;

    public IEnumerable<DocumentResourceData> Documents =>
        Resources.Where(r => r.ResourceType == "document").Select(r => r.Data).Cast<DocumentResourceData>();
}

public class ResolvedResource
{
    public string ResourceType { get; set; }
    public string Name { get; set; }
    public object Data { get; set; }  // Typed based on ResourceType
    public string SourceLevel { get; set; }  // "Profile", "Agent", "Prompt", "Content"
}

public record ContextSource(string Level, string? EntityName, string ContextName);
```

---

## Repository Interfaces

Repositories follow the same patterns as `EfCoreAiProfileRepository` and `EfCoreAiConnectionRepository`, using `IEFCoreScopeProvider<UmbracoAiDbContext>` with explicit scope completion:

```csharp
public interface IAiContextRepository
{
    Task<AiContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AiContext?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);
    Task<AiContext?> GetGlobalDefaultAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AiContext>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<AiContext> Items, int Total)> GetPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);
    Task<AiContext> SaveAsync(AiContext context, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetGlobalDefaultAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAiContentContextAssignmentRepository
{
    Task<AiContentContextAssignment?> GetByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task SaveAsync(AiContentContextAssignment assignment, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid contentId, CancellationToken cancellationToken = default);
}
```

**Example EF Core Repository Implementation:**

```csharp
internal class EfCoreAiContextRepository : IAiContextRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAiDbContext> _scopeProvider;

    public EfCoreAiContextRepository(IEFCoreScopeProvider<UmbracoAiDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public async Task<AiContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiContextEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Contexts
                .Include(c => c.Resources.OrderBy(r => r.SortOrder))
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AiContextFactory.BuildDomain(entity);
    }

    public async Task<AiContext?> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiContextEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.Contexts
                .Include(c => c.Resources.OrderBy(r => r.SortOrder))
                .FirstOrDefaultAsync(c => c.Alias == alias, cancellationToken));

        scope.Complete();
        return entity is null ? null : AiContextFactory.BuildDomain(entity);
    }

    public async Task<(IEnumerable<AiContext> Items, int Total)> GetPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var result = await scope.ExecuteWithContextAsync(async db =>
        {
            IQueryable<AiContextEntity> query = db.Contexts
                .Include(c => c.Resources.OrderBy(r => r.SortOrder));

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(c =>
                    c.Name.Contains(filter) ||
                    c.Alias.Contains(filter));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(c => c.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        });

        scope.Complete();
        return (result.items.Select(AiContextFactory.BuildDomain), result.total);
    }

    public async Task<AiContext> SaveAsync(AiContext context, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        AiContextEntity entity = await scope.ExecuteWithContextAsync(async db =>
        {
            AiContextEntity? existing = await db.Contexts
                .Include(c => c.Resources)
                .FirstOrDefaultAsync(c => c.Id == context.Id, cancellationToken);

            if (existing is null)
            {
                // Insert
                existing = AiContextFactory.BuildEntity(context);
                db.Contexts.Add(existing);
            }
            else
            {
                // Update
                AiContextFactory.UpdateEntity(existing, context);
            }

            await db.SaveChangesAsync(cancellationToken);
            return existing;
        });

        scope.Complete();
        return AiContextFactory.BuildDomain(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAiDbContext> scope = _scopeProvider.CreateScope();

        var deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            var entity = await db.Contexts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (entity is null)
                return false;

            db.Contexts.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }
}
```

---

## How Context is Injected

Context injection uses the `IAiContextFormatter` (see "Context Formatter" section above) to convert resolved resources into text for AI consumption. The formatter uses the `AiContextResourceTypeCollection` to look up each resource type and call its `Format()` method.

### Into AI Prompts

```csharp
public class AiPromptExecutor
{
    public async Task<PromptResult> ExecuteAsync(AiPrompt prompt, PropertyContext propertyCtx)
    {
        // Resolve AI Context for this content's site
        var aiContext = await _contextResolver.ResolveAsync(propertyCtx.ContentId);

        // Format context for injection
        var contextBlock = _contextFormatter.Format(aiContext);

        // Build prompt with context
        var builtPrompt = _templateEngine.Build(prompt.PromptTemplate, new
        {
            content = propertyCtx.CurrentValue,
            context = contextBlock
        });

        // Execute
        return await _chatService.CompleteAsync(prompt.ProfileAlias, builtPrompt);
    }
}
```

### Into AI Workflows

```csharp
public class AiWorkflowExecutor
{
    public async Task<WorkflowResult> ExecuteAsync(AiWorkflow workflow, IContent content)
    {
        // Resolve context once for the workflow
        var aiContext = await _contextResolver.ResolveAsync(content.Id);

        foreach (var step in workflow.Steps)
        {
            // Context available to each step
            var stepContext = new WorkflowStepContext(content, aiContext, ...);
            await step.ExecuteAsync(stepContext);
        }
    }
}
```

### Into Agents

```csharp
public class AgentChatService
{
    public async Task<ChatResponse> ChatAsync(AgentChatRequest request)
    {
        // Resolve context based on current workspace
        var aiContext = await _contextResolver.ResolveAsync(request.CurrentContentId);

        // Format and enrich agent system prompt with context
        var contextBlock = _contextFormatter.Format(aiContext);
        var enrichedSystemPrompt = $"""
            {agent.SystemPrompt}

            {contextBlock}
            """;

        return await _chatService.ChatAsync(enrichedSystemPrompt, request.Messages);
    }
}
```

---

## Agent-Specific Considerations

Agents present unique challenges for AI Context compared to Prompts. While Prompts are single operations, Agents are conversational and context-fluid.

### Key Differences: Prompts vs Agents

| Aspect | Prompts | Agents |
|--------|---------|--------|
| **Scope** | Single content item | Conversational, may span multiple content items |
| **Context timing** | Fully known at execution | Dynamic, changes during conversation |
| **Context injection** | Injected into prompt template | Injected into system prompt |
| **"Current content"** | Explicit (the content being edited) | Implicit (workspace view, last referenced) |

### Where Context Makes Sense for Agents

1. **System prompt enrichment** - An agent helping with Corporate Site content should speak in Corporate Site's voice
2. **Multi-site awareness** - If a user navigates from Corporate Site to Consumer Brand mid-conversation, the agent's tone should adapt
3. **Reference material access** - Attached documents and style guides inform agent responses

### Challenges

1. **Context is dynamic** - Unlike prompts, agents may work across multiple content items in one conversation
2. **What defines "current content"?** - Could be the workspace view, the last mentioned content, or user selection
3. **Resource size** - Documents and cached links may be large; need token budget management

### Wiring Options

#### Option A: System Prompt Enrichment (Simple)

Context is resolved once at conversation start (or when workspace changes) and appended to the agent's system prompt.

```
Agent System Prompt (static, defined per agent)
         |
    + Context Resources (resolved from current content workspace)
         |
    = Enriched System Prompt
```

```csharp
public class AgentExecutor
{
    public async Task<ChatResponse> ExecuteAsync(AgentSession session, string message)
    {
        // Resolve context from session's current content scope
        var context = await _contextResolver.ResolveAsync(session.CurrentContentId);

        // Format and enrich system prompt
        var contextBlock = _contextFormatter.Format(context);
        var enrichedPrompt = $"""
            {session.Agent.SystemPrompt}

            {contextBlock}
            """;

        // Execute with enriched context
        return await _chatService.ChatAsync(enrichedPrompt, session.Messages);
    }
}
```

**Pros**: Simple, predictable, low overhead
**Cons**: All resources injected at once (token usage)

#### Option B: Middleware Approach (Automatic)

Context injection happens automatically via chat middleware, following the existing `IAiChatMiddleware` pattern used in `LoggingChatMiddleware`. This requires no explicit handling in agent code.

```csharp
// Middleware registration (in UmbracoBuilderExtensions.cs or a Composer)
builder.AiChatMiddleware()
    .Append<AiContextInjectionMiddleware>();

// Middleware implementation follows existing pattern
[AiChatMiddleware("context-injection")]
public class AiContextInjectionMiddleware : IAiChatMiddleware
{
    private readonly IAiContextResolver _contextResolver;
    private readonly IAiContextFormatter _contextFormatter;
    private readonly IAgentSessionAccessor _sessionAccessor;

    public AiContextInjectionMiddleware(
        IAiContextResolver contextResolver,
        IAiContextFormatter contextFormatter,
        IAgentSessionAccessor sessionAccessor)
    {
        _contextResolver = contextResolver;
        _contextFormatter = contextFormatter;
        _sessionAccessor = sessionAccessor;
    }

    public IChatClient Apply(IChatClient client)
    {
        // Uses M.E.AI's builder pattern (same as LoggingChatMiddleware)
        return client.AsBuilder()
            .Use(next => new ContextInjectingChatClient(
                next,
                _contextResolver,
                _contextFormatter,
                _sessionAccessor))
            .Build();
    }
}

// Inner chat client that wraps the delegate
internal class ContextInjectingChatClient : DelegatingChatClient
{
    private readonly IAiContextResolver _contextResolver;
    private readonly IAiContextFormatter _contextFormatter;
    private readonly IAgentSessionAccessor _sessionAccessor;

    public ContextInjectingChatClient(
        IChatClient innerClient,
        IAiContextResolver contextResolver,
        IAiContextFormatter contextFormatter,
        IAgentSessionAccessor sessionAccessor)
        : base(innerClient)
    {
        _contextResolver = contextResolver;
        _contextFormatter = contextFormatter;
        _sessionAccessor = sessionAccessor;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Get current session context (if available)
        var session = _sessionAccessor.CurrentSession;
        if (session?.CurrentContentId != null && session.EnableContextInjection)
        {
            var resolutionRequest = new AiContextResolutionRequest
            {
                ContentId = session.CurrentContentId,
                ProfileId = session.ProfileId,
                AgentContextIds = session.Agent?.ContextIds,
                AgentName = session.Agent?.Name
            };

            var context = await _contextResolver.ResolveAsync(resolutionRequest, cancellationToken);
            var contextBlock = _contextFormatter.Format(context);

            // Inject context into system message
            messages = InjectContextIntoSystemMessage(messages, contextBlock);
        }

        return await base.GetResponseAsync(messages, options, cancellationToken);
    }

    private static IEnumerable<ChatMessage> InjectContextIntoSystemMessage(
        IEnumerable<ChatMessage> messages,
        string contextBlock)
    {
        if (string.IsNullOrEmpty(contextBlock))
            return messages;

        var messageList = messages.ToList();
        var systemMessage = messageList.FirstOrDefault(m => m.Role == ChatRole.System);

        if (systemMessage != null)
        {
            // Append context to existing system message
            var index = messageList.IndexOf(systemMessage);
            var enrichedContent = $"{systemMessage.Text}\n\n{contextBlock}";
            messageList[index] = new ChatMessage(ChatRole.System, enrichedContent);
        }
        else
        {
            // Insert new system message with context
            messageList.Insert(0, new ChatMessage(ChatRole.System, contextBlock));
        }

        return messageList;
    }
}
```

**Pros**: Transparent, follows existing middleware patterns, no agent code changes needed
**Cons**: Less control, requires session accessor for context awareness

### Recommendation for Agents

**Opt-in per agent type**: Context injection should be opt-in. Some agents are general-purpose (e.g., "explain this code") and don't need brand context, while content assistants definitely do.

```csharp
[AiAgent("content-assistant", "Content Assistant")]
public class ContentAssistantAgent : AgentBase
{
    public ContentAssistantAgent()
    {
        // Opt-in to context injection
        WithContextInjection(true);
    }
}

[AiAgent("code-helper", "Code Helper")]
public class CodeHelperAgent : AgentBase
{
    public CodeHelperAgent()
    {
        // No context injection - general purpose agent
    }
}
```

### Context Refresh Strategy

When should agent context be refreshed during a conversation?

| Event | Action |
|-------|--------|
| Session starts | Resolve context from initial workspace |
| User navigates to different content | Refresh context (if agent is workspace-aware) |
| User explicitly mentions different site | Consider refreshing (may require NLP detection) |

```csharp
public class AgentSession
{
    public Guid? CurrentContentId { get; private set; }
    public ResolvedAiContext? CurrentContext { get; private set; }

    public async Task SetWorkspaceAsync(Guid contentId)
    {
        if (CurrentContentId == contentId)
            return;

        CurrentContentId = contentId;

        // Refresh context for new workspace
        CurrentContext = await _contextResolver.ResolveAsync(contentId);

        // Optionally notify agent of context change
        await NotifyContextChangeAsync();
    }
}
```

---

## UI Concepts

### Context Management (Settings Section)

```
┌─ AI Context Management ─────────────────────────────────────────┐
│                                                                 │
│  [+ New Context]                                                │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ 🌐 Global Default                              [Edit]     │ │
│  │    Applies to all sites without specific context          │ │
│  │    3 resources configured                                 │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ 🏢 Corporate Site                              [Edit]     │ │
│  │    Root: /corporate-site                                  │ │
│  │    4 resources: Brand Voice, Style Guide, Writing Tips    │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ 🛍️ Consumer Brand                              [Edit]     │ │
│  │    Root: /consumer-brand                                  │ │
│  │    2 resources: Brand Voice, Tone Document                │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Context Editor

```
┌─ Edit AI Context: Corporate Site ──────────────────────────────┐
│                                                            [×] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Name *                                                         │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Corporate Site                                            │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Root Content Node                                              │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ 📁 Corporate Site                               [Change]  │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ═══════════════════ Resources ════════════════════════════    │
│                                                                 │
│  Drag to reorder (order affects injection priority)            │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ ≡ 🎯 Brand Voice                              [Edit] [×]  │ │
│  │   Tone: Professional, authoritative                       │ │
│  │   Audience: B2B technology decision makers                │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ ≡ 📄 Corporate Style Guide                    [Edit] [×]  │ │
│  │   Document: style-guide-2024.pdf (uploaded)               │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ ≡ 🔗 Brand Guidelines Portal                  [Edit] [×]  │ │
│  │   URL: https://brand.corporate.com/guidelines             │ │
│  │   Last fetched: 2 hours ago                               │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ ≡ 📝 Additional Instructions                  [Edit] [×]  │ │
│  │   "Always mention our commitment to sustainability..."    │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  [+ Add Resource ▼]                                             │
│    ├─ Brand Voice                                               │
│    ├─ Document                                                  │
│    ├─ External Link                                             │
│    └─ Text Instructions                                         │
│                                                                 │
│                                         [Cancel]  [Save]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Brand Voice Resource Editor

```
┌─ Edit Resource: Brand Voice ───────────────────────────────────┐
│                                                            [×] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Name *                                                         │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Brand Voice                                               │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Tone Description                                               │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Professional and authoritative, but approachable. We      │ │
│  │ speak as trusted advisors to our clients.                 │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Target Audience                                                │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ B2B technology decision makers: CTOs, IT Directors,       │ │
│  │ and senior developers at enterprise companies.            │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Style Guidelines                                               │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ - Use active voice                                        │ │
│  │ - Be concise and direct                                   │ │
│  │ - Support claims with data where possible                 │ │
│  │ - Use industry terminology appropriately                  │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Patterns to Avoid                                              │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ - Exclamation marks                                       │ │
│  │ - Superlatives (best, greatest, revolutionary)            │ │
│  │ - Buzzwords without substance                             │ │
│  │ - Overly casual language                                  │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│                                         [Cancel]  [Save]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Document Resource Editor

```
┌─ Edit Resource: Document ──────────────────────────────────────┐
│                                                            [×] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Name *                                                         │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Corporate Style Guide                                     │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Description                                                    │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Our official style guide for all written communications   │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Document Source                                                │
│  (•) Upload file                                                │
│      ┌─────────────────────────────────────────────────────┐   │
│      │ 📄 style-guide-2024.pdf                    [Change] │   │
│      │    Uploaded: Jan 15, 2024 (234 KB)                  │   │
│      └─────────────────────────────────────────────────────┘   │
│                                                                 │
│  ( ) Select from Media Library                                  │
│      [Select Media...]                                          │
│                                                                 │
│                                         [Cancel]  [Save]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### External Link Resource Editor

```
┌─ Edit Resource: External Link ─────────────────────────────────┐
│                                                            [×] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Name *                                                         │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Brand Guidelines Portal                                   │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  URL *                                                          │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ https://brand.corporate.com/guidelines                    │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Description                                                    │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Our central brand guidelines portal with latest updates   │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Cache Status                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ✓ Last fetched: 2 hours ago                             │   │
│  │   Content size: 12.4 KB                    [Refresh Now]│   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│                                         [Cancel]  [Save]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## API Design

### Service Interfaces

Services follow the same patterns as `IAiProfileService` and `IAiConnectionService` - using `SaveAsync` for insert-or-update operations and providing both single-item and paged retrieval methods:

```csharp
public interface IAiContextService
{
    // Single-item retrieval
    Task<AiContext?> GetContextAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AiContext?> GetContextByAliasAsync(string alias, CancellationToken cancellationToken = default);
    Task<AiContext?> GetGlobalDefaultAsync(CancellationToken cancellationToken = default);

    // Collection retrieval
    Task<IEnumerable<AiContext>> GetContextsAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<AiContext> Items, int Total)> GetContextsPagedAsync(
        string? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    // Mutations (insert-or-update pattern)
    Task<AiContext> SaveContextAsync(AiContext context, CancellationToken cancellationToken = default);
    Task<bool> DeleteContextAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetGlobalDefaultAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAiContextResourceService
{
    // Resource retrieval within a context
    Task<AiContextResource?> GetResourceAsync(Guid contextId, Guid resourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AiContextResource>> GetResourcesAsync(Guid contextId, CancellationToken cancellationToken = default);

    // Resource mutations (insert-or-update pattern)
    Task<AiContextResource> SaveResourceAsync(Guid contextId, AiContextResource resource, CancellationToken cancellationToken = default);
    Task<bool> DeleteResourceAsync(Guid contextId, Guid resourceId, CancellationToken cancellationToken = default);
    Task ReorderResourcesAsync(Guid contextId, IEnumerable<Guid> resourceIds, CancellationToken cancellationToken = default);

    // External link refresh
    Task RefreshExternalLinkAsync(Guid contextId, Guid resourceId, CancellationToken cancellationToken = default);
}

public interface IAiContentContextAssignmentService
{
    // Content → Context assignments
    Task<Guid?> GetAssignedContextIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task<AiContext?> GetAssignedContextAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task AssignContextAsync(Guid contentId, Guid contextId, CancellationToken cancellationToken = default);
    Task<bool> RemoveAssignmentAsync(Guid contentId, CancellationToken cancellationToken = default);
}

public interface IAiContextResolver
{
    // Full resolution across all levels (profile, agent, prompt, content, global)
    Task<ResolvedAiContext> ResolveAsync(AiContextResolutionRequest request, CancellationToken cancellationToken = default);
}
```

**IdOrAlias Support (following existing pattern):**

```csharp
// Extension methods for IdOrAlias resolution (mirrors IAiProfileService extensions)
public static class AiContextServiceExtensions
{
    public static async Task<Guid?> TryGetContextIdAsync(
        this IAiContextService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias.IsId)
        {
            var context = await service.GetContextAsync(idOrAlias.Id!.Value, cancellationToken);
            return context?.Id;
        }

        if (idOrAlias.IsAlias)
        {
            var context = await service.GetContextByAliasAsync(idOrAlias.Alias!, cancellationToken);
            return context?.Id;
        }

        return null;
    }

    public static async Task<Guid> GetContextIdAsync(
        this IAiContextService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        var id = await service.TryGetContextIdAsync(idOrAlias, cancellationToken);
        return id ?? throw new InvalidOperationException($"Context '{idOrAlias}' not found.");
    }
}
```

### API Endpoints

API endpoints follow the same patterns as existing Management API controllers, using:
- `[ApiVersion("1.0")]` attribute for versioning
- `IdOrAlias` type for dual ID/alias lookups in route parameters
- `IUmbracoMapper` for request/response model mapping
- `ProblemDetails` for error responses

```
# Context CRUD (IdOrAlias pattern - accepts both GUID and alias)
GET    /umbraco/ai/management/api/v1/contexts                     # List all (with paging: ?skip=0&take=100&filter=)
GET    /umbraco/ai/management/api/v1/contexts/{idOrAlias}         # Get by ID or alias
GET    /umbraco/ai/management/api/v1/contexts/global              # Get global default
POST   /umbraco/ai/management/api/v1/contexts                     # Create
PUT    /umbraco/ai/management/api/v1/contexts/{idOrAlias}         # Update
DELETE /umbraco/ai/management/api/v1/contexts/{idOrAlias}         # Delete
PUT    /umbraco/ai/management/api/v1/contexts/{idOrAlias}/set-global  # Set as global default

# Resource management
GET    /umbraco/ai/management/api/v1/contexts/{idOrAlias}/resources                    # List resources
POST   /umbraco/ai/management/api/v1/contexts/{idOrAlias}/resources                    # Add resource
GET    /umbraco/ai/management/api/v1/contexts/{idOrAlias}/resources/{resourceId}       # Get resource
PUT    /umbraco/ai/management/api/v1/contexts/{idOrAlias}/resources/{resourceId}       # Update resource
DELETE /umbraco/ai/management/api/v1/contexts/{idOrAlias}/resources/{resourceId}       # Delete resource
PUT    /umbraco/ai/management/api/v1/contexts/{idOrAlias}/resources/order              # Reorder resources
POST   /umbraco/ai/management/api/v1/contexts/{idOrAlias}/resources/{resourceId}/refresh  # Refresh external link

# Content assignments
GET    /umbraco/ai/management/api/v1/content/{contentId}/context  # Get assigned context
PUT    /umbraco/ai/management/api/v1/content/{contentId}/context  # Assign context
DELETE /umbraco/ai/management/api/v1/content/{contentId}/context  # Remove assignment

# Resolution endpoint (for testing/debugging)
POST   /umbraco/ai/management/api/v1/contexts/resolve             # Resolve with full request body
       { contentId, profileId, promptContextIds, agentContextIds }

# Resource types (read-only - lists available types from collection)
GET    /umbraco/ai/management/api/v1/context-resource-types       # List all registered resource types
```

**Example Controller Implementation:**

```csharp
[ApiVersion("1.0")]
public class AiContextController : AiContextControllerBase
{
    private readonly IAiContextService _contextService;
    private readonly IUmbracoMapper _umbracoMapper;

    public AiContextController(
        IAiContextService contextService,
        IUmbracoMapper umbracoMapper)
    {
        _contextService = contextService;
        _umbracoMapper = umbracoMapper;
    }

    [HttpGet("{idOrAlias}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AiContextResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContext(
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        var contextId = await _contextService.TryGetContextIdAsync(idOrAlias, cancellationToken);
        if (contextId is null)
            return NotFound(new ProblemDetails
            {
                Title = "Context not found",
                Detail = $"No context found with identifier '{idOrAlias}'",
                Status = StatusCodes.Status404NotFound
            });

        var context = await _contextService.GetContextAsync(contextId.Value, cancellationToken);
        return Ok(_umbracoMapper.Map<AiContextResponseModel>(context));
    }

    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedResponseModel<AiContextResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContexts(
        [FromQuery] string? filter = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _contextService.GetContextsPagedAsync(filter, skip, take, cancellationToken);
        var mapped = _umbracoMapper.MapEnumerable<AiContext, AiContextResponseModel>(items);
        return Ok(new PagedResponseModel<AiContextResponseModel>
        {
            Items = mapped,
            Total = total
        });
    }

    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AiContextResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContext(
        AiContextRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
        var context = _umbracoMapper.Map<AiContext>(requestModel)!;
        var saved = await _contextService.SaveContextAsync(context, cancellationToken);
        var response = _umbracoMapper.Map<AiContextResponseModel>(saved);
        return CreatedAtAction(nameof(GetContext), new { idOrAlias = saved.Id }, response);
    }
}
```

---

## Integration Points

### With AI Prompts

Prompts can reference the formatted context block in their templates:

```
Template: "Generate a meta description for: {content}

{context}"
```

The `{context}` variable is automatically populated with all resolved resources formatted appropriately.

### With AI Workflows

Workflow steps automatically receive resolved context:

```csharp
public class LlmSummarizerStep : AiWorkflowStepTypeBase
{
    public override async Task<StepResult> ExecuteAsync(WorkflowStepContext ctx)
    {
        var contextBlock = _contextFormatter.Format(ctx.AiContext);

        var prompt = $"""
            Summarize this content:
            {ctx.GetInput<string>("text")}

            {contextBlock}
            """;

        return await ExecutePromptAsync(prompt);
    }
}
```

### With Agents

Agent system prompts are enriched with all context resources:

```
// Agent sees this automatically in their system prompt:
"You are a content assistant for the Corporate Site.

--- Context ---
Tone: Professional and authoritative, but approachable
Audience: B2B technology decision makers
Style: Use active voice, be concise
Avoid: Exclamation marks, superlatives

Reference Document: Corporate Style Guide
[Content of style guide...]

Additional Instructions:
Always mention our commitment to sustainability..."
```

---

## Questions & Considerations

### 1. Context Inheritance

Should child sites inherit from parent context with overrides?

```
Global Context
└── Corporate Site Context (inherits + overrides tone)
    └── Corporate Blog Context (inherits + overrides audience)
```

**Recommendation**: Start simple with flat contexts. Add inheritance if needed.

### 2. Context Versioning

Should we track context changes over time?

**Recommendation**: V2 consideration. For V1, rely on audit logs.

### 3. Context per Language/Culture

Should context vary by language variant?

**Recommendation**: Consider for V2. Multi-site covers most cases initially.

### 4. Context Import/Export

Should contexts be exportable for sharing across environments?

**Recommendation**: Yes, as JSON. Useful for dev → staging → prod workflows.

### 5. Agent Context Injection Mode

Should context injection be automatic for all agents or opt-in per agent?

**Recommendation**: Opt-in per agent type. General-purpose agents (code helpers, explainers) don't need brand context, while content assistants definitely do. Use a `WithContextInjection()` pattern in agent definitions.

### 6. Agent Context Refresh Strategy

When should agent context be refreshed during a conversation?

- At session start only?
- When workspace changes?

**Recommendation**: Session start + workspace change.

### 7. Token Budget Management

How do we handle contexts with many resources or large documents?

**Recommendation**:
- Sum resource sizes and warn when context exceeds a threshold
- Consider chunking large documents or summarizing them
- For very large reference materials, consider RAG approach in future

### 8. External Link Refresh Strategy

How often should external links be refreshed?

**Recommendation**:
- Manual refresh button in UI for on-demand updates
- Background job for periodic refresh (configurable, e.g., daily)
- Show last-fetched timestamp to users

---

## Recommendation

**Implement as foundation for AI Prompts, AI Workflows, and Agents**.

AI Context should be built first or alongside AI Prompts, as it provides the brand consistency that makes AI-generated content actually useful.

### Implementation Order

**Phase 1: Core Infrastructure (Umbraco.Ai)**
1. `AiContext` and `AiContextResource` domain models (sealed, internal set on Id, required init on Alias)
2. `AiContextResourceTypeCollectionBuilder` and `AiContextResourceTypeCollection` (LazyCollectionBuilderBase)
3. `IAiContextResourceType` interface and `AiContextResourceTypeBase<T>` base class
4. Built-in resource types: `BrandVoiceResourceType`, `DocumentResourceType`, `ExternalLinkResourceType`, `TextResourceType`
5. Resource type auto-discovery via `[AiContextResourceTypeAttribute]`
6. `IAiContextRepository` and `EfCoreAiContextRepository` (with scope provider pattern)
7. `IAiContentContextAssignmentRepository` and EF Core implementation
8. `IAiContextService` and `IAiContentContextAssignmentService` (SaveAsync pattern)
9. `IAiContextResolver` for multi-level resolution
10. `IAiContextFormatter` using `AiContextResourceTypeCollection`
11. API controllers with `IdOrAlias` support and versioning
12. Content context property editor for assignment
13. Management UI in backoffice
14. Add `ContextIds` to `AiProfile` model

**Phase 2: Prompt Integration (Umbraco.Ai.Prompt)**
15. Add `ContextIds` to `AiPrompt` model
16. Context picker UI in prompt editor
17. Inject formatted context block into prompt templates
18. `{context}` variable support in template engine

**Phase 3: Agent Integration (Umbraco.Ai.Agent)**
19. Add `ContextIds` to `AiAgent` model
20. Context picker UI in agent editor
21. `AiContextInjectionMiddleware` for automatic context injection
22. Session-level context resolution with merge
23. Context refresh on workspace navigation

**Phase 4: Workflow Integration**
24. Workflow step context enrichment
25. Context formatter available in step execution

---

## Related Documents

- [AI Prompts](./ai-prompts.md) - Human-initiated single-step operations
- [AI Workflows](./ai-workflows.md) - Automatic multi-step automation
- [Umbraco.Ai.Agents](../umbraco-ai-agents-design.md) - Conversational AI assistants

---

## Related Decisions

| Decision | Current Choice |
|----------|----------------|
| Naming | "AI Context" |
| Architecture | Resource-based (generic, extensible) |
| Context identity | Standalone entities with alias (immutable), reusable across assignments |
| Assignment levels | Content, Profile, Prompt, Agent |
| Content assignment | Via property editor, inherits down tree |
| Profile/Prompt/Agent assignment | Multiple contexts allowed, via UI picker |
| Merge order | Profile → Agent → Prompt → Content (most specific wins) |
| Content inheritance | Walk up tree until assignment found, then global default |
| **Domain models** | `sealed` classes with `internal set` on Id, `required init` on Alias |
| **Service pattern** | `SaveAsync` for insert-or-update, paged queries for lists |
| **Repository pattern** | `IEFCoreScopeProvider<UmbracoAiDbContext>` with explicit scope completion |
| **API pattern** | `IdOrAlias` for dual ID/alias lookups, `[ApiVersion]` for versioning |
| Resource type extensibility | `AiContextResourceTypeBase<T>` with `IAiContextResourceTypeInfrastructure` injection |
| Resource type discovery | `LazyCollectionBuilderBase` + `[AiContextResourceTypeAttribute]` auto-discovery |
| Resource type access | `AiContextResourceTypeCollection` (injected via DI, not a Registry) |
| Resource type components | Definition (C#) + Format method (C#) + UI Editor (TypeScript/Lit) |
| Built-in resource types | brand-voice, document, external-link, text |
| Resource ordering | User-defined sort order within each context |
| **Context formatting** | `IAiContextFormatter` using `AiContextResourceTypeCollection` |
| **Middleware integration** | `IAiChatMiddleware` pattern with `AiContextInjectionMiddleware` |
| Property constraints | Out of scope - schema-level concern, not context |
| Agent context injection | Opt-in via session, controlled by `EnableContextInjection` flag |
| Agent context refresh | Session start + workspace change |
| External link caching | Manual refresh + optional background job |
| Debugging support | `ContextSource` tracking shows where each resource came from |
