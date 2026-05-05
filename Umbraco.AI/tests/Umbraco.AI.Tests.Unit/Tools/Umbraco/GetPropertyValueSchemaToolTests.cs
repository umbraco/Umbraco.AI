using System.Text.Json.Nodes;

using Moq;
using Shouldly;

using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetPropertyValueSchemaToolTests
{
    private readonly Mock<IPropertyEditorSchemaService> _propertyEditorSchemaServiceMock = new();
    private readonly IAITool _tool;

    public GetPropertyValueSchemaToolTests()
    {
        _tool = new GetPropertyValueSchemaTool(_propertyEditorSchemaServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(
            new GetPropertyValueSchemaArgs(Guid.Empty),
            CancellationToken.None);

        var typed = result.ShouldBeOfType<GetPropertyValueSchemaResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldNotBeNull();
        typed.Message.ShouldContain("empty");
        _propertyEditorSchemaServiceMock.Verify(
            x => x.GetSchemaAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithKnownKey_ReturnsSchema()
    {
        var key = Guid.NewGuid();
        var schema = JsonNode.Parse("""{ "type": "object", "properties": { "value": { "type": "string" } } }""")!.AsObject();

        _propertyEditorSchemaServiceMock
            .Setup(x => x.GetSchemaAsync(key))
            .ReturnsAsync(Attempt.SucceedWithStatus(
                PropertyEditorSchemaOperationStatus.Success,
                new PropertyValueSchema(typeof(string), schema)));

        var result = await _tool.ExecuteAsync(
            new GetPropertyValueSchemaArgs(key),
            CancellationToken.None);

        var typed = result.ShouldBeOfType<GetPropertyValueSchemaResult>();
        typed.Success.ShouldBeTrue();
        typed.DataTypeKey.ShouldBe(key);
        typed.ValueClrTypeName.ShouldBe(typeof(string).FullName);
        typed.ValueSchema.ShouldNotBeNull();
        typed.ValueSchema!["type"]?.GetValue<string>().ShouldBe("object");
        typed.Message.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_DataTypeNotFound_ReturnsFriendlyMessage()
    {
        var key = Guid.NewGuid();
        _propertyEditorSchemaServiceMock
            .Setup(x => x.GetSchemaAsync(key))
            .ReturnsAsync(Attempt.FailWithStatus(
                PropertyEditorSchemaOperationStatus.DataTypeNotFound,
                new PropertyValueSchema(null, null)));

        var result = await _tool.ExecuteAsync(
            new GetPropertyValueSchemaArgs(key),
            CancellationToken.None);

        var typed = result.ShouldBeOfType<GetPropertyValueSchemaResult>();
        typed.Success.ShouldBeFalse();
        typed.ValueSchema.ShouldBeNull();
        typed.Message.ShouldNotBeNull();
        typed.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_SchemaNotSupported_ReturnsFriendlyMessage()
    {
        var key = Guid.NewGuid();
        _propertyEditorSchemaServiceMock
            .Setup(x => x.GetSchemaAsync(key))
            .ReturnsAsync(Attempt.FailWithStatus(
                PropertyEditorSchemaOperationStatus.SchemaNotSupported,
                new PropertyValueSchema(null, null)));

        var result = await _tool.ExecuteAsync(
            new GetPropertyValueSchemaArgs(key),
            CancellationToken.None);

        var typed = result.ShouldBeOfType<GetPropertyValueSchemaResult>();
        typed.Success.ShouldBeFalse();
        typed.ValueSchema.ShouldBeNull();
        typed.Message.ShouldNotBeNull();
        typed.Message.ShouldContain("does not expose");
    }

    [Fact]
    public void Description_MentionsValueSchemaAndDataTypeKey()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("JSON Schema");
        description.ShouldContain("DataTypeKey");
    }
}
