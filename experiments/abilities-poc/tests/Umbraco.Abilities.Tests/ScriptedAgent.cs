using System.Text.Json;

using Microsoft.Extensions.AI;

using Umbraco.Abilities.AI;
using Umbraco.Abilities.Samples;

namespace Umbraco.Abilities.Tests;

/// <summary>
/// Plays the part of a model: calls the AI tools by name with JSON arguments, exactly as a chat client's
/// function invocation would. The draft set id is the surface's context, like the Umbraco Draft Sets header.
/// </summary>
public sealed class ScriptedAgent
{
    public ScriptedAgent(SampleSite site)
    {
        Tools = AbilityAITools
            .Create(site.Abilities, site.Invoker, () => new AbilityContext("editor@example.com", "ai", DraftSetId))
            .ToDictionary(t => t.Name);
    }

    public IReadOnlyDictionary<string, AIFunction> Tools { get; }

    public Guid? DraftSetId { get; set; }

    public async Task<JsonElement> CallAsync(string tool, string argumentsJson)
    {
        using var document = JsonDocument.Parse(argumentsJson);
        var arguments = new AIFunctionArguments(document.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => (object?)p.Value.Clone()));

        var result = await Tools[tool].InvokeAsync(arguments);
        return (JsonElement)result!;
    }

    /// <summary>Calls a tool and asserts it succeeded. Returns its output.</summary>
    public async Task<JsonElement> OkAsync(string tool, string argumentsJson)
    {
        var result = await CallAsync(tool, argumentsJson);
        Assert.True(result.GetProperty("success").GetBoolean(), $"{tool} failed: {result}");
        return result.GetProperty("output");
    }

    /// <summary>Calls a tool and asserts it failed. Returns the failure.</summary>
    public async Task<JsonElement> FailAsync(string tool, string argumentsJson)
    {
        var result = await CallAsync(tool, argumentsJson);
        Assert.False(result.GetProperty("success").GetBoolean(), $"{tool} should have failed: {result}");
        return result;
    }

    public async Task<Guid> StartDraftSetAsync(string name)
    {
        var output = await OkAsync("draftSet_create", $$"""{ "name": "{{name}}" }""");
        DraftSetId = output.GetProperty("draftSetId").GetGuid();
        return DraftSetId.Value;
    }
}

public static class JsonAssert
{
    public static IReadOnlyList<string> ProblemPaths(JsonElement validateOutput)
        => validateOutput.GetProperty("entities").EnumerateArray()
            .SelectMany(e => e.GetProperty("problems").EnumerateArray())
            .Select(p => p.GetProperty("path").GetString()!)
            .ToList();
}
