using System.Text.Json;
using Moq;
using Shouldly;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Core.Tests;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Tests;

/// <summary>
/// Regression tests for <see cref="PromptTestFeature.ExtractOutputValue"/>.
/// For prompts with a result type of Single Option / Multiple Options, the underlying chat
/// response is a structured-output JSON envelope (e.g. <c>{"value":"..."}</c>); the unwrapped
/// text lives on <c>resultOptions[].displayValue</c>. Graders must evaluate the unwrapped text,
/// not the JSON envelope (issue #142).
/// </summary>
public class PromptTestFeatureTests
{
    private readonly PromptTestFeature _feature = new(
        Mock.Of<IAIPromptService>(),
        new AITestContextResolver(),
        Mock.Of<IAIEditableModelSchemaBuilder>());

    [Fact]
    public void ExtractOutputValue_SingleOption_ReturnsDisplayValue()
    {
        var transcript = BuildTranscript(new
        {
            content = "{\"value\":\"Hello world\"}",
            resultOptions = new[]
            {
                new { label = "Result", displayValue = "Hello world" }
            }
        });

        _feature.ExtractOutputValue(transcript).ShouldBe("Hello world");
    }

    [Fact]
    public void ExtractOutputValue_MultipleOptions_ReturnsJoinedDisplayValues()
    {
        var transcript = BuildTranscript(new
        {
            content = "{\"options\":[...]}",
            resultOptions = new[]
            {
                new { label = "A", displayValue = "First" },
                new { label = "B", displayValue = "Second" }
            }
        });

        _feature.ExtractOutputValue(transcript)
            .ShouldBe("First" + Environment.NewLine + "Second");
    }

    [Fact]
    public void ExtractOutputValue_NoResultOptions_FallsBackToContent()
    {
        var transcript = BuildTranscript(new
        {
            content = "Plain text response",
            resultOptions = Array.Empty<object>()
        });

        _feature.ExtractOutputValue(transcript).ShouldBe("Plain text response");
    }

    [Fact]
    public void ExtractOutputValue_MissingResultOptions_FallsBackToContent()
    {
        var transcript = BuildTranscript(new { content = "Plain text response" });

        _feature.ExtractOutputValue(transcript).ShouldBe("Plain text response");
    }

    [Fact]
    public void ExtractOutputValue_ErrorTranscript_ReturnsRawObjectText()
    {
        // Error transcripts don't have a "content" property — base behaviour should kick in.
        var transcript = BuildTranscript(new { error = "boom", stackTrace = "..." });

        _feature.ExtractOutputValue(transcript)
            .ShouldContain("\"error\"");
    }

    private static AITestTranscript BuildTranscript(object finalOutput) => new()
    {
        RunId = Guid.NewGuid(),
        FinalOutput = JsonSerializer.SerializeToElement(
            finalOutput,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
    };
}
