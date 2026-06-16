# Umbraco CMS v18 — Impact Report

**Branch:** `feature/cms-v18-prep`
**Source:** Umbraco CMS `18.0.0-beta1`, `18.0.0-beta2`, `18.0.0-rc1` (current latest as of 2026-05-29)
**Goal:** Move every product in this monorepo from CMS `[17.4.0, 17.999.999)` to `[18.0.0, 18.999.999)`, align all add-on package versions to `18.0.0`, and keep the published OpenAPI documents byte-for-byte equivalent so downstream client SDKs (and consumers' regenerated clients) don't drift.

## TL;DR

1. **One real code change** — the OpenAPI/Swashbuckle swap (PR umbraco/Umbraco-CMS#21058 and follow-up #22774) is the only v18 breaking change that hits our compiled code. It touches three `UmbracoBuilderExtensions.cs` files plus four helper/filter classes in `Umbraco.AI.Web`.
2. **One small obsolete-API hit** — `Constants.Security.AllowedApplicationsClaimType` is removed (PR #20124). We're already using it under `#pragma warning disable CS0618`. Trivial replacement.
3. **No other obsolete-removal hit** — none of `IEmailSender`, `IMemberGroupService`, `IDataTypeService`, `IDomainService`, `IContentTypeBaseService`, `ILocalizationService`, `IFileService`, `IMemberService`, `ApplicationMainUrl`, `GetDictionaryValue`, `UrlSegment`, `UmbracoApiController` are referenced in our `src/` code.
4. **Frontend route renames** — `/umbraco/swagger/{name}/swagger.json` → `/umbraco/openapi/{name}.json` in three `generate-openapi.js` scripts, plus the doc snippet in `Umbraco.AI/CLAUDE.md`.
5. **Version coordination** — 20 `version.json` files bump to `18.0.0`. Three of them (`Search`, `Automate`, the Deploy packages) ship a stable major for the first time. CMS package pins move to `[18.0.0, 18.999.999)`. Per-product `Directory.Packages.props` inter-product ranges (`[1.X.Y, 1.999.999)`) all need to be rewritten as `[18.0.0, 18.999.999)`.
6. **Frontend backoffice is reachable via MyGet** — `@umbraco-cms/backoffice` v18 (currently `18.0.0-rc2`) is published to the `umbracoprereleases` MyGet npm feed (`https://www.myget.org/F/umbracoprereleases/npm/`). Adding a scope-restricted `.npmrc` entry unblocks the frontend immediately; no need to wait for the public npm release. **UUI v2** ships under v18 but we're insulated — we have zero direct `@umbraco-ui/*` imports and the announcement explicitly states extension/package authors need no action.

The migration is mechanically straightforward but the OpenAPI work needs care — see the dedicated section below.

---

## 1. OpenAPI Migration (Swashbuckle → Microsoft.AspNetCore.OpenApi)

**Source PRs:** umbraco/Umbraco-CMS#21058 (the swap), umbraco/Umbraco-CMS#22774 (fluent builder), umbraco/Umbraco-CMS#22670 (extension template fix).

### 1.1 What CMS changed

CMS no longer registers Swashbuckle filters globally. Every consumer document must configure its own transformers via `Microsoft.AspNetCore.OpenApi`'s `OpenApiOptions`. The following types/extension points we currently use have been removed:

| Removed | Status in our codebase | Replacement |
|---|---|---|
| `IOperationIdHandler` / `OperationIdHandler` | We extend `OperationIdHandler` in `UmbracoAIApiOperationIdHandler` | Per-document `IOpenApiOperationTransformer` (or `UmbracoOperationIdTransformer`) |
| `ISchemaIdHandler` / `SchemaIdHandler` | We extend `SchemaIdHandler` in `UmbracoAIApiSchemaIdHandler` | Configure `OpenApiOptions.CreateSchemaReferenceId` (`UmbracoSchemaIdGenerator.CreateSchemaReferenceId` for default Umbraco behaviour) |
| `BackOfficeSecurityRequirementsOperationFilterBase` | We extend it in `UmbracoAIManagementApiBackOfficeSecurityRequirementsOperationFilter` | `OpenApiOptions.AddBackofficeSecurityRequirements()` extension method |
| `MimeTypeDocumentFilter` | We register it in `UmbracoBuilderExtensions` | `MimeTypesTransformer` (or covered by the new defaults via `AddBackOfficeOpenApiDocument`) |
| `SwaggerGenOptions` / `SwaggerDoc(...)` | We use both in `WithUmbracoAIManagementApi` and via every product's `UmbracoBuilderExtensions` | `OpenApiOptions` + `AddBackOfficeOpenApiDocument(name, document => document.WithTitle(...).WithBackOfficeAuthentication())` |
| `Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter` (interface) | We implement it in `SwaggerOperationFilter` and `SseResponseOperationFilter` | `Microsoft.AspNetCore.OpenApi.IOpenApiOperationTransformer` (signature is `Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)`) |
| `/umbraco/swagger/{document}/swagger.json` URL | Hard-coded in three `generate-openapi.js` files and the root `Umbraco.AI/CLAUDE.md` snippet | `/umbraco/openapi/{document}.json` |

### 1.2 What we currently do (centralised — good)

`Umbraco.AI/src/Umbraco.AI.Web/Configuration/UmbracoBuilderExtensions.cs:112` exposes a single public extension method, `WithUmbracoAIManagementApi(apiName, configureSwagger, configureJson)`, which is the **only** registration entry point used by all three Web projects:

- `Umbraco.AI.Web/Configuration/UmbracoBuilderExtensions.cs:89`
- `Umbraco.AI.Prompt.Web/Configuration/UmbracoBuilderExtensions.cs:28`
- `Umbraco.AI.Agent.Web/Configuration/UmbracoBuilderExtensions.cs:32`

All three call sites pass an `Action<SwaggerGenOptions>` that just calls `options.SwaggerDoc(apiName, new OpenApiInfo { Title, Version, Description })`. The shared method then registers the four filters (`MimeTypeDocumentFilter`, `UmbracoAIManagementApiBackOfficeSecurityRequirementsOperationFilter`, `SwaggerOperationFilter`, `SseResponseOperationFilter`) and the `IOperationIdHandler` / `ISchemaIdHandler` singletons.

**This means the v18 changes are localised to one method plus four files.** Add-on packages don't need code edits — only a recompile against the new signature.

### 1.3 Official guidance — two supported paths

The CMS team published two pieces of guidance that scope our options sharply:

- **[Announcement #32](https://github.com/umbraco/Announcements/issues/32)** — `Swashbuckle.AspNetCore` is no longer transitively available. Extension authors choose either:
  1. Migrate to `Microsoft.AspNetCore.OpenApi` (`services.AddOpenApi("yourDocumentName")` — Umbraco already configures the library), **or**
  2. Add a direct `PackageReference` to `Swashbuckle.AspNetCore` and keep using it. Swashbuckle and `Microsoft.AspNetCore.OpenApi` coexist in the same app.
- **[Laura Neto's blog post (Umbraco team)](https://dev.to/lauraneto/umbraco-18-and-openapi-a-heads-up-for-extension-developers-1k7)** — the canonical migration recipe for extension authors using `AddBackOfficeOpenApiDocument`.

**Recommendation: Path 1 (migrate).** Path 2 (pin Swashbuckle) is a viable escape hatch if we need to ship under time pressure, but it carries:
- A second OpenAPI generator running in the same app (possible UI-listing quirks).
- Technical debt — we eventually do this migration anyway.
- A direct dependency on a third-party package that the CMS just removed.

Path 1 is small enough (one method rewrite + four file ports) that the upside isn't worth the debt. **Use Path 2 only as a fallback if the byte-equivalence diff in §1.4 turns up something we can't reconcile via transformers.**

### 1.3a Migration recipe (Path 1)

Following the canonical pattern from Laura Neto's blog post, rewrite `WithUmbracoAIManagementApi` against `AddBackOfficeOpenApiDocument` (the fluent builder added in CMS PR #22774). Real-world example (the [`enkelmedia` comment on Announcement #32](https://github.com/umbraco/Announcements/issues/32#issuecomment-4494781605) shows a working "The Dashboard" migration):

```csharp
public static IUmbracoBuilder WithUmbracoAIManagementApi(
    this IUmbracoBuilder builder,
    string apiName,
    string apiTitle,
    string apiDescription,
    Action<OpenApiOptions>? configureOptions = null,
    Action<JsonSerializerOptions>? configureJson = null)
{
    builder.AddBackOfficeOpenApiDocument(apiName, document => document
        .WithTitle(apiTitle)
        .WithBackOfficeAuthentication()
        .WithJsonOptions(Constants.JsonOptionsNames.BackOffice)  // align schema with runtime serialisation
        .ConfigureOpenApiOptions(options =>
        {
            // Replaces UmbracoAIApiOperationIdHandler (lower-cases the action name).
            options.AddOperationTransformer((operation, context, _) =>
            {
                var routeValues = context.Description.ActionDescriptor.RouteValues;
                if (routeValues.TryGetValue("action", out var actionName) &&
                    !string.IsNullOrWhiteSpace(actionName))
                {
                    operation.OperationId = actionName.ToFirstLower();
                }
                return Task.CompletedTask;
            });

            // Replaces SwaggerOperationFilter (reads our [SwaggerOperation] attribute → operation.Id/Summary/Description/Tags).
            options.AddOperationTransformer<SwaggerOperationTransformer>();

            // Replaces SseResponseOperationFilter (adds 200 + text/event-stream response).
            options.AddOperationTransformer<SseResponseOperationTransformer>();

            // Replaces UmbracoAIApiSchemaIdHandler. AddBackOfficeOpenApiDocument already wires up
            // UmbracoSchemaIdGenerator.CreateSchemaReferenceId (it's public in v18); types under
            // Umbraco.Cms.* get Umbraco naming, everything else falls through to the framework default.
            // Verify this produces the same names for our Umbraco.AI.* types — if it diverges, override:
            //   options.CreateSchemaReferenceId = type => type.Namespace?.StartsWith(Constants.AppNamespaceRoot) is true
            //       ? UmbracoSchemaIdGenerator.CreateSchemaReferenceId(new OpenApiSchemaReferenceContext { /* ... */ })
            //       : OpenApiOptions.CreateDefaultSchemaReferenceId(...);

            // Custom type→string mappings (IdOrAlias, System.Type) — verify these still flow through.
            // Microsoft.AspNetCore.OpenApi reads TypeConverters and JsonConverters by default; if either
            // doesn't render as `type: string`, add an IOpenApiSchemaTransformer that rewrites them.

            configureOptions?.Invoke(options);
        }));

    return builder;
}
```

Notes on the shape:

- **`UmbracoOperationIdTransformer` is public in v18** (per Announcement #32) and registered by default on `AddBackOfficeOpenApiDocument`. If its output matches what we want (need to verify against the v17 baseline), we can drop our own custom transformer entirely. Our current handler lowercases the first letter of the action; if Umbraco's transformer produces the same shape we get this for free.
- **`UmbracoSchemaIdGenerator` is public in v18** and wired up by default. Our `UmbracoAIApiSchemaIdHandler` previously delegated to Umbraco's `SchemaIdHandler` for any type in `Constants.AppNamespaceRoot`. The new default should hit them the same way; verification step in §1.4 catches drift.
- **`WithJsonOptions(Constants.JsonOptionsNames.BackOffice)` plus `[JsonOptionsName(Constants.JsonOptionsNames.BackOffice)]` on controllers** — Laura's blog calls this out explicitly to keep schema generation aligned with runtime serialisation. Need to audit whether our controllers already carry this attribute; the v17 setup may have got away without it because Swashbuckle read JSON options differently.
- **`MimeTypeDocumentFilter`** — replaced by Umbraco's `MimeTypesTransformer`, which `AddBackOfficeOpenApiDocument` already registers. We can delete our file.
- **`UmbracoAIManagementApiBackOfficeSecurityRequirementsOperationFilter`** — replaced by `.WithBackOfficeAuthentication()`. Delete our file.
- **`[SwaggerOperation]` attribute** — we own this type; it has no Swashbuckle dependency. The reader (filter) is what changes. Keep the attribute name as-is for now (zero-cost backwards compat for any external callers); we can rename in a later cleanup.

### 1.3b Critical pieces to preserve

To keep the OpenAPI document byte-equivalent in §1.4:

1. **Operation IDs** — match the v17 lowercase-first convention. Whether we use Umbraco's default `UmbracoOperationIdTransformer` or the inline transformer above depends on what Umbraco's default produces; the §1.4 diff settles it.
2. **Schema IDs** — match v17 names for everything in `Umbraco.AI.*`. The default `UmbracoSchemaIdGenerator` should hit these the same way as the v17 path; verify in §1.4.
3. **`[SwaggerOperation]` attribute reads** — `SwaggerOperationTransformer` must reproduce all four properties (`Id` → `OperationId`, `Summary`, `Description`, `Tags` from both class and method) with the same precedence rules as the v17 filter.
4. **SSE response shape** — `text/event-stream` 200 response with a `type: string` schema, replacing the default response. The v17 filter checks for `[Produces("text/event-stream")]`; preserve that exact detection.
5. **OpenAPI version bump** — output is now 3.1.1, not 3.0.4. Nullable representation changes from `"nullable": true` to `"type": ["null", "string"]`. `@hey-api/openapi-ts` 0.97 (which CMS uses internally now) supports this; our pinned version may need to follow.
6. **Polymorphic schema naming change** — CMS PR #21058 notes that `Microsoft.AspNetCore.OpenApi` prefixes derived-type schema names with the base type name (e.g. `IPermissionPresentationModelDocumentPropertyValuePermissionPresentationModel`). If we expose any polymorphic models with `[JsonDerivedType]`, our generated client type names will drift. Audit `MapDefinition` outputs and any `[JsonDerivedType]`-using request/response models. Decision in Open Question #4.

### 1.3c MSBuild change — `InterceptorsNamespaces`

`Microsoft.AspNetCore.OpenApi` uses C# source generators with interceptors. Each Web project that registers an OpenAPI document must add this to its `.csproj`:

```xml
<PropertyGroup>
  <InterceptorsNamespaces>$(InterceptorsNamespaces);Microsoft.AspNetCore.OpenApi.Generated</InterceptorsNamespaces>
</PropertyGroup>
```

Files to touch:
- `Umbraco.AI/src/Umbraco.AI.Web/Umbraco.AI.Web.csproj`
- `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web/Umbraco.AI.Prompt.Web.csproj`
- `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web/Umbraco.AI.Agent.Web.csproj`

Without this, calls to `AddOpenApi(documentName)` silently produce empty documents.

### 1.4 Verification (executed against demo on 2026-05-29 with v18-rc2)

Documents are served at:
- `/umbraco/openapi/ai-management.json` — 61 paths, 79 operations
- `/umbraco/openapi/ai-prompt-management.json` — 6 paths, 9 operations
- `/umbraco/openapi/ai-agent-management.json` — 9 paths, 12 operations

Three separate documents, one per AI product (each has its own `Constants.ManagementApi.ApiName`). Controllers are scoped via `[MapToApi(ApiName)]` on each product's controller base.

**Verified preservation points:**

| Check | Result |
|---|---|
| Operation IDs | Lowercase-first action name preserved (e.g. `completeChat`, `createPrompt`, `agentAliasExists`) — matches v17 convention via `UmbracoAIOperationIdTransformer`. |
| Schema reference IDs (non-polymorphic) | Preserved via `UmbracoSchemaIdGenerator.Generate` for any type under `Umbraco.AI.*`. |
| Polymorphic derived type schema names | Preserved via `PreservePolymorphicSchemaNamesTransformer`. Microsoft.AspNetCore.OpenApi names derived schemas as `{baseSchemaId}{derivedTypeName}` (e.g. `ChatContentPartModelTextChatContentPartModel`); `CreateSchemaReferenceId` only governs the base, not derived. The transformer auto-discovers polymorphic groups via `discriminator.mapping` and shortens the keys back to the v17 short names (e.g. `TextChatContentPartModel`), updating `anyOf` / `oneOf` / `allOf` and discriminator-mapping refs accordingly. |
| SSE responses | 200 + `text/event-stream` preserved on `agents/{idOrAlias}/stream` and `stream-agui` via `SseResponseOperationTransformer`. |
| `IdOrAlias` / `System.Type` schemas | Render as `type: string` automatically (the framework resolves the existing `IdOrAliasJsonConverter` and `JsonStringTypeConverter` from `JsonOptions`; no explicit `IOpenApiSchemaTransformer` needed). |
| Document title and description | Set via the fluent builder + a document transformer for `Info.Description` and `Info.Version`. |

**Known accepted differences from v17:**

- OpenAPI version: `3.0.4` → `3.1.1`
- Nullable representation: `"nullable": true` → `"type": ["null", "string"]`
- `oneOf` wrappers around single `$ref`: removed (this is the framework default)
- The CMS `MimeTypesTransformer` strips redundant `text/json`, `application/*+json`, `text/plain` from responses where `application/json` is present — these were not in our v17 output either (Swashbuckle didn't generate them), so this is a no-op for our documents.

**Step still to do as cross-check:** regenerate the TypeScript client (Phase D) and diff against the dev branch's generated client. Anything beyond import paths, generator-version banner, and the OpenAPI 3.1 representation changes above counts as a regression.

### 1.5 Files touched

| File | Change |
|---|---|
| `Umbraco.AI/src/Umbraco.AI.Web/Configuration/UmbracoBuilderExtensions.cs` | Rewrite `WithUmbracoAIManagementApi` against `AddBackOfficeOpenApiDocument`. Replace the AllowedApplicationsClaimType pragma block (see §2). |
| `Umbraco.AI/src/Umbraco.AI.Web/Api/Common/Configuration/SwaggerOperationFilter.cs` | Port from `IOperationFilter` → `IOpenApiOperationTransformer`. Rename to `SwaggerOperationTransformer`. |
| `Umbraco.AI/src/Umbraco.AI.Web/Api/Common/Configuration/SseResponseOperationFilter.cs` | Same port. Rename to `SseResponseOperationTransformer`. |
| `Umbraco.AI/src/Umbraco.AI.Web/Api/Common/Configuration/UmbracoAIApiOperationIdHandler.cs` | Delete if Umbraco's default `UmbracoOperationIdTransformer` produces the same output (lowercase-first action name); otherwise replace with the inline transformer shown in §1.3a. |
| `Umbraco.AI/src/Umbraco.AI.Web/Api/Common/Configuration/UmbracoAIApiSchemaIdHandler.cs` | Delete — `UmbracoSchemaIdGenerator` (now public) is wired up by default. Re-instate as a `CreateSchemaReferenceId` delegate only if §1.4 turns up name drift. |
| `Umbraco.AI/src/Umbraco.AI.Web/Api/Management/Configuration/UmbracoAIManagementApiBackOfficeSecurityRequirementsOperationFilter.cs` | Delete — replaced by `.WithBackOfficeAuthentication()`. |
| `Umbraco.AI/src/Umbraco.AI.Web/Umbraco.AI.Web.csproj` | Add `<InterceptorsNamespaces>` MSBuild property (§1.3c). |
| `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web/Configuration/UmbracoBuilderExtensions.cs` | Signature change — `Action<SwaggerGenOptions>` → `Action<OpenApiOptions>`. Same `WithUmbracoAIManagementApi` call, just typed differently. |
| `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web/Umbraco.AI.Prompt.Web.csproj` | Add `<InterceptorsNamespaces>` MSBuild property. |
| `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web/Configuration/UmbracoBuilderExtensions.cs` | Same signature change. |
| `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web/Umbraco.AI.Agent.Web.csproj` | Add `<InterceptorsNamespaces>` MSBuild property. |
| `Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/scripts/generate-openapi.js` | URL: `/umbraco/swagger/.../swagger.json` → `/umbraco/openapi/{name}.json`. |
| `Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Client/scripts/generate-openapi.js` | Same. |
| `Umbraco.AI.Agent/src/Umbraco.AI.Agent.Web.StaticAssets/Client/scripts/generate-openapi.js` | Same. |
| `scripts/build/generate-openapi.js` | Same. |
| `Umbraco.AI/CLAUDE.md` | The `npm run generate-client` example URL on line 22. |

### 1.6 Future improvement — build-time OpenAPI generation

`Microsoft.AspNetCore.OpenApi` supports [generating OpenAPI documents at build time](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi#generate-openapi-documents-at-build-time) (called out by Warren Buckley on Announcement #32). This would let `npm run generate-client` work **without a running CMS site** — generators emit the JSON during the `dotnet build` step and the npm script reads it from a known path.

Not required for this migration. Worth a separate ticket once we've shipped v18 stable and want to simplify the developer workflow.

### 1.7 Open question — public API shape

`WithUmbracoAIManagementApi`'s second parameter is `Action<SwaggerGenOptions>` today, which is **part of our public API surface** (the method is public). External consumers who call it pass a Swashbuckle delegate. After v18 this becomes `Action<OpenApiOptions>`. This is unavoidable — Swashbuckle is gone — but it's worth confirming we haven't published any third-party guidance that demonstrates the old shape, and adding a CHANGELOG note flagging it.

Looking at the canonical pattern in Laura Neto's blog and the call sites in our own Prompt/Agent Web projects, the second parameter (`configureSwagger`) is *only ever used to call `SwaggerDoc(name, new OpenApiInfo { Title, Description })`* — i.e. document metadata. None of the three call sites use it for filters, schema mappings, or anything else. We could simplify the public API by:

- Removing the callback parameter entirely.
- Taking `apiTitle` and `apiDescription` as plain `string` parameters (or an `OpenApiInfo` instance).
- Routing everything else through the internal `ConfigureOpenApiOptions`.

This is also raised as Open Question #3 below.

---

## 2. Section authorization — migrated off obsolete claim API ✅

**Source PR:** umbraco/Umbraco-CMS#20124 (removed obsolete *methods*; the constant itself survives).

`Umbraco.AI/src/Umbraco.AI.Web/Configuration/UmbracoBuilderExtensions.cs:57` used to read:

```csharp
#pragma warning disable CS0618 // Type or member is obsolete
policy.RequireClaim(Umbraco.Cms.Core.Constants.Security.AllowedApplicationsClaimType, Core.Constants.Sections.AI);
#pragma warning restore CS0618 // Type or member is obsolete
```

Phase A discovery: `AllowedApplicationsClaimType` is still defined in v18 (`Constants-Security.cs:100`) and still marked `[Obsolete("Please use IUser.AllowedSections instead. Will be removed in V15.")]`. The build passed with our pragma in place — but per the user's direction, we migrated to the non-obsolete pattern.

The canonical v18 pattern is `AllowedApplicationRequirement` + `AllowedApplicationHandler` (CMS `BackOfficeAuthPolicyBuilderExtensions.cs:40-45`). Both are `internal sealed` in CMS so we can't reuse them directly — but the building blocks they sit on are public:

- `MustSatisfyRequirementAuthorizationHandler<T>` (public base class)
- `IAuthorizationHelper` (public, exposes `TryGetUmbracoUser`)
- `IUser.AllowedSections` (the explicit replacement called out in the obsolete message)

**Implementation:** mirror CMS's internal pattern with two new files (~25 lines total):

- `Umbraco.AI.Web/Authorization/AISectionRequirement.cs` — empty `IAuthorizationRequirement` marker.
- `Umbraco.AI.Web/Authorization/AISectionAuthorizationHandler.cs` — `MustSatisfyRequirementAuthorizationHandler<AISectionRequirement>` that calls `_authorizationHelper.TryGetUmbracoUser(...)` and checks `user.AllowedSections.Contains(Constants.Sections.AI)`.

The DI wiring in `UmbracoBuilderExtensions.cs` becomes:

```csharp
builder.Services.AddSingleton<IAuthorizationHandler, AISectionAuthorizationHandler>();
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(AIAuthorizationPolicies.SectionAccessAI, policy =>
    {
        policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.Requirements.Add(new AISectionRequirement());
    });
});
```

No more pragma, no more obsolete reference, and behaviour is identical to the v17 claim check (CMS internally maps `AllowedApplications` claims onto `IUser.AllowedSections`).

## 2a. EF Core casing rename — `IEfCoreScope<>` → `IEFCoreScope<>` ✅

**Source PR:** umbraco/Umbraco-CMS#22313 ("EF Core: Align casing of EF Core code constructs").

Surfaced during the Phase A build (not on the radar from the release-notes analysis because it was just listed as "EF Core: Align casing" without specifics). Affected 17 `.cs` files across `Umbraco.AI`, `Umbraco.AI.Agent`, `Umbraco.AI.Prompt`, and `Umbraco.AI.Search` persistence layers, plus one XML doc `<see cref>` in a test fixture and one reference in `docs/internal/core/ideas/ai-context.md`.

The fix is purely mechanical — a single `sed 's/IEfCoreScope</IEFCoreScope</g'` over all matching files. `IEFCoreScopeProvider` was already uppercase in our code (we use it via the existing CMS naming); only the scope type itself moved.

---

## 3. Removed Obsoletes We Don't Touch

Verified via grep across `src/`:

- `UmbracoApiController` / `UmbracoApiControllerBase` (PR #22692) — **not used**. We use Umbraco's `ManagementApiControllerBase` exclusively.
- `IEmailSender` (PR #22642) — **not used**.
- `IMemberGroupService` (PR #22632) — **not used**.
- `IDataTypeService` obsolete members (PR #22634) — **not used**.
- `IDomainService`, `IContentTypeBaseService` obsolete members (PR #22629) — **not used**.
- `ILocalizationService` obsolete members (PR #22677) — referenced only in `Umbraco.AI.Prompt.Core/Prompts/AIPromptScopeValidator.cs`. Need to check which members; the obsolete-removal PR scoped to specific signatures.
- `IFileService` obsolete members (PR #22675) — **not used**.
- `IMemberService.GetMembersByPropertyValue` (PR #22678) — **not used**.
- `ApplicationMainUrl` nullable (PR #22558) — **not used**.
- `GetDictionaryValue` nullability (PR #21372) — **not used**.
- `UrlSegment` extension method (PR #22682) — **not used**.
- `MoveEventInfo.NewParent` (PR #22728) — **not used**.
- `MigrationBase` and inter-LTS migrations (PR #22618) — **not used**; our migration prefixes are independent (`UmbracoAI_`, `UmbracoAIPrompt_`, `UmbracoAIAgent_`, `UmbracoAISearch_`).

Verify `ILocalizationService` usage in `AIPromptScopeValidator.cs` against the PR diff once we attempt to compile.

---

## 4. Frontend / Backoffice

### 4.1 `@umbraco-cms/backoffice` v18 — available via MyGet now

The public npm `@umbraco-cms/backoffice` registry hasn't published v18 yet (CMS PR #22670 told template users to keep `^17.3.5`). But the **`umbracoprereleases` MyGet feed** carries the full v18 prerelease train as npm packages — at the time of writing the feed has `18.0.0-beta1`, `18.0.0-beta2`, `18.0.0-rc1`, and `18.0.0-rc2`:

- Feed URL: `https://www.myget.org/F/umbracoprereleases/npm/`
- This is the npm-side mirror of the same MyGet account whose NuGet side (`umbracoprereleases/api/v3/index.json`) we already consume from `scripts/install-package-test-site.{ps1,sh}`.

We don't currently have an `.npmrc` in the repo. The cleanest unblock is a root-level `.npmrc` that scopes only the `@umbraco-cms` namespace to MyGet, leaving everything else on the default registry:

```ini
@umbraco-cms:registry=https://www.myget.org/F/umbracoprereleases/npm/
```

Per-workspace `package.json` `peerDependencies` then bumps to `^18.0.0-rc2` (or whatever's latest at the time we wire it in). Once the v18 stable lands on public npm we drop the `.npmrc` and pin to plain `^18.0.0`. CI also needs the `.npmrc` available at install time — most CI runners pick it up from the repo root automatically; verify on the existing `npm install` step.

### 4.2 UUI v2 — we're insulated (no work)

CMS v18 ships UUI 2.0 (`@umbraco-ui/uui` 2.0.0-rc.0 per the v18 dependency notes). UUI 2 is a significant breaking change for *direct* UUI consumers (84 packages collapsed into one, ESM-only, Lit 3, `<uui-popover>` → `<uui-popover-container>`, `<uui-caret>` → `<uui-symbol-expand>`, import path overhauls). [The announcement](https://github.com/umbraco/Announcements/issues/31) is explicit:

> **Extension developers & CMS package authors:** No action needed. UUI is consumed transitively through the CMS with no tag-name, import-path, or API changes.

Verified — grepping our worktree for `@umbraco-ui/` matches only `package-lock.json` (transitive resolution); zero source code imports. Grepping for `uui-popover` (without `-container`) or `uui-caret` likewise matches only the lock file. We're fully insulated. **No UUI work required.**

### 4.3 `@hey-api/openapi-ts`

CMS bumped to 0.97 (PR #22735). If our schema output meaningfully changes shape on the v18 OpenAPI 3.1.1 output, we may need the same bump to handle it. Pin update only — no API surface change expected.

### 4.4 Node.js toolchain — surfaced during the dev merge

The v18 frontend stack requires **Node.js ≥ 24.13** and **npm ≥ 11**:

- `@umbraco-cms/backoffice@18.0.0-rc2` declares `engines: { node: ">=24.13", npm: ">=11" }`.
- `@umbraco-ui/uui@2.0.0-rc.2` declares the same.
- Vite 8 (which dev bumped to alongside the v18 backoffice peer) brings rolldown 1.0.3 as a direct dep; rolldown's per-platform native binaries don't install cleanly on the older npm.

Symptoms on a stale toolchain (Node 22 / npm 10):

- `npm install` succeeds with `EBADENGINE` warnings.
- `rolldown` is listed in `package-lock.json` but never extracted to `node_modules`.
- `npm run build` fails on the first workspace with `Error [ERR_MODULE_NOT_FOUND]: Cannot find package 'rolldown'`.

**Action:** developers and CI must run on Node 24+ / npm 11+ for the v18 branch. `nvm install 24 && nvm use 24` on Windows; equivalent on Linux/macOS. Consider pinning the Node version in a `.nvmrc` and updating `scripts/install-demo-site.{ps1,sh}` to check.

---

## 5. Versioning & Package Pins

### 5.1 Per-product `version.json` bumps

All twenty products bump to `18.0.0`:

| Product | Current | Target | Notes |
|---|---|---|---|
| Umbraco.AI | 1.12.1 | 18.0.0 | |
| Umbraco.AI.Agent | 1.10.3 | 18.0.0 | |
| Umbraco.AI.Agent.UI | 1.0.1 | 18.0.0 | |
| Umbraco.AI.Agent.Copilot | 1.0.1 | 18.0.0 | |
| Umbraco.AI.Agent.Deploy | 1.0.1 | 18.0.0 | |
| Umbraco.AI.Prompt | 1.8.7 | 18.0.0 | |
| Umbraco.AI.Prompt.Deploy | 1.0.1 | 18.0.0 | |
| Umbraco.AI.Search | 1.0.0-beta9 | 18.0.0-beta1 | Stays prerelease — `Umbraco.Cms.Search` upstream isn't shipping stable yet, so we follow. |
| Umbraco.AI.Deploy | 1.0.3 | 18.0.0 | |
| Umbraco.AI.Automate | 1.0.0-alpha1 | 18.0.0 | Promoted to stable — `Umbraco.Automate` upstream is expected to ship stable by then. |
| Umbraco.AI.OpenAI | 1.2.5 | 18.0.0 | |
| Umbraco.AI.Anthropic | 1.3.5 | 18.0.0 | |
| Umbraco.AI.Google | 1.1.10 | 18.0.0 | |
| Umbraco.AI.Amazon | 1.1.7 | 18.0.0 | |
| Umbraco.AI.MicrosoftFoundry | 1.2.5 | 18.0.0 | |
| Umbraco.AI.Mistral | 1.0.5 | 18.0.0 | |
| Umbraco.AI.HuggingFace | 1.0.5 | 18.0.0 | |
| Umbraco.AI.DeepSeek | 1.0.5 | 18.0.0 | |
| Umbraco.AI.FireworksAI | 1.0.5 | 18.0.0 | |
| Umbraco.AI.TogetherAI | 1.0.5 | 18.0.0 | |

### 5.2 Root `Directory.Packages.props` — CMS pins

Lines 17–23 — bump all `Umbraco.Cms.*` floors from `17.4.0` to the eventual `18.0.0` stable (RC range while testing). Range becomes `[18.0.0, 18.999.999)`.

Line 28 — `Umbraco.Deploy.Infrastructure` — must follow CMS major. Range becomes `[18.0.0, 18.999.999)` once Deploy ships its v18.

Line 33 — `Umbraco.Automate.Core` — separate dependency tree (`0.1.x--preview`); only bump when Automate ships a CMS-v18-compatible version.

Line 38 — `Umbraco.Cms.Search.Core` — separate tree (`1.0.0-beta.5`); bump if it ships a v18-compatible build.

Line 90 — `Microsoft.EntityFrameworkCore.Design` floor (`10.0.6`). Carries the comment "Move this floor in lockstep with the host CMS." Check what EF Core v18 ships against (v18 stays on .NET 10, EF Core 10.x).

Line 91 — `SixLabors.ImageSharp` — per memory ([`feedback_imagesharp_must_match_umbraco_cms`](C:\Users\me\.claude\projects\D--Work-Umbraco-Umbraco-AI\memory\feedback_imagesharp_must_match_umbraco_cms.md)), this must match `Umbraco.Cms`'s transitive version exactly. Check what v18 ships and update the floor in lockstep. **Don't bump independently.**

### 5.3 Inter-product pins in root `Directory.Packages.props`

Lines 50–61 — every `Umbraco.AI.*` range becomes `[18.0.0, 18.999.999)`.

### 5.4 Per-product `Directory.Packages.props` overrides

Each product folder has its own `Directory.Packages.props` with explicit pins to the products it depends on. Every one of these needs to be rewritten from `[1.X.Y, 1.999.999)` to `[18.0.0, 18.999.999)`. Audit per product:

```
Umbraco.AI.Agent/Directory.Packages.props
Umbraco.AI.Agent.UI/Directory.Packages.props
Umbraco.AI.Agent.Copilot/Directory.Packages.props
Umbraco.AI.Agent.Deploy/Directory.Packages.props
Umbraco.AI.Prompt/Directory.Packages.props
Umbraco.AI.Prompt.Deploy/Directory.Packages.props
Umbraco.AI.Search/Directory.Packages.props
Umbraco.AI.Deploy/Directory.Packages.props
Umbraco.AI.Automate/Directory.Packages.props
... (each provider package)
```

(Need to grep for actual presence — some products may not override.)

### 5.5 Frontend version coordination

Each workspace `package.json` may have its own peer dependency on sibling workspaces (e.g. Agent.UI peers `@umbraco-ai/agent: ^1.0.0`). All of these need to bump to `^18.0.0`. Root `peerDependencyVersions` block similarly.

---

## 6. Documentation Impact (`Umbraco.Docs/ai-in-umbraco/1/`)

Public documentation at `D:\Work\Umbraco\Umbraco.Docs\ai-in-umbraco\1\` was audited for references to surfaces this migration touches. The doc impact is small — most user-facing API endpoints (`/umbraco/ai/management/api/v1/...`) are *our* routes and don't move; only the OpenAPI-document URLs change (`/umbraco/swagger/...` → `/umbraco/openapi/...`), and those aren't referenced in the public docs.

### 6.1 Required updates

| File | Line | Change |
|---|---|---|
| `README.md` | 92 | "Umbraco CMS 17.1 or later" → "Umbraco CMS 18.0 or later" |
| `getting-started/README.md` | 14 | "Umbraco CMS 17.1 or later installed" → "Umbraco CMS 18.0 or later" |
| `add-ons/deploy/installation.md` | 13 | "Umbraco CMS 17.0 or later" → "Umbraco CMS 18.0 or later" |

### 6.2 Verify-after-migration (likely fine, must confirm against the golden-file diff)

These files contain `$type` discriminator examples for our three polymorphic hierarchies. The `$type` discriminator survives the OpenAPI 3.0 → 3.1 transition — but the *generated TS client type names* may change unless we successfully wire `CreateSchemaReferenceId` (Decision #4). If we end up accepting renames, the textual examples in these files don't change but any prose referring to TS type names does:

| File | Line(s) | What to confirm |
|---|---|---|
| `add-ons/agent/api/create.md` | 31, 56 | `"$type": "standard"` and `"$type": "orchestrated"` request body examples — discriminator value unchanged, JSON shape unchanged |
| `add-ons/agent/reference/ai-agent.md` | 27, 56, 69–76 | `IAIAgentConfig?` interface signature, `AIAgentType` enum — these are C# types we own, unaffected by OpenAPI change |
| `add-ons/agent/scopes.md` | 69 | `AIStandardAgentConfig` C# code example — unaffected |
| `management-api/profiles/create.md` | 35 | `"$type": "chat"` request body — same as above |

### 6.3 No-change confirmations (do not edit)

- `management-api/**/*.md` — all our REST endpoints under `/umbraco/ai/management/api/v1/` are stable. The OpenAPI document URL changes but the documented REST API endpoints don't.
- `getting-started/installation.md` — `dotnet add package` snippets don't pin version numbers (good practice).
- `extending/providers/creating-a-provider.md:266` — example `<Version>1.0.0</Version>` is a placeholder in a *custom provider* `.csproj` example, not an Umbraco.AI version.
- No public doc references `WithUmbracoAIManagementApi`, so its redesign (Decision #3) is invisible to documented consumers.
- No public doc shows `/umbraco/swagger/...` URLs.

### 6.4 Where doc updates land

The docs repo is independent of this monorepo. After the code migration ships, raise a corresponding PR against `Umbraco.Docs` covering §6.1 (and §6.2 if the polymorphic preservation work ends up changing client type names). Don't try to land code + docs in lockstep — version-string updates can ship after the package release.

## 7. Release Manifest

`/release-management` will need to create a `release/2026.05.N` (or `release/2026.06.N`) branch with `release-manifest.json` listing **every product**. Recommend the object form with empty `exclude`:

```json
{ "include": ["Umbraco.AI", "Umbraco.AI.Agent", "...", "Umbraco.AI.TogetherAI"], "exclude": [] }
```

so CI accounts for everything in one ship.

---

## 8. Suggested Sequencing

This is a non-trivial migration and should be staged so we can validate each phase against a real running CMS-v18-rc1 site before moving on. Concrete phasing:

1. **Phase A — Dependency pins (compile-only)**
   - Bump CMS pins in `Directory.Packages.props` to `[18.0.0-rc1, 18.999.999)`.
   - Compile. Expect failures only in the OpenAPI-touching files plus the `AllowedApplicationsClaimType` line.

2. **Phase B — Auth claim fix**
   - Swap the `AllowedApplicationsClaimType` pragma block (§2) for the v18 policy/requirement.
   - Compile again.

3. **Phase C — OpenAPI port**
   - Rewrite `WithUmbracoAIManagementApi` and the four filter/handler files (§1.5).
   - Compile. Stand up the demo site. Verify all three OpenAPI documents render at `/umbraco/openapi/{name}.json`.
   - Capture golden-file diff against v17 baseline (§1.4). Iterate until clean.

4. **Phase D — Frontend URL renames**
   - Update four `generate-openapi.js` files and the CLAUDE.md doc snippet.
   - Run `npm run generate-client` against the v18 site, `git diff` the generated TypeScript clients.

5. **Phase E — Version alignment**
   - Bump all twenty `version.json` files to `18.0.0`.
   - Rewrite inter-product ranges in root `Directory.Packages.props` and per-product overrides.
   - Add root `.npmrc` scoping `@umbraco-cms` to the MyGet `umbracoprereleases` feed.
   - Bump root `peerDependencyVersions["@umbraco-cms/backoffice"]` to `^18.0.0-rc2` (latest on MyGet at the time) and propagate to every workspace `package.json`.

6. **Phase F — Release manifest + changelog**
   - Run `/release-management` to detect changed products, generate the manifest, and write changelogs.
   - Commit on a `release/2026.MM.N` branch.

7. **Phase G — Pin cleanup (post-public-npm)**
   - When `@umbraco-cms/backoffice` v18 lands on public npm, drop the `.npmrc` MyGet line and re-pin peer deps to plain `^18.0.0`. This is a follow-up commit, not a blocker for shipping.

Each phase is its own commit (or set of related commits) so reverts are clean.

---

## 9. Decisions

All four open questions have been resolved:

1. **Prerelease tags** — `Umbraco.AI.Automate` promotes to **stable `18.0.0`** (upstream `Umbraco.Automate` is expected to ship stable in time). `Umbraco.AI.Search` stays prerelease as **`18.0.0-beta1`** until `Umbraco.Cms.Search` upstream ships stable. Table in §5.1 reflects this.
2. **Target CMS version** — track **`18.0.0-rc2`** during development. Flip the floor to **`18.0.0`** in a follow-up commit before the release branch is cut.
3. **`WithUmbracoAIManagementApi` contract** — **redesign for clarity, keep it open for extension.** Drop the unused `Action<SwaggerGenOptions>` parameter and take `apiTitle` / `apiDescription` as strings; expose an `Action<OpenApiOptions>` callback for downstream extension (transformers, custom JsonOptions, etc.). Document the new shape in the Web project's CHANGELOG entry — this is a breaking change for any external caller, but they have to recompile against v18 anyway.
4. **Polymorphic schema naming** — **try to preserve v17 client type names via a custom `CreateSchemaReferenceId` delegate.** If the implementation gets messy (fighting the framework, brittle string-matching, etc.), stop and escalate with a written summary of what was tried and why it isn't clean. Avoid technical debt for cosmetic naming parity. If we end up accepting renames, document them in the release notes so consumers can update their generated clients in one pass.

(The previous "wait for public npm" question was resolved earlier — MyGet npm feed unblocks the frontend; see §4.1.)
