using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Profile.Controllers;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Profile;

public class DeleteProfileControllerTests
{
    private readonly Mock<IAiProfileRepository> _profileRepositoryMock;
    private readonly DeleteProfileController _controller;

    public DeleteProfileControllerTests()
    {
        _profileRepositoryMock = new Mock<IAiProfileRepository>();
        _controller = new DeleteProfileController(_profileRepositoryMock.Object);
    }

    #region DeleteProfile - By ID

    [Fact]
    public async Task DeleteProfile_WithExistingId_ReturnsOk()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileRepositoryMock
            .Setup(x => x.DeleteAsync(profileId, It.IsAny<CancellationToken>()))
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
        // DeleteAsync returns false when profile doesn't exist
        _profileRepositoryMock
            .Setup(x => x.DeleteAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteProfile(new IdOrAlias(profileId));

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task DeleteProfile_WithId_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileRepositoryMock
            .Setup(x => x.DeleteAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _controller.DeleteProfile(new IdOrAlias(profileId));

        // Assert
        _profileRepositoryMock.Verify(x => x.DeleteAsync(profileId, It.IsAny<CancellationToken>()), Times.Once);
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

        _profileRepositoryMock
            .Setup(x => x.GetByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _profileRepositoryMock
            .Setup(x => x.DeleteAsync(profileId, It.IsAny<CancellationToken>()))
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

        _profileRepositoryMock
            .Setup(x => x.GetByAliasAsync(alias, It.IsAny<CancellationToken>()))
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
