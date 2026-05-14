using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

public class AIAgentExecutionOptionsTests
{
    [Fact]
    public void AdditionalProperties_DefaultsToNull()
    {
        var options = new AIAgentExecutionOptions();
        options.AdditionalProperties.ShouldBeNull();
    }

    [Fact]
    public void AdditionalProperties_PreservesAssignedValues()
    {
        var props = new Dictionary<string, object?>
        {
            ["key-a"] = "value-a",
            ["key-b"] = 42,
        };

        var options = new AIAgentExecutionOptions { AdditionalProperties = props };

        options.AdditionalProperties.ShouldNotBeNull();
        options.AdditionalProperties!["key-a"].ShouldBe("value-a");
        options.AdditionalProperties["key-b"].ShouldBe(42);
    }
}
