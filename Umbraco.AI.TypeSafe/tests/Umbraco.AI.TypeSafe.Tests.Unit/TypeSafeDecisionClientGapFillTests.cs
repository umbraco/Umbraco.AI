// DR-2 — Connect to TypeSafe AI (AC9, AC9b): complete, position-keyed distributions from Jev's answers.
#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

// Supersedes the label-keyed score-probability case in TypeSafeDecisionClientResponseTests.

using Umbraco.AI.Core.Decision;
using Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

namespace Umbraco.AI.TypeSafe.Tests.Unit;

public class TypeSafeDecisionClientGapFillTests
{
    private static async Task<AIDecisionAnswer> AnswerAsync(AIDecisionQuestion question, string jevAnswer)
    {
        var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(
            """{"model":"jev-latest","answers":{"q":""" + jevAnswer + """},"usage":{"input_tokens":1,"output_tokens":1}}"""));
        var client = await TypeSafeTestHost.CreateClientAsync(handler);

        var response = await client.GetResponseAsync(new AIDecisionRequest { State = "text", Questions = [question] });
        return response.Answers["q"];
    }

    #region Happy path

    public class GivenJevOmitsTheMiddleScoreLevel
    {
        private readonly AIScoreDecisionAnswer _answer = (AIScoreDecisionAnswer)AnswerAsync(
                new AIScoreDecisionQuestion
                {
                    Id = "q",
                    Instructions = "Rate it",
                    Levels = [new AIDecisionScoreLevel("poor"), new AIDecisionScoreLevel("ok"), new AIDecisionScoreLevel("good")],
                },
                """{"score":1.6,"legend":{"0":"poor","1":"ok","2":"good"},"probabilities":{"0":0.2,"2":0.8},"confidence":0.8}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void KeysProbabilitiesByLevelPosition() => _answer.Probabilities.Keys.ShouldBe([0, 1, 2], ignoreOrder: true);

        [Fact]
        public void FillsTheOmittedLevelWithZero() => _answer.Probabilities[1].ShouldBe(0);

        [Fact]
        public void KeepsTheReportedLevels() => _answer.Probabilities[2].ShouldBe(0.8);
    }

    public class GivenJevOmitsAChoiceOption
    {
        private readonly AIChoiceDecisionAnswer _answer = (AIChoiceDecisionAnswer)AnswerAsync(
                new AIChoiceDecisionQuestion
                {
                    Id = "q",
                    Instructions = "Pick",
                    Options = [new AIDecisionOption("a"), new AIDecisionOption("b"), new AIDecisionOption("c")],
                },
                """{"choice":"a","probabilities":{"a":0.7,"b":0.3},"confidence":0.7}""")
            .GetAwaiter().GetResult();

        [Fact]
        public void FillsTheOmittedOptionWithZero() => _answer.Probabilities["c"].ShouldBe(0);
    }

    #endregion
}
