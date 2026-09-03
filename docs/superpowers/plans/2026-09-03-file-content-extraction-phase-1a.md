# File Content Extraction — Phase 1a Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the AI actually read `.txt`, `.csv`, and `.md` files (and, as a free side effect,
`.docx`/`.xlsx`/`.pptx`) attached in chat or fetched via the `get_umbraco_media` tool — today it
either sees nothing or gets a metadata-only stub.

**Architecture:** Umbraco.AI already has a working, pluggable text-extraction pipeline
(`IAIFileProcessingHandler` → `AIFileProcessingChatMiddleware`) that converts file attachments
into text before they reach the AI provider. It has a handler for Office documents and one for
audio, but none for plain text — and a separate, unrelated extension whitelist
(`AIUmbracoMediaResolver`) blocks those same file types (plus Office formats) from ever being
read when the file comes from an Umbraco Media item. This plan adds the missing plain-text
handler and widens the whitelist. No new abstractions, no new dependencies — this reuses the
existing handler pattern exactly as OpenXML and audio do today.

**Tech Stack:** .NET 10, xUnit, Shouldly, Moq (existing test stack — no new packages).

**Spec:** `docs/superpowers/specs/2026-09-03-file-content-extraction-design.md` (Phase 1a section
specifically; Phase 1b "Resources panel" and Phase 2 "PDF" are explicitly out of scope here).

## Global Constraints

- No new NuGet dependencies (Phase 1a only touches formats .NET can decode natively).
- Truncate extracted text at 100,000 characters with the marker
  `[Content truncated due to size limits]`, matching `OpenXmlFileProcessingHandler`'s existing
  convention exactly — pull this into one shared constant rather than duplicating the number.
- New handler classes are `internal sealed`, matching `OpenXmlFileProcessingHandler` and
  `AudioTranscriptionFileProcessingHandler`.
- Feature-sliced structure: new files live flat inside the existing `FileProcessing/` and
  `Media/` folders in `Umbraco.AI.Core` — no new subfolders.
- `.pdf` stays unsupported after this plan (Phase 2, separate dependency, separate plan) — Task 3
  includes a test asserting this explicitly, so a future change can't silently widen scope here.

---

## File Structure

| File | Responsibility |
|---|---|
| `Umbraco.AI.Core/FileProcessing/AIFileProcessingConstants.cs` (new) | Single source of truth for the 100,000-character truncation cap, shared by every text-extraction handler. |
| `Umbraco.AI.Core/FileProcessing/OpenXmlFileProcessingHandler.cs` (modify) | Switch its local truncation constant to the shared one. No behavior change. |
| `Umbraco.AI.Core/FileProcessing/PlainTextFileProcessingHandler.cs` (new) | Decodes `text/plain`, `text/csv`, `text/markdown` bytes as UTF-8 and returns them, truncating per the shared cap. |
| `Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs` (modify) | Registers the new handler in the `AIFileProcessingHandlers()` chain. |
| `Umbraco.AI.Core/Media/AIMediaExtensionResolver.cs` (new) | Pure extension→MIME-type lookup, extracted out of `AIUmbracoMediaResolver` so it's unit-testable without Umbraco's media/file-system infrastructure. Carries the widened list (adds `.txt`/`.md`/`.csv`/`.docx`/`.xlsx`/`.pptx` to the existing image/audio set). |
| `Umbraco.AI.Core/Media/AIUmbracoMediaResolver.cs` (modify) | Delegates to `AIMediaExtensionResolver` instead of its own private dictionary. No other behavior change. |

Task order: 1 → 2 → 3. Task 3 (the `Media` namespace) has no code dependency on Tasks 1–2 (the
`FileProcessing` namespace) and could run in parallel if using subagent-driven-development, but
is listed last for a simpler linear read.

---

### Task 1: Extract the shared truncation constant

**Files:**
- Create: `Umbraco.AI/src/Umbraco.AI.Core/FileProcessing/AIFileProcessingConstants.cs`
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/FileProcessing/OpenXmlFileProcessingHandler.cs:22,54,57`
- Test: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/FileProcessing/OpenXmlFileProcessingHandlerTests.cs` (existing — no new test, used as a regression check)

**Interfaces:**
- Produces: `Umbraco.AI.Core.FileProcessing.AIFileProcessingConstants.MaxExtractedCharacters` (`internal const int`, value `100_000`) — consumed by Task 2's handler.

