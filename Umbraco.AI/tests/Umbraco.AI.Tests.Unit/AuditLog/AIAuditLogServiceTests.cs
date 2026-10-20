using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Core.TaskQueue;

namespace Umbraco.AI.Tests.Unit.AuditLog;

public class AIAuditLogServiceTests
{
    private readonly Mock<IAIAuditLogRepository> _repositoryMock;
    private readonly AIAuditLogService _service;

    public AIAuditLogServiceTests()
    {
        _repositoryMock = new Mock<IAIAuditLogRepository>();
        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIAuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIAuditLog audit, CancellationToken _) => audit);

        var optionsMock = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        optionsMock.Setup(x => x.CurrentValue).Returns(new AIAuditLogOptions());

        _service = new AIAuditLogService(
            _repositoryMock.Object,
            optionsMock.Object,
            new Mock<IBackgroundTaskQueue>().Object,
            NullLoggerFactory.Instance);
    }

    [Theory]
    [InlineData(AIProviderErrorCategory.Authentication, AIAuditLogErrorCategory.Authentication)]
    [InlineData(AIProviderErrorCategory.RateLimited, AIAuditLogErrorCategory.RateLimiting)]
    [InlineData(AIProviderErrorCategory.NotFound, AIAuditLogErrorCategory.ModelNotFound)]
    [InlineData(AIProviderErrorCategory.InvalidRequest, AIAuditLogErrorCategory.InvalidRequest)]
    [InlineData(AIProviderErrorCategory.Transient, AIAuditLogErrorCategory.ServerError)]
    [InlineData(AIProviderErrorCategory.NetworkError, AIAuditLogErrorCategory.NetworkError)]
    [InlineData(AIProviderErrorCategory.Cancelled, AIAuditLogErrorCategory.Unknown)]
    [InlineData(AIProviderErrorCategory.Unknown, AIAuditLogErrorCategory.Unknown)]
    public async Task QueueRecordAuditLogFailureAsync_WithClassifiedProviderException_ReadsCategoryDirectly(
        AIProviderErrorCategory providerCategory, AIAuditLogErrorCategory expected)
    {
        // A friendly message that intentionally contains none of the substrings the legacy
        // string-matching fallback looks for, proving the category comes from the exception's
        // own Category rather than being re-derived from its message text.
        var exception = new AIProviderException(new AIProviderErrorInfo(
            providerCategory, "Something specific happened.", ProviderCode: null, RawMessage: "raw"));
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        await _service.QueueRecordAuditLogFailureAsync(audit, prompt: null, exception);

        audit.ErrorCategory.ShouldBe(expected);
    }

    [Fact]
    public async Task QueueRecordAuditLogFailureAsync_WithUnclassifiedException_FallsBackToMessageMatching()
    {
        var exception = new InvalidOperationException("Request failed with rate limit exceeded");
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        await _service.QueueRecordAuditLogFailureAsync(audit, prompt: null, exception);

        audit.ErrorCategory.ShouldBe(AIAuditLogErrorCategory.RateLimiting);
    }

    [Fact]
    public async Task CleanupOldAuditLogsAsync_FailsRunningAuditLogsOlderThanTimeout()
    {
        var service = CreateService(new AIAuditLogOptions { StaleRunningTimeoutMinutes = 60 });
        DateTime? threshold = null;
        _repositoryMock
            .Setup(x => x.FailRunningOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((DateTime t, string _, CancellationToken _) => threshold = t)
            .ReturnsAsync(1);

        await service.CleanupOldAuditLogsAsync();

        threshold.ShouldNotBeNull();
        threshold.Value.ShouldBe(DateTime.UtcNow.AddMinutes(-60), TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CleanupOldAuditLogsAsync_WithTimeoutDisabled_DoesNotFailRunningAuditLogs(int timeoutMinutes)
    {
        var service = CreateService(new AIAuditLogOptions { StaleRunningTimeoutMinutes = timeoutMinutes });

        await service.CleanupOldAuditLogsAsync();

        _repositoryMock.Verify(
            x => x.FailRunningOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task QueueStartAuditLogAsync_InsideItsOwnScope_LeavesParentEmpty()
    {
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        using (AIAuditScope.Begin(audit.Id))
        {
            await _service.QueueStartAuditLogAsync(audit);
        }

        audit.ParentAuditLogId.ShouldBeNull();
    }

    [Fact]
    public async Task QueueStartAuditLogAsync_InsideAnOuterScope_UsesItAsParent()
    {
        var parentId = Guid.NewGuid();
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        using (AIAuditScope.Begin(parentId))
        {
            await _service.QueueStartAuditLogAsync(audit);
        }

        audit.ParentAuditLogId.ShouldBe(parentId);
    }

    [Fact]
    public async Task QueueRecordAuditLogFailureAsync_WithEmailAtResultLimit_RedactsBeforeTruncating()
    {
        // The 1,000-character result limit falls inside the email's local part. Truncating first
        // would store "jane.longername.fir", which no email pattern matches once the @ is cut off.
        var service = CreateService(new AIAuditLogOptions
        {
            RedactionPatterns = [@"[\w.+-]+@[\w-]+\.[\w.]+"],
            MaxToolResultLength = 1000,
        });
        var result = new string('y', 980) + " jane.longername.firstpart@example.com";
        var prompt = new AIAuditPrompt
        {
            Capability = AICapability.Chat,
            Data = new List<ChatMessage>
            {
                new(ChatRole.Tool, [new FunctionResultContent("tc_001", result)]),
            },
        };
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        await service.QueueRecordAuditLogFailureAsync(audit, prompt, new InvalidOperationException("boom"));

        audit.PromptSnapshot.ShouldNotBeNull();
        audit.PromptSnapshot.ShouldNotContain("jane.");
        audit.PromptSnapshot.ShouldContain("[REDACTED]");
    }

    [Fact]
    public async Task QueueRecordAuditLogFailureAsync_WithEscapableCharactersInArguments_RedactsThem()
    {
        // The default JSON encoder writes + as \u002B, which would hide this address from the pattern.
        var service = CreateService(new AIAuditLogOptions
        {
            RedactionPatterns = [@"[\w.+-]+@[\w-]+\.[\w.]+"],
        });
        var prompt = new AIAuditPrompt
        {
            Capability = AICapability.Chat,
            Data = new List<ChatMessage>
            {
                new(ChatRole.Assistant,
                [
                    new FunctionCallContent("tc_001", "send", new Dictionary<string, object?> { ["to"] = "jane+news@example.com" }),
                ]),
            },
        };
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        await service.QueueRecordAuditLogFailureAsync(audit, prompt, new InvalidOperationException("boom"));

        audit.PromptSnapshot.ShouldNotBeNull();
        audit.PromptSnapshot.ShouldNotContain("example.com");
        audit.PromptSnapshot.ShouldContain("send({\"to\":\"[REDACTED]\"})");
    }

    [Fact]
    public async Task QueueRecordAuditLogFailureAsync_WithDefaultResultLimit_KeepsCommerceSizedResults()
    {
        var service = CreateService(new AIAuditLogOptions());
        var result = new string('y', 2000);
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        await service.QueueRecordAuditLogFailureAsync(audit, CreateToolResultPrompt(result), new InvalidOperationException("boom"));

        audit.PromptSnapshot.ShouldNotBeNull();
        audit.PromptSnapshot.ShouldContain(result);
        audit.PromptSnapshot.ShouldNotContain("(truncated,");
    }

    [Fact]
    public async Task QueueRecordAuditLogFailureAsync_WithLoweredArgumentsLimit_TruncatesAtIt()
    {
        var service = CreateService(new AIAuditLogOptions { MaxToolArgumentsLength = 20 });
        var prompt = new AIAuditPrompt
        {
            Capability = AICapability.Chat,
            Data = new List<ChatMessage>
            {
                new(ChatRole.Assistant,
                [
                    new FunctionCallContent("tc_001", "search", new Dictionary<string, object?> { ["query"] = new string('x', 100) }),
                ]),
            },
        };
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        await service.QueueRecordAuditLogFailureAsync(audit, prompt, new InvalidOperationException("boom"));

        audit.PromptSnapshot.ShouldNotBeNull();
        audit.PromptSnapshot.ShouldContain("search({\"query\":\"xxxxxxxxxx... (truncated, 112 chars)");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task QueueRecordAuditLogFailureAsync_WithResultLimitDisabled_KeepsFullResult(int maxLength)
    {
        var service = CreateService(new AIAuditLogOptions { MaxToolResultLength = maxLength });
        var result = new string('y', 20000);
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        await service.QueueRecordAuditLogFailureAsync(audit, CreateToolResultPrompt(result), new InvalidOperationException("boom"));

        audit.PromptSnapshot.ShouldNotBeNull();
        audit.PromptSnapshot.ShouldContain(result);
        audit.PromptSnapshot.ShouldNotContain("(truncated,");
    }

    private static AIAuditPrompt CreateToolResultPrompt(string result) => new()
    {
        Capability = AICapability.Chat,
        Data = new List<ChatMessage>
        {
            new(ChatRole.Tool, [new FunctionResultContent("tc_001", result)]),
        },
    };

    private AIAuditLogService CreateService(AIAuditLogOptions options)
    {
        var optionsMock = new Mock<IOptionsMonitor<AIAuditLogOptions>>();
        optionsMock.Setup(x => x.CurrentValue).Returns(options);

        return new AIAuditLogService(
            _repositoryMock.Object,
            optionsMock.Object,
            new Mock<IBackgroundTaskQueue>().Object,
            NullLoggerFactory.Instance);
    }
}
