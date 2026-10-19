using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core.Events;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

public class AIPromptServiceLoggingTests
{
    private readonly InMemoryAIPromptRepository _repository = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<ILogger<AIPromptService>> _logger = new();

    private AIPromptService CreateService() => new(
        _repository,
        Mock.Of<IAIEntityVersionService>(),
        Mock.Of<IAIChatService>(),
        Mock.Of<IAIPromptTemplateService>(),
        _scopeFactory.Object,
        new AIToolCollection(() => []),
        Mock.Of<IAIFunctionFactory>(),
        Mock.Of<IAIRuntimeContextScopeProvider>(),
        new AIRuntimeContextContributorCollection(() => []),
        Mock.Of<IEventAggregator>(),
        Mock.Of<IAIPromptPropertyValueSchemaResolver>(),
        new AISimplifiedPropertyValueTransformerCollection(() => []),
        _logger.Object);

    private static readonly AIPromptExecutionRequest Request = new()
    {
        EntityId = Guid.NewGuid(),
        EntityType = "document",
        PropertyAlias = "title",
        ContentTypeAlias = "homePage",
    };

    private async Task<Guid> SavePromptAsync()
    {
        var prompt = await _repository.SaveAsync(
            new AIPrompt { Alias = "summarize", Name = "Summarize", Instructions = "Summarize" });
        return prompt.Id;
    }

    private void VerifyLoggedOnce(LogLevel level, Times? errorTimes = null)
        => _logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            errorTimes ?? Times.Once());

    [Fact]
    public async Task ExecutePromptAsync_WhenThePromptIsRejected_LogsAWarningWithTheException()
    {
        var promptId = Guid.NewGuid();
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateService().ExecutePromptAsync(promptId, Request));

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(promptId.ToString())),
                thrown,
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
        VerifyLoggedOnce(LogLevel.Error, Times.Never());
    }

    [Fact]
    public async Task ExecutePromptAsync_WhenSomethingUnexpectedFails_LogsAnErrorWithTheException()
    {
        var promptId = await SavePromptAsync();
        var failure = new NullReferenceException("boom");
        _scopeFactory.Setup(x => x.CreateScope()).Throws(failure);

        var thrown = await Should.ThrowAsync<NullReferenceException>(
            () => CreateService().ExecutePromptAsync(promptId, Request));

        thrown.ShouldBeSameAs(failure);
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                failure,
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
        VerifyLoggedOnce(LogLevel.Warning, Times.Never());
    }

    [Fact]
    public async Task ExecutePromptAsync_WhenCancelled_LogsNothing()
    {
        var promptId = await SavePromptAsync();
        _scopeFactory.Setup(x => x.CreateScope()).Throws(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(
            () => CreateService().ExecutePromptAsync(promptId, Request));

        _logger.Invocations.Count(i => i.Method.Name == nameof(ILogger.Log)).ShouldBe(0);
    }
}
