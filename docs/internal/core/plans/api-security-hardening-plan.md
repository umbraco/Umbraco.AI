# API Security Hardening Implementation Plan

This plan addresses security improvements for the Chat and Embedding API endpoints, focusing on:
- P0: Rate Limiting
- P0: Input Size Validation (MaxLength)
- P1: Error Response Sanitization
- P2: Audit Logging

**Excluded from this plan:** Default profile fallback changes (#4) and per-profile authorization (#5).

---

## Overview

### Current State

| Area | Status | Risk |
|------|--------|------|
| Rate Limiting | Not implemented | High - cost amplification attacks |
| Input Validation | Partial (`[Required]`, `[MinLength]` only) | High - DoS via large payloads |
| Error Handling | Exception details exposed to clients | Medium - information disclosure |
| Audit Logging | M.E.AI logging middleware exists | Low - no API-level audit trail |

### Target State

- Rate limiting on all AI endpoints (configurable per-endpoint)
- MaxLength validation on all string inputs
- Generic error messages to clients, detailed logs server-side
- Structured audit logging for all AI API calls

---

## Phase 1: Input Validation (P0)

**Goal:** Prevent large payload attacks by adding size constraints to all request models.

### Step 1.1: Add Validation Constants to Constants.cs

Add validation limits to the existing `Constants.cs` file using static class grouping.

**File:** `src/Umbraco.AI.Web/Constants.cs`

Add the following static class inside the `Constants` class:

```csharp
/// <summary>
/// Defines validation constraints for API request models.
/// </summary>
public static class Validation
{
    /// <summary>
    /// Validation limits for chat requests.
    /// </summary>
    public static class Chat
    {
        /// <summary>
        /// Maximum length of message content in characters (~32KB).
        /// </summary>
        public const int MaxMessageContentLength = 32_000;

        /// <summary>
        /// Maximum number of messages per request.
        /// </summary>
        public const int MaxMessagesPerRequest = 100;

        /// <summary>
        /// Maximum length of role string (user, assistant, system, tool).
        /// </summary>
        public const int MaxRoleLength = 50;
    }

    /// <summary>
    /// Validation limits for embedding requests.
    /// </summary>
    public static class Embedding
    {
        /// <summary>
        /// Maximum length of each text value in characters (~8KB).
        /// </summary>
        public const int MaxTextLength = 8_000;

        /// <summary>
        /// Maximum number of values per request.
        /// </summary>
        public const int MaxValuesPerRequest = 100;
    }

    /// <summary>
    /// Common validation limits for entities.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Maximum length of alias strings.
        /// </summary>
        public const int MaxAliasLength = 100;

        /// <summary>
        /// Maximum length of name strings.
        /// </summary>
        public const int MaxNameLength = 200;

        /// <summary>
        /// Maximum length of individual tag strings.
        /// </summary>
        public const int MaxTagLength = 50;

        /// <summary>
        /// Maximum number of tags per entity.
        /// </summary>
        public const int MaxTagsPerEntity = 20;

        /// <summary>
        /// Maximum length of provider ID strings.
        /// </summary>
        public const int MaxProviderIdLength = 100;

        /// <summary>
        /// Maximum length of model ID strings.
        /// </summary>
        public const int MaxModelIdLength = 200;
    }
}
```

### Step 1.2: Update Chat Request Models

**File:** `src/Umbraco.AI.Web/Api/Management/Chat/Models/ChatRequestModel.cs`

```csharp
public class ChatRequestModel
{
    public IdOrAlias? ProfileIdOrAlias { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(Constants.Validation.Chat.MaxMessagesPerRequest)]
    public IReadOnlyList<ChatMessageModel> Messages { get; set; } = [];
}
```

**File:** `src/Umbraco.AI.Web/Api/Management/Chat/Models/ChatMessageModel.cs`

```csharp
public class ChatMessageModel
{
    [Required]
    [MaxLength(Constants.Validation.Chat.MaxRoleLength)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(Constants.Validation.Chat.MaxMessageContentLength)]
    public string Content { get; set; } = string.Empty;
}
```

### Step 1.3: Update Embedding Request Model

**File:** `src/Umbraco.AI.Web/Api/Management/Embedding/Models/GenerateEmbeddingRequestModel.cs`

```csharp
public class GenerateEmbeddingRequestModel
{
    public IdOrAlias? ProfileIdOrAlias { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(Constants.Validation.Embedding.MaxValuesPerRequest)]
    [MaxItemLength(Constants.Validation.Embedding.MaxTextLength)]
    public required IReadOnlyList<string> Values { get; init; }
}
```

**Note:** `[MaxLength]` on `IReadOnlyList<string>` validates collection size. For individual string lengths, create a custom validation attribute (see Step 1.4).

### Step 1.4: Create Custom Validation for Collection Item Lengths

**File:** `src/Umbraco.AI.Web/Api/Management/Common/Validation/MaxItemLengthAttribute.cs`

```csharp
namespace Umbraco.AI.Web.Api.Management.Common.Validation;

/// <summary>
/// Validates that all string items in a collection do not exceed a maximum length.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MaxItemLengthAttribute : ValidationAttribute
{
    public int MaxLength { get; }

    public MaxItemLengthAttribute(int maxLength)
    {
        MaxLength = maxLength;
        ErrorMessage = $"Each item must not exceed {maxLength} characters.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IEnumerable<string> items)
            return ValidationResult.Success;

        var index = 0;
        foreach (var item in items)
        {
            if (item?.Length > MaxLength)
            {
                return new ValidationResult(
                    $"Item at index {index} exceeds maximum length of {MaxLength} characters.",
                    new[] { validationContext.MemberName ?? string.Empty });
            }
            index++;
        }

        return ValidationResult.Success;
    }
}
```

### Step 1.5: Update Connection/Profile Request Models

**File:** `src/Umbraco.AI.Web/Api/Management/Connection/Models/CreateConnectionRequestModel.cs`

```csharp
[Required]
[MaxLength(Constants.Validation.Common.MaxAliasLength)]
public required string Alias { get; init; }

[Required]
[MaxLength(Constants.Validation.Common.MaxNameLength)]
public required string Name { get; init; }

[Required]
[MaxLength(Constants.Validation.Common.MaxProviderIdLength)]
public required string ProviderId { get; init; }
```

**File:** `src/Umbraco.AI.Web/Api/Management/Profile/Models/CreateProfileRequestModel.cs`

```csharp
[Required]
[MaxLength(Constants.Validation.Common.MaxAliasLength)]
public required string Alias { get; init; }

[Required]
[MaxLength(Constants.Validation.Common.MaxNameLength)]
public required string Name { get; init; }

[MaxLength(Constants.Validation.Common.MaxTagsPerEntity)]
[MaxItemLength(Constants.Validation.Common.MaxTagLength)]
public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
```

### Step 1.6: Tests

**File:** `tests/Umbraco.AI.Tests.Unit/Api/Management/Chat/ChatRequestModelValidationTests.cs`

```csharp
public class ChatRequestModelValidationTests
{
    [Fact]
    public void Validate_WithOversizedMessageContent_ReturnsValidationError()
    {
        // Arrange
        var model = new ChatRequestModel
        {
            Messages = new[]
            {
                new ChatMessageModel
                {
                    Role = "user",
                    Content = new string('x', Constants.Validation.Chat.MaxMessageContentLength + 1)
                }
            }
        };

        // Act
        var results = ValidateModel(model);

        // Assert
        results.ShouldContain(r => r.MemberNames.Contains("Content"));
    }

    [Fact]
    public void Validate_WithTooManyMessages_ReturnsValidationError()
    {
        // Arrange
        var messages = Enumerable.Range(0, Constants.Validation.Chat.MaxMessagesPerRequest + 1)
            .Select(_ => new ChatMessageModel { Role = "user", Content = "test" })
            .ToList();

        var model = new ChatRequestModel { Messages = messages };

        // Act
        var results = ValidateModel(model);

        // Assert
        results.ShouldContain(r => r.MemberNames.Contains("Messages"));
    }
}
```

---

## Phase 2: Error Response Sanitization (P1)

**Goal:** Prevent information disclosure by returning generic error messages while logging details server-side.

### Step 2.1: Create Custom Exception Types

**File:** `src/Umbraco.AI.Core/Exceptions/AIException.cs`

```csharp
namespace Umbraco.AI.Core.Exceptions;

/// <summary>
/// Base exception for AI operations.
/// </summary>
public class AIException : Exception
{
    /// <summary>
    /// User-safe error message (no sensitive details).
    /// </summary>
    public string UserMessage { get; }

    /// <summary>
    /// Error code for client handling.
    /// </summary>
    public string ErrorCode { get; }

    public AIException(string errorCode, string userMessage, string internalMessage, Exception? innerException = null)
        : base(internalMessage, innerException)
    {
        ErrorCode = errorCode;
        UserMessage = userMessage;
    }
}

public class AIProfileNotFoundException : AIException
{
    public AIProfileNotFoundException(string identifier)
        : base("PROFILE_NOT_FOUND", "The requested AI profile was not found.",
               $"Profile not found: {identifier}") { }
}

public class AIConnectionNotFoundException : AIException
{
    public AIConnectionNotFoundException(Guid connectionId)
        : base("CONNECTION_NOT_FOUND", "The requested AI connection was not found.",
               $"Connection not found: {connectionId}") { }
}

public class AIProviderException : AIException
{
    public AIProviderException(string userMessage, string internalMessage, Exception? innerException = null)
        : base("PROVIDER_ERROR", userMessage, internalMessage, innerException) { }
}

public class AIRateLimitException : AIException
{
    public TimeSpan? RetryAfter { get; }

    public AIRateLimitException(TimeSpan? retryAfter = null)
        : base("RATE_LIMITED", "Too many requests. Please try again later.",
               "Rate limit exceeded")
    {
        RetryAfter = retryAfter;
    }
}
```

### Step 2.2: Create Error Response Factory

**File:** `src/Umbraco.AI.Web/Api/Management/Common/Errors/AIProblemDetailsFactory.cs`

```csharp
namespace Umbraco.AI.Web.Api.Management.Common.Errors;

/// <summary>
/// Factory for creating sanitized ProblemDetails responses.
/// </summary>
public static class AIProblemDetailsFactory
{
    public static ProblemDetails FromException(Exception ex, bool includeDetails = false)
    {
        return ex switch
        {
            AIProfileNotFoundException e => new ProblemDetails
            {
                Title = "Profile Not Found",
                Detail = e.UserMessage,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["errorCode"] = e.ErrorCode }
            },
            AIConnectionNotFoundException e => new ProblemDetails
            {
                Title = "Connection Not Found",
                Detail = e.UserMessage,
                Status = StatusCodes.Status404NotFound,
                Extensions = { ["errorCode"] = e.ErrorCode }
            },
            AIRateLimitException e => new ProblemDetails
            {
                Title = "Rate Limit Exceeded",
                Detail = e.UserMessage,
                Status = StatusCodes.Status429TooManyRequests,
                Extensions =
                {
                    ["errorCode"] = e.ErrorCode,
                    ["retryAfter"] = e.RetryAfter?.TotalSeconds
                }
            },
            AIException e => new ProblemDetails
            {
                Title = "AI Operation Failed",
                Detail = e.UserMessage,
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errorCode"] = e.ErrorCode }
            },
            _ => new ProblemDetails
            {
                Title = "An error occurred",
                Detail = includeDetails ? ex.Message : "An unexpected error occurred while processing your request.",
                Status = StatusCodes.Status500InternalServerError,
                Extensions = { ["errorCode"] = "INTERNAL_ERROR" }
            }
        };
    }
}
```

### Step 2.3: Update Controllers to Use Sanitized Errors

**File:** `src/Umbraco.AI.Web/Api/Management/Chat/Controllers/CompleteChatController.cs`

```csharp
[HttpPost("complete")]
public async Task<IActionResult> CompleteChat(ChatRequestModel requestModel, CancellationToken cancellationToken = default)
{
    try
    {
        var messages = requestModel.Messages.Select(ToMessage).ToList();

        var profileId = requestModel.ProfileIdOrAlias != null
            ? await _profileService.TryGetProfileIdAsync(requestModel.ProfileIdOrAlias, cancellationToken)
            : null;

        var response = profileId.HasValue
            ? await _chatService.GetResponseAsync(profileId.Value, messages, cancellationToken: cancellationToken)
            : await _chatService.GetResponseAsync(messages, cancellationToken: cancellationToken);

        return Ok(ToChatResponseModel(response));
    }
    catch (AIException ex)
    {
        _logger.LogWarning(ex, "Chat completion failed: {ErrorCode}", ex.ErrorCode);
        return StatusCode(
            AIProblemDetailsFactory.FromException(ex).Status ?? 400,
            AIProblemDetailsFactory.FromException(ex));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during chat completion");
        return StatusCode(500, AIProblemDetailsFactory.FromException(ex));
    }
}
```

### Step 2.4: Update Stream Controller Error Handling

**File:** `src/Umbraco.AI.Web/Api/Management/Chat/Controllers/StreamChatController.cs`

```csharp
private async Task WriteErrorToStream(Exception ex, CancellationToken cancellationToken)
{
    var problem = AIProblemDetailsFactory.FromException(ex);
    Response.StatusCode = problem.Status ?? 400;

    var errorPayload = new
    {
        error = problem.Title,
        errorCode = problem.Extensions.GetValueOrDefault("errorCode"),
        // Do NOT include ex.Message - use sanitized detail only
        detail = problem.Detail
    };

    var json = JsonSerializer.Serialize(errorPayload);
    await Response.WriteAsync($"data: {json}\n\n", cancellationToken);

    // Log full details server-side
    _logger.LogError(ex, "Streaming chat failed");
}
```

### Step 2.5: Update Services to Throw Custom Exceptions

**File:** `src/Umbraco.AI.Core/Chat/AIChatService.cs` (and similar services)

```csharp
public async Task<ChatResponse> GetResponseAsync(Guid profileId, IEnumerable<ChatMessage> messages, ...)
{
    var profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken)
        ?? throw new AIProfileNotFoundException(profileId.ToString());

    var connection = await _connectionRepository.GetByIdAsync(profile.ConnectionId, cancellationToken)
        ?? throw new AIConnectionNotFoundException(profile.ConnectionId);

    try
    {
        return await client.GetResponseAsync(messages, options, cancellationToken);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
    {
        throw new AIRateLimitException();
    }
    catch (Exception ex)
    {
        throw new AIProviderException(
            "The AI provider encountered an error processing your request.",
            $"Provider {profile.ProviderId} failed: {ex.Message}",
            ex);
    }
}
```

---

## Phase 3: Rate Limiting (P0)

**Goal:** Prevent abuse by limiting request frequency per user/endpoint.

### Step 3.1: Rate Limiting Package

ASP.NET Core includes rate limiting middleware built-in via `Microsoft.AspNetCore.RateLimiting` (part of the framework, no additional package needed for .NET 8+).

**Note:** Umbraco CMS does **not** currently call `app.UseRateLimiter()`. We need to register it via Umbraco's pipeline filter system.

### Step 3.2: Add Rate Limiting Constants to Constants.cs

**File:** `src/Umbraco.AI.Web/Constants.cs`

Add the following static class inside the `Constants` class:

```csharp
/// <summary>
/// Defines rate limiting policy names and defaults.
/// </summary>
public static class RateLimiting
{
    /// <summary>
    /// Rate limiting policy names.
    /// </summary>
    public static class Policies
    {
        /// <summary>
        /// Policy name for chat endpoints.
        /// </summary>
        public const string Chat = "UmbracoAIChat";

        /// <summary>
        /// Policy name for embedding endpoints.
        /// </summary>
        public const string Embedding = "UmbracoAIEmbedding";
    }

    /// <summary>
    /// Default rate limit settings for chat endpoints.
    /// </summary>
    public static class ChatDefaults
    {
        public const int PermitLimit = 60;
        public const int WindowSeconds = 60;
        public const int QueueLimit = 2;
    }

    /// <summary>
    /// Default rate limit settings for embedding endpoints.
    /// </summary>
    public static class EmbeddingDefaults
    {
        public const int PermitLimit = 100;
        public const int WindowSeconds = 60;
        public const int QueueLimit = 5;
    }
}
```

### Step 3.3: Create Rate Limiting Configuration Options

**File:** `src/Umbraco.AI.Core/Models/AIRateLimitOptions.cs`

```csharp
namespace Umbraco.AI.Core.Models;

/// <summary>
/// Configuration for AI API rate limiting.
/// </summary>
public class AIRateLimitOptions
{
    /// <summary>
    /// Enable or disable rate limiting globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Chat endpoint rate limit settings.
    /// </summary>
    public RateLimitSettings Chat { get; set; } = new();

    /// <summary>
    /// Embedding endpoint rate limit settings.
    /// </summary>
    public RateLimitSettings Embedding { get; set; } = new();
}

/// <summary>
/// Rate limit settings for a specific endpoint type.
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Maximum requests allowed in the time window.
    /// </summary>
    public int? PermitLimit { get; set; }

    /// <summary>
    /// Time window in seconds.
    /// </summary>
    public int? WindowSeconds { get; set; }

    /// <summary>
    /// Number of requests to queue when limit is reached.
    /// </summary>
    public int? QueueLimit { get; set; }
}
```

Update `AIOptions`:

**File:** `src/Umbraco.AI.Core/Models/AIOptions.cs`

```csharp
public class AIOptions
{
    public string? DefaultChatProfileAlias { get; set; }
    public string? DefaultEmbeddingProfileAlias { get; set; }

    /// <summary>
    /// Rate limiting configuration for AI endpoints.
    /// </summary>
    public AIRateLimitOptions RateLimiting { get; set; } = new();
}
```

### Step 3.4: Create Pipeline Filter for Rate Limiting Middleware

Since Umbraco does not call `app.UseRateLimiter()`, we need to add it via Umbraco's pipeline filter system.

**File:** `src/Umbraco.AI.Web/Configuration/UmbracoAIRateLimitingPipelineFilter.cs`

```csharp
using Microsoft.AspNetCore.Builder;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Umbraco.AI.Web.Configuration;

/// <summary>
/// Pipeline filter that adds rate limiting middleware to the Umbraco pipeline.
/// </summary>
internal sealed class UmbracoAIRateLimitingPipelineFilter : UmbracoPipelineFilter
{
    public UmbracoAIRateLimitingPipelineFilter()
        : base("UmbracoAIRateLimiting")
    {
        // Add rate limiter middleware before routing (PreRouting stage)
        // This ensures rate limiting happens early in the pipeline
        PreRouting = app => app.UseRateLimiter();
    }
}
```

### Step 3.5: Register Rate Limiting Services and Middleware

**File:** `src/Umbraco.AI.Web/Configuration/UmbracoBuilderExtensions.cs`

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

public static IUmbracoBuilder AddUmbracoAIWeb(this IUmbracoBuilder builder)
{
    // ... existing registrations

    // Configure rate limiting services
    ConfigureRateLimiting(builder);

    return builder;
}

private static void ConfigureRateLimiting(IUmbracoBuilder builder)
{
    // Get configuration
    var aiOptions = builder.Config
        .GetSection("Umbraco:AI")
        .Get<AIOptions>() ?? new AIOptions();

    if (!aiOptions.RateLimiting.Enabled)
        return;

    // Add rate limiting services
    builder.Services.AddRateLimiter(options =>
    {
        // Chat endpoint policy - per user
        options.AddPolicy(Constants.RateLimiting.Policies.Chat, context =>
        {
            var userId = context.User?.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";

            return RateLimitPartition.GetSlidingWindowLimiter(userId, _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = aiOptions.RateLimiting.Chat.PermitLimit
                        ?? Constants.RateLimiting.ChatDefaults.PermitLimit,
                    Window = TimeSpan.FromSeconds(
                        aiOptions.RateLimiting.Chat.WindowSeconds
                        ?? Constants.RateLimiting.ChatDefaults.WindowSeconds),
                    SegmentsPerWindow = 4,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = aiOptions.RateLimiting.Chat.QueueLimit
                        ?? Constants.RateLimiting.ChatDefaults.QueueLimit
                });
        });

        // Embedding endpoint policy - per user
        options.AddPolicy(Constants.RateLimiting.Policies.Embedding, context =>
        {
            var userId = context.User?.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";

            return RateLimitPartition.GetSlidingWindowLimiter(userId, _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = aiOptions.RateLimiting.Embedding.PermitLimit
                        ?? Constants.RateLimiting.EmbeddingDefaults.PermitLimit,
                    Window = TimeSpan.FromSeconds(
                        aiOptions.RateLimiting.Embedding.WindowSeconds
                        ?? Constants.RateLimiting.EmbeddingDefaults.WindowSeconds),
                    SegmentsPerWindow = 4,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = aiOptions.RateLimiting.Embedding.QueueLimit
                        ?? Constants.RateLimiting.EmbeddingDefaults.QueueLimit
                });
        });

        // Custom rejection response
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                ? retry
                : TimeSpan.FromSeconds(60);

            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();

            var problem = new ProblemDetails
            {
                Title = "Rate Limit Exceeded",
                Detail = "Too many requests. Please try again later.",
                Status = 429,
                Extensions =
                {
                    ["errorCode"] = "RATE_LIMITED",
                    ["retryAfterSeconds"] = (int)retryAfter.TotalSeconds
                }
            };

            await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        };
    });

    // Register pipeline filter to add UseRateLimiter() middleware
    builder.Services.Configure<UmbracoPipelineOptions>(options =>
    {
        options.AddFilter(new UmbracoAIRateLimitingPipelineFilter());
    });
}
```

### Step 3.6: Apply Rate Limiting to Controllers

**File:** `src/Umbraco.AI.Web/Api/Management/Chat/Controllers/CompleteChatController.cs`

```csharp
using Microsoft.AspNetCore.RateLimiting;

