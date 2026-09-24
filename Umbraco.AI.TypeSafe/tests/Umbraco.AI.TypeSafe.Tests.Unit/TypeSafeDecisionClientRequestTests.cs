// DR-2 — Connect to TypeSafe AI (AC2-AC8: what goes over the wire)
#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using System.Text.Json;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

namespace Umbraco.AI.TypeSafe.Tests.Unit;

/// <summary>
/// Asserts on the captured request body, not on what we handed the client. The spike showed every guessed
/// detail of Jev's wire format was wrong on first contact, so these pin the confirmed shape.
/// </summary>
public class TypeSafeDecisionClientRequestTests
{
    private const string BinaryAnswer = """{"model":"jev-latest","answers":{"q":{"noul":0.97}},"usage":{"input_tokens":1,"output_tokens":1}}""";
    private const string ChoiceAnswer = """{"model":"jev-latest","answers":{"q":{"choice":"a","probabilities":{"a":1.0},"confidence":1.0}},"usage":{"input_tokens":1,"output_tokens":1}}""";
    private const string ScoreAnswer = """{"model":"jev-latest","answers":{"q":{"score":0.0,"legend":{"0":"poor","1":"ok","2":"good"},"probabilities":{"0":1.0},"confidence":1.0}},"usage":{"input_tokens":1,"output_tokens":1}}""";

    private static async Task<(ScriptedHttpMessageHandler Handler, JsonElement Question, JsonElement Body)> SendAsync(
        AIDecisionQuestion question,
        string answer,
        AIDecisionOptions? options = null)
    {
        var handler = new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Json(answer));
        var client = await TypeSafeTestHost.CreateClientAsync(handler);

        await client.AskAsync(question, options);

        var body = JsonDocument.Parse(handler.RequestBodies.Single()).RootElement;
        return (handler, body.GetProperty("questions").GetProperty("q"), body);
    }

    public class GivenABinaryQuestionWithNoCriteria
    {
        private readonly (ScriptedHttpMessageHandler Handler, JsonElement Question, JsonElement Body) _sent;

        public GivenABinaryQuestionWithNoCriteria()
            => _sent = SendAsync(new AIBinaryDecisionQuestion { Instructions = "Is this spam?" }, BinaryAnswer).GetAwaiter().GetResult();

        [Fact]
        public void PostsToTheSystemOneEndpoint()
        {
            _sent.Handler.Requests.Single().RequestUri.ShouldBe(new Uri("https://api.typesafe.ai/v1/systemone"));
        }

        [Fact]
        public void UsesPost()
        {
            _sent.Handler.Requests.Single().Method.ShouldBe(HttpMethod.Post);
        }

        [Fact]
        public void SendsTheApiKeyAsABearerToken()
        {
            _sent.Handler.Requests.Single().Headers.Authorization!.ToString().ShouldBe("Bearer test-api-key");
        }

        [Fact]
        public void SendsTheNoulType()
        {
            _sent.Question.GetProperty("type").GetString().ShouldBe("noul");
        }

        [Fact]
        public void OmitsTheCriteriaPropertyEntirely()
        {
            // Jev rejects "criteria": null for noul; the property must be absent, not null.
            _sent.Question.TryGetProperty("criteria", out _).ShouldBeFalse();
        }
    }

    public class GivenABinaryQuestionWithCriteria
    {
        private readonly JsonElement _criteria;

        public GivenABinaryQuestionWithCriteria()
        {
            var question = new AIBinaryDecisionQuestion
            {
                Instructions = "Is this spam?",
                TrueCriteria = "Promotional or scam",
                FalseCriteria = "A genuine comment",
            };
            _criteria = SendAsync(question, BinaryAnswer).GetAwaiter().GetResult().Question.GetProperty("criteria");
        }

        [Fact]
        public void SendsTheTrueCriteria()
        {
            _criteria.GetProperty("true").GetString().ShouldBe("Promotional or scam");
        }

        [Fact]
        public void SendsTheFalseCriteria()
        {
            _criteria.GetProperty("false").GetString().ShouldBe("A genuine comment");
        }
    }

    public class GivenAChoiceQuestion
    {
        private readonly JsonElement _question;

        public GivenAChoiceQuestion()
        {
            var question = new AIChoiceDecisionQuestion
            {
                Instructions = "Which agent?",
                Options = [new AIDecisionOption("a", "A"), new AIDecisionOption("b")],
            };
            _question = SendAsync(question, ChoiceAnswer).GetAwaiter().GetResult().Question;
        }

        [Fact]
        public void SendsTheChoiceType()
        {
            _question.GetProperty("type").GetString().ShouldBe("choice");
        }

        [Fact]
        public void SendsEachOptionKeyWithItsDescription()
        {
            _question.GetProperty("criteria").GetProperty("a").GetString().ShouldBe("A");
        }

        [Fact]
        public void FallsBackToTheKeyWhenAnOptionHasNoDescription()
        {
            _question.GetProperty("criteria").GetProperty("b").GetString().ShouldBe("b");
        }
    }

    public class GivenAScoreQuestion
    {
        private readonly JsonElement _question;

        public GivenAScoreQuestion()
        {
            var question = new AIScoreDecisionQuestion { Instructions = "How good?", Levels = ["poor", "ok", "good"] };
            _question = SendAsync(question, ScoreAnswer).GetAwaiter().GetResult().Question;
        }

        [Fact]
        public void SendsTheScoreType()
        {
            _question.GetProperty("type").GetString().ShouldBe("score");
        }

        [Fact]
        public void SendsTheLevelsInOrderAsCriteria()
        {
            _question.GetProperty("criteria").EnumerateArray().Select(e => e.GetString()).ShouldBe(["poor", "ok", "good"]);
        }
    }

    public class GivenAQuestionWithContext
    {
        private readonly (ScriptedHttpMessageHandler Handler, JsonElement Question, JsonElement Body) _sent;

        public GivenAQuestionWithContext()
        {
            var question = new AIBinaryDecisionQuestion { Instructions = "Is this spam?", Context = "Buy cheap watches" };
            _sent = SendAsync(question, BinaryAnswer).GetAwaiter().GetResult();
        }

        [Fact]
        public void SendsTheContextAsState()
        {
            _sent.Body.GetProperty("state").GetString().ShouldBe("Buy cheap watches");
        }

        [Fact]
        public void SendsTheInstructionsAsInstructions()
        {
            _sent.Question.GetProperty("instructions").GetString().ShouldBe("Is this spam?");
        }
    }

    public class GivenAQuestionWithNoContext
    {
        private readonly JsonElement _body;

        public GivenAQuestionWithNoContext()
            => _body = SendAsync(new AIBinaryDecisionQuestion { Instructions = "Is the sky blue?" }, BinaryAnswer).GetAwaiter().GetResult().Body;

        [Fact]
        public void SendsTheInstructionsAsState()
        {
            _body.GetProperty("state").GetString().ShouldBe("Is the sky blue?");
        }
    }

    public class GivenAModelIdInTheOptions
    {
        private readonly JsonElement _body;

        public GivenAModelIdInTheOptions()
            => _body = SendAsync(
                new AIBinaryDecisionQuestion { Instructions = "Is this spam?" },
                BinaryAnswer,
                new AIDecisionOptions { ModelId = "jev-latest" }).GetAwaiter().GetResult().Body;

        [Fact]
        public void SendsItAsTheModel()
        {
            _body.GetProperty("model").GetString().ShouldBe("jev-latest");
        }
    }
}
