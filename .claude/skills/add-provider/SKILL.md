---
name: add-provider
description: Scaffolds a new AI provider package (Umbraco.AI.X) following the established provider plugin pattern. Use when adding support for a new AI vendor (e.g., Cohere, Groq, xAI, DeepSeek). Covers code, config, marketplace metadata, install scripts, and CI registration.
argument-hint: [vendor name, e.g. "Cohere"]
---

# Add AI Provider

Scaffold a new Umbraco.AI provider package for a given AI vendor, matching the conventions of the 6 existing providers (OpenAI, Anthropic, Google, Amazon, MicrosoftFoundry, Mistral).

## When to use

User asks to "add a provider for X", "wire up Y to Umbraco.AI", or similar.

**Anthropic and Mistral are the simplest references to copy from** — single-project structure, chat-only (Anthropic) or chat + embedding (Mistral). Read those before writing new code.

## Current environment

- Working directory: !`pwd`
- Git branch: !`git branch --show-current 2>/dev/null || echo "not in git repo"`

## Workflow

1. Research the vendor's .NET SDK
2. Decide capabilities and scope
3. Create feature branch
4. Scaffold the provider
5. Register across the monorepo
6. Build + smoke-test in the demo site
7. Commit + push + PR

---

## 1. Research phase

Before writing any code, answer these questions. Web searches + `WebFetch` on the SDK repo are usually enough.

| Question | Why it matters |
|---|---|
| Is there a Microsoft.Extensions.AI-compatible .NET SDK? | If yes, wiring is trivial. If no, consider wrapping an OpenAI-compatible endpoint or writing a thin HTTP client. |
| What's the NuGet package name + latest stable version? | Goes into `Directory.Packages.props`. |
| License? | MIT/Apache preferred. Flag restrictive licenses to user. |
| How do you get `IChatClient` from the SDK's client? | Determines the `CreateClient` body. Common patterns: `.AsIChatClient(modelId)`, `.SomeProperty` (already IChatClient), or wrap via `ChatClientBuilder`. |
| How do you get `IEmbeddingGenerator<string, Embedding<float>>`? | Same question for embeddings. |
| How do you list models? | Usually `client.Models.GetAsync()` or similar — returns a list with `.Id` per model. |
| What's the default endpoint? Can it be overridden? | Determines whether `Settings` has an `Endpoint` field. |
| Model naming conventions? | Drives the regex include/exclude patterns in capabilities. List a few examples. |

If the SDK doesn't expose a `models` endpoint, the capability can return a hard-coded list from a static array.

## 2. Decide scope

Match the user's intent. By default, if unsure:

| Capability | Include |
|---|---|
| Chat | Yes if SDK supports text generation (nearly always) |
| Embedding | Yes if the vendor offers embedding models |
| Speech-to-text | Only if explicitly asked — rare |

Skip unusual capabilities (moderation, OCR, image gen) unless Umbraco.AI.Core has a capability base class for them. Check `Umbraco.AI/src/Umbraco.AI.Core/Providers/` for `AI*CapabilityBase` classes before promising support.

## 3. Feature branch

```bash
git checkout -b feature/<provider-id>-provider
```

## 4. Scaffold the provider

Directory layout (use Anthropic as the template):

```
Umbraco.AI.<ProviderName>/
├── src/Umbraco.AI.<ProviderName>/
│   ├── <ProviderName>Provider.cs
│   ├── <ProviderName>ProviderSettings.cs
│   ├── <ProviderName>ChatCapability.cs
│   ├── <ProviderName>EmbeddingCapability.cs        # if applicable
│   ├── <ProviderName>ModelUtilities.cs
│   ├── Umbraco.AI.<ProviderName>.csproj
│   ├── .gitignore                                   # contains: !wwwroot/
│   └── wwwroot/
│       ├── umbraco-package.json
│       └── lang/en.js
├── Umbraco.AI.<ProviderName>.slnx
├── Directory.Build.props
├── version.json                                     # start at "1.0.0"
├── changelog.config.json                            # { "scopes": ["<provider-id>"] }
├── CHANGELOG.md
├── README.md
├── CLAUDE.md
├── umbraco-marketplace.json
└── umbraco-marketplace-readme.md
```

### Key code patterns to copy

**Provider class** — `[AIProvider("<id>", "<Display Name>")]`, inherit `AIProviderBase<TSettings>`, take `IAIProviderInfrastructure` + `IMemoryCache` in ctor, call `WithCapability<...>()` for each capability, expose a `static CreateXClient(settings)` factory and an `internal async Task<IReadOnlyList<string>> GetAvailableModelIdsAsync(settings, ct)` with 1-hour caching keyed off the API key hash.

**Settings class**:

```csharp
public class <ProviderName>ProviderSettings
{
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    // Optional — omit if vendor doesn't support custom endpoints
    [AIField]
    public string? Endpoint { get; set; } = "https://api.vendor.com";
}
```