[EnableRateLimiting(Constants.RateLimiting.Policies.Chat)]
[HttpPost("complete")]
public async Task<IActionResult> CompleteChat(...)
```

**File:** `src/Umbraco.AI.Web/Api/Management/Chat/Controllers/StreamChatController.cs`

```csharp
using Microsoft.AspNetCore.RateLimiting;

[EnableRateLimiting(Constants.RateLimiting.Policies.Chat)]
[HttpPost("stream")]
public async Task StreamChat(...)
```

**File:** `src/Umbraco.AI.Web/Api/Management/Embedding/Controllers/GenerateEmbeddingController.cs`

```csharp
using Microsoft.AspNetCore.RateLimiting;

[EnableRateLimiting(Constants.RateLimiting.Policies.Embedding)]
[HttpPost("generate")]
public async Task<IActionResult> GenerateEmbeddings(...)
```

### Step 3.7: Configuration Example

**appsettings.json:**

```json
{
  "Umbraco": {
    "AI": {
      "DefaultChatProfileAlias": "default-chat",
      "RateLimiting": {
        "Enabled": true,
        "Chat": {
          "PermitLimit": 60,
          "WindowSeconds": 60,
          "QueueLimit": 2
        },
        "Embedding": {
          "PermitLimit": 100,
          "WindowSeconds": 60,
          "QueueLimit": 5
        }
      }
    }
  }
}
```

---

## Phase 4: Audit Logging (P2)

**Goal:** Create an audit trail for all AI API operations.

### Step 4.1: Create Audit Log Model

**File:** `src/Umbraco.AI.Core/Audit/AIAuditEntry.cs`

```csharp
namespace Umbraco.AI.Core.Audit;

