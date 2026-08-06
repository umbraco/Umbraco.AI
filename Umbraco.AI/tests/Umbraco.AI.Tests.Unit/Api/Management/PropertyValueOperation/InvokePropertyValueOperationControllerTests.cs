using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Web.Api.Management.PropertyValueOperation.Controllers;
using Umbraco.AI.Web.Api.Management.PropertyValueOperation.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Tests.Unit.Api.Management.PropertyValueOperation;

public class InvokePropertyValueOperationControllerTests
{
    private readonly Mock<IAIPropertyValueDispatcher> _dispatcherMock;
    private readonly InvokePropertyValueOperationController _controller;

    public InvokePropertyValueOperationControllerTests()
    {
        _dispatcherMock = new Mock<IAIPropertyValueDispatcher>();
        _controller = new InvokePropertyValueOperationController(_dispatcherMock.Object);
    }

    [Fact]
    public async Task Invoke_PassesRequestToDispatcher_AndReturnsResponse()
    {
        // Arrange
        var newRoot = new JsonObject { ["items"] = new JsonArray() };
        var blockKey = Guid.NewGuid();

        _dispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(newRoot, blockKey));

        var request = new PropertyValueOperationRequestModel
        {
            Path = new List<AIPropertyPathSegment> { AIPropertyPathSegment.ForProperty("contentBlocks") },
            Operation = AIPropertyOperation.AddItem,
            DocumentMetadata = new AIDocumentMetadata(
                ContentTypeKey: Guid.NewGuid(),
                Variants: [new AIVariantId(null, null)],
                IsVariant: false,
                IsSegmented: false),
        };

        // Act
        var result = await _controller.Invoke(request);

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<PropertyValueOperationResponseModel>();
        response.Success.ShouldBeTrue();
        response.NewRootValue.ShouldBe(newRoot);
        response.BlockKey.ShouldBe(blockKey);
        response.Error.ShouldBeNull();
    }

    [Fact]
    public async Task Invoke_OnDispatcherFailure_ReturnsErrorPayloadWithSuccessFalse()
    {
        // Arrange
        var error = new AIPropertyValueOperationError(
            AIPropertyValueOperationError.Codes.NoHandler,
            "no handler for foo");

        _dispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Fail(error));

        var request = new PropertyValueOperationRequestModel
        {
            Path = new List<AIPropertyPathSegment> { AIPropertyPathSegment.ForProperty("foo") },
            Operation = AIPropertyOperation.AddItem,
            DocumentMetadata = new AIDocumentMetadata(
                ContentTypeKey: Guid.NewGuid(),
                Variants: [new AIVariantId(null, null)],
                IsVariant: false,
                IsSegmented: false),
        };

        // Act
        var result = await _controller.Invoke(request);

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<PropertyValueOperationResponseModel>();
        response.Success.ShouldBeFalse();
        response.Error.ShouldBe(error);
        response.NewRootValue.ShouldBeNull();
    }

    /// <summary>
    /// Load-bearing test for the staged-value rule: the dispatcher MUST receive whatever rootValue
    /// the caller supplies, byte-for-byte. Any future change that re-reads the value from a
    /// repository inside the controller (or dispatcher) silently breaks the design that lets
    /// frontend tools transport unsaved staged changes through this endpoint.
    /// </summary>
    [Fact]
    public async Task Invoke_PassesSuppliedRootValueToDispatcher_VerbatimAndUnmodified()
    {
        // Arrange
        // The supplied root value contains a marker the dispatcher must see. If the controller
        // ever swaps it for something else (e.g. re-fetches from DB), this test fails.
        var stagedValue = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["__stagedMarker__"] = "user-typed-this-but-has-not-saved",
        };

        AIPropertyValueDispatchRequest? captured = null;
        _dispatcherMock
            .Setup(d => d.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AIPropertyValueDispatchRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(null));

        var request = new PropertyValueOperationRequestModel
        {
            Path = new List<AIPropertyPathSegment> { AIPropertyPathSegment.ForProperty("rows") },
            Operation = AIPropertyOperation.AddItem,
            RootValue = stagedValue,
            DocumentMetadata = new AIDocumentMetadata(
                ContentTypeKey: Guid.NewGuid(),
                Variants: [new AIVariantId(null, null)],
                IsVariant: false,
                IsSegmented: false),
        };

        // Act
        await _controller.Invoke(request);

        // Assert
        captured.ShouldNotBeNull();
        captured!.RootValue.ShouldBe(stagedValue);
        captured.RootValue!["__stagedMarker__"]!.GetValue<string>()
            .ShouldBe("user-typed-this-but-has-not-saved");
    }

    [Fact]
    public async Task Invoke_NullBody_ReturnsBadRequest()
    {
        // Arrange & Act
        var result = await _controller.Invoke(null!);

        // Assert
        result.Result.ShouldBeOfType<BadRequestResult>();
    }

    /// <summary>
    /// Load-bearing test for the auth boundary (issue #306): this endpoint is a pure transform over a
    /// caller-supplied value, so it must NOT require the AI section. Requiring it forces editors who
    /// use AI features in the content workspace to also be granted full AI administration access.
    /// </summary>
    [Fact]
    public void Controller_DoesNotRequireAISectionAccess()
    {
        // Arrange & Act
        var policies = typeof(InvokePropertyValueOperationController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(a => a.Policy)
            .ToList();

        // Assert
        policies.ShouldNotContain(AIAuthorizationPolicies.SectionAccessAI);
    }

    /// <summary>
    /// The endpoint must still be behind backoffice authentication — dropping
    /// <c>SectionAccessAI</c> must not leave it anonymous.
    /// </summary>
    [Fact]
    public void Controller_StillRequiresBackOfficeAccess()
    {
        // Arrange & Act
        var policies = typeof(InvokePropertyValueOperationController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(a => a.Policy)
            .ToList();

        // Assert
        policies.ShouldContain(AuthorizationPolicies.BackOfficeAccess);
    }
}
