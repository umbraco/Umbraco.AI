using Microsoft.Extensions.AI;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Core.Guardrails.Middleware;
using Umbraco.AI.Core.Guardrails.Resolvers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Middleware;

public class AIGuardrailChatClientTests
{
    private readonly Mock<IAIRuntimeContextAccessor> _runtimeContextAccessorMock;
    private readonly Mock<IAIGuardrailResolutionService> _resolutionServiceMock;

    public AIGuardrailChatClientTests()
    {
        _runtimeContextAccessorMock = new Mock<IAIRuntimeContextAccessor>();
        _resolutionServiceMock = new Mock<IAIGuardrailResolutionService>();

        // Default: no guardrail evaluation context
        _runtimeContextAccessorMock.Setup(x => x.Context).Returns((AIRuntimeContext?)null);
    }

    #region Pre-Generate Redaction

    [Fact]
    public async Task GetResponseAsync_PreGenerateRedactRule_RedactsUserMessage()
    {
        // Arrange
        var rule = new AIGuardrailRuleBuilder()
            .WithEvaluatorId("regex")
            .WithName("SSN Redactor")
            .AsPreGenerate()
            .AsRedact()
            .WithConfig(new { pattern = @"\d{3}-\d{2}-\d{4}", ignoreCase = false })
            .Build();

        var evaluator = new RegexGuardrailEvaluator(new Mock<IAIEditableModelSchemaBuilder>().Object);

        var resolved = new AIResolvedGuardrails
        {
            AllRules = [rule],
            PreGenerateRules = [rule],
            PostGenerateRules = []
        };

        _resolutionServiceMock
            .Setup(x => x.ResolveGuardrailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);

        var fakeClient = new FakeChatClient("AI response");
        var evaluatorCollection = CreateEvaluatorCollection(evaluator);

        var client = new AIGuardrailChatClient(
            fakeClient,
            _runtimeContextAccessorMock.Object,
            _resolutionServiceMock.Object,
            evaluatorCollection);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "My SSN is 123-45-6789 and also 987-65-4321")
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert — the message sent to the inner client should have redacted SSNs
        var sentMessages = fakeClient.ReceivedMessages[0].ToList();
        var userMsg = sentMessages.Last(m => m.Role == ChatRole.User);
        userMsg.Text.ShouldBe("My SSN is [REDACTED] and also [REDACTED]");
    }

    [Fact]
    public async Task GetResponseAsync_PostGenerateRedactRule_RedactsResponseContent()
    {
        // Arrange
        var rule = new AIGuardrailRuleBuilder()
            .WithEvaluatorId("contains")
            .WithName("Secret Redactor")
            .AsPostGenerate()
            .AsRedact()
            .WithConfig(new { searchPattern = "secret-value", ignoreCase = true })
            .Build();

        var evaluator = new ContainsGuardrailEvaluator(new Mock<IAIEditableModelSchemaBuilder>().Object);

        var resolved = new AIResolvedGuardrails
        {
            AllRules = [rule],
            PreGenerateRules = [],
            PostGenerateRules = [rule]
        };

        _resolutionServiceMock
            .Setup(x => x.ResolveGuardrailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);

        var fakeClient = new FakeChatClient("The password is secret-value, keep it safe");
        var evaluatorCollection = CreateEvaluatorCollection(evaluator);

        var client = new AIGuardrailChatClient(
            fakeClient,
            _runtimeContextAccessorMock.Object,
            _resolutionServiceMock.Object,
            evaluatorCollection);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "What is the password?")
        };

        // Act
        var response = await client.GetResponseAsync(messages);

        // Assert
        response.Text.ShouldBe("The password is [REDACTED], keep it safe");
    }

    [Fact]
    public async Task GetResponseAsync_BlockAndRedactRules_BlockTakesPrecedence()
    {
        // Arrange
        var blockRule = new AIGuardrailRuleBuilder()
            .WithEvaluatorId("contains")
            .WithName("Blocker")
            .AsPreGenerate()
            .AsBlock()
            .WithConfig(new { searchPattern = "forbidden", ignoreCase = true })
            .Build();

        var redactRule = new AIGuardrailRuleBuilder()
            .WithEvaluatorId("regex")
            .WithName("Redactor")
            .AsPreGenerate()
            .AsRedact()
            .WithConfig(new { pattern = @"\d+", ignoreCase = false })
            .Build();

        var containsEvaluator = new ContainsGuardrailEvaluator(new Mock<IAIEditableModelSchemaBuilder>().Object);
        var regexEvaluator = new RegexGuardrailEvaluator(new Mock<IAIEditableModelSchemaBuilder>().Object);

        var resolved = new AIResolvedGuardrails
        {
            AllRules = [blockRule, redactRule],
            PreGenerateRules = [blockRule, redactRule],
            PostGenerateRules = []
        };

        _resolutionServiceMock
            .Setup(x => x.ResolveGuardrailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);

        var fakeClient = new FakeChatClient("response");
        var evaluatorCollection = CreateEvaluatorCollection(containsEvaluator, regexEvaluator);

        var client = new AIGuardrailChatClient(
            fakeClient,
            _runtimeContextAccessorMock.Object,
            _resolutionServiceMock.Object,
            evaluatorCollection);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "This is forbidden content with 12345")
        };

        // Act & Assert — Block should win over Redact
        await Should.ThrowAsync<AIGuardrailBlockedException>(
            () => client.GetResponseAsync(messages));
    }

    [Fact]
    public async Task GetResponseAsync_RedactWithOverlappingMatches_MergesCorrectly()
    {
        // Arrange — two regex rules with overlapping matches
        var rule1 = new AIGuardrailRuleBuilder()
            .WithEvaluatorId("regex")
            .WithName("Rule 1")
            .AsPreGenerate()
            .AsRedact()
            .WithConfig(new { pattern = @"sensitive data", ignoreCase = true })
            .Build();

        var rule2 = new AIGuardrailRuleBuilder()
            .WithEvaluatorId("contains")
            .WithName("Rule 2")
            .AsPreGenerate()
            .AsRedact()
            .WithConfig(new { searchPattern = "data here", ignoreCase = true })
            .Build();

        var regexEvaluator = new RegexGuardrailEvaluator(new Mock<IAIEditableModelSchemaBuilder>().Object);
        var containsEvaluator = new ContainsGuardrailEvaluator(new Mock<IAIEditableModelSchemaBuilder>().Object);

        var resolved = new AIResolvedGuardrails
        {
            AllRules = [rule1, rule2],
            PreGenerateRules = [rule1, rule2],
            PostGenerateRules = []
        };

        _resolutionServiceMock
            .Setup(x => x.ResolveGuardrailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);

        var fakeClient = new FakeChatClient("response");
        var evaluatorCollection = CreateEvaluatorCollection(regexEvaluator, containsEvaluator);

        var client = new AIGuardrailChatClient(
            fakeClient,
            _runtimeContextAccessorMock.Object,
            _resolutionServiceMock.Object,
            evaluatorCollection);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "This has sensitive data here and more")
        };

        // Act
        await client.GetResponseAsync(messages);

        // Assert — overlapping "sensitive data" and "data here" should merge into one redaction
        var sentMessages = fakeClient.ReceivedMessages[0].ToList();
        var userMsg = sentMessages.Last(m => m.Role == ChatRole.User);
        userMsg.Text.ShouldBe("This has [REDACTED] and more");
    }

    [Fact]
    public async Task GetStreamingResponseAsync_PreGenerateRedactRule_RedactsUserMessage()
    {
        // Arrange
        var rule = new AIGuardrailRuleBuilder()
            .WithEvaluatorId("contains")
            .WithName("PII Redactor")
            .AsPreGenerate()
            .AsRedact()
            .WithConfig(new { searchPattern = "secret", ignoreCase = false })
            .Build();

        var evaluator = new ContainsGuardrailEvaluator(new Mock<IAIEditableModelSchemaBuilder>().Object);

        var resolved = new AIResolvedGuardrails
        {
            AllRules = [rule],
            PreGenerateRules = [rule],
            PostGenerateRules = []
        };

        _resolutionServiceMock
            .Setup(x => x.ResolveGuardrailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);

        var fakeClient = new FakeChatClient("streaming response");
        var evaluatorCollection = CreateEvaluatorCollection(evaluator);

        var client = new AIGuardrailChatClient(
            fakeClient,
            _runtimeContextAccessorMock.Object,
            _resolutionServiceMock.Object,
            evaluatorCollection);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "My secret password is secret")
        };

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(messages))
        {
            updates.Add(update);
        }

        // Assert — user message should be redacted before streaming starts
        var sentMessages = fakeClient.ReceivedMessages[0].ToList();
        var userMsg = sentMessages.Last(m => m.Role == ChatRole.User);
        userMsg.Text.ShouldBe("My [REDACTED] password is [REDACTED]");
        updates.ShouldNotBeEmpty();
    }

    #endregion

    #region Helpers

    private static AIGuardrailEvaluatorCollection CreateEvaluatorCollection(params IAIGuardrailEvaluator[] evaluators)
    {
        return new AIGuardrailEvaluatorCollection(() => evaluators);
    }

    #endregion
}
