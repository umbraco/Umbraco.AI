using Microsoft.AspNetCore.Mvc;
using Moq;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Web.Api.Management.Tool.Controllers;
using Umbraco.AI.Web.Api.Management.Tool.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.ToolScope;

public class AllToolScopeControllerTests
{
    private readonly Mock<IUmbracoMapper> _umbracoMapperMock;

    public AllToolScopeControllerTests()
    {
        _umbracoMapperMock = new Mock<IUmbracoMapper>();
    }

    [Fact]
    public async Task GetAllToolScopes_WithRegisteredScopes_ReturnsOkWithMappedScopes()
    {
        // Arrange
        var scope1 = CreateFakeToolScope("content-read", "icon-folder");
        var scope2 = CreateFakeToolScope("media-write", "icon-picture", true);

        var collection = new AIToolScopeCollection(() => new[] { scope1, scope2 });
        var controller = new AllToolScopeController(collection, _umbracoMapperMock.Object);

        var responseModels = new List<ToolScopeItemResponseModel>
        {
            new() { Id = "content-read", Icon = "icon-folder", IsDestructive = false, Domain = "Content" },
            new() { Id = "media-write", Icon = "icon-picture", IsDestructive = true, Domain = "Media" }
        };

        _umbracoMapperMock
            .Setup(x => x.MapEnumerable<IAIToolScope, ToolScopeItemResponseModel>(It.IsAny<IEnumerable<IAIToolScope>>()))
            .Returns(responseModels);

        // Act
        var result = await controller.GetAllToolScopes();

        // Assert
        result.ShouldBeOfType<ActionResult<IEnumerable<ToolScopeItemResponseModel>>>();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var scopes = okResult.Value.ShouldBeAssignableTo<IEnumerable<ToolScopeItemResponseModel>>()!.ToArray();
        scopes.Length.ShouldBe(2);
        scopes[0].Id.ShouldBe("content-read");
        scopes[1].Id.ShouldBe("media-write");
    }

    [Fact]
    public async Task GetAllToolScopes_WithNoScopes_ReturnsOkWithEmptyCollection()
    {
        // Arrange
        var collection = new AIToolScopeCollection(() => Enumerable.Empty<IAIToolScope>());
        var controller = new AllToolScopeController(collection, _umbracoMapperMock.Object);

        _umbracoMapperMock
            .Setup(x => x.MapEnumerable<IAIToolScope, ToolScopeItemResponseModel>(It.IsAny<IEnumerable<IAIToolScope>>()))
            .Returns(new List<ToolScopeItemResponseModel>());

        // Act
        var result = await controller.GetAllToolScopes();

        // Assert
        result.ShouldBeOfType<ActionResult<IEnumerable<ToolScopeItemResponseModel>>>();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var scopes = okResult.Value.ShouldBeAssignableTo<IEnumerable<ToolScopeItemResponseModel>>()!;
        scopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllToolScopes_OrdersByDomainThenId()
    {
        // Arrange
        var scope1 = CreateFakeToolScope("search", "icon-search", domain: "General");
        var scope2 = CreateFakeToolScope("content-read", "icon-folder", domain: "Content");
        var scope3 = CreateFakeToolScope("navigation", "icon-sitemap", domain: "General");

        var collection = new AIToolScopeCollection(() => new[] { scope1, scope2, scope3 });
        var controller = new AllToolScopeController(collection, _umbracoMapperMock.Object);

        var capturedScopes = new List<IAIToolScope>();
        _umbracoMapperMock
            .Setup(x => x.MapEnumerable<IAIToolScope, ToolScopeItemResponseModel>(It.IsAny<IEnumerable<IAIToolScope>>()))
            .Callback<IEnumerable<IAIToolScope>>(scopes => capturedScopes.AddRange(scopes))
            .Returns(new List<ToolScopeItemResponseModel>());

        // Act
        await controller.GetAllToolScopes();

        // Assert
        capturedScopes.Count.ShouldBe(3);
        // Should be ordered by domain first (Content, General, General), then by id (navigation before search)
        capturedScopes[0].Id.ShouldBe("content-read");
        capturedScopes[0].Domain.ShouldBe("Content");
        capturedScopes[1].Id.ShouldBe("navigation");
        capturedScopes[1].Domain.ShouldBe("General");
        capturedScopes[2].Id.ShouldBe("search");
        capturedScopes[2].Domain.ShouldBe("General");
    }

    private static IAIToolScope CreateFakeToolScope(string id, string icon, bool isDestructive = false, string domain = "Content")
    {
        var mock = new Mock<IAIToolScope>();
        mock.Setup(x => x.Id).Returns(id);
        mock.Setup(x => x.Icon).Returns(icon);
        mock.Setup(x => x.IsDestructive).Returns(isDestructive);
        mock.Setup(x => x.Domain).Returns(domain);
        return mock.Object;
    }
}