/// <summary>
/// Represents an audit log entry for AI operations.
/// </summary>
public record AIAuditEntry
{
    public required string Operation { get; init; }
    public required string UserId { get; init; }
    public string? UserName { get; init; }
    public string? ProfileAlias { get; init; }
    public Guid? ProfileId { get; init; }
    public string? Endpoint { get; init; }
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
```

### Step 4.2: Create Audit Logger Interface

**File:** `src/Umbraco.AI.Core/Audit/IAIAuditLogger.cs`

```csharp
namespace Umbraco.AI.Core.Audit;

/// <summary>
/// Logs audit entries for AI operations.
/// </summary>
public interface IAIAuditLogger
{
    void Log(AIAuditEntry entry);
    Task LogAsync(AIAuditEntry entry, CancellationToken cancellationToken = default);
}
```

### Step 4.3: Create Structured Logging Implementation

**File:** `src/Umbraco.AI.Core/Audit/StructuredAIAuditLogger.cs`

```csharp
namespace Umbraco.AI.Core.Audit;

/// <summary>
/// Audit logger that writes to Microsoft.Extensions.Logging with structured data.
/// </summary>
public class StructuredAIAuditLogger : IAIAuditLogger
{
    private readonly ILogger<StructuredAIAuditLogger> _logger;

