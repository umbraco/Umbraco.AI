using System.Text.Json;
using Shouldly;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.AGUI.Models;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIToolConverterTests
{
    private readonly AGUIToolConverter _converter = new();

    private static AGUITool Tool(string name, IReadOnlyDictionary<string, object?>? metadata = null) => new()
    {
        Name = name,
        Description = $"{name} description",
        Metadata = metadata,
    };

    #region Shape

    [Fact]
    public void ConvertToFrontendTools_WithNullTools_ReturnsNull()
    {
        _converter.ConvertToFrontendTools(null).ShouldBeNull();
    }

    [Fact]
    public void ConvertToFrontendTools_WithEmptyTools_ReturnsNull()
    {
        _converter.ConvertToFrontendTools([]).ShouldBeNull();
    }

    [Fact]
    public void ConvertToFrontendTools_PreservesToolAndOrder()
    {
        var result = _converter.ConvertToFrontendTools([Tool("a"), Tool("b")])!.ToList();

        result.Count.ShouldBe(2);
        result[0].Tool.Name.ShouldBe("a");
        result[1].Tool.Name.ShouldBe("b");
    }

    #endregion

    #region Metadata extraction

    [Fact]
    public void ConvertToFrontendTools_NoMetadata_DefaultsScopeNullAndNotDestructive()
    {
        var result = _converter.ConvertToFrontendTools([Tool("t")])!.Single();

        result.Scope.ShouldBeNull();
        result.IsDestructive.ShouldBeFalse();
    }

    [Fact]
    public void ConvertToFrontendTools_StringMetadata_ReadsScopeAndIsDestructive()
    {
        var tool = Tool("t", new Dictionary<string, object?>
        {
            ["scope"] = "entity-write",
            ["isDestructive"] = true,
        });

        var result = _converter.ConvertToFrontendTools([tool])!.Single();

        result.Scope.ShouldBe("entity-write");
        result.IsDestructive.ShouldBeTrue();
    }

    [Fact]
    public void ConvertToFrontendTools_JsonElementMetadata_ReadsScopeAndIsDestructive()
    {
        // The wire deserializes metadata values to JsonElement, so the converter must
        // unwrap JsonElement string/bool — not just native CLR types.
        using var doc = JsonDocument.Parse("""{ "scope": "entity-publish", "isDestructive": true }""");
        var metadata = new Dictionary<string, object?>
        {
            ["scope"] = doc.RootElement.GetProperty("scope"),
            ["isDestructive"] = doc.RootElement.GetProperty("isDestructive"),
        };

        var result = _converter.ConvertToFrontendTools([Tool("t", metadata)])!.Single();

        result.Scope.ShouldBe("entity-publish");
        result.IsDestructive.ShouldBeTrue();
    }

    [Fact]
    public void ConvertToFrontendTools_IsDestructiveFalse_IsNotDestructive()
    {
        using var doc = JsonDocument.Parse("""{ "isDestructive": false }""");
        var metadata = new Dictionary<string, object?>
        {
            ["isDestructive"] = doc.RootElement.GetProperty("isDestructive"),
        };

        _converter.ConvertToFrontendTools([Tool("t", metadata)])!.Single().IsDestructive.ShouldBeFalse();
    }

    #endregion
}
