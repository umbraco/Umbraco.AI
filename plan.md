# File Uploads & AI Processing - Implementation Plan

## AG-UI Multimodal Messages Draft Alignment

The [AG-UI multimodal draft](https://docs.ag-ui.com/drafts/multimodal-messages) (Implemented, Oct 2025) extends message `content` to be either a `string` or an array of `InputContent` parts:

- **`TextInputContent`** — `{ type: "text", text: string }`
- **`BinaryInputContent`** — `{ type: "binary", mimeType: string, data?: string (base64), url?: string, id?: string, filename?: string }`

At least one of `data`, `url`, or `id` must be provided on `BinaryInputContent`. The `id` field is designed for **reference to pre-uploaded/server-stored content** — the spec leaves resolution to the implementation.

Our implementation follows this spec exactly in the AGUI library, then layers server-side file processing on top.

---

## Architecture Overview

```
Turn 1 (file attached):

  Frontend ──── BinaryInputContent { data: "base64...", mimeType, filename }
     │
     │  POST /stream-agui
     ▼
  AGUIStreamingService
     │
     ├─ IAGUIFileProcessor.ProcessInboundAsync(messages, threadId)
     │   ├─ Finds BinaryInputContent with data (base64)
     │   ├─ Decodes + stores in temp (scoped to threadId)
     │   ├─ Returns rewritten messages: data → id reference
     │   └─ Also returns resolved messages with raw bytes for converter
     │
     ├─ AGUIMessageConverter.ConvertToChatMessages(resolvedMessages)
     │   ├─ TextInputContent → M.E.AI TextContent
     │   └─ BinaryInputContent (with bytes) → M.E.AI DataContent
     │
     ├─ agent.RunStreamingAsync(chatMessages) → LLM
     │
     └─ Emits MessagesSnapshotEvent with rewritten messages (id refs, no base64)
            │
            ▼
  Frontend receives snapshot → replaces local history (lightweight)

Turn 2+ (no file, but history has file reference):

  Frontend ──── BinaryInputContent { id: "tmp-abc123", mimeType, filename }
     │
     │  POST /stream-agui
     ▼
  AGUIStreamingService
     │
     ├─ IAGUIFileProcessor.ProcessInboundAsync(messages, threadId)
     │   ├─ Finds BinaryInputContent with id
     │   ├─ Resolves id → reads bytes from temp store
     │   └─ Returns resolved messages with raw bytes for converter
     │
     ├─ AGUIMessageConverter.ConvertToChatMessages(resolvedMessages)
     │   └─ BinaryInputContent (with bytes) → M.E.AI DataContent
     │
     └─ agent.RunStreamingAsync(chatMessages) → LLM
```

---

## Implementation Steps

### Phase 1: AGUI Library — Multimodal Content Types

**Package:** `Umbraco.AI.AGUI`

#### 1.1 Add content part models

Create new models in `Models/` following the AG-UI draft spec:

- `AGUIInputContent.cs` — Base class with `[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]` and derived type attributes
- `AGUITextInputContent.cs` — `{ type: "text", text: string }`
- `AGUIBinaryInputContent.cs` — `{ type: "binary", mimeType: string, data?: string, url?: string, id?: string, filename?: string }`

#### 1.2 Extend AGUIMessage

The AG-UI spec allows `content` to be either a `string` or `InputContent[]`. Use a custom `JsonConverter` on `AGUIMessage` that handles both:

```csharp
// When content is a JSON string → Content is set, ContentParts is null
// When content is a JSON array  → ContentParts is set, Content derived from text parts
```

Properties:
- `Content` (string?) — backward compatible, text-only content
- `ContentParts` (IEnumerable<AGUIInputContent>?) — multimodal content parts

#### 1.3 Unit tests

- Serialization/deserialization of text-only messages (backward compat)
- Deserialization of multimodal content arrays
- Round-trip for BinaryInputContent with base64 data
- Round-trip for BinaryInputContent with id reference (no data)
- Edge cases: empty content, mixed text+binary parts

---

### Phase 2: Agent.Core — File Processor + Message Converter

**Package:** `Umbraco.AI.Agent.Core`

#### 2.1 IAGUIFileProcessor — server-side file processing

New interface in `Agent.Core/AGUI/`:

```csharp
public interface IAGUIFileProcessor
{
    /// <summary>
    /// Processes inbound messages: stores base64 data, resolves id references.
    /// Returns two sets of messages:
    /// - Rewritten: base64 replaced with id refs (for MessagesSnapshotEvent)
    /// - Resolved: binary content populated with raw bytes (for converter)
    /// </summary>
    Task<AGUIFileProcessorResult> ProcessInboundAsync(
        IEnumerable<AGUIMessage> messages,
        string threadId,
        CancellationToken cancellationToken = default);
}

public sealed class AGUIFileProcessorResult
{
    /// <summary>Messages with base64 data swapped to id references (lightweight, for snapshot).</summary>
    public required IEnumerable<AGUIMessage> RewrittenMessages { get; init; }

    /// <summary>Messages with binary content resolved to bytes (for converter → LLM).</summary>
    public required IEnumerable<AGUIMessage> ResolvedMessages { get; init; }
}
```

#### 2.2 IAGUIFileStore — thread-scoped temp storage

```csharp
public interface IAGUIFileStore
{
    Task<string> StoreAsync(string threadId, byte[] data, string mimeType, string? filename,
        CancellationToken cancellationToken = default);
    Task<AGUIStoredFile?> ResolveAsync(string threadId, string fileId,
        CancellationToken cancellationToken = default);
    Task CleanupThreadAsync(string threadId, CancellationToken cancellationToken = default);
}

public sealed class AGUIStoredFile
{
    public required byte[] Data { get; init; }
    public required string MimeType { get; init; }
    public string? Filename { get; init; }
}
```

Default implementation: file-on-disk in Umbraco temp folder, directory per thread ID.

#### 2.3 Extend AGUIMessageConverter

Update `ConvertToChatMessage()` to handle `ContentParts`:

```csharp
// If ContentParts is present, convert each part:
// - TextInputContent → TextContent
// - BinaryInputContent (with resolved bytes) → DataContent(bytes, mimeType)
// Falls back to existing Content string behavior for plain messages
```

The converter stays pure — no file I/O. It receives already-resolved messages from the file processor.

#### 2.4 Extend ConvertFromChatMessage (reverse direction)

Handle `DataContent` in M.E.AI messages when converting back to AGUI format (for messages snapshot events).

#### 2.5 Wire into AGUIStreamingService

In `StreamCoreAsync`, before the existing message conversion (line 114):

```csharp
// Process files: store base64 data, resolve id references
var fileResult = await _fileProcessor.ProcessInboundAsync(request.Messages, request.ThreadId, cancellationToken);

// Convert resolved messages (with bytes) to M.E.AI format
var chatMessages = _messageConverter.ConvertToChatMessages(fileResult.ResolvedMessages);

// ... existing streaming logic ...

// After run completes, emit snapshot with lightweight references
yield return emitter.EmitMessagesSnapshot(fileResult.RewrittenMessages);
```

#### 2.6 Unit tests

- File processor: base64 → store + rewrite to id
- File processor: id → resolve bytes
- File processor: pass-through for text-only messages
- Converter: multimodal AGUIMessage with bytes → ChatMessage with DataContent
- Converter: ChatMessage with DataContent → AGUIMessage with ContentParts
- Backward compatibility: plain string messages still work
- Integration: full flow from inbound base64 → stored → resolved → DataContent

---

### Phase 3: Frontend Transport — TypeScript Types

**Package:** `Umbraco.AI.Agent.Web.StaticAssets`

#### 3.1 Add TypeScript content part types

In `transport/types.ts`, add:
```typescript
interface UaiTextInputContent { type: "text"; text: string; }
interface UaiBinaryInputContent {
  type: "binary";
  mimeType: string;
  data?: string;      // base64 (initial upload)
  url?: string;
  id?: string;        // server reference (after snapshot)
  filename?: string;
}
type UaiInputContent = UaiTextInputContent | UaiBinaryInputContent;
```

#### 3.2 Extend UaiChatMessage

Add optional `contentParts?: UaiInputContent[]` to `UaiChatMessage`. When present, `content` becomes a display-friendly text summary.

#### 3.3 Update UaiAgentClient message conversion

In `uai-agent-client.ts`, update `#toAGUIMessage()` to pass `contentParts` through to the AG-UI message format when present.

#### 3.4 Handle MessagesSnapshotEvent

The `onMessagesSnapshot` callback already exists in the run controller — it replaces the local message history. After the first turn with files, the backend emits a snapshot with id-based references, and the frontend adopts them automatically. Subsequent turns send the lightweight references.

---

### Phase 4: Chat Input — File Upload UI

**Package:** `Umbraco.AI.Agent.UI`

#### 4.1 Add file picker to chat input

Modify `input.element.ts`:
- Add paperclip/attachment button in `left-actions` area (next to agent selector)
- Hidden `<input type="file" multiple accept="image/*,.pdf,.txt,.csv,.doc,.docx">` triggered by button
- Track selected files in `_attachments` state array

#### 4.2 Attachment preview strip

Add a preview area between the textarea and the divider:
- Image files: small thumbnail previews with remove button
- Documents: chip with filename + file type icon + remove button
- Show file size
- Drag-and-drop support on the input area

#### 4.3 Update send event

When dispatching the `send` event, include attachments:
- Read files as base64 via `FileReader`
- Build `UaiInputContent[]` array: text content + binary parts
- Dispatch with `detail: { text, contentParts }` instead of just string

#### 4.4 Update chat component

Modify `chat.element.ts` to pass the enriched send payload to `context.sendUserMessage()`.

#### 4.5 File validation

- Max file size (configurable, default 10MB)
- Allowed MIME types (configurable allow-list)
- Visual feedback for rejected files

---

### Phase 5: Chat Context — Wire Through

**Package:** `Umbraco.AI.Agent.UI` + `Umbraco.AI.Agent.Copilot`

#### 5.1 Update UaiChatContextApi

Extend `sendUserMessage` signature:
```typescript
sendUserMessage(content: string, contentParts?: UaiInputContent[]): Promise<void>;
```

#### 5.2 Update Copilot context implementation

Wire `contentParts` through `CopilotContext → RunController → AgentClient → HTTP transport`.

#### 5.3 Update message display

In `message.element.ts`, render inline images and file attachment chips for user messages that contain binary content parts.

---

### Phase 6: Allowed file types & size configuration (stretch)

- Configurable via `UaiChatContextApi` or a new configuration context
- Default image types: PNG, JPEG, GIF, WebP
- Default document types: PDF, TXT, CSV
- Max file size setting
- Could be agent-specific (some agents handle images, others don't)

---

## File Changes Summary

| File | Change |
|------|--------|
| **AGUI Library** | |
| `AGUI/Models/AGUIInputContent.cs` | **New** — Base content part type with polymorphic discriminator |
| `AGUI/Models/AGUITextInputContent.cs` | **New** — Text content part |
| `AGUI/Models/AGUIBinaryInputContent.cs` | **New** — Binary content part |
| `AGUI/Models/AGUIMessage.cs` | **Modified** — Add ContentParts, custom JSON converter |
| **Agent.Core** | |
| `Agent.Core/AGUI/IAGUIFileProcessor.cs` | **New** — File processing interface + result type |
| `Agent.Core/AGUI/AGUIFileProcessor.cs` | **New** — Default implementation |
| `Agent.Core/AGUI/IAGUIFileStore.cs` | **New** — Thread-scoped file storage interface |
| `Agent.Core/AGUI/AGUIFileStore.cs` | **New** — Temp folder implementation |
| `Agent.Core/AGUI/AGUIMessageConverter.cs` | **Modified** — Handle ContentParts → M.E.AI DataContent |
| `Agent.Core/AGUI/AGUIStreamingService.cs` | **Modified** — File processing + MessagesSnapshotEvent |
| **Frontend Transport** | |
| `Agent.Web.StaticAssets/.../transport/types.ts` | **Modified** — Add InputContent types |
| `Agent.Web.StaticAssets/.../transport/uai-agent-client.ts` | **Modified** — Pass ContentParts |
| **Chat UI** | |
| `Agent.UI/.../chat/context.ts` | **Modified** — Extend sendUserMessage signature |
| `Agent.UI/.../chat/components/input.element.ts` | **Modified** — File picker + preview strip |
| `Agent.UI/.../chat/components/message.element.ts` | **Modified** — Render image/file attachments |
| `Agent.UI/.../chat/services/run.controller.ts` | **Modified** — Wire contentParts |
| `Copilot/.../copilot/copilot.context.ts` | **Modified** — Wire contentParts |
| **Tests** | |
| Tests (multiple) | **New/Modified** — Unit tests for all layers |

## Key Design Decisions

1. **Follow AG-UI multimodal draft exactly** — Use `TextInputContent` and `BinaryInputContent` with `type` discriminator, not custom extensions via `forwardedProps`.

2. **Server-side file processing with thread-scoped storage** — Frontend sends base64 `data` on first upload. Backend stores files in temp, scoped to `threadId`. Returns lightweight `id` references via `MessagesSnapshotEvent`. Subsequent turns send `id` only — backend resolves to bytes.

3. **File processing in streaming service, not converter** — `IAGUIFileProcessor` runs in `AGUIStreamingService.StreamCoreAsync` where the full `AGUIRunRequest` (including `threadId`) is available. Converter stays pure format mapping with no file I/O.

4. **MessagesSnapshotEvent for history rewriting** — Already exists in AGUI library but not emitted. Used to swap base64 → id refs in frontend history after the first turn. Frontend's existing `onMessagesSnapshot` handler replaces local state automatically.

5. **Backward compatible** — Plain string `content` still works. `ContentParts` is optional and only used when files are attached. No breaking changes to existing message flow.

6. **Shared in Agent.UI** — All chat surfaces (Copilot, future workspace chat) get file upload support automatically.

7. **M.E.AI DataContent is the backend target** — Already proven in the Prompt package's `ImageTemplateVariableProcessor` and `AIRuntimeContext.AddData()`.