    public StructuredAIAuditLogger(ILogger<StructuredAIAuditLogger> logger)
    {
        _logger = logger;
    }

    public void Log(AIAuditEntry entry)
    {
        _logger.LogInformation(
            "AI Operation: {Operation} | User: {UserId} ({UserName}) | Profile: {ProfileAlias} | " +
            "Endpoint: {Endpoint} | Tokens: {InputTokens}/{OutputTokens} | " +
            "Success: {Success} | Duration: {Duration}ms | Error: {ErrorCode}",
            entry.Operation,
            entry.UserId,
            entry.UserName ?? "unknown",
            entry.ProfileAlias ?? "default",
            entry.Endpoint,
            entry.InputTokens,
            entry.OutputTokens,
            entry.Success,
            entry.Duration.TotalMilliseconds,
            entry.ErrorCode);
    }

    public Task LogAsync(AIAuditEntry entry, CancellationToken cancellationToken = default)
    {
        Log(entry);
        return Task.CompletedTask;
    }
}
```

### Step 4.4: Create Action Filter for Automatic Audit Logging

**File:** `src/Umbraco.AI.Web/Api/Management/Common/Filters/AIAuditLogFilter.cs`

```csharp
namespace Umbraco.AI.Web.Api.Management.Common.Filters;

/// <summary>
/// Action filter that automatically logs AI API operations.
/// </summary>
public class AIAuditLogFilter : IAsyncActionFilter
{
    private readonly IAIAuditLogger _auditLogger;

