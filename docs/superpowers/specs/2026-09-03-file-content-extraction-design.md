# File Content Extraction (CSV/TXT/MD/Office/PDF) — Design

**Date:** 2026-09-03
**Repo:** `Umbraco.AI` (monorepo) — primarily `Umbraco.AI.Core`, with a follow-up touch in
`Umbraco.AI.Agent.Core` for the Copilot "Resources" panel
**Status:** Local design only — no ticket filed yet
**Scope:** Core fix applies to both `v18/dev` and `v17/dev` (see "Version sync" below)

## Problem

Reported via the Copilot chat: attaching `product-snapshot-sep-2026.csv` directly in chat, or
adding it as a "Resource" pointing at an Umbraco Media item, never lets the AI read its
contents. Three symptoms observed:

1. **Direct chat attachment** — user attaches the CSV to a chat message and asks "what's in
   this file?". The AI replies it sees no attachment at all.
2. **Agent tool fetch** — the AI calls `get_umbraco_media` (an existing tool) against the same
   file (now stored as an Umbraco Media item) and the tool call succeeds, but the AI still says
   it has no way to read the file's contents.
3. **"Resources" panel** (Copilot side panel, distinct from the "Contexts" panel) — the file is
   attached as an always-on resource. The AI only ever learns the file's name and byte count
   (`product-snapshot-sep-2026.csv (48,423 bytes)`), never its content.

## What already exists (verified in code)

Umbraco.AI already has a working, pluggable text-extraction pipeline — this is not a
greenfield feature, it's closing gaps in something that's already half-built:

- **`IAIFileProcessingHandler`** (`Umbraco.AI.Core/FileProcessing/IAIFileProcessingHandler.cs`)
  — pluggable interface: `CanHandleAsync(mimeType)` / `ProcessAsync(bytes, mimeType, filename)`
  → `AIFileProcessingResult(Content, WasTruncated)`.
- **`OpenXmlFileProcessingHandler`** already extracts text from `.docx`, `.xlsx`, `.pptx` (via
  `DocumentFormat.OpenXml`, already a Core package dependency), with a 100,000-character
  truncation cap and a `[Content truncated due to size limits]` marker.
- **`AudioTranscriptionFileProcessingHandler`** transcribes audio via the speech-to-text
  service, when a default STT profile is configured.
- **`AIFileProcessingChatMiddleware` / `AIFileProcessingChatClient`** (registered as
  `IAIChatMiddleware`) scan every outgoing chat message for `DataContent`, and for each one, ask
  the handler collection for a match. If a handler is found, the binary attachment is replaced
  with a `TextContent` (`[File: name]\n<extracted text>`) before the request reaches the
  provider. No handler match → passed through untouched (works today for images, since
  providers handle those natively).
- Handlers are registered in order via `builder.AIFileProcessingHandlers().Append<T>()`
  (`AIFileProcessingHandlerCollectionBuilder`); first match wins.

**The gap is narrow**: no handler exists yet for plain text (`.txt`, `.csv`, `.md`) or PDF, and
a *second*, unrelated whitelist (`IAIUmbracoMediaResolver`) blocks those extensions — plus
`.docx`/`.xlsx`/`.pptx` — from ever being read when the file comes from an Umbraco Media item
rather than a raw chat upload.

### The two separate code paths behind symptoms 1–3

