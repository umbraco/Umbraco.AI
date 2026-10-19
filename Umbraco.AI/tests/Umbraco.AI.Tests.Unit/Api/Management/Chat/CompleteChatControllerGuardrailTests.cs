using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Core.InlineChat;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Chat.Controllers;
using Umbraco.AI.Web.Api.Management.Chat.Models;
using Umbraco.Cms.Core.Mapping;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Api.Management.Chat;

/// <summary>
/// Verifies that a guardrail-blocked chat completion is surfaced as a structured 422
/// response rather than propagating <see cref="AIGuardrailBlockedException"/> as an
/// unhandled HTTP 500.
/// </summary>
public class CompleteChatControllerGuardrailTests
{
    private readonly Mock<IAIChatService> _chatServiceMock = new();
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly Mock<IUmbracoMapper> _mapperMock = new();
    private readonly CompleteChatController _controller;

    public CompleteChatControllerGuardrailTests()
    {
        _controller = new CompleteChatController(
            _chatServiceMock.Object,
            _profileServiceMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task CompleteChat_WhenGuardrailBlocks_Returns422WithProblemDetails()
    {
#pragma warning disable CS0618 // Content is obsolete — convenient for the test request
        var requestModel = new ChatRequestModel
        {
            Messages = [new ChatMessageModel { Role = "user", Content = "hello" }]
        };
#pragma warning restore CS0618

        _mapperMock
            .Setup(x => x.MapEnumerable<ChatMessageModel, ChatMessage>(It.IsAny<IEnumerable<ChatMessageModel>>()))
            .Returns([new ChatMessage(ChatRole.User, "hello")]);

        var evaluationResult = new AIGuardrailEvaluationResult
        {
            Action = AIGuardrailAction.Block,
            Phase = AIGuardrailPhase.PostGenerate,
            RuleResults =
            [
                new AIGuardrailRuleResult
                {
                    Rule = new AIGuardrailRule
                    {
                        EvaluatorId = "contains",
                        Name = "Block secret",
                        GuardrailName = "Test policy"
                    },
                    EvaluatorResult = new AIGuardrailResult
                    {
                        EvaluatorId = "contains",
                        Flagged = true,
                        Reason = "matched forbidden term"
                    }
                }
            ]
        };

        _chatServiceMock
            .Setup(x => x.GetChatResponseAsync(
                It.IsAny<Action<AIChatBuilder>>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIGuardrailBlockedException(evaluationResult));

        var result = await _controller.CompleteChat(null, requestModel);

        var unprocessable = result.ShouldBeOfType<UnprocessableEntityObjectResult>();
        unprocessable.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);

        var problemDetails = unprocessable.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Chat blocked by guardrail policy");
        problemDetails.Status.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        problemDetails.Extensions["phase"].ShouldBe("response");

        var flaggedRules = problemDetails.Extensions["flaggedRules"].ShouldBeAssignableTo<IEnumerable<string>>();
        flaggedRules!.ShouldContain("Test policy > Block secret");
    }

    private static ChatRequestModel HelloRequest()
    {
#pragma warning disable CS0618 // Content is obsolete — convenient for the test request
        return new ChatRequestModel
        {
            Messages = [new ChatMessageModel { Role = "user", Content = "hello" }]
        };
#pragma warning restore CS0618
    }

    [Fact]
    public async Task CompleteChat_WithNonExistingProfileId_Returns404AndNeverCallsChatService()
    {
        // An ID is returned without a lookup, so the controller must check existence itself
        var profileId = Guid.NewGuid();
        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        var result = await _controller.CompleteChat(new IdOrAlias(profileId), HelloRequest());

        var notFound = result.ShouldBeOfType<NotFoundObjectResult>();
        notFound.Value.ShouldBeOfType<ProblemDetails>().Title.ShouldBe("Profile not found");
        _chatServiceMock.Verify(
            x => x.GetChatResponseAsync(
                It.IsAny<Action<AIChatBuilder>>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteChat_WhenServiceReportsSomethingNotFound_Returns400WithTheReason()
    {
        _mapperMock
            .Setup(x => x.MapEnumerable<ChatMessageModel, ChatMessage>(It.IsAny<IEnumerable<ChatMessageModel>>()))
            .Returns([new ChatMessage(ChatRole.User, "hello")]);
        _chatServiceMock
            .Setup(x => x.GetChatResponseAsync(
                It.IsAny<Action<AIChatBuilder>>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Default Chat profile with alias 'x' not found."));

        var result = await _controller.CompleteChat(null, HelloRequest());

        var problemDetails = result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Type.ShouldBe("Error");
        problemDetails.Detail.ShouldBe("Default Chat profile with alias 'x' not found.");
    }
}
