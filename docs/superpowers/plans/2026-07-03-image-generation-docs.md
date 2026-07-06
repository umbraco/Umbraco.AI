# Image Generation Capability Documentation — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Document the experimental Image Generation capability across the `17/` and `18/` Umbraco.AI doc lines, at full parity with the existing Speech-to-Text capability docs.

**Architecture:** Documentation-only change in the `Umbraco.Docs` repo. Each page is authored in `18/ai-in-umbraco/` and mirrored byte-for-byte to `17/ai-in-umbraco/` with `cp` (all target files are currently identical across the two lines). Content is grounded in verified source signatures — no invented APIs. Every new page carries an experimental-warning hint; the canonical "how to enable" section lives once in the Using-the-API overview and is cross-linked.

**Tech Stack:** GitBook markdown (`{% code %}`, `{% hint %}`, `{% content-ref %}` blocks, YAML frontmatter), `SUMMARY.md` navigation.

## Global Constraints

- **Repo:** `Umbraco.Docs`. **Branch:** `ai/image-generation-docs` (already created and checked out).
- **Two version lines, byte-identical:** every file lands in **both** `17/ai-in-umbraco/…` and `18/ai-in-umbraco/…`. Author in `18/`, then `cp` to `17/`. No version-specific content — the capability is identical on both lines.
- **Experimental framing:** feature flag is `Umbraco:AI:Experimental:ImageGeneration` (default `false`); diagnostic id is `UMBRACOAI_IMAGEGEN`; M.E.AI diagnostic is `MEAI001`. When disabled: capability hidden from discovery, not selectable in the profile editor, REST endpoint returns **404**.
- **Enablement is config-only** via `appsettings.json`. No backoffice toggle enables it; once enabled, a "Default Image Generation Profile" setting appears under Settings > AI > Settings.
- **Only OpenAI** currently implements Image Generation. Default model `gpt-image-1`; also matches `dall-e-*`.
- **Enum:** `AICapability.ImageGeneration = 5`.
- **Management API route:** `/umbraco/ai/management/api/v1/image-generation/generate` (route segment `image-generation`, Swagger group `Image Generation`).
- **Style:** follow the `umbraco-docs-content` skill (sentence-case headings, GitBook frontmatter `description`, code-fence titles). Invoke `umbraco-docs-content` before writing prose in each content task.
- **Commit trailer:** end every commit message with
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`
- **All C# / TypeScript snippets in this plan are copied from verified source** (see the design doc dated 2026-07-03). Reproduce them exactly; do not paraphrase signatures.

---

## Verified source reference (do not re-derive)

**`IAIImageGenerationService`** (`Umbraco.AI.Core.ImageGeneration`, carries `[Experimental(UMBRACOAI_IMAGEGEN)]`):

```csharp
Task<ImageGenerationResponse> GenerateImagesAsync(
    Action<AIImageGenerationBuilder> configure, string prompt, CancellationToken cancellationToken = default);

Task<ImageGenerationResponse> GenerateImagesAsync(
    Action<AIImageGenerationBuilder> configure, string prompt,
    IEnumerable<AIContent>? originalImages, CancellationToken cancellationToken = default);

Task<IImageGenerator> CreateImageGeneratorAsync(
    Action<AIImageGenerationBuilder> configure, CancellationToken cancellationToken = default);

Task<AITrackedImageResult<TResult>> InvokeWithTrackingAsync<TResult>(
    Action<AIImageGenerationBuilder> configure,
    Func<IImageGenerator, CancellationToken, Task<AITrackedImageResult<TResult>>> operation,
    CancellationToken cancellationToken = default);

Task<AISupportedImageModels> GetSupportedModelsAsync(
    Action<AIImageGenerationBuilder> configure, CancellationToken cancellationToken = default);
