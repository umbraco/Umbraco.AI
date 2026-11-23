using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.PropertyEditors;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.AI.Tests.Unit.PropertyEditors;

public class AIContextPickerPropertyValueConverterTests
{
    private readonly Mock<IAIContextService> _contextServiceMock = new();
    private readonly AIContextPickerPropertyValueConverter _converter;

    public AIContextPickerPropertyValueConverterTests()
    {
        _converter = new AIContextPickerPropertyValueConverter(_contextServiceMock.Object);
    }

    [Fact]
    public void ConvertIntermediateToObject_Multiple_LoadsAllIdsInOneCallAndKeepsSelectionOrder()
    {
        // Arrange
        var first = new AIContextBuilder().WithId(Guid.NewGuid()).WithAlias("first").Build();
        var second = new AIContextBuilder().WithId(Guid.NewGuid()).WithAlias("second").Build();
        var third = new AIContextBuilder().WithId(Guid.NewGuid()).WithAlias("third").Build();
        Guid[] ids = [third.Id, first.Id, second.Id];

        // The service returns contexts in a different order to the selection
        _contextServiceMock
            .Setup(x => x.GetContextsByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { first, second, third });

        // Act
        var result = Convert(ids, multiple: true);

        // Assert
        var contexts = result.ShouldBeAssignableTo<IEnumerable<AIContext>>()!.ToList();
        contexts.Select(c => c.Alias).ShouldBe(["third", "first", "second"]);
        _contextServiceMock.Verify(
            x => x.GetContextsByIdsAsync(ids, It.IsAny<CancellationToken>()),
            Times.Once);
        _contextServiceMock.Verify(
            x => x.GetContextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void ConvertIntermediateToObject_Multiple_SkipsUnknownIds()
    {
        // Arrange
        var known = new AIContextBuilder().WithId(Guid.NewGuid()).WithAlias("known").Build();
        Guid[] ids = [Guid.NewGuid(), known.Id, Guid.NewGuid()];

        _contextServiceMock
            .Setup(x => x.GetContextsByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { known });

        // Act
        var result = Convert(ids, multiple: true);

        // Assert
        var contexts = result.ShouldBeAssignableTo<IEnumerable<AIContext>>()!.ToList();
        contexts.Count.ShouldBe(1);
        contexts[0].Id.ShouldBe(known.Id);
    }

    [Fact]
    public void ConvertIntermediateToObject_Multiple_KeepsDuplicateSelections()
    {
        // Arrange
        var context = new AIContextBuilder().WithId(Guid.NewGuid()).WithAlias("dup").Build();
        Guid[] ids = [context.Id, context.Id];

        _contextServiceMock
            .Setup(x => x.GetContextsByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { context });

        // Act
        var result = Convert(ids, multiple: true);

        // Assert
        var contexts = result.ShouldBeAssignableTo<IEnumerable<AIContext>>()!.ToList();
        contexts.Count.ShouldBe(2);
        contexts.ShouldAllBe(c => c.Id == context.Id);
    }

    [Fact]
    public void ConvertIntermediateToObject_Single_ReturnsFirstFoundContext()
    {
        // Arrange
        var context = new AIContextBuilder().WithId(Guid.NewGuid()).WithAlias("single").Build();

        _contextServiceMock
            .Setup(x => x.GetContextsByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { context });

        // Act
        var result = Convert(new[] { context.Id }, multiple: false);

        // Assert
        var single = result.ShouldBeOfType<AIContext>();
        single.Id.ShouldBe(context.Id);
    }

    [Fact]
    public void ConvertIntermediateToObject_Single_WhenContextMissing_ReturnsNull()
    {
        // Arrange
        _contextServiceMock
            .Setup(x => x.GetContextsByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AIContext>());

        // Act
        var result = Convert(new[] { Guid.NewGuid() }, multiple: false);

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConvertIntermediateToObject_WithNoIds_DoesNotQueryContexts(bool multiple)
    {
        // Act
        var result = Convert(Array.Empty<Guid>(), multiple);

        // Assert
        if (multiple)
        {
            result.ShouldBeAssignableTo<IEnumerable<AIContext>>()!.ShouldBeEmpty();
        }
        else
        {
            result.ShouldBeNull();
        }

        _contextServiceMock.VerifyNoOtherCalls();
    }

    private object? Convert(Guid[] ids, bool multiple)
    {
        var config = new Dictionary<string, object> { ["multiple"] = multiple };
        var dataType = new PublishedDataType(
            1,
            Umbraco.AI.Core.Constants.PropertyEditors.Aliases.ContextPicker,
            Umbraco.AI.Core.Constants.PropertyEditors.Aliases.ContextPicker,
            new Lazy<object?>(() => config));

        var propertyType = new Mock<IPublishedPropertyType>();
        propertyType.SetupGet(x => x.DataType).Returns(dataType);

        return _converter.ConvertIntermediateToObject(
            Mock.Of<IPublishedElement>(),
            propertyType.Object,
            PropertyCacheLevel.Element,
            ids,
            preview: false);
    }
}
