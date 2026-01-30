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
            .Setup(x => x.GetProfilesPagedAsync(null, null, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((profiles.AsEnumerable(), profiles.Count));

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
            .Setup(x => x.GetProfilesPagedAsync(null, AiCapability.Chat, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((chatProfiles.AsEnumerable(), chatProfiles.Count));

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
            .Setup(x => x.GetProfilesPagedAsync(null, null, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((profiles.AsEnumerable(), profiles.Count));

        _mapperMock
            .Setup(x => x.MapEnumerable<AiProfile, ProfileItemResponseModel>(It.IsAny<IEnumerable<AiProfile>>()))
            .Returns(new List<ProfileItemResponseModel>());

        // Act - passing invalid capability falls back to null capability (all profiles)
        var result = await _controller.GetAllProfiles(capability: "InvalidCapability");

        // Assert
        _profileServiceMock.Verify(x => x.GetProfilesPagedAsync(null, null, 0, 100, It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(x => x.GetProfilesPagedAsync(null, AiCapability.Embedding, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((embeddingProfiles.AsEnumerable(), embeddingProfiles.Count));

        _mapperMock
            .Setup(x => x.MapEnumerable<AiProfile, ProfileItemResponseModel>(It.IsAny<IEnumerable<AiProfile>>()))
            .Returns(new List<ProfileItemResponseModel>());

        // Act - using lowercase
        await _controller.GetAllProfiles(capability: "embedding");

        // Assert
        _profileServiceMock.Verify(x => x.GetProfilesPagedAsync(null, AiCapability.Embedding, 0, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllProfiles_WithEmptyList_ReturnsEmptyPagedViewModel()
    {
        // Arrange
        _profileServiceMock
            .Setup(x => x.GetProfilesPagedAsync(null, null, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enumerable.Empty<AiProfile>(), 0));

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
        var allProfiles = Enumerable.Range(1, 10)
            .Select(i => new AiProfileBuilder().WithAlias($"profile-{i}").Build())
            .ToList();

        // The paged method returns only the requested page
        var pagedProfiles = allProfiles.Skip(2).Take(3).ToList();

        _profileServiceMock
            .Setup(x => x.GetProfilesPagedAsync(null, null, 2, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((pagedProfiles.AsEnumerable(), 10)); // Total is still 10

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

    [Fact]
    public async Task GetAllProfiles_WithFilter_ReturnsFilteredProfiles()
    {
        // Arrange
        var filteredProfiles = new List<AiProfile>
        {
            new AiProfileBuilder().WithAlias("test-profile").WithName("Test Profile").Build()
        };

        var responseModels = filteredProfiles.Select(p => new ProfileItemResponseModel
        {
            Id = p.Id,
            Alias = p.Alias,
            Name = p.Name,
            Capability = p.Capability.ToString()
        }).ToList();

        _profileServiceMock
            .Setup(x => x.GetProfilesPagedAsync("test", null, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((filteredProfiles.AsEnumerable(), filteredProfiles.Count));

        _mapperMock
            .Setup(x => x.MapEnumerable<AiProfile, ProfileItemResponseModel>(It.IsAny<IEnumerable<AiProfile>>()))
            .Returns(responseModels);

        // Act
        var result = await _controller.GetAllProfiles(filter: "test");

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ProfileItemResponseModel>>();
        viewModel.Total.ShouldBe(1);
        _profileServiceMock.Verify(x => x.GetProfilesPagedAsync("test", null, 0, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllProfiles_WithFilterAndCapability_ReturnsFilteredProfiles()
    {
        // Arrange
        var filteredProfiles = new List<AiProfile>
        {
            new AiProfileBuilder().WithAlias("chat-test").WithName("Chat Test").WithCapability(AiCapability.Chat).Build()
        };

        var responseModels = filteredProfiles.Select(p => new ProfileItemResponseModel
        {
            Id = p.Id,
            Alias = p.Alias,
            Name = p.Name,
            Capability = p.Capability.ToString()
        }).ToList();

        _profileServiceMock
            .Setup(x => x.GetProfilesPagedAsync("test", AiCapability.Chat, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((filteredProfiles.AsEnumerable(), filteredProfiles.Count));

        _mapperMock
            .Setup(x => x.MapEnumerable<AiProfile, ProfileItemResponseModel>(It.IsAny<IEnumerable<AiProfile>>()))
            .Returns(responseModels);

        // Act
        var result = await _controller.GetAllProfiles(filter: "test", capability: "Chat");

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var viewModel = okResult.Value.ShouldBeOfType<PagedViewModel<ProfileItemResponseModel>>();
        viewModel.Total.ShouldBe(1);
        _profileServiceMock.Verify(x => x.GetProfilesPagedAsync("test", AiCapability.Chat, 0, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
