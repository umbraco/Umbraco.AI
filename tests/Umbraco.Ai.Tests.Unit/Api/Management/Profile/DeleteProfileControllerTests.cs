using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Profiles;
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

    #region Delete

    [Fact]
    public async Task Delete_WithExistingProfile_ReturnsOk()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileRepositoryMock
            .Setup(x => x.DeleteAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(profileId);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistingProfile_Returns404NotFound()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileRepositoryMock
            .Setup(x => x.DeleteAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(profileId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task Delete_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileRepositoryMock
            .Setup(x => x.DeleteAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _controller.Delete(profileId);

        // Assert
        _profileRepositoryMock.Verify(x => x.DeleteAsync(profileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
