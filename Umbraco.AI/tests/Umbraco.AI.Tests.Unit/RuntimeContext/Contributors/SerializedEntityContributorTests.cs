using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.RuntimeContext.Contributors;

namespace Umbraco.AI.Tests.Unit.RuntimeContext.Contributors;

public class SerializedEntityContributorTests
{
    private readonly Mock<IAIEntityContextHelper> _contextHelperMock;
    private readonly SerializedEntityContributor _contributor;

    public SerializedEntityContributorTests()
    {
        _contextHelperMock = new Mock<IAIEntityContextHelper>();
        _contributor = new SerializedEntityContributor(_contextHelperMock.Object);
    }

    [Fact]
    public void Contribute_WithValidSerializedEntity_ProcessesEntity()
    {
        // Arrange
        var entityJson = """
            {
                "entityType": "document",
                "unique": "doc-123",
                "name": "Test Document",
                "data": {
                    "contentType": "blogPost",
                    "properties": []
                }
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Key = "entity",
            Type = "entity",
            Value = entityJson
        };

        var context = new AIRuntimeContext([contextItem]);

        _contextHelperMock
            .Setup(x => x.BuildContextDictionary(It.IsAny<AISerializedEntity>()))
            .Returns(new Dictionary<string, object?> { ["test"] = "value" });

        _contextHelperMock
            .Setup(x => x.FormatForLlm(It.IsAny<AISerializedEntity>()))
            .Returns("Formatted entity context");

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts.Count.ShouldBe(1);
        context.SystemMessageParts[0].ShouldBe("Formatted entity context");
        context.Variables.ShouldContainKey("test");
        context.Data.ShouldContainKey(Constants.ContextKeys.EntityType);
    }

    [Fact]
    public void Contribute_WithValidEntity_ExtractsEntityId()
    {
        // Arrange
        var entityGuid = Guid.NewGuid();
        var entityJson = $$"""
            {
                "entityType": "document",
                "unique": "{{entityGuid}}",
                "name": "Test",
                "data": {}
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Key = "entity",
            Type = "entity",
            Value = entityJson
        };

        var context = new AIRuntimeContext([contextItem]);

        _contextHelperMock
            .Setup(x => x.BuildContextDictionary(It.IsAny<AISerializedEntity>()))
            .Returns([]);

        _contextHelperMock
            .Setup(x => x.FormatForLlm(It.IsAny<AISerializedEntity>()))
            .Returns("Test");

        // Act
        _contributor.Contribute(context);

        // Assert
        context.Data.ShouldContainKey(Constants.ContextKeys.EntityId);
        context.Data[Constants.ContextKeys.EntityId].ShouldBe(entityGuid);
    }

    [Fact]
    public void Contribute_WithParentUnique_ExtractsParentEntityId()
    {
        // Arrange
        var parentGuid = Guid.NewGuid();
        var entityJson = $$"""
            {
                "entityType": "document",
                "unique": "new",
                "name": "New Document",
                "parentUnique": "{{parentGuid}}",
                "data": {}
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Key = "entity",
            Type = "entity",
            Value = entityJson
        };

        var context = new AIRuntimeContext([contextItem]);

        _contextHelperMock
            .Setup(x => x.BuildContextDictionary(It.IsAny<AISerializedEntity>()))
            .Returns([]);

        _contextHelperMock
            .Setup(x => x.FormatForLlm(It.IsAny<AISerializedEntity>()))
            .Returns("Test");

        // Act
        _contributor.Contribute(context);

        // Assert
        context.Data.ShouldContainKey(Constants.ContextKeys.ParentEntityId);
        context.Data[Constants.ContextKeys.ParentEntityId].ShouldBe(parentGuid);
    }

    [Fact]
    public void Contribute_WithMissingDataField_DoesNotProcess()
    {
        // Arrange - missing data field
        var entityJson = """
            {
                "entityType": "document",
                "unique": "doc-123",
                "name": "Test"
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Key = "entity",
            Type = "entity",
            Value = entityJson
        };

        var context = new AIRuntimeContext([contextItem]);

        // Act
        _contributor.Contribute(context);

        // Assert - should not process entity without data field
        context.SystemMessageParts.Count.ShouldBe(0);
        _contextHelperMock.Verify(x => x.FormatForLlm(It.IsAny<AISerializedEntity>()), Times.Never);
    }

    [Fact]
    public void Contribute_WithDataNotAnObject_DoesNotProcess()
    {
        // Arrange - data is a string, not an object
        var entityJson = """
            {
                "entityType": "document",
                "unique": "doc-123",
                "name": "Test",
                "data": "not-an-object"
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Key = "entity",
            Type = "entity",
            Value = entityJson
        };

        var context = new AIRuntimeContext([contextItem]);

        // Act
        _contributor.Contribute(context);

        // Assert - should not process
        context.SystemMessageParts.Count.ShouldBe(0);
    }

    [Fact]
    public void Contribute_WithInvalidJson_DoesNotProcess()
    {
        // Arrange
        var contextItem = new AIRequestContextItem
        {
            Key = "entity",
            Type = "entity",
            Value = "{ invalid json }"
        };

        var context = new AIRuntimeContext([contextItem]);

        // Act
        _contributor.Contribute(context);

        // Assert - should silently ignore
        context.SystemMessageParts.Count.ShouldBe(0);
    }

    [Fact]
    public void Contribute_WithNonJsonValue_DoesNotProcess()
    {
        // Arrange
        var contextItem = new AIRequestContextItem
        {
            Key = "entity",
            Type = "entity",
            Value = "plain text, not json"
        };

        var context = new AIRuntimeContext([contextItem]);

        // Act
        _contributor.Contribute(context);

        // Assert - should not process
        context.SystemMessageParts.Count.ShouldBe(0);
    }

    [Fact]
    public void Contribute_WithEmptyEntityType_DoesNotProcess()
    {
        // Arrange
        var entityJson = """
            {
                "entityType": "",
                "unique": "doc-123",
                "name": "Test",
                "data": {}
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Key = "entity",
            Type = "entity",
            Value = entityJson
        };

        var context = new AIRuntimeContext([contextItem]);

        // Act
        _contributor.Contribute(context);

        // Assert - should not process entity with empty required fields
        context.SystemMessageParts.Count.ShouldBe(0);
    }

    [Fact]
    public void Contribute_CallsContextHelperMethods()
    {
        // Arrange
        var entityJson = """
            {
                "entityType": "product",
                "unique": "prod-456",
                "name": "Widget",
                "data": {
                    "sku": "12345"
                }
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Key = "entity",
            Type = "entity",
            Value = entityJson
        };

        var context = new AIRuntimeContext([contextItem]);

        var testVariables = new Dictionary<string, object?> { ["sku"] = "12345" };
        _contextHelperMock
            .Setup(x => x.BuildContextDictionary(It.IsAny<AISerializedEntity>()))
            .Returns(testVariables);

        _contextHelperMock
            .Setup(x => x.FormatForLlm(It.IsAny<AISerializedEntity>()))
            .Returns("Formatted output");

        // Act
        _contributor.Contribute(context);

        // Assert - verify both methods were called
        _contextHelperMock.Verify(x => x.BuildContextDictionary(It.Is<AISerializedEntity>(e =>
            e.EntityType == "product" &&
            e.Unique == "prod-456" &&
            e.Name == "Widget")), Times.Once);

        _contextHelperMock.Verify(x => x.FormatForLlm(It.Is<AISerializedEntity>(e =>
            e.EntityType == "product")), Times.Once);

        context.Variables["sku"].ShouldBe("12345");
    }
}
