using System.Text.Json;
using System.Text.Json.Nodes;

using Moq;
using Shouldly;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class SetUmbracoContentValueToolTests
{
    private readonly Mock<IContentEditingService> _contentEditingServiceMock = new();
    private readonly Mock<IAIPropertyValueDispatcher> _dispatcherMock = new();
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock = new();
    private readonly IAITool _tool;

    public SetUmbracoContentValueToolTests()
    {
        _tool = new SetUmbracoContentValueTool(_contentEditingServiceMock.Object, _dispatcherMock.Object, _authorizerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_DispatchesSetValueWithWrappedValueArg()
    {
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeMock = new Mock<ISimpleContentType>();
        contentTypeMock.Setup(x => x.Key).Returns(Guid.NewGuid());
        var contentMock = new Mock<IContent>();
        contentMock.Setup(x => x.ContentType).Returns(contentTypeMock.Object);
        contentMock.Setup(x => x.Name).Returns("Home");

        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(contentMock.Object);

        AIPropertyValueDispatchRequest? captured = null;
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AIPropertyValueDispatchRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("hello")));
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var args = new SetUmbracoContentValueArgs(
            key,
            [new UmbracoPropertyPathSegmentArg("title", null)],
            JsonDocument.Parse("\"hello\"").RootElement);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<SetUmbracoContentValueResult>();
        typed.Success.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured!.Operation.ShouldBe(AIPropertyOperation.SetValue);
        var argsObj = captured.Args.ShouldBeOfType<JsonObject>();
        argsObj["value"]!.GetValue<string>().ShouldBe("hello");
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("block");
    }
}
