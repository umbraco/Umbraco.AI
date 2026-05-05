using Umbraco.AI.Core;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.RuntimeContext.Contributors;

namespace Umbraco.AI.Tests.Unit.RuntimeContext.Contributors;

public class SerializedElementContributorTests
{
    private readonly Mock<IAIEntityContextHelper> _contextHelperMock;
    private readonly SerializedElementContributor _contributor;

    public SerializedElementContributorTests()
    {
        _contextHelperMock = new Mock<IAIEntityContextHelper>();
        _contributor = new SerializedElementContributor(_contextHelperMock.Object);
    }

    [Fact]
    public void Contribute_WithValidSerializedElement_ProcessesElement()
    {
        // Arrange
        var elementJson = """
            {
                "elementType": "block",
                "unique": "block-123",
                "name": "Hero block",
                "data": {
                    "contentType": "heroBlock",
                    "properties": []
                }
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Description = "Test element",
            Value = elementJson
        };

        var context = new AIRuntimeContext([contextItem]);

        _contextHelperMock
            .Setup(x => x.BuildContextDictionary(It.IsAny<AISerializedEntity>()))
            .Returns(new Dictionary<string, object?> { ["heading"] = "Hello" });

        _contextHelperMock
            .Setup(x => x.FormatElementForLlm(It.IsAny<AISerializedEntity>()))
            .Returns("Formatted element context");

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts.Count.ShouldBe(1);
        context.SystemMessageParts[0].ShouldBe("Formatted element context");
        context.Variables["heading"].ShouldBe("Hello");
        context.Data.ShouldContainKey(Constants.ContextKeys.SerializedElement);
        context.Data[Constants.ContextKeys.ElementType].ShouldBe("block");
    }

    [Fact]
    public void Contribute_WithCultureField_PassesCultureToHelper()
    {
        // Regression test for the multi-variant prompt bug. Blocks inherit
        // their parent document's variant context; the contributor must
        // forward `culture`/`segment` to the helper so it can pick the
        // matching property entry rather than the last-iterated one.
        var elementJson = """
            {
                "elementType": "block",
                "unique": "block-multi-variant",
                "name": "Hero block",
                "culture": "sv-SE",
                "segment": null,
                "data": {
                    "contentType": "heroBlock",
                    "properties": [
                        { "alias": "heading", "value": "Svensk rubrik", "culture": "sv-SE", "segment": null },
                        { "alias": "heading", "value": "Deutsche Überschrift", "culture": "de-DE", "segment": null }
                    ]
                }
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Description = "Multi-variant block",
            Value = elementJson
        };

        var context = new AIRuntimeContext([contextItem]);

        _contextHelperMock
            .Setup(x => x.BuildContextDictionary(It.IsAny<AISerializedEntity>()))
            .Returns([]);

        _contextHelperMock
            .Setup(x => x.FormatElementForLlm(It.IsAny<AISerializedEntity>()))
            .Returns("formatted");

        // Act
        _contributor.Contribute(context);

        // Assert — entity passed to the helper carries Culture so it can pick
        // the matching property entry rather than the last-iterated one.
        _contextHelperMock.Verify(x => x.BuildContextDictionary(It.Is<AISerializedEntity>(e =>
            e.Culture == "sv-SE" &&
            e.Segment == null)), Times.Once);
    }

    [Fact]
    public void Contribute_WithMissingDataField_DoesNotProcess()
    {
        var elementJson = """
            {
                "elementType": "block",
                "unique": "block-123",
                "name": "Hero"
            }
            """;

        var contextItem = new AIRequestContextItem
        {
            Description = "Test element",
            Value = elementJson
        };

        var context = new AIRuntimeContext([contextItem]);

        _contributor.Contribute(context);

        context.SystemMessageParts.Count.ShouldBe(0);
        _contextHelperMock.Verify(x => x.FormatElementForLlm(It.IsAny<AISerializedEntity>()), Times.Never);
    }
}
