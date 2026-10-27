using Moq;
using Umbraco.AI.Core.Tools.Umbraco;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

/// <summary>
/// <see cref="IUmbracoReadAuthorizer"/> doubles for read-tool tests.
/// </summary>
internal static class TestReadAuthorizers
{
    /// <summary>
    /// An authorizer that allows every read, for tests about a tool's own behaviour.
    /// </summary>
    public static Mock<IUmbracoReadAuthorizer> AllowAll()
    {
        var mock = new Mock<IUmbracoReadAuthorizer>();
        mock.Setup(x => x.CanReadContentAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        mock.Setup(x => x.CanSeeContentInTreeAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(true);
        mock.Setup(x => x.CanReadMediaAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        return mock;
    }
}
