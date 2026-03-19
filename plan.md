# File Uploads & AI Processing - Implementation Plan

## AG-UI Multimodal Messages Draft Alignment

The [AG-UI multimodal draft](https://docs.ag-ui.com/drafts/multimodal-messages) (Implemented, Oct 2025) extends message `content` to be either a `string` or an array of `InputContent` parts:

- **`TextInputContent`** — `{ type: "text", text: string }`
- **`BinaryInputContent`** — `{ type: "binary", mimeType: string, data?: string (base64), url?: string, id?: string, filename?: string }`

Our implementation follows this spec exactly in the AGUI library, then layers Umbraco-specific handling on top.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│  Frontend (Agent.UI)                                    │
│  ┌─────────────────────────────────────────────────┐    │
│  │ <uai-chat-input>                                │    │
│  │  - File picker button (paperclip icon)          │    │
│  │  - Attachment preview strip (thumbnails/chips)  │    │
│  │  - On send: include files as BinaryInputContent │    │
│  └─────────────────────────────────────────────────┘    │
│                        │                                │
│  Message sent with content: InputContent[]              │
│  (text + binary parts with base64 data)                 │
└────────────────────────┬────────────────────────────────┘
                         │ POST /stream-agui
                         ▼
┌─────────────────────────────────────────────────────────┐
│  AGUI Library (Protocol Layer)                          │
│  - AGUIInputContent / AGUITextInputContent /            │
│    AGUIBinaryInputContent models                        │
│  - AGUIMessage.Content stays string for back-compat     │
│  - AGUIMessage.ContentParts: AGUIInputContent[]? (new)  │
│  - JSON polymorphic deserialization via "type" field    │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│  Agent.Core (Message Converter)                         │
│  - AGUIMessageConverter handles ContentParts            │
│  - TextInputContent → M.E.AI TextContent               │
│  - BinaryInputContent → M.E.AI DataContent(bytes, mime)│
│  - Falls back to Content string for plain messages      │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│  M.E.AI (Microsoft.Extensions.AI)                       │
│  - ChatMessage with mixed AIContent list                │
│  - DataContent(byte[], mediaType) for binary            │
│  - TextContent(string) for text                         │
│  - Sent to LLM providers (OpenAI vision, etc.)         │
└─────────────────────────────────────────────────────────┘
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

Add a new property to `AGUIMessage`:
```csharp
[JsonPropertyName("content")]
// Content becomes a polymorphic field:
// - string (backward compatible)
// - InputContent[] (multimodal)
```

**Approach:** Use a custom `JsonConverter` on AGUIMessage that handles the `content` field being either a `string` or an `array`. Add a `ContentParts` property for the multimodal case. A helper resolves which is populated:

```csharp
// When content is a string → Content is set, ContentParts is null
// When content is an array → ContentParts is set, Content is derived from text parts
```

#### 1.3 Unit tests

- Serialization/deserialization of text-only messages (backward compat)
- Deserialization of multimodal content arrays
- Round-trip for BinaryInputContent with base64 data
- Edge cases: empty content, mixed text+binary parts

---

### Phase 2: Agent.Core — Message Converter

**Package:** `Umbraco.AI.Agent.Core`

#### 2.1 Extend AGUIMessageConverter

Update `ConvertToChatMessage()` to handle `ContentParts`:

```csharp
// If ContentParts is present, convert each part:
// - TextInputContent → TextContent
// - BinaryInputContent → DataContent (decode base64 → bytes)
// Falls back to existing Content string behavior
```

#### 2.2 Extend ConvertFromChatMessage (reverse direction)

Handle `DataContent` in M.E.AI messages when converting back to AGUI format (for messages snapshot events).

#### 2.3 Unit tests

- Convert multimodal AGUIMessage → ChatMessage with DataContent
- Convert ChatMessage with DataContent → AGUIMessage with ContentParts
- Backward compatibility: plain string messages still work

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
  data?: string;      // base64
  url?: string;
  id?: string;
  filename?: string;
}
type UaiInputContent = UaiTextInputContent | UaiBinaryInputContent;
```

#### 3.2 Extend UaiChatMessage

Add optional `contentParts?: UaiInputContent[]` to `UaiChatMessage`. When present, `content` becomes a display-friendly text summary.

#### 3.3 Update UaiAgentClient message conversion

In `uai-agent-client.ts`, update `#toAGUIMessage()` to pass `contentParts` through to the AG-UI message format when present.

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
| `AGUI/Models/AGUIInputContent.cs` | **New** — Base content part type with polymorphic discriminator |
| `AGUI/Models/AGUITextInputContent.cs` | **New** — Text content part |
| `AGUI/Models/AGUIBinaryInputContent.cs` | **New** — Binary content part |
| `AGUI/Models/AGUIMessage.cs` | **Modified** — Add ContentParts, custom JSON converter |
| `Agent.Core/AGUI/AGUIMessageConverter.cs` | **Modified** — Handle ContentParts ↔ M.E.AI |
| `Agent.Web.StaticAssets/.../transport/types.ts` | **Modified** — Add InputContent types |
| `Agent.Web.StaticAssets/.../transport/uai-agent-client.ts` | **Modified** — Pass ContentParts |
| `Agent.UI/.../chat/context.ts` | **Modified** — Extend sendUserMessage signature |
| `Agent.UI/.../chat/components/input.element.ts` | **Modified** — File picker + preview strip |
| `Agent.UI/.../chat/components/message.element.ts` | **Modified** — Render image/file attachments |
| `Agent.UI/.../chat/services/run.controller.ts` | **Modified** — Wire contentParts |
| `Copilot/.../copilot/copilot.context.ts` | **Modified** — Wire contentParts |
| Tests (multiple) | **New/Modified** — Unit tests for all layers |

## Key Design Decisions

1. **Follow AG-UI multimodal draft exactly** — Use `TextInputContent` and `BinaryInputContent` with `type` discriminator, not custom extensions via `forwardedProps`.

2. **Base64 inline data for initial implementation** — The AG-UI draft supports `data` (base64), `url`, and `id` fields on `BinaryInputContent`. We start with base64 `data` for simplicity. A server upload endpoint returning URLs can be added later for large files using the `url` field.

3. **Backward compatible** — Plain string `content` still works. `ContentParts` is optional and only used when files are attached.

4. **Shared in Agent.UI** — All chat surfaces (Copilot, future workspace chat) get file upload support automatically.

5. **M.E.AI DataContent is the backend target** — This is already proven in the Prompt package's `ImageTemplateVariableProcessor` and `AIRuntimeContext.AddData()`.
