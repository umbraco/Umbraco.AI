using System.Text.Json;
using Shouldly;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.AGUI.Models;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIToolConverterTests
{
    private readonly AGUIToolConverter _converter = new();

    #region ConvertToFrontendTools Tests

    [Fact]
    public void ConvertToFrontendTools_WithNullTools_ReturnsNull()
    {
        // Act
        var result = _converter.ConvertToFrontendTools(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertToFrontendTools_WithEmptyTools_ReturnsNull()
    {
        // Arrange
        var tools = Enumerable.Empty<AGUITool>();

        // Act
        var result = _converter.ConvertToFrontendTools(tools);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertToFrontendTools_WithSingleTool_ReturnsListWithOneTool()
    {
        // Arrange
        var tools = new List<AGUITool>
        {
            new()
            {
                Name = "test-tool",
                Description = "A test tool",
                Parameters = new AGUIToolParameters
                {
                    Type = "object",
                    Properties = JsonSerializer.SerializeToElement(new { name = new { type = "string" } })
                }
            }
        };

        // Act
        var result = _converter.ConvertToFrontendTools(tools);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBeOfType<AIFrontendToolFunction>();
    }

    [Fact]
    public void ConvertToFrontendTools_WithMultipleTools_ReturnsAllTools()
    {
        // Arrange
        var tools = new List<AGUITool>
        {
            new()
            {
                Name = "tool-1",
                Description = "First tool",
                Parameters = new AGUIToolParameters
                {
                    Type = "object",
                    Properties = JsonSerializer.SerializeToElement(new { })
                }
            },
            new()
            {
                Name = "tool-2",
                Description = "Second tool",
                Parameters = new AGUIToolParameters
                {
                    Type = "object",
                    Properties = JsonSerializer.SerializeToElement(new { })
                }
            },
            new()
            {
                Name = "tool-3",
                Description = "Third tool",
                Parameters = new AGUIToolParameters
                {
                    Type = "object",
                    Properties = JsonSerializer.SerializeToElement(new { })
                }
            }
        };

        // Act
        var result = _converter.ConvertToFrontendTools(tools);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void ConvertToFrontendTools_PreservesToolNames()
    {
        // Arrange
        var tools = new List<AGUITool>
        {
            new()
            {
                Name = "confirm_action",
                Description = "Confirms an action",
                Parameters = new AGUIToolParameters
                {
                    Type = "object",
                    Properties = JsonSerializer.SerializeToElement(new { })
                }
            },
            new()
            {
                Name = "request_approval",
                Description = "Requests approval",
                Parameters = new AGUIToolParameters
                {
                    Type = "object",
                    Properties = JsonSerializer.SerializeToElement(new { })
                }
            }
        };

        // Act
        var result = _converter.ConvertToFrontendTools(tools);

        // Assert
        result.ShouldNotBeNull();
        var toolNames = result.Select(t => t.Name).ToList();
        toolNames.ShouldContain("confirm_action");
        toolNames.ShouldContain("request_approval");
    }

    [Fact]
    public void ConvertToFrontendTools_ReturnsAIFrontendToolFunctionInstances()
    {
        // Arrange
        var tools = new List<AGUITool>
        {
            new()
            {
                Name = "frontend-tool",
                Description = "A frontend tool",
                Parameters = new AGUIToolParameters
                {
                    Type = "object",
                    Properties = JsonSerializer.SerializeToElement(new { param = new { type = "string" } })
                }
            }
        };

        // Act
        var result = _converter.ConvertToFrontendTools(tools);

        // Assert
        result.ShouldNotBeNull();
        result.All(t => t is AIFrontendToolFunction).ShouldBeTrue();
    }

    #endregion
}
