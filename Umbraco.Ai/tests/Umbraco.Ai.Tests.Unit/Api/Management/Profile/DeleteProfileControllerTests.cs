using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Profile.Controllers;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Profile;

public class DeleteProfileControllerTests
{
    private readonly Mock<IAiProfileService> _profileServiceMock;
    private readonly DeleteProfileController _controller;

    public DeleteProfileControllerTests()
    {
        _profileServiceMock = new Mock<IAiProfileService>();
        _controller = new DeleteProfileController(_profileServiceMock.Object);
    }

    #region DeleteProfile - By ID

    [Fact]
    public async Task DeleteProfile_WithExistingId_ReturnsOk()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileServiceMock
            .Setup(x => x.DeleteProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteProfile(new IdOrAlias(profileId));

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task DeleteProfile_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        // TryGetProfileIdAsync returns the ID directly (no lookup for IDs)
        // DeleteProfileAsync returns false when profile doesn't exist
        _profileServiceMock
            .Setup(x => x.DeleteProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteProfile(new IdOrAlias(profileId));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task DeleteProfile_WithId_CallsServiceWithCorrectId()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileServiceMock
            .Setup(x => x.DeleteProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _controller.DeleteProfile(new IdOrAlias(profileId));

        // Assert
        _profileServiceMock.Verify(x => x.DeleteProfileAsync(profileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteProfile - By Alias

    [Fact]
    public async Task DeleteProfile_WithExistingAlias_ReturnsOk()
    {
        // Arrange
        var alias = "my-profile";
        var profileId = Guid.NewGuid();
        var profile = new AiProfileBuilder().WithId(profileId).WithAlias(alias).Build();

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _profileServiceMock
            .Setup(x => x.DeleteProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteProfile(new IdOrAlias(alias));

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task DeleteProfile_WithNonExistingAlias_Returns404NotFound()
    {
        // Arrange
        var alias = "non-existing";

        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act
        var result = await _controller.DeleteProfile(new IdOrAlias(alias));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    #endregion
}