| Symptom | Path | Governing code |
|---|---|---|
| 1. Direct chat attachment | `AGUIFileProcessor` (Agent) stores the upload, converts it to AG-UI content, which becomes M.E.AI `DataContent` on the chat message. Checked only against `ContentSettings.IsFileAllowedForUpload`. | Reaches `AIFileProcessingChatMiddleware` — fixed by adding a handler (no resolver whitelist involved). |
| 2. `get_umbraco_media` tool | `GetUmbracoMediaItem.cs` calls `IAIUmbracoMediaResolver.ResolveAsync(mediaKey)`, which rejects any extension not in its hard-coded `ExtensionToMediaType` dictionary (images + audio only) **before the file is even opened**. On success it calls `AIRuntimeContext.AddData(bytes, mediaType)`, which is injected as `DataContent` on the next turn by `AIRuntimeContextInjectingChatClient` — and *that* message does reach `AIFileProcessingChatMiddleware`. | Blocked at the resolver whitelist — never reaches the middleware today. |
| 3. "Resources" panel | Not yet pinned down to an exact class in this session (see "Open question" below). Confirmed it does **not** go through `AIContextResourceType` (the "Contexts" panel's mechanism — built-in types are only `text` and `brand-voice`); most likely renders via the entity-adapter/formatter pipeline (`MediaEntityAdapter` → `CmsEntityFormatHelper.FormatCmsEntity`), which only prints CMS property values (name, byte size, etc.), never file content. | Needs its own fix — extracting text isn't enough if nothing calls the extractor. |

## Goals

- Fix symptom 1 and 2 by teaching the existing file-processing pipeline about plain-text
  formats, and by opening up `IAIUmbracoMediaResolver`'s extension whitelist so Office and
  text files can be read from Umbraco Media at all (Office extraction already works — it's
  just unreachable today).
- Fix symptom 3 by making the "Resources" panel's media formatting call the same
  text-extraction building block, instead of only ever showing file metadata.
- Land PDF support as a clearly separate, later phase — it needs a new dependency and has no
  existing scaffolding to lean on.

## Non-goals

- OCR / scanned (image-only) PDFs — different technology, own project if ever needed.
- Any change to how images or audio are handled — those already work.
- Filing a ticket or committing to a delivery date — this doc is for local review only.

## Phase 1a — Plain text handler + resolver whitelist (fixes symptoms 1 and 2)

**New handler**: `PlainTextFileProcessingHandler` implementing `IAIFileProcessingHandler`,
registered alongside the existing handlers via `builder.AIFileProcessingHandlers().Append<...>()`.

- Handles `text/plain`, `text/csv`, `text/markdown`.
- `ProcessAsync` decodes the bytes as UTF-8 and returns them as-is (no restructuring needed —
  unlike Office XML, there's no markup to strip).
- Apply the same truncation convention as `OpenXmlFileProcessingHandler`: cap at 100,000
  characters, append `[Content truncated due to size limits]` when exceeded. Reuse the same
  constant rather than duplicating the magic number — worth promoting `MaxCharacters` to a
  shared constant while touching this code.

**Resolver whitelist** (`AIUmbracoMediaResolver.ExtensionToMediaType`,
`Umbraco.AI.Core/Media/AIUmbracoMediaResolver.cs:14`): add entries so files stored as Umbraco
Media can be read at all:

| Extension | MIME type |
|---|---|
| `.txt` | `text/plain` |
| `.md` | `text/markdown` |
| `.csv` | `text/csv` |
| `.docx` | `application/vnd.openxmlformats-officedocument.wordprocessingml.document` |
| `.xlsx` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` |
| `.pptx` | `application/vnd.openxmlformats-officedocument.presentationml.presentation` |

The `.docx`/`.xlsx`/`.pptx` rows are a zero-cost addition — `OpenXmlFileProcessingHandler`
already handles those MIME types; they're just unreachable via the media resolver today.

**Why this is enough for symptoms 1 and 2**: both paths ultimately produce a `DataContent` that
flows through `AIFileProcessingChatMiddleware`, which already does the "does a handler exist for
this MIME type → replace with extracted text" work. Symptom 1 doesn't touch the resolver at all
(raw chat uploads aren't Umbraco Media), so the handler alone fixes it. Symptom 2 needs both the
resolver change (so the tool can read the file) and the handler (so the extracted text is usable).

**Verify during implementation**: confirm the middleware ordering means
`AIRuntimeContextInjectingChatClient` (which injects tool-added `DataContent`) runs *before*
`AIFileProcessingChatClient` sees the message, so tool-fetched files get processed. Both are
registered as `IAIChatMiddleware`; check `UmbracoBuilderExtensions` for the current append order
and adjust with `InsertBefore`/`InsertAfter` if needed.

## Phase 1b — "Resources" panel fix (fixes symptom 3)

**Open question (needs a short investigation spike before/at the start of implementation):**
this session could not pin down the exact class that formats a "Resources"-panel Media item for
the LLM. Ruled out: `AIContextResourceType` (Contexts panel's mechanism — no `media`/`entity`
resource type exists among the built-ins). Best lead: the entity-adapter/formatter pipeline
(`MediaEntityAdapter.FormatForLlm` → `CmsEntityFormatHelper.FormatCmsEntity`,
`Umbraco.AI.Core/EntityAdapter/Adapters/MediaEntityAdapter.cs`), which today only prints CMS
property values. The implementation plan should start by locating the exact call site (likely
by reproducing the panel in the running demo site and tracing the request), then apply the fix
below.

**Fix, once located**: when formatting a Media entity for LLM consumption, resolve the
underlying file via `IAIUmbracoMediaResolver` and, if its MIME type has a matching
`IAIFileProcessingHandler`, append the extracted text to the formatted output (keep the existing
name/size line — it's useful context, just not sufficient on its own). Apply the same
truncation cap as Phase 1a. This matters more here than elsewhere: a Resource marked "Always" is
re-sent on *every* turn of the conversation, so an uncapped attachment is a recurring token cost,
not a one-off.

## Phase 2 — PDF (later, separate follow-up)

- PDF text extraction has no existing scaffolding in this repo and needs a new third-party
  dependency (unlike Office formats, .NET has no built-in PDF text reader). Per repo convention,
  a new dependency needs sign-off before it's added — this doc does not commit to one.
- Same handler shape as Phase 1a: a new `IAIFileProcessingHandler` for `application/pdf`,
  registered the same way, same truncation convention.
- Same resolver-whitelist change needed: add `.pdf` → `application/pdf` to
  `AIUmbracoMediaResolver.ExtensionToMediaType`.
- Scanned/image-only PDFs (no embedded text layer) are explicitly out of scope (see Non-goals).

## Error handling

- Unreadable/corrupt files: handlers should catch parse failures and return an empty or
  best-effort partial result rather than throwing — consistent with how
  `AIUmbracoMediaResolver.ResolveAsync` already swallows exceptions and logs a warning instead
  of failing the whole request.
- Disallowed extensions (per CMS `ContentSettings.IsFileAllowedForUpload`) continue to be
  silently skipped, same as today — no change to that check.
- Oversized files: truncate with a visible marker rather than failing outright, matching
  existing `OpenXmlFileProcessingHandler` behavior.

## Testing

- Unit tests for `PlainTextFileProcessingHandler`: handles each MIME type, truncates correctly,
  handles empty/whitespace-only files.
- Unit tests for the widened `AIUmbracoMediaResolver.ExtensionToMediaType` map (extend existing
  resolver tests with the new extensions).
- Integration-style test through `AIFileProcessingChatClient` confirming a `DataContent` with
  `text/csv` becomes `TextContent` in the outgoing message.
- Resources-panel fix: add a test once the exact formatter class is confirmed, covering both the
  "file has a handler" and "file has no handler, falls back to metadata-only" cases.

## Version sync

This is a `Umbraco.AI.Core` change with no CMS-major-specific dependency, so per the repo's
multi-version policy it should land on both `v18/dev` and `v17/dev` (both currently in
"Features + bug fixes" phase). Confirm with whoever picks this up whether to implement once and
backport, or branch from `v17/dev` directly if v17 is the primary target.

## Summary of file touches (Phase 1)

| File | Change |
|---|---|
| `Umbraco.AI.Core/FileProcessing/PlainTextFileProcessingHandler.cs` | New handler |
| `Umbraco.AI.Core/FileProcessing/OpenXmlFileProcessingHandler.cs` | Extract shared `MaxCharacters` constant (minor cleanup while touching this area) |
| `Umbraco.AI.Core/Media/AIUmbracoMediaResolver.cs` | Add 6 extensions to `ExtensionToMediaType` |
| Composer registering `AIFileProcessingHandlers()` | Append the new handler |
| `Umbraco.AI.Core/EntityAdapter/Adapters/MediaEntityAdapter.cs` (or wherever Phase 1b's spike lands) | Include extracted text for text-extractable Media |