```

**`AIImageGenerationBuilder`** methods: `.WithAlias(string)` (**required**), `.WithName(string)`, `.WithDescription(string?)`, `.WithProfile(Guid)`, `.WithProfile(string)`, `.WithImageGenerationOptions(ImageGenerationOptions)`, `.WithOriginalImages(IEnumerable<AIContent>)`, `.WithContextItems(IEnumerable<AIRequestContextItem>)`, `.WithGuardrails(params Guid[])`, `.WithGuardrails(params string[])`, `.SetGuardrails(params Guid[])`, `.SetGuardrails(params string[])`, `.WithAdditionalProperties(IReadOnlyDictionary<string, object?>)`.

**`AISupportedImageModels`**: `IReadOnlyList<AIModelDescriptor> Models` (each descriptor's `Metadata` carries per-model constraints such as `image.supportedSizes`, `image.maxEdge`, `image.supportsEdit`, `image.supportsMask`) and `string ModelId` (the resolved bound model).

**`AITrackedImageResult<TResult>`**: `TResult Result` (required), `UsageDetails? Usage`, `int? ImageCount`.

**Provider capability base** (`Umbraco.AI.Core.Providers`, carries `[Experimental(UMBRACOAI_IMAGEGEN)]` via the interface):

```csharp
public abstract class AIImageGeneratorCapabilityBase<TSettings>(IAIProvider provider)
    : AICapabilityBase<TSettings>(provider), IAICapability<TSettings>, IAIImageGeneratorCapability
    where TSettings : class
{
    public override AICapability Kind => AICapability.ImageGeneration;

    // Override this (or CreateGeneratorAsync) to create an IImageGenerator
    protected virtual IImageGenerator CreateGenerator(TSettings settings, string? modelId) { /* ... */ }

    protected virtual Task<IImageGenerator> CreateGeneratorAsync(
        TSettings settings, string? modelId, CancellationToken cancellationToken = default) { /* ... */ }

    // Implement this: return available models (with constraint metadata)
    protected abstract Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        TSettings settings, CancellationToken cancellationToken = default);
}
```

**OpenAI GetService escape hatch:** the OpenAI generator wraps the M.E.AI adapter so `generator.GetService(typeof(OpenAI.OpenAIClient))` returns the un-bound `OpenAIClient`; from there call `.GetImageClient("gpt-image-1")` for provider-native masked outpainting (Tier 3). Raw calls made this way bypass usage/audit middleware — wrap them in `InvokeWithTrackingAsync` to stay visible.

**Management API request model (`GenerateImageRequestModel`):** `string Prompt` (required), `string? ProfileIdOrAlias`, `int? Count`, `string? Size` (`"{w}x{h}"`), `string? ResponseFormat` (`"url"`/`"data"`/`"hosted"`), `IReadOnlyList<ImageInputModel>? OriginalImages` (each: `string Data` base64 no prefix, `string MediaType`). **Response (`GenerateImageResponseModel`):** `Images[]` (each `string? Data`, `string? Url`, `string? MediaType`) + optional `Usage` (`InputTokens`, `OutputTokens`, `TotalTokens`).

**Frontend (`@umbraco-ai/core`):** `UaiImageGenerationController` with `async generate(prompt: string, options?: UaiImageGenerationOptions): Promise<{ data?: UaiImageGenerationResult; error?: unknown }>`. `UaiImageGenerationOptions`: `profileIdOrAlias?`, `count?`, `size?`, `responseFormat?`, `originalImages?: UaiImageInput[]`, `signal?`. `UaiImageGenerationResult`: `images: UaiGeneratedImage[]` (`data?`, `url?`, `mediaType?`) + `usage?`. `UaiImageInput`: `{ data, mediaType }`.

**Profile settings (`AIImageGenerationProfileSettings`):** `Size?`, `Quality?`, `Style?`, `MediaType?` (policy defaults; count/response-format are per-call, not here).

---

## File Structure

New files (each authored in `18/ai-in-umbraco/`, mirrored to `17/ai-in-umbraco/`):

| File | Responsibility |
|------|----------------|
| `using-the-api/image-generation/README.md` | Capability overview, **canonical enablement section**, the 3 tiers |
| `using-the-api/image-generation/generating-images.md` | Text-to-image via `GenerateImagesAsync`; `GetSupportedModelsAsync` validation |
| `using-the-api/image-generation/editing-images.md` | Maskless edit (Tier 2), masked outpainting escape hatch (Tier 3), `InvokeWithTrackingAsync` |
| `extending/providers/image-generation-capability.md` | Implement `AIImageGeneratorCapabilityBase<TSettings>` |
| `management-api/image-generation/README.md` | REST section overview + endpoint table |
| `management-api/image-generation/generate-image.md` | `POST /image-generation/generate` reference |
| `frontend/image-generation-controller.md` | `UaiImageGenerationController` usage |

Edited files (edit in `18/`, `cp` to `17/`):

| File | Change |
|------|--------|
| `reference/models/ai-capability.md` | Add `ImageGeneration = 5` |
| `concepts/capabilities.md` | Add capability row, section, interface, profile relationship, `HasCapability` example |
| `reference/configuration/ai-options.md` | Add `DefaultImageGenerationProfileAlias` + `Umbraco:AI:Experimental` section |
| `providers/openai.md` | Add Image Generation to description + intro |
| `providers/README.md` | Add Image Generation rows to capability matrices |
| `SUMMARY.md` | Wire all new pages into navigation |

**Mirror + SUMMARY convention used in every task:**
1. Create/edit the file(s) under `18/ai-in-umbraco/`.
2. Add the nav entry to `18/ai-in-umbraco/SUMMARY.md`.
3. Mirror to `17/`: `cp 18/ai-in-umbraco/<path> 17/ai-in-umbraco/<path>` for each changed file **and** for `SUMMARY.md`.
4. Verify parity + link resolution, then commit.

---

## Task 1: Add `ImageGeneration` to the AICapability reference

**Files:**
- Modify: `18/ai-in-umbraco/reference/models/ai-capability.md`
- Mirror: `17/ai-in-umbraco/reference/models/ai-capability.md`

**Interfaces:**
- Produces: the reference page that later pages link to for the enum value.

- [ ] **Step 1: Edit the enum definition block** in `18/ai-in-umbraco/reference/models/ai-capability.md`

Replace:
```csharp
public enum AICapability
{
    Chat = 0,
    Embedding = 1,
    SpeechToText = 4
}
```
with:
```csharp
public enum AICapability
{
    Chat = 0,
    Embedding = 1,
    SpeechToText = 4,
    ImageGeneration = 5
}
```

- [ ] **Step 2: Add the values-table row.** After the `SpeechToText` row in the Values table, add:
```
| `ImageGeneration` | 5   | Text-to-image and image editing      | Experimental |
```

- [ ] **Step 3: Update the Notes list.** Replace the line
  `- \`Chat\`, \`Embedding\`, and \`SpeechToText\` are currently implemented`
  with two lines:
```
- `Chat`, `Embedding`, and `SpeechToText` are generally available
- `ImageGeneration` is experimental — hidden unless the `Umbraco:AI:Experimental:ImageGeneration` flag is enabled (see [Using the Image Generation API](../../using-the-api/image-generation/README.md))
```

- [ ] **Step 4: Mirror to 17**

```bash
cp 18/ai-in-umbraco/reference/models/ai-capability.md 17/ai-in-umbraco/reference/models/ai-capability.md
```

- [ ] **Step 5: Verify parity**

Run: `diff 17/ai-in-umbraco/reference/models/ai-capability.md 18/ai-in-umbraco/reference/models/ai-capability.md && echo PARITY_OK`
Expected: `PARITY_OK` (no diff output).

- [ ] **Step 6: Commit**

```bash
git add 17/ai-in-umbraco/reference/models/ai-capability.md 18/ai-in-umbraco/reference/models/ai-capability.md
git commit -m "docs(ai): Add ImageGeneration to AICapability reference

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Add the Image Generation section to the Capabilities concept page

**Files:**
- Modify: `18/ai-in-umbraco/concepts/capabilities.md`
- Mirror: `17/ai-in-umbraco/concepts/capabilities.md`

**Interfaces:**
- Consumes: the enum reference from Task 1 (link target).
- Produces: the concept anchor other pages link to via `../concepts/capabilities.md`.

- [ ] **Step 1: Add to the "Available Capabilities" table.** Add a row after Speech-to-Text:
```
| **Image Generation** | Text-to-image generation and image editing (experimental) | `IImageGenerator` |
```

- [ ] **Step 2: Add a capability section** after the "Speech-to-Text Capability" section:

```markdown
## Image Generation Capability

{% hint style="warning" %}
Image Generation is **experimental**. It is hidden unless the `Umbraco:AI:Experimental:ImageGeneration` feature flag is enabled, and the C# API surface carries the `UMBRACOAI_IMAGEGEN` diagnostic, which consumers must suppress. See [Using the Image Generation API](../using-the-api/image-generation/README.md) for how to enable it.
{% endhint %}

The Image Generation capability produces images from text prompts:

- Text-to-image generation
- Maskless image editing (transform supplied images)
- Provider-native masked outpainting via an escape hatch
- Per-model size and capability constraints

{% code title="Example.cs" %}

```csharp
#pragma warning disable UMBRACOAI_IMAGEGEN

var response = await _imageGenerationService.GenerateImagesAsync(
    img => img.WithAlias("hero-banner"),
    "A serene mountain landscape at dawn");

// response.Contents contains the generated image content
```

{% endcode %}
```

- [ ] **Step 3: Add to the profile-relationship diagram.** After the `Speech-to-Text Profile` block in the code diagram, add:
```
Image Generation Profile
    ├── Capability: ImageGeneration
    ├── Connection: OpenAI Prod
    ├── Model: gpt-image-1
    └── Settings: AIImageGenerationProfileSettings
```

- [ ] **Step 4: Add a `HasCapability` example.** After the `IAISpeechToTextCapability` check block:
```csharp
if (provider.HasCapability<IAIImageGeneratorCapability>())
{
    // Provider supports image generation
}
```

- [ ] **Step 5: Add a Related link.** In the "Related" list, add:
```
- [Image Generation API](../using-the-api/image-generation/README.md) - Use the Image Generation capability
```

- [ ] **Step 6: Mirror to 17**

```bash
cp 18/ai-in-umbraco/concepts/capabilities.md 17/ai-in-umbraco/concepts/capabilities.md
```

- [ ] **Step 7: Verify parity**

Run: `diff 17/ai-in-umbraco/concepts/capabilities.md 18/ai-in-umbraco/concepts/capabilities.md && echo PARITY_OK`
Expected: `PARITY_OK`.

- [ ] **Step 8: Commit**

```bash
git add 17/ai-in-umbraco/concepts/capabilities.md 18/ai-in-umbraco/concepts/capabilities.md
git commit -m "docs(ai): Document Image Generation in the capabilities concept

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Using-the-API overview + canonical enablement

