using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core.Events;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

public class AIPromptServiceIdTests
{
    private readonly InMemoryAIPromptRepository _repository = new();

    private AIPromptService CreateService() => new(
        _repository,
        Mock.Of<IAIEntityVersionService>(),
        Mock.Of<IAIChatService>(),
        Mock.Of<IAIPromptTemplateService>(),
        Mock.Of<IServiceScopeFactory>(),
        new AIToolCollection(() => []),
        Mock.Of<IAIFunctionFactory>(),
        Mock.Of<IAIRuntimeContextScopeProvider>(),
        new AIRuntimeContextContributorCollection(() => []),
        Mock.Of<IEventAggregator>(),
        Mock.Of<IAIPromptPropertyValueSchemaResolver>(),
        new AISimplifiedPropertyValueTransformerCollection(() => []),
        Mock.Of<ILogger<AIPromptService>>());

    [Fact]
    public async Task SavePromptAsync_WithIdConstructor_KeepsTheId()
    {
        var knownId = Guid.NewGuid();
        var prompt = new AIPrompt(knownId) { Alias = "known-id", Name = "Known Id", Instructions = "Do it" };

        var result = await CreateService().SavePromptAsync(prompt);

        result.Id.ShouldBe(knownId);
        (await _repository.GetByIdAsync(knownId)).ShouldNotBeNull();
    }

    [Fact]
    public async Task SavePromptAsync_WithParameterlessConstructor_GeneratesAnId()
    {
        var prompt = new AIPrompt { Alias = "no-id", Name = "No Id", Instructions = "Do it" };

        var result = await CreateService().SavePromptAsync(prompt);

        result.Id.ShouldNotBe(Guid.Empty);
    }
}
