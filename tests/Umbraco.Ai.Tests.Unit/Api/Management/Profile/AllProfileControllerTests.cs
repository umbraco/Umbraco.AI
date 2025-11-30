using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Management.Profile.Controllers;
using Umbraco.Ai.Web.Api.Management.Profile.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Profile;

public class AllProfileControllerTests
{
    private readonly Mock<IAiProfileService> _profileServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly AllProfileController _controller;

    public AllProfileControllerTests()
    {
        _profileServiceMock = new Mock<IAiProfileService>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new AllProfileController(_profileServiceMock.Object, _mapperMock.Object);
    }

    #region GetAllProfiles

    [Fact]
    public async Task GetAllProfiles_WithNoFilter_ReturnsAllProfiles()
    {
        // Arrange
        var profiles = new List<AiProfile>
        {
            new AiProfileBuilder().WithAlias("profile-1").WithCapability(AiCapability.Chat).Build(),
            new AiProfileBuilder().WithAlias("profile-2").WithCapability(AiCapability.Embedding).Build()
        };

        var responseModels = profiles.Select(p => new ProfileItemResponseModel
        {
            Id = p.Id,
            Alias = p.Alias,
            Name = p.Name,
            Capability = p.Capability.ToString()
        }).ToList();

        _profileServiceMock
            .Setup(x => x.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profiles);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiProfile, ProfileItemResponseModel>(It.IsAny<IEnumerable<AiProfile>>()))
            .Returns(responseModels);

        // Act
        var result = await _controller.GetAllProfiles();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ProfileItemResponseModel>>();
        viewModel.Total.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllProfiles_WithCapabilityFilter_ReturnsFilteredProfiles()
    {
        // Arrange
        var chatProfiles = new List<AiProfile>
        {
            new AiProfileBuilder().WithAlias("chat-1").WithCapability(AiCapability.Chat).Build()
        };

        var responseModels = chatProfiles.Select(p => new ProfileItemResponseModel
        {
            Id = p.Id,
            Alias = p.Alias,
            Name = p.Name,
            Capability = p.Capability.ToString()
        }).ToList();

        _profileServiceMock
            .Setup(x => x.GetProfilesAsync(AiCapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatProfiles);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiProfile, ProfileItemResponseModel>(It.IsAny<IEnumerable<AiProfile>>()))
            .Returns(responseModels);

        // Act
        var result = await _controller.GetAllProfiles(capability: "Chat");

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ProfileItemResponseModel>>();
        viewModel.Total.ShouldBe(1);
    }

    [Fact]
    public async Task GetAllProfiles_WithInvalidCapabilityFilter_ReturnsAllProfiles()
    {
        // Arrange
        var profiles = new List<AiProfile>
        {
            new AiProfileBuilder().Build()
        };

        _profileServiceMock
            .Setup(x => x.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profiles);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiProfile, ProfileItemResponseModel>(It.IsAny<IEnumerable<AiProfile>>()))
            .Returns(new List<ProfileItemResponseModel>());

        // Act - passing invalid capability falls back to GetAllProfilesAsync
        var result = await _controller.GetAllProfiles(capability: "InvalidCapability");

        // Assert
        _profileServiceMock.Verify(x => x.GetAllProfilesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _profileServiceMock.Verify(x => x.GetProfilesAsync(It.IsAny<AiCapability>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllProfiles_WithCaseInsensitiveCapability_ReturnsFilteredProfiles()
    {
        // Arrange
        var embeddingProfiles = new List<AiProfile>
        {
            new AiProfileBuilder().WithCapability(AiCapability.Embedding).Build()
        };

        _profileServiceMock
            .Setup(x => x.GetProfilesAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingProfiles);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiProfile, ProfileItemResponseModel>(It.IsAny<IEnumerable<AiProfile>>()))
            .Returns(new List<ProfileItemResponseModel>());

        // Act - using lowercase
        await _controller.GetAllProfiles(capability: "embedding");

        // Assert
        _profileServiceMock.Verify(x => x.GetProfilesAsync(AiCapability.Embedding, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllProfiles_WithEmptyList_ReturnsEmptyPagedViewModel()
    {
        // Arrange
        _profileServiceMock
            .Setup(x => x.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiProfile>());

        _mapperMock
            .Setup(x => x.MapEnumerable<AiProfile, ProfileItemResponseModel>(It.IsAny<IEnumerable<AiProfile>>()))
            .Returns(new List<ProfileItemResponseModel>());

        // Act
        var result = await _controller.GetAllProfiles();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ProfileItemResponseModel>>();
        viewModel.Total.ShouldBe(0);
        viewModel.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllProfiles_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var profiles = Enumerable.Range(1, 10)
            .Select(i => new AiProfileBuilder().WithAlias($"profile-{i}").Build())
            .ToList();

        _profileServiceMock
            .Setup(x => x.GetAllProfilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profiles);

        _mapperMock
            .Setup(x => x.MapEnumerable<AiProfile, ProfileItemResponseModel>(It.IsAny<IEnumerable<AiProfile>>()))
            .Returns((IEnumerable<AiProfile> items) => items.Select(p => new ProfileItemResponseModel
            {
                Id = p.Id,
                Alias = p.Alias,
                Name = p.Name,
                Capability = p.Capability.ToString()
            }).ToList());

        // Act
        var result = await _controller.GetAllProfiles(skip: 2, take: 3);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ProfileItemResponseModel>>();
        viewModel.Total.ShouldBe(10); // Total count before pagination
        viewModel.Items.Count().ShouldBe(3); // Only 3 items returned due to take
    }

    #endregion
}