    public AIAuditLogFilter(IAIAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var user = context.HttpContext.User;

        // Determine operation type from controller/action
        var operation = GetOperationName(context);
        var profileInfo = ExtractProfileInfo(context);

        ActionExecutedContext result;
        string? errorCode = null;
        bool success = true;

        try
        {
            result = await next();

            if (result.Exception != null)
            {
                success = false;
                errorCode = result.Exception is AIException aiEx ? aiEx.ErrorCode : "UNHANDLED";
            }
            else if (result.Result is ObjectResult { StatusCode: >= 400 })
            {
                success = false;
                errorCode = "HTTP_ERROR";
            }
        }
        catch (Exception ex)
        {
            success = false;
            errorCode = ex is AIException aiEx ? aiEx.ErrorCode : "UNHANDLED";
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var entry = new AIAuditEntry
            {
                Operation = operation,
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
                UserName = user.Identity?.Name,
                ProfileAlias = profileInfo.Alias,
                ProfileId = profileInfo.Id,
                Endpoint = context.HttpContext.Request.Path,
                Success = success,
                ErrorCode = errorCode,
                Duration = stopwatch.Elapsed
            };

            await _auditLogger.LogAsync(entry);
        }
    }

    private static string GetOperationName(ActionExecutingContext context)
    {
        var controller = context.Controller.GetType().Name.Replace("Controller", "");
        var action = context.ActionDescriptor.DisplayName;
        return $"{controller}.{action}";
    }

