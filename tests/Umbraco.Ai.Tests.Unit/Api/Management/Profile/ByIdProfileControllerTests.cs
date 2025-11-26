using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Management.Profile.Controllers;
using Umbraco.Ai.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Profile;

public class ByIdProfileControllerTests
{
    private readonly Mock<IAiProfileRepository> _profileRepositoryMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly ByIdProfileController _controller;

    public ByIdProfileControllerTests()
    {
        _profileRepositoryMock = new Mock<IAiProfileRepository>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new ByIdProfileController(_profileRepositoryMock.Object, _mapperMock.Object);
    }

    #region ById

    [Fact]
    public async Task ById_WithExistingId_ReturnsProfile()
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

        _profileRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _mapperMock
            .Setup(x => x.Map<ProfileResponseModel>(profile))
            .Returns(responseModel);

        // Act
        var result = await _controller.ById(profileId);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var model = okResult.Value.ShouldBeOfType<ProfileResponseModel>();
        model.Id.ShouldBe(profileId);
        model.Alias.ShouldBe("test-profile");
    }

    [Fact]
    public async Task ById_WithNonExistingId_Returns404NotFound()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _profileRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiProfile?)null);

        // Act
        var result = await _controller.ById(profileId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task ById_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new AiProfileBuilder().WithId(profileId).Build();

        _profileRepositoryMock
            .Setup(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _mapperMock
            .Setup(x => x.Map<ProfileResponseModel>(It.IsAny<AiProfile>()))
            .Returns(new ProfileResponseModel());

        // Act
        await _controller.ById(profileId);

        // Assert
        _profileRepositoryMock.Verify(x => x.GetByIdAsync(profileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
