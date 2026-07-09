using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.AuditLog;
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
    public async Task RecordAuditLogFailureAsync_WithClassifiedProviderException_ReadsCategoryDirectly(
        AIProviderErrorCategory providerCategory, AIAuditLogErrorCategory expected)
    {
        // A friendly message that intentionally contains none of the substrings the legacy
        // string-matching fallback looks for, proving the category comes from the exception's
        // own Category rather than being re-derived from its message text.
        var exception = new AIProviderException(new AIProviderErrorInfo(
            providerCategory, "Something specific happened.", ProviderCode: null, RawMessage: "raw"));
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        await _service.RecordAuditLogFailureAsync(audit, prompt: null, exception);

        audit.ErrorCategory.ShouldBe(expected);
    }

    [Fact]
    public async Task RecordAuditLogFailureAsync_WithUnclassifiedException_FallsBackToMessageMatching()
    {
        var exception = new InvalidOperationException("Request failed with rate limit exceeded");
        var audit = new AIAuditLog { Id = Guid.NewGuid() };

        await _service.RecordAuditLogFailureAsync(audit, prompt: null, exception);

        audit.ErrorCategory.ShouldBe(AIAuditLogErrorCategory.RateLimiting);
    }
}