    private static (string? Alias, Guid? Id) ExtractProfileInfo(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("requestModel", out var model))
        {
            var profileProp = model?.GetType().GetProperty("ProfileIdOrAlias");
            if (profileProp?.GetValue(model) is IdOrAlias idOrAlias)
            {
                return (idOrAlias.Alias, idOrAlias.Id);
            }
        }
        return (null, null);
    }
}
```

### Step 4.5: Apply Filter to AI Controllers

**File:** `src/Umbraco.AI.Web/Api/Management/Common/Controllers/UmbracoAICoreManagementControllerBase.cs`

```csharp
[ServiceFilter(typeof(AIAuditLogFilter))]
public abstract class UmbracoAICoreManagementControllerBase : UmbracoAIManagementControllerBase
{
    // ...
}
```

### Step 4.6: Register Audit Services

**File:** `src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs`

```csharp
// In AddUmbracoAICore()
builder.Services.AddSingleton<IAIAuditLogger, StructuredAIAuditLogger>();
```

**File:** `src/Umbraco.AI.Web/Configuration/UmbracoBuilderExtensions.cs`

```csharp
// In AddUmbracoAIWeb()
builder.Services.AddScoped<AIAuditLogFilter>();
```

---

## Implementation Order

### Sprint 1: Input Validation (P0)
1. Add `Validation` static class to `Constants.cs`
2. Create `MaxItemLengthAttribute.cs`
3. Update `ChatRequestModel.cs` and `ChatMessageModel.cs`
4. Update `GenerateEmbeddingRequestModel.cs`
5. Update Connection/Profile request models
6. Write validation tests

### Sprint 2: Error Sanitization (P1)
1. Create `AIException.cs` hierarchy
2. Create `AIProblemDetailsFactory.cs`
3. Update `CompleteChatController.cs`
4. Update `StreamChatController.cs`
5. Update `GenerateEmbeddingController.cs`
6. Update services to throw custom exceptions
7. Write error handling tests

### Sprint 3: Rate Limiting (P0)
1. Add `RateLimiting` static class to `Constants.cs`
2. Create `AIRateLimitOptions.cs`
3. Update `AIOptions.cs`
4. Create `UmbracoAIRateLimitingPipelineFilter.cs`
5. Add rate limiting configuration in `UmbracoBuilderExtensions.cs`
6. Apply `[EnableRateLimiting]` to controllers
7. Write rate limiting integration tests

### Sprint 4: Audit Logging (P2)
1. Create `AIAuditEntry.cs`
2. Create `IAIAuditLogger.cs`
3. Create `StructuredAIAuditLogger.cs`
4. Create `AIAuditLogFilter.cs`
5. Register services
6. Apply filter to base controller
7. Write audit logging tests

---

## Files to Create

| File | Phase |
|------|-------|
| `src/Umbraco.AI.Web/Api/Management/Common/Validation/MaxItemLengthAttribute.cs` | 1 |
| `src/Umbraco.AI.Core/Exceptions/AIException.cs` | 2 |
| `src/Umbraco.AI.Web/Api/Management/Common/Errors/AIProblemDetailsFactory.cs` | 2 |
| `src/Umbraco.AI.Core/Models/AIRateLimitOptions.cs` | 3 |
| `src/Umbraco.AI.Web/Configuration/UmbracoAIRateLimitingPipelineFilter.cs` | 3 |
| `src/Umbraco.AI.Core/Audit/AIAuditEntry.cs` | 4 |
| `src/Umbraco.AI.Core/Audit/IAIAuditLogger.cs` | 4 |
| `src/Umbraco.AI.Core/Audit/StructuredAIAuditLogger.cs` | 4 |
| `src/Umbraco.AI.Web/Api/Management/Common/Filters/AIAuditLogFilter.cs` | 4 |

## Files to Modify

| File | Phase |
|------|-------|
| `src/Umbraco.AI.Web/Constants.cs` | 1, 3 |
| `src/Umbraco.AI.Web/Api/Management/Chat/Models/ChatRequestModel.cs` | 1 |
| `src/Umbraco.AI.Web/Api/Management/Chat/Models/ChatMessageModel.cs` | 1 |
| `src/Umbraco.AI.Web/Api/Management/Embedding/Models/GenerateEmbeddingRequestModel.cs` | 1 |
| `src/Umbraco.AI.Web/Api/Management/Connection/Models/CreateConnectionRequestModel.cs` | 1 |
| `src/Umbraco.AI.Web/Api/Management/Profile/Models/CreateProfileRequestModel.cs` | 1 |
| `src/Umbraco.AI.Web/Api/Management/Chat/Controllers/CompleteChatController.cs` | 2, 3 |
| `src/Umbraco.AI.Web/Api/Management/Chat/Controllers/StreamChatController.cs` | 2, 3 |
| `src/Umbraco.AI.Web/Api/Management/Embedding/Controllers/GenerateEmbeddingController.cs` | 2, 3 |
| `src/Umbraco.AI.Core/Models/AIOptions.cs` | 3 |
| `src/Umbraco.AI.Web/Configuration/UmbracoBuilderExtensions.cs` | 3, 4 |
| `src/Umbraco.AI.Core/Configuration/UmbracoBuilderExtensions.cs` | 4 |

---

## Configuration Documentation

Add to `docs/public/configuration.md`:

```markdown
## Rate Limiting