**Files:**
- Create: `18/ai-in-umbraco/using-the-api/image-generation/README.md`
- Modify: `18/ai-in-umbraco/SUMMARY.md`
- Mirror both to `17/`.

**Interfaces:**
- Produces: `using-the-api/image-generation/README.md` — the canonical "Enabling image generation" anchor that every other new page links to.

- [ ] **Step 1: Invoke the docs content skill.** `Skill: umbraco-docs-content` — follow its style rules while writing this page.

- [ ] **Step 2: Create `18/ai-in-umbraco/using-the-api/image-generation/README.md`** with this exact content:

````markdown
---
description: >-
    Generate and edit images from text prompts using the experimental Image Generation API.
---

# Image Generation

{% hint style="warning" %}
Image Generation is **experimental**. The API surface may change between releases. It is disabled by default — see [Enabling image generation](#enabling-image-generation) below.
{% endhint %}

The Image Generation API produces images from text prompts and edits existing images. It is a thin layer over [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/)'s `IImageGenerator`, adding Umbraco profiles, connections, guardrails, and observability (auditing, telemetry, usage tracking).

## Enabling image generation

Image Generation is gated behind a feature flag. Until it is enabled:

- the capability is hidden from discovery and is **not selectable** in the profile editor,
- profiles using it cannot be created, and
- the Management API endpoint returns **404**.

Enable it in `appsettings.json`:

{% code title="appsettings.json" %}

```json
{
    "Umbraco": {
        "AI": {
            "Experimental": {
                "ImageGeneration": true
            }
        }
    }
}
```

{% endcode %}

The C# API surface is annotated with the `UMBRACOAI_IMAGEGEN` diagnostic. Because it is experimental, suppress the diagnostic in files that call it:

{% code title="C#" %}

```csharp
#pragma warning disable UMBRACOAI_IMAGEGEN
```

{% endcode %}

Once enabled, restart the site. A **Default Image Generation Profile** setting appears under **Settings > AI > Settings**, and Image Generation becomes selectable when creating a profile.

## What you can do

Image Generation supports three tiers of use:

| Tier | Scenario | How |
| --- | --- | --- |
| 1. Text-to-image | Generate images from a prompt | [`GenerateImagesAsync`](generating-images.md) |
| 2. Maskless edit | Transform supplied images with a prompt | [`GenerateImagesAsync` with original images](editing-images.md) |
| 3. Masked outpainting | Provider-native masked edits | [`GetService` escape hatch](editing-images.md#masked-outpainting-tier-3) |

## IAIImageGenerationService

The primary interface for image-generation operations. It follows the same builder pattern as the other capability services.

{% code title="IAIImageGenerationService.cs" %}

```csharp
public interface IAIImageGenerationService
{
    Task<ImageGenerationResponse> GenerateImagesAsync(
        Action<AIImageGenerationBuilder> configure,
        string prompt,
        CancellationToken cancellationToken = default);

    Task<ImageGenerationResponse> GenerateImagesAsync(
        Action<AIImageGenerationBuilder> configure,
        string prompt,
        IEnumerable<AIContent>? originalImages,
        CancellationToken cancellationToken = default);

    Task<IImageGenerator> CreateImageGeneratorAsync(
        Action<AIImageGenerationBuilder> configure,
        CancellationToken cancellationToken = default);

    Task<AITrackedImageResult<TResult>> InvokeWithTrackingAsync<TResult>(
        Action<AIImageGenerationBuilder> configure,
        Func<IImageGenerator, CancellationToken, Task<AITrackedImageResult<TResult>>> operation,
        CancellationToken cancellationToken = default);

    Task<AISupportedImageModels> GetSupportedModelsAsync(
        Action<AIImageGenerationBuilder> configure,
        CancellationToken cancellationToken = default);
}
```

{% endcode %}

## AIImageGenerationBuilder

All methods accept an `Action<AIImageGenerationBuilder>` to configure the request:

| Method | Description |
| --- | --- |
| `.WithAlias(string alias)` | **Required.** Sets an alias for auditing and telemetry. |
| `.WithProfile(Guid profileId)` | Selects a profile by ID. Uses the default image-generation profile if omitted. |
| `.WithProfile(string profileAlias)` | Selects a profile by alias. |
| `.WithImageGenerationOptions(ImageGenerationOptions options)` | Overrides profile defaults (size, count, response format). |
| `.WithOriginalImages(IEnumerable<AIContent> images)` | Supplies images to edit (maskless edit — Tier 2). |
| `.WithGuardrails(params string[] aliases)` | Adds guardrails on top of the profile's (additive). |
| `.SetGuardrails(params string[] aliases)` | Replaces the profile's guardrails. |
| `.WithContextItems(IEnumerable<AIRequestContextItem> items)` | Attaches context items to the request. |

## Setting up image generation

### 1. Enable the feature flag

See [Enabling image generation](#enabling-image-generation) above.

### 2. Install a provider with Image Generation support

Currently, OpenAI is the only provider with Image Generation support:

{% code title="Terminal" %}

```bash
dotnet add package Umbraco.AI.OpenAI
```

{% endcode %}

### 3. Create a connection and profile

Create an OpenAI connection in the backoffice, then create a profile with the **Image Generation** capability. Supported models include:

| Model | Notes |
| --- | --- |
| `gpt-image-1` | Default. Sizes 1024x1024, 1024x1536, 1536x1024; supports editing and masks. |
| `dall-e-3` | Sizes 1024x1024, 1792x1024, 1024x1792; no editing. |
| `dall-e-2` | Sizes 256x256, 512x512, 1024x1024; supports editing and masks. |

## In this section

{% content-ref url="generating-images.md" %}
[Generating Images](generating-images.md)
{% endcontent-ref %}

{% content-ref url="editing-images.md" %}
[Editing Images](editing-images.md)
{% endcontent-ref %}

## Related

- [Capabilities](../../concepts/capabilities.md) - Available capability types
- [OpenAI Provider](../../providers/openai.md) - Provider with Image Generation support
- [Image Generation Controller](../../frontend/image-generation-controller.md) - Frontend API
- [Image Generation Management API](../../management-api/image-generation/README.md) - REST endpoints
````

- [ ] **Step 3: Wire into `18/ai-in-umbraco/SUMMARY.md`.** Under `## Using the API`, replace the single line
  `* [Speech-to-Text](using-the-api/speech-to-text.md)`
  by inserting the Image Generation entry immediately after it:
```
* [Speech-to-Text](using-the-api/speech-to-text.md)
* [Image Generation](using-the-api/image-generation/README.md)
  * [Generating Images](using-the-api/image-generation/generating-images.md)
  * [Editing Images](using-the-api/image-generation/editing-images.md)
```
(The child pages are created in Tasks 4 and 5; adding the nav entries now keeps a single SUMMARY edit region. GitBook tolerates entries whose files land in the same PR.)

- [ ] **Step 4: Mirror to 17**

```bash
mkdir -p 17/ai-in-umbraco/using-the-api/image-generation
cp 18/ai-in-umbraco/using-the-api/image-generation/README.md 17/ai-in-umbraco/using-the-api/image-generation/README.md
cp 18/ai-in-umbraco/SUMMARY.md 17/ai-in-umbraco/SUMMARY.md
```

- [ ] **Step 5: Verify parity**

Run:
```bash
diff -r 17/ai-in-umbraco/using-the-api/image-generation 18/ai-in-umbraco/using-the-api/image-generation && \
diff 17/ai-in-umbraco/SUMMARY.md 18/ai-in-umbraco/SUMMARY.md && echo PARITY_OK
```
Expected: `PARITY_OK`.

- [ ] **Step 6: Commit**

```bash
git add 17/ai-in-umbraco 18/ai-in-umbraco
git commit -m "docs(ai): Add Image Generation API overview and enablement

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Generating Images page (text-to-image)

**Files:**
- Create: `18/ai-in-umbraco/using-the-api/image-generation/generating-images.md`
- Mirror to `17/`. (SUMMARY entry already added in Task 3.)

**Interfaces:**
- Consumes: `README.md` overview (Task 3) as `README.md` link target.

- [ ] **Step 1: Invoke `umbraco-docs-content`.**

- [ ] **Step 2: Create `18/ai-in-umbraco/using-the-api/image-generation/generating-images.md`:**

````markdown
---
description: >-
    Generate images from a text prompt and validate model constraints up front.
---

# Generating Images

{% hint style="warning" %}
Image Generation is experimental and disabled by default. See [Enabling image generation](README.md#enabling-image-generation).
{% endhint %}

Use `GenerateImagesAsync` to produce images from a text prompt (Tier 1).

## Basic usage

{% code title="HeroBanner.cs" %}

```csharp
#pragma warning disable UMBRACOAI_IMAGEGEN

using Microsoft.Extensions.AI;
using Umbraco.AI.Core.ImageGeneration;

public class HeroBanner
{
    private readonly IAIImageGenerationService _imageGenerationService;

    public HeroBanner(IAIImageGenerationService imageGenerationService)
    {
        _imageGenerationService = imageGenerationService;
    }

    public async Task<IReadOnlyList<AIContent>> Generate()
    {
        var response = await _imageGenerationService.GenerateImagesAsync(
            img => img.WithAlias("hero-banner"),
            "A serene mountain landscape at dawn");

        return response.Contents;
    }
}
```

{% endcode %}

Each item in `response.Contents` is a `DataContent` (inline bytes) or `UriContent` (hosted URL), depending on the model and requested response format.

## Choosing a profile and options

{% code title="Builder example" %}

```csharp
#pragma warning disable MEAI001 // ImageGenerationOptions is experimental in M.E.AI

var response = await _imageGenerationService.GenerateImagesAsync(
    img => img
        .WithAlias("product-shot")
        .WithProfile("marketing-images")
        .WithImageGenerationOptions(new ImageGenerationOptions
        {
            Count = 2,
            ImageSize = new Size(1024, 1024),
        }),
    "A product photo of a ceramic coffee mug on a wooden table",
    cancellationToken);
```

{% endcode %}

Profile-level defaults (size, quality, style, output media type) come from `AIImageGenerationProfileSettings`; options passed to the builder override them per call.

## Validating models and sizes up front

Different models support different sizes. Call `GetSupportedModelsAsync` to read the resolved model and its constraints before generating, so you can fail early with a clear message rather than getting a wrong-ratio result:

{% code title="Validate.cs" %}

```csharp
#pragma warning disable UMBRACOAI_IMAGEGEN

var supported = await _imageGenerationService.GetSupportedModelsAsync(
    img => img.WithAlias("hero-banner").WithProfile("marketing-images"));

var boundModel = supported.Models.First(m => m.Model.ModelId == supported.ModelId);

// Per-model constraints are exposed via descriptor metadata
if (boundModel.Metadata.TryGetValue("image.supportedSizes", out var sizes))
{
    // e.g. "1024x1024,1024x1536,1536x1024"
    Console.WriteLine($"{supported.ModelId} supports: {sizes}");
}
```

{% endcode %}

Constraint metadata keys include `image.supportedSizes`, `image.maxEdge`, `image.supportsEdit`, and `image.supportsMask`.

## Observability

`GenerateImagesAsync` runs through the full middleware pipeline — usage tracking, audit entries, guardrails, and telemetry — and publishes `AIImageGenerationExecutingNotification` / `AIImageGenerationExecutedNotification`. Every call requires an alias (`.WithAlias(...)`) so it can be attributed in analytics and audit logs.

## Related

- [Image Generation overview](README.md) - Enablement and service surface
- [Editing Images](editing-images.md) - Maskless edit and masked outpainting
- [Usage Analytics](../../backoffice/usage-analytics.md) - Where generations are recorded
````

- [ ] **Step 3: Mirror to 17**

```bash
cp 18/ai-in-umbraco/using-the-api/image-generation/generating-images.md 17/ai-in-umbraco/using-the-api/image-generation/generating-images.md
```

- [ ] **Step 4: Verify parity**

Run: `diff 17/ai-in-umbraco/using-the-api/image-generation/generating-images.md 18/ai-in-umbraco/using-the-api/image-generation/generating-images.md && echo PARITY_OK`
Expected: `PARITY_OK`.

- [ ] **Step 5: Commit**

```bash
git add 17/ai-in-umbraco/using-the-api/image-generation/generating-images.md 18/ai-in-umbraco/using-the-api/image-generation/generating-images.md
git commit -m "docs(ai): Add Generating Images guide

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Editing Images page (maskless edit + masked outpainting)

**Files:**
- Create: `18/ai-in-umbraco/using-the-api/image-generation/editing-images.md`
- Mirror to `17/`.

**Interfaces:**
- Consumes: `README.md`, `generating-images.md`. Must include an anchor `masked-outpainting-tier-3` (linked from Task 3's tier table) — keep the exact heading `## Masked outpainting (Tier 3)`.

- [ ] **Step 1: Invoke `umbraco-docs-content`.**

- [ ] **Step 2: Create `18/ai-in-umbraco/using-the-api/image-generation/editing-images.md`:**

````markdown
---
description: >-
    Edit existing images with a prompt (maskless), and reach the provider-native client for masked outpainting.
---

# Editing Images

{% hint style="warning" %}
Image Generation is experimental and disabled by default. See [Enabling image generation](README.md#enabling-image-generation).
{% endhint %}

## Maskless edit (Tier 2)

Supply one or more original images alongside the prompt. The model transforms the supplied image(s) rather than generating from scratch. Use the `GenerateImagesAsync` overload that takes `originalImages`:

{% code title="EditImage.cs" %}

```csharp
#pragma warning disable UMBRACOAI_IMAGEGEN

using Microsoft.Extensions.AI;
using Umbraco.AI.Core.ImageGeneration;

public async Task<IReadOnlyList<AIContent>> AddSnow(byte[] originalPng)
{
    var original = new DataContent(originalPng, "image/png");

    var response = await _imageGenerationService.GenerateImagesAsync(
        img => img.WithAlias("seasonal-edit").WithProfile("marketing-images"),
        "Add a light dusting of snow to the scene",
        new AIContent[] { original });

    return response.Contents;
}
```

{% endcode %}

{% hint style="info" %}
Not every model supports editing. Check the `image.supportsEdit` constraint from [`GetSupportedModelsAsync`](generating-images.md#validating-models-and-sizes-up-front) before offering an edit workflow.
{% endhint %}

## Masked outpainting (Tier 3)

Masked editing (supplying a mask to control which region changes) is not expressible through the Microsoft.Extensions.AI abstraction. For it, reach the provider-native client through the scoped generator's `GetService`.

`CreateImageGeneratorAsync` returns an `IImageGenerator` that forwards `GetService` through the full pipeline. For OpenAI, resolve the un-bound `OpenAIClient` and pick your model and size at call time:

{% code title="MaskedEdit.cs" %}

```csharp
#pragma warning disable UMBRACOAI_IMAGEGEN

using OpenAI;

var generator = await _imageGenerationService.CreateImageGeneratorAsync(
    img => img.WithAlias("masked-outpaint").WithProfile("marketing-images"));

var openAiClient = (OpenAIClient?)generator.GetService(typeof(OpenAIClient));
var imageClient = openAiClient!.GetImageClient("gpt-image-1");

// Use imageClient's native mask/edit APIs here.
```

{% endcode %}

{% hint style="warning" %}
`GetService` is OpenAI/Azure-OpenAI specific — other providers will not return an `OpenAIClient`. Raw calls made this way **bypass the usage and audit middleware**. To keep them visible in analytics and audit, wrap them in `InvokeWithTrackingAsync` (below).
{% endhint %}

## Keeping raw calls tracked

`InvokeWithTrackingAsync` opens a scope, builds the scoped generator, runs your operation, and records usage + audit around it. Return an `AITrackedImageResult<TResult>` reporting what the raw call produced:

{% code title="TrackedRawCall.cs" %}

```csharp
#pragma warning disable UMBRACOAI_IMAGEGEN

using Microsoft.Extensions.AI;
using OpenAI;
using Umbraco.AI.Core.ImageGeneration;

var tracked = await _imageGenerationService.InvokeWithTrackingAsync(
    img => img.WithAlias("masked-outpaint").WithProfile("marketing-images"),
    async (generator, ct) =>
    {
        var client = (OpenAIClient)generator.GetService(typeof(OpenAIClient))!;
        var imageClient = client.GetImageClient("gpt-image-1");

        // ... perform the provider-native masked edit, obtain bytes ...
        byte[] resultBytes = await DoMaskedEditAsync(imageClient, ct);

        return new AITrackedImageResult<byte[]>
        {
            Result = resultBytes,
            ImageCount = 1,
        };
    });

byte[] edited = tracked.Result;
```

{% endcode %}

## Related

- [Generating Images](generating-images.md) - Text-to-image and model validation
- [Image Generation overview](README.md) - Enablement and service surface
- [OpenAI Provider](../../providers/openai.md) - The provider that supports the escape hatch
````

- [ ] **Step 3: Mirror to 17**

```bash
cp 18/ai-in-umbraco/using-the-api/image-generation/editing-images.md 17/ai-in-umbraco/using-the-api/image-generation/editing-images.md
```

- [ ] **Step 4: Verify parity**

Run: `diff 17/ai-in-umbraco/using-the-api/image-generation/editing-images.md 18/ai-in-umbraco/using-the-api/image-generation/editing-images.md && echo PARITY_OK`
Expected: `PARITY_OK`.

- [ ] **Step 5: Commit**

```bash
git add 17/ai-in-umbraco/using-the-api/image-generation/editing-images.md 18/ai-in-umbraco/using-the-api/image-generation/editing-images.md
git commit -m "docs(ai): Add Editing Images guide

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: Provider extension page (custom Image Generation capability)

**Files:**
- Create: `18/ai-in-umbraco/extending/providers/image-generation-capability.md`
- Modify: `18/ai-in-umbraco/SUMMARY.md`
- Mirror both to `17/`.

**Interfaces:**
- Consumes: `chat-capability.md`, `embedding-capability.md`, `speech-to-text-capability.md`, `creating-a-provider.md` (sibling links).

- [ ] **Step 1: Invoke `umbraco-docs-content`.**

- [ ] **Step 2: Create `18/ai-in-umbraco/extending/providers/image-generation-capability.md`:**

````markdown
---
description: >-
    Implement the experimental image-generation capability for your custom provider.
---

# Image Generation Capability

{% hint style="warning" %}
Image Generation is **experimental**. When the `Umbraco:AI:Experimental:ImageGeneration` feature flag is off, the capability is hidden from discovery and not selectable in the profile editor — even if a provider implements it.
{% endhint %}

The image-generation capability produces images from prompts. Implement it by extending `AIImageGeneratorCapabilityBase<TSettings>`.

## Base Class

{% code title="AIImageGeneratorCapabilityBase<TSettings>" %}

```csharp
public abstract class AIImageGeneratorCapabilityBase<TSettings>(IAIProvider provider)
    : AICapabilityBase<TSettings>(provider), IAICapability<TSettings>, IAIImageGeneratorCapability
    where TSettings : class
{
    public override AICapability Kind => AICapability.ImageGeneration;

    // Override this (or CreateGeneratorAsync) to create an IImageGenerator
    protected virtual IImageGenerator CreateGenerator(TSettings settings, string? modelId) { /* ... */ }

    // Override this for an async variant
    protected virtual Task<IImageGenerator> CreateGeneratorAsync(
        TSettings settings, string? modelId, CancellationToken cancellationToken = default) { /* ... */ }

    // Implement this: return available models (with constraint metadata)
    protected abstract Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        TSettings settings,
        CancellationToken cancellationToken = default);
}
```

{% endcode %}

{% hint style="info" %}
`IImageGenerator` is marked `[Experimental("MEAI001")]` in Microsoft.Extensions.AI, and the Umbraco capability surface carries the `UMBRACOAI_IMAGEGEN` diagnostic. Add both `#pragma warning disable MEAI001` and `#pragma warning disable UMBRACOAI_IMAGEGEN` to your implementation files.
{% endhint %}

## Basic Implementation

{% code title="MyImageGenerationCapability.cs" %}

```csharp
#pragma warning disable MEAI001
#pragma warning disable UMBRACOAI_IMAGEGEN

using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

public class MyImageGenerationCapability : AIImageGeneratorCapabilityBase<MyProviderSettings>
{
    public MyImageGenerationCapability(IAIProvider provider) : base(provider) { }

    protected override IImageGenerator CreateGenerator(MyProviderSettings settings, string? modelId)
    {
        return new MyImageGenerator(settings, modelId ?? "default-image-model");
    }

    protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        MyProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var models = new List<AIModelDescriptor>
        {
            new(
                new AIModelRef(Provider.Id, "image-standard"),
                "Standard Image Model",
                new Dictionary<string, string>
                {
                    ["image.supportedSizes"] = "1024x1024",
                    ["image.maxEdge"] = "1024",
                    ["image.supportsEdit"] = "false",
                    ["image.supportsMask"] = "false",
                })
        };
        return Task.FromResult<IReadOnlyList<AIModelDescriptor>>(models);
    }
}
```

{% endcode %}

{% hint style="info" %}
Surface per-model constraints via `AIModelDescriptor` metadata (`image.supportedSizes`, `image.maxEdge`, `image.supportsEdit`, `image.supportsMask`). Consumers read these through `GetSupportedModelsAsync` to validate requests before calling.
{% endhint %}

## Register in Provider

Add the image-generation capability in your provider constructor:

{% code title="MyProvider.cs" %}

```csharp
[AIProvider("myprovider", "My AI Provider")]
public class MyProvider : AIProviderBase<MyProviderSettings>
{
    public MyProvider(IAIProviderInfrastructure infrastructure)
        : base(infrastructure)
    {
        WithCapability<MyChatCapability>();
        WithCapability<MyImageGenerationCapability>();  // Add image-generation support
    }
}
```

{% endcode %}

## Exposing a provider-native client (optional)

To let consumers reach your provider's native client for masked outpainting (Tier 3), return an `IImageGenerator` that overrides `GetService` to resolve that client (as the OpenAI provider does with `OpenAIClient`). Wrap the M.E.AI adapter in a `DelegatingImageGenerator` and return your native client for its type in `GetService`.

## Related

- [Creating a Provider](creating-a-provider.md) - Provider setup
- [Chat Capability](chat-capability.md) - Chat capability implementation
- [Speech-to-Text Capability](speech-to-text-capability.md) - Speech-to-text capability implementation
- [Image Generation API](../../using-the-api/image-generation/README.md) - Using the Image Generation API
````

- [ ] **Step 3: Wire into `18/ai-in-umbraco/SUMMARY.md`.** Under `## Extending` > `Custom Providers`, after
  `  * [Speech-to-Text Capability](extending/providers/speech-to-text-capability.md)` add:
```
  * [Image Generation Capability](extending/providers/image-generation-capability.md)
```

- [ ] **Step 4: Mirror to 17**

```bash
cp 18/ai-in-umbraco/extending/providers/image-generation-capability.md 17/ai-in-umbraco/extending/providers/image-generation-capability.md
cp 18/ai-in-umbraco/SUMMARY.md 17/ai-in-umbraco/SUMMARY.md
```

- [ ] **Step 5: Verify parity**

Run:
```bash
diff 17/ai-in-umbraco/extending/providers/image-generation-capability.md 18/ai-in-umbraco/extending/providers/image-generation-capability.md && \
diff 17/ai-in-umbraco/SUMMARY.md 18/ai-in-umbraco/SUMMARY.md && echo PARITY_OK
```
Expected: `PARITY_OK`.

- [ ] **Step 6: Commit**

```bash
git add 17/ai-in-umbraco 18/ai-in-umbraco
git commit -m "docs(ai): Add Image Generation provider capability guide

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Management API pages

**Files:**
- Create: `18/ai-in-umbraco/management-api/image-generation/README.md`
- Create: `18/ai-in-umbraco/management-api/image-generation/generate-image.md`
- Modify: `18/ai-in-umbraco/SUMMARY.md`
- Mirror all to `17/`.

**Interfaces:**
- Consumes: nothing new. Produces the `management-api/image-generation/*` pages linked from Task 3's README.

- [ ] **Step 1: Invoke `umbraco-docs-content`.**

- [ ] **Step 2: Create `18/ai-in-umbraco/management-api/image-generation/README.md`:**

````markdown
---
description: >-
    REST API endpoints for generating and editing images (experimental).
---

# Image Generation API

{% hint style="warning" %}
Image Generation is **experimental**. When the `Umbraco:AI:Experimental:ImageGeneration` feature flag is disabled, these endpoints return **404**. See [Enabling image generation](../../using-the-api/image-generation/README.md#enabling-image-generation).
{% endhint %}

The Image Generation API generates images from a text prompt, with optional maskless editing of supplied images.

## Base URL

```
/umbraco/ai/management/api/v1/image-generation
```

## Authentication

All endpoints require backoffice authentication with the `Umb.AI.Management.Api` authorization policy.

## Endpoints

| Method | Endpoint | Description |
| ------ | -------- | ----------- |
| POST   | `/umbraco/ai/management/api/v1/image-generation/generate` | [Generate images](generate-image.md) |

## In This Section

{% content-ref url="generate-image.md" %}
[Generate Images](generate-image.md)
{% endcontent-ref %}
````

- [ ] **Step 3: Create `18/ai-in-umbraco/management-api/image-generation/generate-image.md`:**

````markdown
---
description: >-
    Generate one or more images from a text prompt, with optional maskless edit.
---

# Generate Images

{% hint style="warning" %}
Returns **404** when the `Umbraco:AI:Experimental:ImageGeneration` feature flag is disabled.
{% endhint %}

```http
POST /umbraco/ai/management/api/v1/image-generation/generate
```

## Request Body

{% code title="application/json" %}

```json
{
  "prompt": "A serene mountain landscape at dawn",
  "profileIdOrAlias": "marketing-images",
  "count": 1,
  "size": "1024x1024",
  "responseFormat": "data",
  "originalImages": [
    { "data": "<base64>", "mediaType": "image/png" }
  ]
}
```

{% endcode %}

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| `prompt` | string | Yes | The text prompt describing the desired image(s). |
| `profileIdOrAlias` | string | No | Profile ID or alias. Uses the default image-generation profile if omitted. |
| `count` | int | No | Number of images to generate. |
| `size` | string | No | Image size as `"{width}x{height}"` (for example, `"1024x1024"`). |
| `responseFormat` | string | No | `"url"`, `"data"`, or `"hosted"`. |
| `originalImages` | array | No | Base64 images to edit (maskless edit). Each: `{ "data", "mediaType" }`. Masked outpainting is not exposed over REST. |

## Response

{% code title="200 OK" %}

```json
{
  "images": [
    { "data": "<base64>", "url": null, "mediaType": "image/png" }
  ],
  "usage": {
    "inputTokens": 0,
    "outputTokens": 0,
    "totalTokens": 0
  }
}
```

{% endcode %}

| Property | Type | Description |
| --- | --- | --- |
| `images` | array | Generated images. Each has `data` (base64) and/or `url`, plus `mediaType`. |
| `usage` | object | Optional token usage (`inputTokens`, `outputTokens`, `totalTokens`), when reported. |

## Status Codes

| Code | Meaning |
| --- | --- |
| 200 | Images generated. |
| 400 | Invalid prompt, invalid base64 image data, or generation failed. |
| 404 | Feature flag disabled, or the profile was not found. |

## Example

{% code title="cURL" %}

```bash
curl -X POST "https://your-site.com/umbraco/ai/management/api/v1/image-generation/generate" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "prompt": "A serene mountain landscape at dawn", "size": "1024x1024" }'
```

{% endcode %}

## Related

- [Image Generation API overview](README.md)
- [Using the Image Generation API](../../using-the-api/image-generation/README.md)
````

- [ ] **Step 4: Wire into `18/ai-in-umbraco/SUMMARY.md`.** Under `## Management API`, after the Embeddings block
  (`* [Embeddings](management-api/embeddings/README.md)` and its `  * [Generate]…` child) add:
```
* [Image Generation](management-api/image-generation/README.md)
  * [Generate Images](management-api/image-generation/generate-image.md)
```

- [ ] **Step 5: Mirror to 17**

```bash
mkdir -p 17/ai-in-umbraco/management-api/image-generation
cp 18/ai-in-umbraco/management-api/image-generation/README.md 17/ai-in-umbraco/management-api/image-generation/README.md
cp 18/ai-in-umbraco/management-api/image-generation/generate-image.md 17/ai-in-umbraco/management-api/image-generation/generate-image.md
cp 18/ai-in-umbraco/SUMMARY.md 17/ai-in-umbraco/SUMMARY.md
```

- [ ] **Step 6: Verify parity**

Run:
```bash
diff -r 17/ai-in-umbraco/management-api/image-generation 18/ai-in-umbraco/management-api/image-generation && \
diff 17/ai-in-umbraco/SUMMARY.md 18/ai-in-umbraco/SUMMARY.md && echo PARITY_OK
```
Expected: `PARITY_OK`.

- [ ] **Step 7: Commit**

```bash
git add 17/ai-in-umbraco 18/ai-in-umbraco
git commit -m "docs(ai): Add Image Generation management API reference

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: Frontend controller page

**Files:**
- Create: `18/ai-in-umbraco/frontend/image-generation-controller.md`
- Modify: `18/ai-in-umbraco/SUMMARY.md`
- Mirror both to `17/`.

**Interfaces:**
- Consumes: `chat-controller.md`, `types.md`, and the using-the-api README (links).

- [ ] **Step 1: Invoke `umbraco-docs-content`.**

- [ ] **Step 2: Create `18/ai-in-umbraco/frontend/image-generation-controller.md`:**

````markdown
---
description: >-
    Generate images from a text prompt in custom backoffice elements (experimental).
---

# Image Generation Controller

{% hint style="warning" %}
Image Generation is experimental. `generate` returns an error result (server 404) unless the `Umbraco:AI:Experimental:ImageGeneration` feature flag is enabled. See [Enabling image generation](../using-the-api/image-generation/README.md#enabling-image-generation).
{% endhint %}

`UaiImageGenerationController` provides a frontend API for generating images by calling the Management API. It follows the same controller pattern as [UaiChatController](chat-controller.md).

## Import

{% code title="Import" %}

```typescript
import {
    UaiImageGenerationController,
    type UaiImageGenerationOptions,
    type UaiImageGenerationResult,
    type UaiGeneratedImage,
    type UaiImageInput,
} from "@umbraco-ai/core";
```

{% endcode %}

## Constructor

{% code title="Constructor" %}

```typescript
new UaiImageGenerationController(host: UmbControllerHost)
```

{% endcode %}

| Parameter | Type | Description |
| --- | --- | --- |
| `host` | `UmbControllerHost` | The controller host (usually `this` in a Lit element). |

## Methods

### generate

Sends a prompt to the Management API and returns the generated image(s).

{% code title="Signature" %}

```typescript
async generate(
    prompt: string,
    options?: UaiImageGenerationOptions
): Promise<{ data?: UaiImageGenerationResult; error?: unknown }>
```

{% endcode %}

| Parameter | Type | Description |
| --- | --- | --- |
| `prompt` | `string` | The text prompt describing the desired image(s). |
| `options` | `UaiImageGenerationOptions` | Optional configuration (see below). |

## Options

{% code title="UaiImageGenerationOptions" %}

```typescript
interface UaiImageGenerationOptions {
    /** Profile ID (GUID) or alias. If omitted, uses the default image-generation profile. */
    profileIdOrAlias?: string;
    /** Number of images to generate. */
    count?: number;
    /** Image size as "{width}x{height}" (e.g. "1024x1024"). */
    size?: string;
    /** Response format: "url", "data", or "hosted". */
    responseFormat?: string;
    /** Original images to edit (maskless edit). */
    originalImages?: UaiImageInput[];
    /** AbortSignal for cancellation. */
    signal?: AbortSignal;
}
```

{% endcode %}

## Result

{% code title="UaiImageGenerationResult" %}

```typescript
interface UaiGeneratedImage {
    data?: string;      // base64, when returned inline
    url?: string;       // URL, when hosted
    mediaType?: string; // e.g. "image/png"
}

interface UaiImageGenerationResult {
    images: UaiGeneratedImage[];
    usage?: {
        inputTokens?: number;
        outputTokens?: number;
        totalTokens?: number;
    };
}
```

{% endcode %}

## Example

{% code title="image-prompt.element.ts" %}

```typescript
import { LitElement, html } from "lit";
import { customElement, state } from "lit/decorators.js";
import { UaiImageGenerationController } from "@umbraco-ai/core";

@customElement("image-prompt")
export class ImagePromptElement extends LitElement {
    #controller = new UaiImageGenerationController(this);

    @state() private _src?: string;
    @state() private _busy = false;

    async #generate() {
        this._busy = true;
        const { data, error } = await this.#controller.generate(
            "A serene mountain landscape at dawn",
            { size: "1024x1024" },
        );
        this._busy = false;

        if (data?.images.length) {
            const img = data.images[0];
            this._src = img.url ?? `data:${img.mediaType};base64,${img.data}`;
        } else {
            console.error("Image generation failed:", error);
        }
    }

    render() {
        return html`
            <button @click=${this.#generate} ?disabled=${this._busy}>
                ${this._busy ? "Generating…" : "Generate"}
            </button>
            ${this._src ? html`<img src=${this._src} alt="Generated" />` : ""}
        `;
    }
}

declare global {
    interface HTMLElementTagNameMap {
        "image-prompt": ImagePromptElement;
    }
}
```

{% endcode %}

## Related

- [Chat Controller](chat-controller.md) - Chat completions in the frontend
- [Image Generation API](../using-the-api/image-generation/README.md) - Backend image-generation service
- [Types](types.md) - All frontend type definitions
````

- [ ] **Step 3: Wire into `18/ai-in-umbraco/SUMMARY.md`.** Under `## Frontend`, after
  `* [Speech-to-Text Controller](frontend/speech-to-text-controller.md)` add:
```
* [Image Generation Controller](frontend/image-generation-controller.md)
```

- [ ] **Step 4: Mirror to 17**

```bash
cp 18/ai-in-umbraco/frontend/image-generation-controller.md 17/ai-in-umbraco/frontend/image-generation-controller.md
cp 18/ai-in-umbraco/SUMMARY.md 17/ai-in-umbraco/SUMMARY.md
```

- [ ] **Step 5: Verify parity**

Run:
```bash
diff 17/ai-in-umbraco/frontend/image-generation-controller.md 18/ai-in-umbraco/frontend/image-generation-controller.md && \
diff 17/ai-in-umbraco/SUMMARY.md 18/ai-in-umbraco/SUMMARY.md && echo PARITY_OK
```
Expected: `PARITY_OK`.

- [ ] **Step 6: Commit**

```bash
git add 17/ai-in-umbraco 18/ai-in-umbraco
git commit -m "docs(ai): Add Image Generation frontend controller guide

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: Provider docs + configuration reference

**Files:**
- Modify: `18/ai-in-umbraco/providers/openai.md`
- Modify: `18/ai-in-umbraco/providers/README.md`
- Modify: `18/ai-in-umbraco/reference/configuration/ai-options.md`
- Mirror all to `17/`.

**Interfaces:**
- Consumes: nothing. Produces provider/config pages that link back to the Image Generation API.

- [ ] **Step 1: `providers/openai.md` — description + intro.** Replace the frontmatter description
  `Configure OpenAI as an AI provider for chat, embedding, and speech-to-text capabilities.`
  with
  `Configure OpenAI as an AI provider for chat, embedding, speech-to-text, and image-generation capabilities.`
  Then replace the intro sentence
  `OpenAI provides access to GPT and text-embedding models, supporting Chat, Embedding, and Speech-to-Text capabilities.`
  with
  `OpenAI provides access to GPT, text-embedding, and image models, supporting Chat, Embedding, Speech-to-Text, and Image Generation capabilities. Image Generation is experimental — see [Enabling image generation](../using-the-api/image-generation/README.md#enabling-image-generation).`

- [ ] **Step 2: `providers/openai.md` — add a Related link.** In the "Related" list add:
```
- [Image Generation API](../using-the-api/image-generation/README.md) - Generate images with OpenAI
```

- [ ] **Step 3: `providers/README.md` — "Available Providers" table.** Change the OpenAI row's Capabilities cell from
  `Chat, Embedding, Speech-to-Text` to
  `Chat, Embedding, Speech-to-Text, Image Generation (experimental)`.

- [ ] **Step 4: `providers/README.md` — "Capabilities Needed" matrix.** After the `Speech-to-Text` row add:
```
| Image Generation | Yes    | No        | No     | No     | No         |
```

- [ ] **Step 5: `reference/configuration/ai-options.md` — properties.** Add to the `AIOptions` class-definition block, after `DefaultSpeechToTextProfileAlias`:
```csharp
    public string? DefaultImageGenerationProfileAlias { get; set; }
```
  and add a matching row to the Properties table:
```
| `DefaultImageGenerationProfileAlias` | `string?` | Fallback default profile alias for image generation (experimental) |
```

- [ ] **Step 6: `reference/configuration/ai-options.md` — experimental section.** After the `## Configuration` appsettings block, append:

````markdown
## Experimental Features

Experimental capabilities are hidden and inert until enabled under the `Umbraco:AI:Experimental` section. Each flag defaults to `false`.

{% code title="appsettings.json" %}

```json
{
    "Umbraco": {
        "AI": {
            "Experimental": {
                "ImageGeneration": true
            }
        }
    }
}
```

{% endcode %}

| Flag | Default | Description |
| --- | --- | --- |
| `ImageGeneration` | `false` | Enables the [Image Generation](../../using-the-api/image-generation/README.md) capability. When off, the capability is hidden from discovery, not selectable in the profile editor, and its REST endpoint returns 404. |
````

- [ ] **Step 7: Mirror to 17**

```bash
cp 18/ai-in-umbraco/providers/openai.md 17/ai-in-umbraco/providers/openai.md
cp 18/ai-in-umbraco/providers/README.md 17/ai-in-umbraco/providers/README.md
cp 18/ai-in-umbraco/reference/configuration/ai-options.md 17/ai-in-umbraco/reference/configuration/ai-options.md
```

- [ ] **Step 8: Verify parity**

Run:
```bash
diff 17/ai-in-umbraco/providers/openai.md 18/ai-in-umbraco/providers/openai.md && \
diff 17/ai-in-umbraco/providers/README.md 18/ai-in-umbraco/providers/README.md && \
diff 17/ai-in-umbraco/reference/configuration/ai-options.md 18/ai-in-umbraco/reference/configuration/ai-options.md && echo PARITY_OK
```
Expected: `PARITY_OK`.

- [ ] **Step 9: Commit**

```bash
git add 17/ai-in-umbraco 18/ai-in-umbraco
git commit -m "docs(ai): Note Image Generation support in provider and config docs

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: Final verification

**Files:** none created; verification only.

**Interfaces:** Consumes all prior tasks.

- [ ] **Step 1: Every SUMMARY link resolves to a real file (both versions).** Run this from the repo root:

```bash
for v in 17 18; do
  base="$v/ai-in-umbraco"
  echo "== $base =="
  grep -oE '\]\(([^)]+\.md)\)' "$base/SUMMARY.md" | sed -E 's/\]\(([^)]+)\)/\1/' | while read -r link; do
    [ -f "$base/$link" ] || echo "MISSING: $base/$link"
  done
done
echo "LINK_CHECK_DONE"
```
Expected: `LINK_CHECK_DONE` with **no** `MISSING:` lines.

- [ ] **Step 2: 17↔18 parity across every new/edited path.**

```bash
diff -r 17/ai-in-umbraco 18/ai-in-umbraco && echo "TREE_PARITY_OK"
```
Expected: `TREE_PARITY_OK` (the two trees remain identical).

- [ ] **Step 3: Intra-doc anchor check.** Confirm the Tier-3 link target exists. Run:

```bash
grep -q "^## Masked outpainting (Tier 3)" 18/ai-in-umbraco/using-the-api/image-generation/editing-images.md && \
grep -q "masked-outpainting-tier-3" 18/ai-in-umbraco/using-the-api/image-generation/README.md && echo ANCHOR_OK
```
Expected: `ANCHOR_OK`.

- [ ] **Step 4: Reader-perspective review.** `Skill: review-docs` over the new pages (`using-the-api/image-generation/`, `extending/providers/image-generation-capability.md`, `management-api/image-generation/`, `frontend/image-generation-controller.md`). Apply any resulting fixes to `18/`, re-`cp` to `17/`, and commit with:

```bash
git commit -m "docs(ai): Apply review feedback to Image Generation docs

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 5: Confirm the backport question.** Both `17/` and `18/` are covered by this plan. Report to the user that no further version line needs updating (v16 and below are out of active feature support), per the CLAUDE.md "Keep Active Versions in Sync" rule.

- [ ] **Step 6: Finish the branch.** `Skill: superpowers:finishing-a-development-branch` to decide push/PR for `ai/image-generation-docs`.

---

## Self-Review (completed during authoring)

- **Spec coverage:** every file in the design's File Plan maps to a task — reference model (T1), capabilities concept (T2), using-the-api README+enablement (T3), generating-images (T4), editing-images (T5), provider extension (T6), management API (T7), frontend controller (T8), provider+config docs (T9), verification+backport (T10). ✅
- **Placeholder scan:** no TBD/TODO; all snippets are concrete and copied from verified source. ✅
- **Type consistency:** service signatures, builder method names, `AITrackedImageResult<TResult>` shape, REST model field names, and frontend `Uai*` type names all match the verified source reference. The Tier-3 anchor (`masked-outpainting-tier-3`) is produced in T5 and consumed in T3/T5 — checked in T10 Step 3. ✅
- **Mirror strategy:** all six edited files confirmed byte-identical across 17/18 before editing, so author-in-18-then-`cp` is safe for both new and edited files. ✅