This is a pure refactor (no behavior change), so it's verified by the existing test suite rather
than a new test — `OpenXmlFileProcessingHandlerTests.ProcessAsync_WithLargeContent_TruncatesAndIndicates`
already asserts the exact truncation behavior this constant drives.

- [ ] **Step 1: Create the shared constant**

Create `Umbraco.AI/src/Umbraco.AI.Core/FileProcessing/AIFileProcessingConstants.cs`:

```csharp
namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// Shared limits for file-processing handlers that extract text from uploaded files.
/// </summary>
internal static class AIFileProcessingConstants
{
    /// <summary>
    /// The maximum number of characters a handler will return before truncating,
    /// to keep a single attached file from consuming excessive context budget.
    /// </summary>
    public const int MaxExtractedCharacters = 100_000;
}
```

- [ ] **Step 2: Point `OpenXmlFileProcessingHandler` at the shared constant**

In `Umbraco.AI/src/Umbraco.AI.Core/FileProcessing/OpenXmlFileProcessingHandler.cs`, remove this
line:

```csharp
    private const int MaxCharacters = 100_000;
```

And replace this block:

```csharp
        var wasTruncated = content.Length > MaxCharacters;
        if (wasTruncated)
        {
            content = content[..MaxCharacters] + "\n\n[Content truncated due to size limits]";
        }
```

with:

```csharp
        var wasTruncated = content.Length > AIFileProcessingConstants.MaxExtractedCharacters;
        if (wasTruncated)
        {
            content = content[..AIFileProcessingConstants.MaxExtractedCharacters] + "\n\n[Content truncated due to size limits]";
        }
```

Both files are in the same `Umbraco.AI.Core.FileProcessing` namespace, so no new `using` is needed.

- [ ] **Step 3: Run the existing OpenXML tests to confirm no regression**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~OpenXmlFileProcessingHandlerTests"`
Expected: All tests PASS, including `ProcessAsync_WithLargeContent_TruncatesAndIndicates`.

- [ ] **Step 4: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/FileProcessing/AIFileProcessingConstants.cs Umbraco.AI/src/Umbraco.AI.Core/FileProcessing/OpenXmlFileProcessingHandler.cs
git commit -m "refactor(core): Extract shared file-processing truncation constant"
```

---

### Task 2: Plain-text file processing handler

**Files:**
- Create: `Umbraco.AI/src/Umbraco.AI.Core/FileProcessing/PlainTextFileProcessingHandler.cs`
- Create: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/FileProcessing/PlainTextFileProcessingHandlerTests.cs`
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs:112-115`

**Interfaces:**
- Consumes: `Umbraco.AI.Core.FileProcessing.AIFileProcessingConstants.MaxExtractedCharacters` (Task 1), `IAIFileProcessingHandler` (existing interface), `AIFileProcessingResult(string Content, bool WasTruncated)` (existing record).
- Produces: `Umbraco.AI.Core.FileProcessing.PlainTextFileProcessingHandler` — no other task depends on this directly; it's wired into the DI collection by this task's own registration step.

- [ ] **Step 1: Write the failing tests**

Create `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/FileProcessing/PlainTextFileProcessingHandlerTests.cs`:

```csharp
using System.Text;
using Umbraco.AI.Core.FileProcessing;

namespace Umbraco.AI.Tests.Unit.FileProcessing;

public class PlainTextFileProcessingHandlerTests
{
    private readonly PlainTextFileProcessingHandler _handler = new();

    #region CanHandle