Umbraco.AI includes built-in rate limiting to prevent abuse of AI endpoints.

### Default Limits

| Endpoint | Requests | Window | Queue |
|----------|----------|--------|-------|
| Chat | 60 | 1 minute | 2 |
| Embedding | 100 | 1 minute | 5 |

### Configuration

```json
{
  "Umbraco": {
    "AI": {
      "RateLimiting": {
        "Enabled": true,
        "Chat": {
          "PermitLimit": 60,
          "WindowSeconds": 60,
          "QueueLimit": 2
        },
        "Embedding": {
          "PermitLimit": 100,
          "WindowSeconds": 60,
          "QueueLimit": 5
        }
      }
    }
  }
}
```

### Disabling Rate Limiting

Set `Enabled` to `false` to disable rate limiting (not recommended for production):

```json
{
  "Umbraco": {
    "AI": {
      "RateLimiting": {
        "Enabled": false
      }
    }
  }
}
```
```

---

## Testing Strategy

### Unit Tests

- Validation attribute tests (valid/invalid inputs)
- AIProblemDetailsFactory tests (exception mapping)
- Custom exception tests

### Integration Tests

- Rate limiting behavior (requests allowed/rejected)
- End-to-end validation (oversized payloads rejected)
- Audit log filter execution

### Manual Testing

- Verify 429 responses with correct headers
- Verify generic error messages (no stack traces)
- Verify audit logs appear in logging output
