using System;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;

using Xunit;

namespace Umbraco.AI.Deploy.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAIUserGroupMapperTests
{
    private readonly Mock<IUserGroupService> _service = new();

    [Fact]
    public async Task GetAliasAsync_WithKnownKey_ReturnsTheAlias()
    {
        var key = Guid.NewGuid();
        _service.Setup(x => x.GetAsync(key)).ReturnsAsync(Mock.Of<IUserGroup>(g => g.Alias == "editors"));

        (await UmbracoAIUserGroupMapper.GetAliasAsync(_service.Object, key.ToString())).ShouldBe("editors");
    }

    [Fact]
    public async Task GetAliasAsync_WithUnknownKey_ReturnsTheKeyRaw()
    {
        var key = Guid.NewGuid();
        _service.Setup(x => x.GetAsync(key)).ReturnsAsync((IUserGroup?)null);

        (await UmbracoAIUserGroupMapper.GetAliasAsync(_service.Object, key.ToString())).ShouldBe(key.ToString());
    }

    [Fact]
    public async Task FindTargetAsync_WithAlias_LooksUpByAlias()
    {
        var group = Mock.Of<IUserGroup>();
        _service.Setup(x => x.GetAsync("editors")).ReturnsAsync(group);

        (await UmbracoAIUserGroupMapper.FindTargetAsync(_service.Object, "editors")).ShouldBeSameAs(group);
    }

    [Fact]
    public async Task FindTargetAsync_WithGuidValue_LooksUpByKey()
    {
        var key = Guid.NewGuid();
        var group = Mock.Of<IUserGroup>();
        _service.Setup(x => x.GetAsync(key)).ReturnsAsync(group);

        (await UmbracoAIUserGroupMapper.FindTargetAsync(_service.Object, key.ToString())).ShouldBeSameAs(group);
    }

    [Fact]
    public async Task GetTargetKeysAsync_WithAliasAndKey_ResolvesBoth()
    {
        var keyValue = Guid.NewGuid();
        var aliasTarget = Guid.NewGuid();
        var keyTarget = Guid.NewGuid();
        _service.Setup(x => x.GetAsync("editors")).ReturnsAsync(Mock.Of<IUserGroup>(g => g.Key == aliasTarget));
        _service.Setup(x => x.GetAsync(keyValue)).ReturnsAsync(Mock.Of<IUserGroup>(g => g.Key == keyTarget));

        var keys = await UmbracoAIUserGroupMapper.GetTargetKeysAsync(
            _service.Object, ["editors", keyValue.ToString()], "agent", "a");

        keys.ShouldBe([aliasTarget, keyTarget]);
    }

    [Fact]
    public async Task GetTargetKeysAsync_WithMissingGroup_ThrowsNamingTheValueAndEntity()
    {
        _service.Setup(x => x.GetAsync("ghosts")).ReturnsAsync((IUserGroup?)null);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            UmbracoAIUserGroupMapper.GetTargetKeysAsync(_service.Object, ["ghosts"], "prompt", "my-prompt"));

        ex.Message.ShouldContain("Prompt 'my-prompt'");
        ex.Message.ShouldContain("'ghosts'");
        ex.Message.ShouldContain("deploy the prompt again");
    }
}
