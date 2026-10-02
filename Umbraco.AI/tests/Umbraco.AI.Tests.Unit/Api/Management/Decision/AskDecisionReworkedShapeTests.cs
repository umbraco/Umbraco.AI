// DR-4 — Ask decisions over the Management API (AC1, AC3, AC11b): the reworked request/response shapes.
#pragma warning disable UMBRACOAI_DECISION // Tests the experimental Decision controller

// The controller calls IAIDecisionService.GetDecisionResponseAsync(Action<AIDecisionBuilder>,
// AIDecisionRequest, CancellationToken) with one question and flattens the single answer.
// AskDecisionRequestModel has a top-level `State`; question models have no `Context`; score levels
// are DecisionScoreLevelModel { Description }. BinaryDecisionResponseModel has TrueProbability (no
// Answer/Confidence); ScoreDecisionResponseModel has Score, Confidence?, Probabilities keyed by level
// index (no Level). Replaces the binary/score scenarios in AskDecisionControllerTests that asserted
// the old fields.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Decision.Controllers;
using Umbraco.AI.Web.Api.Management.Decision.Models;

namespace Umbraco.AI.Tests.Unit.Api.Management.Decision;

// Controller is the real entry point, constructed directly as AskDecisionControllerTests does; the
// Decision service is the mocked collaborator.
public class AskDecisionReworkedShapeTests
{
    private static AskDecisionController CreateController(Mock<IAIDecisionService> decisionService)
    {
        var experimental = new Mock<IAIExperimentalFeatures>();
        experimental.Setup(x => x.IsCapabilityEnabled(AICapability.Decision)).Returns(true);
        return new AskDecisionController(decisionService.Object, new Mock<IAIProfileService>().Object, experimental.Object);
    }

    /// <summary>Answers the only question in whatever request the controller sends.</summary>
    private static Mock<IAIDecisionService> ServiceAnswering(AIDecisionAnswer answer, Action<AIDecisionRequest>? capture = null)
    {
        var service = new Mock<IAIDecisionService>();
        service
            .Setup(x => x.GetDecisionResponseAsync(
                It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIDecisionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIDecisionBuilder>, AIDecisionRequest, CancellationToken>((_, r, _) => capture?.Invoke(r))
            .ReturnsAsync((Action<AIDecisionBuilder> _, AIDecisionRequest r, CancellationToken _) => new AIDecisionResponse
            {
                Answers = new Dictionary<string, AIDecisionAnswer> { [r.Questions[0].Id!] = answer },
                ModelId = "jev-1.13.0",
            });
        return service;
    }

    private static AskDecisionRequestModel Binary() => new()
    {
        State = "Buy cheap watches",
        Question = new BinaryDecisionQuestionModel { Instructions = "Is this spam?" },
    };

    private static AskDecisionRequestModel Score() => new()
    {
        Question = new ScoreDecisionQuestionModel
        {
            Instructions = "How good?",
            Levels = [new DecisionScoreLevelModel { Description = "poor" }, new DecisionScoreLevelModel { Description = "ok" }, new DecisionScoreLevelModel { Description = "good" }],
        },
    };

    #region Happy path

    public class GivenABinaryQuestionWithState
    {
        private readonly IActionResult _result;
        private AIDecisionRequest? _sent;

        public GivenABinaryQuestionWithState()
        {
            var service = ServiceAnswering(new AIBinaryDecisionAnswer { TrueProbability = 0.97 }, r => _sent = r);
            _result = CreateController(service).Ask(Binary()).GetAwaiter().GetResult();
        }

        [Fact]
        public void SendsTheStateOnTheRequest() => _sent!.State.ShouldBe("Buy cheap watches");

        [Fact]
        public void MapsTheTrueProbability()
            => ((BinaryDecisionResponseModel)((OkObjectResult)_result).Value!).TrueProbability.ShouldBe(0.97);

        [Fact]
        public void MapsTheModelId()
            => ((BinaryDecisionResponseModel)((OkObjectResult)_result).Value!).ModelId.ShouldBe("jev-1.13.0");

        [Fact]
        public void SerializesWithoutAConfidence()
            => JsonSerializer.Serialize<DecisionResponseModel>((DecisionResponseModel)((OkObjectResult)_result).Value!)
                .ShouldNotContain("\"confidence\"");
    }

    public class GivenAScoreQuestion
    {
        private readonly IActionResult _result;

        public GivenAScoreQuestion()
        {
            var service = ServiceAnswering(new AIScoreDecisionAnswer
            {
                Score = 1.8,
                Confidence = 0.8,
                Probabilities = new Dictionary<int, double> { [0] = 0.05, [1] = 0.15, [2] = 0.8 },
            });
            _result = CreateController(service).Ask(Score()).GetAwaiter().GetResult();
        }

        [Fact]
        public void MapsTheScore()
            => ((ScoreDecisionResponseModel)((OkObjectResult)_result).Value!).Score.ShouldBe(1.8);

        [Fact]
        public void KeysProbabilitiesByLevelIndex()
            => JsonSerializer.Serialize<DecisionResponseModel>((DecisionResponseModel)((OkObjectResult)_result).Value!)
                .ShouldContain("\"probabilities\":{\"0\":0.05,\"1\":0.15,\"2\":0.8}");

        [Fact]
        public void SerializesWithoutALevel()
            => JsonSerializer.Serialize<DecisionResponseModel>((DecisionResponseModel)((OkObjectResult)_result).Value!)
                .ShouldNotContain("\"level\"");
    }

    #endregion

    #region Sad path

    public class GivenAnInconsistentProviderAnswer
    {
        [Fact]
        public async Task ReturnsTheProviderErrorProblemDetails()
        {
            var service = new Mock<IAIDecisionService>();
            service
                .Setup(x => x.GetDecisionResponseAsync(
                    It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIDecisionRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AIProviderException(new AIProviderErrorInfo(
                    AIProviderErrorCategory.Unknown, "The provider's answer didn't cover every option.", null, "incomplete distribution")));

            var result = await CreateController(service).Ask(Binary());

            ((ProblemDetails)((ObjectResult)result).Value!).Title.ShouldBe("Decision request failed");
        }
    }

    #endregion
}