**Capability class** — primary constructor taking provider, inherit `AIChatCapabilityBase<TSettings>`, define `DefaultXModel` const, include/exclude regex arrays, override `GetModelsAsync` (filter cached model list + map through `<ProviderName>ModelUtilities.FormatDisplayName`), override `CreateClient` (or `CreateClientAsync` if model resolution needs the API).

**Creating the `IChatClient`** — depends on the SDK:

```csharp
// Pattern A: SDK has .AsIChatClient(modelId) that bakes modelId in
return <Provider>Provider.Create<Provider>Client(settings).AsIChatClient(modelId ?? DefaultChatModel);

// Pattern B: SDK exposes an IChatClient but doesn't take modelId
var client = <Provider>Provider.Create<Provider>Client(settings).Completions; // or similar
return new ChatClientBuilder(client)
    .ConfigureOptions(options => options.ModelId ??= modelId ?? DefaultChatModel)
    .Build();
```

Same decision applies to `IEmbeddingGenerator` — use `EmbeddingGeneratorBuilder<string, Embedding<float>>` for Pattern B.

**ModelUtilities** — lives in `namespace Umbraco.AI.Extensions` (NOT the provider's namespace), `internal static class`, exposes `FormatDisplayName(string modelId)`. Format rules vary per vendor. Look at `AnthropicModelUtilities` (handles date suffixes, compound version numbers) for a non-trivial example and `MistralModelUtilities` for a simple one.

### csproj

Copy Anthropic's csproj verbatim, change:
- `<Title>`, `<Description>`, `<PackageTags>` — vendor-specific
- `<StaticWebAssetBasePath>App_Plugins/UmbracoAI<ProviderName></StaticWebAssetBasePath>` — must match `wwwroot/umbraco-package.json` path references
- `<PackageReference Include="<VendorSdkPackage>" />` — the SDK
- `InternalsVisibleTo` target — `Umbraco.AI.<ProviderName>.Tests.Unit` (even though no tests exist)

### wwwroot/umbraco-package.json

```json
{
  "name": "Umbraco AI <ProviderName> Provider",
  "$schema": "../umbraco-package-schema.json",
  "extensions": [
    {
      "type": "localization",
      "alias": "Uai.<ProviderName>.Localization.En",
      "weight": -100,
      "name": "English",
      "meta": { "culture": "en" },
      "js": "/App_Plugins/UmbracoAI<ProviderName>/lang/en.js"
    }
  ],
  "version": "1.0.0"
}
```

### wwwroot/lang/en.js

```javascript
export default {
    uaiFields: {
        <providerId>ApiKeyLabel: "<ProviderName> API Key",
        <providerId>ApiKeyDescription: "Enter your <ProviderName> API key to enable AI features.",
        // Add entries for any extra Settings fields (endpoint, orgId, etc.)
    },
};
```

The `uaiFields.<providerId><PropertyName>Label` / `Description` convention is what binds the UI strings to the `[AIField]` properties via the provider's `id`.

### Other top-level files

Copy Anthropic's versions and adjust:
- **`Directory.Build.props`** — change `<Product>` and `<PackageProjectUrl>`. Shared logo reference (`../assets/logo-128.png`) and LICENSE stay as-is.
- **`Umbraco.AI.<ProviderName>.slnx`** — single project reference
- **`version.json`** — start at `"1.0.0"`, copy rest verbatim
- **`changelog.config.json`** — `{ "scopes": ["<provider-id>"] }` — this is what makes the scope valid for commitlint
- **`CHANGELOG.md`** — Initial release entry with today's date
- **`README.md`** — describe features, models, requirements
- **`CLAUDE.md`** — per-package dev guide. Note: the Anthropic/OpenAI CLAUDE.mds have slightly stale examples — always read actual source for current conventions.
- **`umbraco-marketplace.json`** — `Category: "Artificial Intelligence"`, list provider-appropriate tags. Update `RelatedPackages` to point to two or three other providers.
- **`umbraco-marketplace-readme.md`** — short marketplace description

## 5. Register across the monorepo

These edits are mandatory. Missing any one means CI or the demo site won't pick up your provider.

| File | Change |
|---|---|
| `Directory.Packages.props` (root) | Add `<PackageVersion Include="<VendorSdkPackage>" Version="x.y.z" />` under the "Provider packages" group |
| `Umbraco.AI.slnx` (root) | Add `<Folder Name="/Providers/<ProviderName>/">` with the csproj — keep alphabetical order |
| `scripts/install-demo-site.sh` | Two places: `add_product_projects "Umbraco.AI.<ProviderName>" "<ProviderName>"` and `dotnet add "$DEMO_PROJECT" reference …` |
| `scripts/install-demo-site.ps1` | Same, PS1 syntax |
| `scripts/install-package-test-site.sh` | `dotnet add package Umbraco.AI.<ProviderName> $PRERELEASE_FLAG` in the provider section |
| `scripts/install-package-test-site.ps1` | `Install-Package "Umbraco.AI.<ProviderName>"` in the provider section |
| `azure-pipelines.yml` | Add entry to `level1Products` matrix with `name`, `changeVar: <ProperCase>Changed`, `hasNpm: false` |

### Do NOT touch these (I've verified — adding your provider here would be inconsistent with the established pattern)

- `.github/ISSUE_TEMPLATE/01_bug_report.yml` / `.github/DISCUSSION_TEMPLATE/ideas.yml` — list only Core + OpenAI + Anthropic + a few add-ons. Google/Amazon/MicrosoftFoundry/Mistral aren't there either. Needs a separate coordinated cleanup.
- `.github/workflows/auto-labeler.yml` — same rationale.
- `.github/actions/pack-product/action.yml` and `.azure-pipelines/templates/*.yml` — parametric, no hard-coded provider list.
- `release-manifest.json` — only required on `release/*` branches; the release skill picks up new products automatically.

## 6. Build + test

```bash
dotnet build Umbraco.AI.<ProviderName>/Umbraco.AI.<ProviderName>.slnx
```

First build generates `packages.lock.json` — commit it.

Then start the demo site:

```
/demo-site-management start
```

If demo not yet installed:

```
/repo-setup   # pick "Demo site only" to skip the heavier stuff, or "Full setup" first time
```

Log in (admin@example.com / password1234) → AI section → Connections → New connection. Your provider should appear in the dropdown. Plug in a real API key, create a profile, try the provider from any AI feature.

**Providers don't have test projects by convention.** Manual smoke test via the demo site is the validation. If the provider doesn't show up in the dropdown, check that:
- `[AIProvider]` attribute is present and the class is public
- `packages.lock.json` was regenerated (delete + rebuild if in doubt)
- The demo site's csproj (gitignored, created per-dev by the installer) includes a ProjectReference to your provider — if you ran the install-demo-site script, this should happen automatically

## 7. Commit + push + PR

Two-commit layout works well:

```bash
# Commit 1 — core provider package
git add Directory.Packages.props Umbraco.AI.slnx Umbraco.AI.<ProviderName>/
git commit -m "feat(<provider-id>): Add <ProviderName> AI provider"

# Commit 2 — registration elsewhere
git add azure-pipelines.yml scripts/install-demo-site.{sh,ps1} scripts/install-package-test-site.{sh,ps1}
git commit -m "chore(<provider-id>,ci): Register <ProviderName> in install scripts and CI pipeline"

git push -u origin feature/<provider-id>-provider
```

Commitlint enforces: sentence-case subject, scope declared in a `changelog.config.json`, valid types. Your new `<provider-id>` scope is picked up automatically from the `changelog.config.json` you added.

Then open the PR via the URL GitHub prints, or `gh pr create`.

## Gotchas (learned from adding Mistral)

- **`demo/Umbraco.AI.DemoSite/*.csproj` is gitignored** — the installer generates it per-developer. Adding a ProjectReference to your local copy isn't enough; you MUST update the install scripts, or other developers won't have your provider registered.
- **`wwwroot/` at the repo root is gitignored** — opt back in per-provider with a local `.gitignore` containing `!wwwroot/` inside the provider's source directory.
- **Providers have no test projects** — every provider's csproj declares `InternalsVisibleTo "Umbraco.AI.<ProviderName>.Tests.Unit"`, but the test projects don't actually exist. Keep the attribute for consistency; don't create a test project just for your provider — it'd be the only one and set an inconsistent precedent.
- **CLAUDE.md in existing providers is slightly stale** — Anthropic's shows `[AIField("api-key", "API Key", AIFieldType.Password)]` but the actual source uses `[AIField(IsSensitive = true)]`. Always read the actual `.cs` file when matching conventions, not the docs.
- **Modeld filtering relies on conventions** — if the vendor adds a new model family next year, your regex won't cover it. Prefer broader patterns (e.g., `^mistral-` catches all current and future `mistral-*` families) over hard-coded model lists.
- **Vendor SDK may not bake modelId into its IChatClient** — use the `ChatClientBuilder.ConfigureOptions(o => o.ModelId ??= …)` pattern in that case. Same for embeddings with `EmbeddingGeneratorBuilder`.
- **`npm install` sometimes times out** on first run — `npm install --fetch-timeout=600000` is the workaround.

## Reference providers (by complexity)

| Provider | Good example of |
|---|---|
| Umbraco.AI.Anthropic | Simplest single-capability provider; `.AsIChatClient(modelId)` pattern |
| Umbraco.AI.Mistral | Chat + embedding; `ChatClientBuilder.ConfigureOptions` pattern for SDKs that don't bake modelId |
| Umbraco.AI.Google | Async model resolution (`CreateClientAsync`); source-generated regex |
| Umbraco.AI.OpenAI | Most capabilities (chat, embedding, speech-to-text); multiple model ID namespaces |
