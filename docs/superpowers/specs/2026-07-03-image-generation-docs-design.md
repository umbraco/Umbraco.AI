# Image Generation Capability — Documentation Design

**Date:** 2026-07-03
**Repo:** `Umbraco.Docs`
**Branch:** `ai/image-generation-docs`
**Scope:** Add documentation for the Image Generation capability to **both** the `17/` and `18/` doc lines.

## Goal

Document the Image Generation capability at **full parity** with the existing capabilities
(Chat, Embedding, Speech-to-Text), covering every surface that exists in code: the developer
API, the provider-extension base class, the Management API, the frontend controller, and the
concept/reference pages. Content lands identically in `17/ai-in-umbraco/` and
`18/ai-in-umbraco/` (the two trees are currently identical file sets, and the capability is the
same on both lines).

## Background (verified against source)

- Image Generation is a **new capability** exposed by `IAIImageGenerationService`
  (`Umbraco.AI/src/Umbraco.AI.Core/ImageGeneration/IAIImageGenerationService.cs`).
- It is **experimental**: gated behind the `Umbraco:AI:Experimental:ImageGeneration` feature
  flag (default `false`) and the `UMBRACOAI_IMAGEGEN` diagnostic (`[Experimental]`), which
  consumers must suppress with `#pragma warning disable UMBRACOAI_IMAGEGEN`. When disabled the
  capability is hidden from discovery, not selectable in the profile editor, and its REST
  endpoint returns 404.
- **Enablement is config-only** via `appsettings.json`:
  `{ "Umbraco": { "AI": { "Experimental": { "ImageGeneration": true } } } }`. Once enabled, a
  "Default Image Generation Profile" setting appears in the backoffice (Settings > AI > Settings)
  and ImageGeneration profiles become creatable.
- Enum value: `AICapability.ImageGeneration = 5`.
- Service surface (`IAIImageGenerationService`):
  - `GenerateImagesAsync(configure, prompt, ct)` — text-to-image (Tier 1).
  - `GenerateImagesAsync(configure, prompt, originalImages, ct)` — maskless edit (Tier 2).
  - `CreateImageGeneratorAsync(configure, ct)` — reusable scoped `IImageGenerator`; forwards
    `GetService` through the pipeline for the provider-native client (masked outpainting, Tier 3).
  - `InvokeWithTrackingAsync<TResult>(configure, operation, ct)` — keeps raw provider-native
    calls visible in usage/audit even though they bypass the middleware pipeline.
  - `GetSupportedModelsAsync(configure, ct)` — returns `AISupportedImageModels` (per-model
    size constraints + resolved bound model) for up-front validation.
- Provider extension base class: `AIImageGeneratorCapabilityBase<TSettings>`; override
  `GetModelsAsync`. Files require both `#pragma warning disable MEAI001` and
  `#pragma warning disable UMBRACOAI_IMAGEGEN`.
- **Only OpenAI** currently implements it (`OpenAIImageGeneratorCapability`, default model
  `gpt-image-1`, also matches `dall-e*`).
- Management API: `GenerateImageController` / `ImageGenerationControllerBase` under
  `Umbraco.AI.Web/Api/Management/ImageGeneration/`.
- Frontend controller: `image-generation.controller.ts` under
  `Web.StaticAssets/Client/src/image-generation/`.

## Precedent

Speech-to-Text is the most recently added capability and the closest model. Its docs span:
`concepts/capabilities.md`, `using-the-api/speech-to-text.md`,
`extending/providers/speech-to-text-capability.md`, `frontend/speech-to-text-controller.md`,
and `reference/models/ai-capability.md`. Image Generation follows the same shape, adding a
Management API page (which STT lacks) and splitting the usage guide into a folder (text-to-image
vs. editing are distinct enough to warrant separate pages).

## File Plan (mirrored across `17/` and `18/`)

### New pages

| Path (under `{17,18}/ai-in-umbraco/`) | Purpose | Modeled on |
|------|---------|-----------|
| `using-the-api/image-generation/README.md` | Overview + **Enabling image generation** (canonical) + the 3 tiers | `using-the-api/speech-to-text.md` + chat README |
| `using-the-api/image-generation/generating-images.md` | `GenerateImagesAsync` text-to-image; `GetSupportedModelsAsync`; size/model validation | `using-the-api/chat/basic-chat.md` |
| `using-the-api/image-generation/editing-images.md` | Maskless edit (Tier 2); masked outpainting escape hatch (Tier 3); `InvokeWithTrackingAsync` | — |
| `extending/providers/image-generation-capability.md` | Implement `AIImageGeneratorCapabilityBase<TSettings>` | `extending/providers/speech-to-text-capability.md` |
| `management-api/image-generation/README.md` | Section overview | `management-api/embeddings/README.md` |
| `management-api/image-generation/generate-image.md` | `GenerateImageController` endpoint (+ 404-when-disabled note) | sibling management-api pages |
| `frontend/image-generation-controller.md` | `image-generation.controller.ts` usage | `frontend/speech-to-text-controller.md` |

### Edited pages

| Path | Change |
|------|--------|
| `concepts/capabilities.md` | Add Image Generation to the capability table; add a capability section; add interface example; add to profile-relationship diagram; add `HasCapability` example; add Related link |
| `reference/models/ai-capability.md` | Add `ImageGeneration = 5` enum value |
| `reference/configuration/ai-options.md` | Add `DefaultImageGenerationProfileAlias`; document the `Umbraco:AI:Experimental` section |
| `providers/openai.md` | Add Image Generation to the frontmatter `description` and the intro capability list (also check `providers/README.md` for a capability matrix to update) |
| `SUMMARY.md` | Wire all new pages into the nav |

## Experimental Handling

- Every **new** page opens with a `{% hint style="warning" %}` experimental banner
  (consistent wording), matching the hint style already used on the STT capability page.
- The canonical "how to enable" content lives **once** in
  `using-the-api/image-generation/README.md` and is cross-linked from `concepts/capabilities.md`
  and `reference/configuration/ai-options.md`.
- Pages that describe gated surfaces (Management API, provider capability) note the
  hidden/404/not-selectable behavior when the flag is off.

## Conventions

- GitBook frontmatter with `description`; `{% code title=... %}` and `{% hint %}` blocks.
- Sentence-case headings; follow the `umbraco-docs-content` skill at write time.
- Net-new paths only ⇒ no `.gitbook.yaml` redirects required.
- All C# examples grounded in real signatures from the source files listed above; show the
  required `#pragma warning disable MEAI001` / `UMBRACOAI_IMAGEGEN` where relevant.

## Versioning Strategy

Author once, apply identically to `17/` and `18/`. No forward-merge between version lines.
Diff the two trees at the end to guarantee parity.

## Verification (before "done")

Local GitBook build/serve is not practical, so verify by:
1. Confirming every new/edited `SUMMARY.md` entry resolves to a real file (both versions).
2. Diffing `17/` vs `18/` new files for byte parity (only intentional version-specific notes,
   if any, should differ).
3. Running the `review-docs` skill over the new pages.

## Out of Scope

- No changes to the product code — documentation only.
- No `umbraco-in-ai` (the separate "using AI to build with Umbraco" space) changes.
- No new screenshots/assets unless a page clearly needs one (decide at write time; prefer none).
