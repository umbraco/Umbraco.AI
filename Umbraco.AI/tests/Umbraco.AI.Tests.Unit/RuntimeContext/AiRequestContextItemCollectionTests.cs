using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Tests.Unit.RuntimeContext;

public class AIRequestContextItemCollectionTests
{
    [Fact]
    public void Handle_WhenMatchFound_InvokesHandlerAndReturnsTrue()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" },
            new AIRequestContextItem { Description = "Item 2", Value = "value2" }
        };
        var collection = new AIRequestContextItemCollection(items);
        var handledItem = (AIRequestContextItem?)null;

        // Act
        var result = collection.Handle(
            item => item.Description == "Item 2",
            item => handledItem = item);

        // Assert
        result.ShouldBeTrue();
        handledItem.ShouldNotBeNull();
        handledItem.Description.ShouldBe("Item 2");
    }

    [Fact]
    public void Handle_WhenMatchFound_MarksItemAsHandled()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" }
        };
        var collection = new AIRequestContextItemCollection(items);

        // Act
        collection.Handle(
            item => item.Description == "Item 1",
            _ => { });

        // Assert
        collection.IsHandled(items[0]).ShouldBeTrue();
        collection.HandledCount.ShouldBe(1);
    }

    [Fact]
    public void Handle_WhenNoMatch_ReturnsFalseAndDoesNotInvokeHandler()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" }
        };
        var collection = new AIRequestContextItemCollection(items);
        var handlerInvoked = false;

        // Act
        var result = collection.Handle(
            item => item.Description == "Nonexistent",
            _ => handlerInvoked = true);

        // Assert
        result.ShouldBeFalse();
        handlerInvoked.ShouldBeFalse();
        collection.HandledCount.ShouldBe(0);
    }

    [Fact]
    public void Handle_SkipsAlreadyHandledItems()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" },
            new AIRequestContextItem { Description = "Item 1", Value = "value2" }
        };
        var collection = new AIRequestContextItemCollection(items);
        var handledValues = new List<string?>();

        // Act - Handle first matching item
        collection.Handle(
            item => item.Description == "Item 1",
            item => handledValues.Add(item.Value));

        // Act - Try to handle again (should get the second one)
        collection.Handle(
            item => item.Description == "Item 1",
            item => handledValues.Add(item.Value));

        // Assert
        handledValues.Count.ShouldBe(2);
        handledValues[0].ShouldBe("value1");
        handledValues[1].ShouldBe("value2");
    }

    [Fact]
    public void HandleAll_InvokesHandlerForAllMatches()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "TypeA", Value = "value1" },
            new AIRequestContextItem { Description = "TypeB", Value = "value2" },
            new AIRequestContextItem { Description = "TypeA", Value = "value3" }
        };
        var collection = new AIRequestContextItemCollection(items);
        var handledValues = new List<string?>();

        // Act
        collection.HandleAll(
            item => item.Description == "TypeA",
            item => handledValues.Add(item.Value));

        // Assert
        handledValues.Count.ShouldBe(2);
        handledValues.ShouldContain("value1");
        handledValues.ShouldContain("value3");
    }

    [Fact]
    public void HandleAll_MarksAllMatchedItemsAsHandled()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "TypeA", Value = "value1" },
            new AIRequestContextItem { Description = "TypeB", Value = "value2" },
            new AIRequestContextItem { Description = "TypeA", Value = "value3" }
        };
        var collection = new AIRequestContextItemCollection(items);

        // Act
        collection.HandleAll(
            item => item.Description == "TypeA",
            _ => { });

        // Assert
        collection.IsHandled(items[0]).ShouldBeTrue();
        collection.IsHandled(items[1]).ShouldBeFalse();
        collection.IsHandled(items[2]).ShouldBeTrue();
        collection.HandledCount.ShouldBe(2);
    }

    [Fact]
    public void HandleAll_SkipsAlreadyHandledItems()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "TypeA", Value = "value1" },
            new AIRequestContextItem { Description = "TypeA", Value = "value2" }
        };
        var collection = new AIRequestContextItemCollection(items);

        // Handle first item individually
        collection.Handle(
            item => item.Description == "TypeA",
            _ => { });

        var handledValues = new List<string?>();

        // Act - HandleAll should skip the already handled item
        collection.HandleAll(
            item => item.Description == "TypeA",
            item => handledValues.Add(item.Value));

        // Assert
        handledValues.Count.ShouldBe(1);
        handledValues[0].ShouldBe("value2");
    }

    [Fact]
    public void HandleUnhandled_InvokesHandlerForAllUnhandled()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" },
            new AIRequestContextItem { Description = "Item 2", Value = "value2" },
            new AIRequestContextItem { Description = "Item 3", Value = "value3" }
        };
        var collection = new AIRequestContextItemCollection(items);

        // Handle first item
        collection.Handle(
            item => item.Description == "Item 1",
            _ => { });

        var handledValues = new List<string?>();

        // Act
        collection.HandleUnhandled(item => handledValues.Add(item.Value));

        // Assert
        handledValues.Count.ShouldBe(2);
        handledValues.ShouldContain("value2");
        handledValues.ShouldContain("value3");
    }

    [Fact]
    public void HandleUnhandled_MarksAllItemsAsHandled()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" },
            new AIRequestContextItem { Description = "Item 2", Value = "value2" }
        };
        var collection = new AIRequestContextItemCollection(items);

        // Act
        collection.HandleUnhandled(_ => { });

        // Assert
        collection.IsHandled(items[0]).ShouldBeTrue();
        collection.IsHandled(items[1]).ShouldBeTrue();
        collection.HandledCount.ShouldBe(2);
    }

    [Fact]
    public void HandleUnhandled_WhenAllHandled_DoesNotInvokeHandler()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" }
        };
        var collection = new AIRequestContextItemCollection(items);

        // Handle all items first
        collection.HandleUnhandled(_ => { });

        var handlerInvoked = false;

        // Act
        collection.HandleUnhandled(_ => handlerInvoked = true);

        // Assert
        handlerInvoked.ShouldBeFalse();
    }

    [Fact]
    public void IsHandled_ReturnsTrueForHandledItem()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" }
        };
        var collection = new AIRequestContextItemCollection(items);

        collection.Handle(
            item => item.Description == "Item 1",
            _ => { });

        // Act & Assert
        collection.IsHandled(items[0]).ShouldBeTrue();
    }

    [Fact]
    public void IsHandled_ReturnsFalseForUnhandledItem()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" }
        };
        var collection = new AIRequestContextItemCollection(items);

        // Act & Assert
        collection.IsHandled(items[0]).ShouldBeFalse();
    }

    [Fact]
    public void HandledCount_ReturnsCorrectCount()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" },
            new AIRequestContextItem { Description = "Item 2", Value = "value2" },
            new AIRequestContextItem { Description = "Item 3", Value = "value3" }
        };
        var collection = new AIRequestContextItemCollection(items);

        // Act
        collection.Handle(item => item.Description == "Item 1", _ => { });
        collection.Handle(item => item.Description == "Item 2", _ => { });

        // Assert
        collection.HandledCount.ShouldBe(2);
    }

    [Fact]
    public void Handle_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = new AIRequestContextItemCollection([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            collection.Handle(null!, _ => { }));
    }

    [Fact]
    public void Handle_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = new AIRequestContextItemCollection([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            collection.Handle(_ => true, null!));
    }

    [Fact]
    public void HandleAll_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = new AIRequestContextItemCollection([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            collection.HandleAll(null!, _ => { }));
    }

    [Fact]
    public void HandleAll_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = new AIRequestContextItemCollection([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            collection.HandleAll(_ => true, null!));
    }

    [Fact]
    public void HandleUnhandled_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = new AIRequestContextItemCollection([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            collection.HandleUnhandled(null!));
    }

    [Fact]
    public void Count_ReturnsCorrectItemCount()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" },
            new AIRequestContextItem { Description = "Item 2", Value = "value2" }
        };
        var collection = new AIRequestContextItemCollection(items);

        // Act & Assert
        collection.Count.ShouldBe(2);
    }

    [Fact]
    public void Indexer_ReturnsCorrectItem()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" },
            new AIRequestContextItem { Description = "Item 2", Value = "value2" }
        };
        var collection = new AIRequestContextItemCollection(items);

        // Act & Assert
        collection[0].Description.ShouldBe("Item 1");
        collection[1].Description.ShouldBe("Item 2");
    }

    [Fact]
    public void GetEnumerator_IteratesAllItems()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Item 1", Value = "value1" },
            new AIRequestContextItem { Description = "Item 2", Value = "value2" }
        };
        var collection = new AIRequestContextItemCollection(items);
        var enumerated = new List<AIRequestContextItem>();

        // Act
        foreach (var item in collection)
        {
            enumerated.Add(item);
        }

        // Assert
        enumerated.Count.ShouldBe(2);
        enumerated[0].Description.ShouldBe("Item 1");
        enumerated[1].Description.ShouldBe("Item 2");
    }
}
