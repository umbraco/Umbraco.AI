using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Profile.Controllers;
using Umbraco.Ai.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Profile;

public class ByIdOrAliasProfileControllerTests
{
    private readonly Mock<IAiProfileService> _profileServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly ByIdOrAliasProfileController _controller;

    public ByIdOrAliasProfileControllerTests()
    {
        _profileServiceMock = new Mock<IAiProfileService>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new ByIdOrAliasProfileController(_profileServiceMock.Object, _mapperMock.Object);
    }

    #region GetProfileByIdOrAlias - With ID

    [Fact]
    public async Task GetProfileByIdOrAlias_WithExistingId_ReturnsProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new AiProfileBuilder()
            .WithId(profileId)
            .WithAlias("test-profile")
            .WithName("Test Profile")
            .Build();

        var responseModel = new ProfileResponseModel
        {
            Id = profile.Id,
            Alias = profile.Alias,
            Name = profile.Name,
            Capability = profile.Capability.ToString(),
            ConnectionId = profile.ConnectionId
        };

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _mapperMock
            .Setup(x => x.Map<ProfileResponseModel>(profile))
            .Returns(responseModel);

        var idOrAlias = new IdOrAlias(profileId);

        // Act
        var result = await _controller.GetProfileByIdOrAlias(idOrAlias);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var model = okResult.Value.ShouldBeOfType<ProfileResponseModel>();
        model.Id.ShouldBe(profileId);
        model.Alias.ShouldBe("test-profile");
    }

    [Fact]
    public async Task GetProfileByIdOrAlias_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        var idOrAlias = new IdOrAlias(profileId);

        // Act
        var result = await _controller.GetProfileByIdOrAlias(idOrAlias);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task GetProfileByIdOrAlias_WithId_CallsServiceGetProfileAsync()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new AiProfileBuilder().WithId(profileId).Build();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _mapperMock
            .Setup(x => x.Map<ProfileResponseModel>(It.IsAny<AiProfile>()))
            .Returns(new ProfileResponseModel());

        var idOrAlias = new IdOrAlias(profileId);

        // Act
        await _controller.GetProfileByIdOrAlias(idOrAlias);

        // Assert
        _profileServiceMock.Verify(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetProfileByIdOrAlias - With Alias

    [Fact]
    public async Task GetProfileByIdOrAlias_WithExistingAlias_ReturnsProfile()
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

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _mapperMock
            .Setup(x => x.Map<ProfileResponseModel>(profile))
            .Returns(responseModel);

        var idOrAlias = new IdOrAlias(alias);

        // Act
        var result = await _controller.GetProfileByIdOrAlias(idOrAlias);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var model = okResult.Value.ShouldBeOfType<ProfileResponseModel>();
        model.Alias.ShouldBe(alias);
        model.Name.ShouldBe("My Chat Profile");
    }

    [Fact]
    public async Task GetProfileByIdOrAlias_WithNonExistingAlias_Returns404NotFound()
    {
        // Arrange
        var alias = "non-existent-alias";

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        var idOrAlias = new IdOrAlias(alias);

        // Act
        var result = await _controller.GetProfileByIdOrAlias(idOrAlias);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task GetProfileByIdOrAlias_WithAlias_CallsServiceGetProfileByAliasAsync()
    {
        // Arrange
        var alias = "my-chat-profile";
        var profile = new AiProfileBuilder().WithAlias(alias).Build();

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _mapperMock
            .Setup(x => x.Map<ProfileResponseModel>(It.IsAny<AiProfile>()))
            .Returns(new ProfileResponseModel());

        var idOrAlias = new IdOrAlias(alias);

        // Act
        await _controller.GetProfileByIdOrAlias(idOrAlias);

        // Assert
        _profileServiceMock.Verify(x => x.GetProfileByAliasAsync(alias, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
