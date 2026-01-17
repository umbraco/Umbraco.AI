using System.Text.Json;
using Shouldly;
using Umbraco.Ai.Agent.Core.Agui;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.Agui.Models;
using Xunit;

namespace Umbraco.Ai.Agent.Tests.Unit.Agui;

public class AguiToolConverterTests
{
    private readonly AguiToolConverter _converter = new();

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
        var tools = Enumerable.Empty<AguiTool>();

        // Act
        var result = _converter.ConvertToFrontendTools(tools);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertToFrontendTools_WithSingleTool_ReturnsListWithOneTool()
    {
        // Arrange
        var tools = new List<AguiTool>
        {
            new()
            {
                Name = "test-tool",
                Description = "A test tool",
                Parameters = new AguiToolParameters
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
        result[0].ShouldBeOfType<FrontendToolFunction>();
    }

    [Fact]
    public void ConvertToFrontendTools_WithMultipleTools_ReturnsAllTools()
    {
        // Arrange
        var tools = new List<AguiTool>
        {
            new()
            {
                Name = "tool-1",
                Description = "First tool",
                Parameters = new AguiToolParameters
                {
                    Type = "object",
                    Properties = JsonSerializer.SerializeToElement(new { })
                }
            },
            new()
            {
                Name = "tool-2",
                Description = "Second tool",
                Parameters = new AguiToolParameters
                {
                    Type = "object",
                    Properties = JsonSerializer.SerializeToElement(new { })
                }
            },
            new()
            {
                Name = "tool-3",
                Description = "Third tool",
                Parameters = new AguiToolParameters
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
        var tools = new List<AguiTool>
        {
            new()
            {
                Name = "confirm_action",
                Description = "Confirms an action",
                Parameters = new AguiToolParameters
                {
                    Type = "object",
                    Properties = JsonSerializer.SerializeToElement(new { })
                }
            },
            new()
            {
                Name = "request_approval",
                Description = "Requests approval",
                Parameters = new AguiToolParameters
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
    public void ConvertToFrontendTools_ReturnsFrontendToolFunctionInstances()
    {
        // Arrange
        var tools = new List<AguiTool>
        {
            new()
            {
                Name = "frontend-tool",
                Description = "A frontend tool",
                Parameters = new AguiToolParameters
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
        result.All(t => t is FrontendToolFunction).ShouldBeTrue();
    }

    #endregion
}