    [Theory]
    [InlineData("text/plain", true)]
    [InlineData("text/csv", true)]
    [InlineData("text/markdown", true)]
    [InlineData("application/pdf", false)]
    [InlineData("image/png", false)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", false)]
    [InlineData("application/octet-stream", false)]
    public async Task CanHandleAsync_WithMimeType_ReturnsExpected(string mimeType, bool expected)
    {
        (await _handler.CanHandleAsync(mimeType)).ShouldBe(expected);
    }

    #endregion

    #region ProcessAsync

    [Fact]
    public async Task ProcessAsync_WithPlainText_ReturnsContentUnchanged()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello World\nSecond line");

        // Act
        var result = await _handler.ProcessAsync(data, "text/plain", "notes.txt");

        // Assert
        result.Content.ShouldBe("Hello World\nSecond line");
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithCsv_ReturnsRawCsvText()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("name,age\nAlice,30\nBob,25");

        // Act
        var result = await _handler.ProcessAsync(data, "text/csv", "people.csv");

        // Assert
        result.Content.ShouldBe("name,age\nAlice,30\nBob,25");
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithMarkdown_ReturnsRawMarkdownText()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("# Title\n\nSome **bold** text.");

        // Act
        var result = await _handler.ProcessAsync(data, "text/markdown", "readme.md");

        // Assert
        result.Content.ShouldBe("# Title\n\nSome **bold** text.");
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyFile_ReturnsEmptyContent()
    {
        // Arrange
        var data = Array.Empty<byte>();

        // Act
        var result = await _handler.ProcessAsync(data, "text/plain", "empty.txt");

        // Assert
        result.Content.ShouldBeEmpty();
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithLargeContent_TruncatesAndIndicates()
    {
        // Arrange - content exceeding 100K characters
        var data = Encoding.UTF8.GetBytes(new string('A', 110_000));

        // Act
        var result = await _handler.ProcessAsync(data, "text/plain", "big.txt");

        // Assert
        result.WasTruncated.ShouldBeTrue();
        result.Content.ShouldContain("[Content truncated due to size limits]");
        result.Content.Length.ShouldBeLessThan(110_000);
    }

    #endregion
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~PlainTextFileProcessingHandlerTests"`
Expected: FAIL to compile — `PlainTextFileProcessingHandler` does not exist yet.

- [ ] **Step 3: Implement the handler**

Create `Umbraco.AI/src/Umbraco.AI.Core/FileProcessing/PlainTextFileProcessingHandler.cs`:

```csharp
using System.Text;

namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// Extracts text content from plain-text files (CSV, Markdown, plain text).
/// </summary>
internal sealed class PlainTextFileProcessingHandler : IAIFileProcessingHandler
{
    private static readonly HashSet<string> SupportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/csv",
        "text/markdown",
    };

    /// <inheritdoc />
    public Task<bool> CanHandleAsync(string mimeType, CancellationToken cancellationToken = default)
        => Task.FromResult(SupportedMimeTypes.Contains(mimeType));

    /// <inheritdoc />
    public Task<AIFileProcessingResult> ProcessAsync(
        ReadOnlyMemory<byte> data,
        string mimeType,
        string? filename,
        CancellationToken cancellationToken = default)
    {
        var content = Encoding.UTF8.GetString(data.Span);

        var wasTruncated = content.Length > AIFileProcessingConstants.MaxExtractedCharacters;
        if (wasTruncated)
        {
            content = content[..AIFileProcessingConstants.MaxExtractedCharacters] + "\n\n[Content truncated due to size limits]";
        }

        return Task.FromResult(new AIFileProcessingResult(content, wasTruncated));
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~PlainTextFileProcessingHandlerTests"`
Expected: All tests PASS.

- [ ] **Step 5: Register the handler**

In `Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs`, change:

```csharp
        // File processing handlers (extensible - add custom handlers via AIFileProcessingHandlers())
        builder.AIFileProcessingHandlers()
            .Append<OpenXmlFileProcessingHandler>()
            .Append<AudioTranscriptionFileProcessingHandler>();
```

to:

```csharp
        // File processing handlers (extensible - add custom handlers via AIFileProcessingHandlers())
        builder.AIFileProcessingHandlers()
            .Append<OpenXmlFileProcessingHandler>()
            .Append<PlainTextFileProcessingHandler>()
            .Append<AudioTranscriptionFileProcessingHandler>();
```

- [ ] **Step 6: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/FileProcessing/PlainTextFileProcessingHandler.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/FileProcessing/PlainTextFileProcessingHandlerTests.cs Umbraco.AI/src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs
git commit -m "feat(core): Extract text from plain-text file attachments (csv, txt, md)"
```

---

### Task 3: Widen the Umbraco Media extension whitelist

**Files:**
- Create: `Umbraco.AI/src/Umbraco.AI.Core/Media/AIMediaExtensionResolver.cs`
- Create: `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Media/AIMediaExtensionResolverTests.cs`
- Modify: `Umbraco.AI/src/Umbraco.AI.Core/Media/AIUmbracoMediaResolver.cs:14-33,292-297`

**Interfaces:**
- Produces: `Umbraco.AI.Core.Media.AIMediaExtensionResolver.TryGetMediaType(string extension, out string? mediaType)` (`internal static`, returns `bool`) — consumed by `AIUmbracoMediaResolver.LoadFromPath` in this same task.

**Why extract this into its own class:** `AIUmbracoMediaResolver` has zero existing unit tests
today, and testing it directly would mean standing up Umbraco's `MediaFileManager`/`IMediaService`
infrastructure just to check a MIME-type lookup table. Pulling the pure lookup into its own file
gives it a real, isolated unit test with no CMS mocking required, without changing any observable
behavior of the resolver itself.

- [ ] **Step 1: Write the failing tests**

Create `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Media/AIMediaExtensionResolverTests.cs`:

```csharp
using Umbraco.AI.Core.Media;

namespace Umbraco.AI.Tests.Unit.Media;

public class AIMediaExtensionResolverTests
{
    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".webp", "image/webp")]
    [InlineData(".bmp", "image/bmp")]
    [InlineData(".mp3", "audio/mpeg")]
    [InlineData(".wav", "audio/wav")]
    [InlineData(".m4a", "audio/mp4")]
    [InlineData(".mp4", "audio/mp4")]
    [InlineData(".ogg", "audio/ogg")]
    [InlineData(".oga", "audio/ogg")]
    [InlineData(".webm", "audio/webm")]
    [InlineData(".flac", "audio/flac")]
    [InlineData(".txt", "text/plain")]
    [InlineData(".md", "text/markdown")]
    [InlineData(".csv", "text/csv")]
    [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    public void TryGetMediaType_WithSupportedExtension_ReturnsExpectedMediaType(string extension, string expectedMediaType)
    {
        var result = AIMediaExtensionResolver.TryGetMediaType(extension, out var mediaType);

        result.ShouldBeTrue();
        mediaType.ShouldBe(expectedMediaType);
    }

    [Theory]
    [InlineData(".exe")]
    [InlineData(".pdf")]
    [InlineData(".zip")]
    [InlineData("")]
    public void TryGetMediaType_WithUnsupportedExtension_ReturnsFalse(string extension)
    {
        var result = AIMediaExtensionResolver.TryGetMediaType(extension, out var mediaType);

        result.ShouldBeFalse();
        mediaType.ShouldBeNull();
    }

    [Theory]
    [InlineData(".JPG", "image/jpeg")]
    [InlineData(".CSV", "text/csv")]
    public void TryGetMediaType_IsCaseInsensitive(string extension, string expectedMediaType)
    {
        var result = AIMediaExtensionResolver.TryGetMediaType(extension, out var mediaType);

        result.ShouldBeTrue();
        mediaType.ShouldBe(expectedMediaType);
    }
}
```

Note the `.pdf` case lives in the *unsupported* theory — PDF is Phase 2 (its own plan, its own
dependency decision). This test pins that boundary down so it can't drift by accident.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~AIMediaExtensionResolverTests"`
Expected: FAIL to compile — `AIMediaExtensionResolver` does not exist yet.

- [ ] **Step 3: Implement the resolver**

Create `Umbraco.AI/src/Umbraco.AI.Core/Media/AIMediaExtensionResolver.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Media;

/// <summary>
/// Maps file extensions to the MIME types <see cref="IAIUmbracoMediaResolver"/> understands.
/// Extracted from <see cref="AIUmbracoMediaResolver"/> so the lookup can be unit tested without
/// the Umbraco CMS media/file-system infrastructure that resolver depends on.
/// </summary>
internal static class AIMediaExtensionResolver
{
    private static readonly Dictionary<string, string> ExtensionToMediaType = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp",

        // Audio
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".m4a"] = "audio/mp4",
        [".mp4"] = "audio/mp4",
        [".ogg"] = "audio/ogg",
        [".oga"] = "audio/ogg",
        [".webm"] = "audio/webm",
        [".flac"] = "audio/flac",

        // Plain text
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".csv"] = "text/csv",

        // Office documents
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    };

    /// <summary>
    /// Attempts to resolve the MIME type for a file extension (e.g. <c>.png</c>).
    /// </summary>
    /// <param name="extension">The file extension, including the leading dot.</param>
    /// <param name="mediaType">The resolved MIME type, when found.</param>
    /// <returns><c>true</c> if the extension is recognized; otherwise <c>false</c>.</returns>
    public static bool TryGetMediaType(string extension, [NotNullWhen(true)] out string? mediaType)
        => ExtensionToMediaType.TryGetValue(extension, out mediaType);
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~AIMediaExtensionResolverTests"`
Expected: All tests PASS.

- [ ] **Step 5: Wire `AIUmbracoMediaResolver` to the new resolver**

In `Umbraco.AI/src/Umbraco.AI.Core/Media/AIUmbracoMediaResolver.cs`, remove this block (the
private dictionary, currently at the top of the class):

```csharp
    private static readonly Dictionary<string, string> ExtensionToMediaType = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp",

        // Audio
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".m4a"] = "audio/mp4",
        [".mp4"] = "audio/mp4",
        [".ogg"] = "audio/ogg",
        [".oga"] = "audio/ogg",
        [".webm"] = "audio/webm",
        [".flac"] = "audio/flac",
    };
```

And in `LoadFromPath`, replace:

```csharp
        // Get file extension for media type
        var extension = Path.GetExtension(filePath);
        if (!ExtensionToMediaType.TryGetValue(extension, out var mediaType))
        {
            _logger.LogWarning("Unsupported media extension: {Extension}", extension);
            return null;
        }
```

with:

```csharp
        // Get file extension for media type
        var extension = Path.GetExtension(filePath);
        if (!AIMediaExtensionResolver.TryGetMediaType(extension, out var mediaType))
        {
            _logger.LogWarning("Unsupported media extension: {Extension}", extension);
            return null;
        }
```

Both files are in the same `Umbraco.AI.Core.Media` namespace, so no new `using` is needed.

- [ ] **Step 6: Run the full Media test folder to confirm no regression**

Run: `dotnet test Umbraco.AI/Umbraco.AI.slnx --filter "FullyQualifiedName~Umbraco.AI.Tests.Unit.Media"`
Expected: All tests PASS (`AIImageCropperTests`, `AIImageDownscalerTests`, and the two new
`AIMediaExtensionResolverTests` theories) — the crop/downscale tests are unaffected since they
don't touch extension resolution, but this confirms nothing in the `Media` folder broke.

- [ ] **Step 7: Commit**

```bash
git add Umbraco.AI/src/Umbraco.AI.Core/Media/AIMediaExtensionResolver.cs Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Media/AIMediaExtensionResolverTests.cs Umbraco.AI/src/Umbraco.AI.Core/Media/AIUmbracoMediaResolver.cs
git commit -m "feat(core): Allow text and Office file extensions through the Umbraco media resolver"
```

---

## Post-plan verification (manual, not a task)

Once all three tasks are done, the spec's symptoms 1 and 2 should be fixed:

1. Attach a `.csv` file directly in a Copilot chat message and ask what's in it — the AI should
   now read and describe the rows.
2. Upload the same file as an Umbraco Media item, reference it in a prompt/agent flow that calls
   `get_umbraco_media`, and confirm the AI can describe its contents.
3. As a bonus check: repeat with a `.docx` file — it should now work too, since Task 3 opened the
   whitelist for a handler (`OpenXmlFileProcessingHandler`) that already existed.

This manual check isn't a task with its own commit — it's a sanity pass against the demo site
before calling Phase 1a done. Symptom 3 (the "Resources" panel) is Phase 1b, a separate plan.

## Self-Review Notes

- **Spec coverage:** Phase 1a's two spec deliverables — the plain-text handler and the widened
  resolver whitelist (including the "free" `.docx`/`.xlsx`/`.pptx` unlock) — are both covered
  (Tasks 2 and 3). The shared-constant cleanup the spec called out is Task 1. Phase 1b and Phase
  2 are out of scope by design and not included here.
- **Redundant test avoided:** the spec's testing section mentions an "integration-style test
  through `AIFileProcessingChatClient`". `AIFileProcessingChatClientTests.cs` already has a
  `FakeHandler` covering `text/csv` end-to-end at that layer — adding a second, near-identical
  test using the real `PlainTextFileProcessingHandler` would just re-test the same client logic
  with different fake data. Skipped as redundant per this repo's stated testing philosophy
  (avoid arbitrary/passthrough tests); Task 2's own unit tests already prove the handler's
  behavior in isolation.
- **No registration test:** the `UmbracoBuilderExtensions.cs` composer wiring in Task 2 Step 5 is
  boilerplate DI registration with no branching logic, consistent with why the existing
  `OpenXmlFileProcessingHandler`/`AudioTranscriptionFileProcessingHandler` registrations have no
  dedicated test either.
