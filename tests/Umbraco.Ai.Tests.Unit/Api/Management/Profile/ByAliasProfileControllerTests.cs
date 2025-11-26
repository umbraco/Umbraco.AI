using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Management.Profile.Controllers;
using Umbraco.Ai.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Profile;

public class ByAliasProfileControllerTests
{
    private readonly Mock<IAiProfileRepository> _profileRepositoryMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly ByAliasProfileController _controller;

    public ByAliasProfileControllerTests()
    {
        _profileRepositoryMock = new Mock<IAiProfileRepository>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new ByAliasProfileController(_profileRepositoryMock.Object, _mapperMock.Object);
    }

    #region ByAlias

    [Fact]
    public async Task ByAlias_WithExistingAlias_ReturnsProfile()
    {
        // Arrange
        var alias = "my-chat-profile";
        var profile = new AiProfileBuilder()
            .WithAlias(alias)
            .WithName("My Chat Profile")
            .Build();

        var responseModel = new ProfileResponseModel
        {
            Id = profile.Id,
            Alias = profile.Alias,
            Name = profile.Name,
            Capability = profile.Capability.ToString(),
            ConnectionId = profile.ConnectionId
        };

        _profileRepositoryMock
            .Setup(x => x.GetByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _mapperMock
            .Setup(x => x.Map<ProfileResponseModel>(profile))
            .Returns(responseModel);

        // Act
        var result = await _controller.ByAlias(alias);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var model = okResult.Value.ShouldBeOfType<ProfileResponseModel>();
        model.Alias.ShouldBe(alias);
        model.Name.ShouldBe("My Chat Profile");
    }

    [Fact]
    public async Task ByAlias_WithNonExistingAlias_Returns404NotFound()
    {
        // Arrange
        var alias = "non-existent-alias";

        _profileRepositoryMock
            .Setup(x => x.GetByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act
        var result = await _controller.ByAlias(alias);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task ByAlias_CallsRepositoryWithCorrectAlias()
    {
        // Arrange
        var alias = "my-chat-profile";
        var profile = new AiProfileBuilder().WithAlias(alias).Build();

        _profileRepositoryMock
            .Setup(x => x.GetByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _mapperMock
            .Setup(x => x.Map<ProfileResponseModel>(It.IsAny<AiProfile>()))
            .Returns(new ProfileResponseModel());

        // Act
        await _controller.ByAlias(alias);

        // Assert
        _profileRepositoryMock.Verify(x => x.GetByAliasAsync(alias, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
