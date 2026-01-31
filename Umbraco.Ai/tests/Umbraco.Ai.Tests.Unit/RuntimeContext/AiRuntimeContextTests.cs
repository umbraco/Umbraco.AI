using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Tests.Unit.RuntimeContext;

public class AiRuntimeContextTests
{
    [Fact]
    public void HandleRequestContextItem_WhenMatchFound_InvokesHandlerAndReturnsTrue()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "Item 1", Value = "value1" },
            new AiRequestContextItem { Description = "Item 2", Value = "value2" }
        };
        var context = new AiRuntimeContext(items);
        var handledItem = (AiRequestContextItem?)null;

        // Act
        var result = context.HandleRequestContextItem(
            item => item.Description == "Item 2",
            item => handledItem = item);

        // Assert
        result.ShouldBeTrue();
        handledItem.ShouldNotBeNull();
        handledItem.Description.ShouldBe("Item 2");
    }

    [Fact]
    public void HandleRequestContextItem_WhenMatchFound_MarksItemAsHandled()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "Item 1", Value = "value1" }
        };
        var context = new AiRuntimeContext(items);

        // Act
        context.HandleRequestContextItem(
            item => item.Description == "Item 1",
            _ => { });

        // Assert
        context.IsRequestContextItemHandled(items[0]).ShouldBeTrue();
        context.HandledRequestContextItemCount.ShouldBe(1);
    }

    [Fact]
    public void HandleRequestContextItem_WhenNoMatch_ReturnsFalseAndDoesNotInvokeHandler()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "Item 1", Value = "value1" }
        };
        var context = new AiRuntimeContext(items);
        var handlerInvoked = false;

        // Act
        var result = context.HandleRequestContextItem(
            item => item.Description == "Nonexistent",
            _ => handlerInvoked = true);

        // Assert
        result.ShouldBeFalse();
        handlerInvoked.ShouldBeFalse();
        context.HandledRequestContextItemCount.ShouldBe(0);
    }

    [Fact]
    public void HandleRequestContextItem_SkipsAlreadyHandledItems()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "Item 1", Value = "value1" },
            new AiRequestContextItem { Description = "Item 1", Value = "value2" }
        };
        var context = new AiRuntimeContext(items);
        var handledValues = new List<string?>();

        // Act - Handle first matching item
        context.HandleRequestContextItem(
            item => item.Description == "Item 1",
            item => handledValues.Add(item.Value));

        // Act - Try to handle again (should get the second one)
        context.HandleRequestContextItem(
            item => item.Description == "Item 1",
            item => handledValues.Add(item.Value));

        // Assert
        handledValues.Count.ShouldBe(2);
        handledValues[0].ShouldBe("value1");
        handledValues[1].ShouldBe("value2");
    }

    [Fact]
    public void HandleRequestContextItems_InvokesHandlerForAllMatches()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "TypeA", Value = "value1" },
            new AiRequestContextItem { Description = "TypeB", Value = "value2" },
            new AiRequestContextItem { Description = "TypeA", Value = "value3" }
        };
        var context = new AiRuntimeContext(items);
        var handledValues = new List<string?>();

        // Act
        context.HandleRequestContextItems(
            item => item.Description == "TypeA",
            item => handledValues.Add(item.Value));

        // Assert
        handledValues.Count.ShouldBe(2);
        handledValues.ShouldContain("value1");
        handledValues.ShouldContain("value3");
    }

    [Fact]
    public void HandleRequestContextItems_MarksAllMatchedItemsAsHandled()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "TypeA", Value = "value1" },
            new AiRequestContextItem { Description = "TypeB", Value = "value2" },
            new AiRequestContextItem { Description = "TypeA", Value = "value3" }
        };
        var context = new AiRuntimeContext(items);

        // Act
        context.HandleRequestContextItems(
            item => item.Description == "TypeA",
            _ => { });

        // Assert
        context.IsRequestContextItemHandled(items[0]).ShouldBeTrue();
        context.IsRequestContextItemHandled(items[1]).ShouldBeFalse();
        context.IsRequestContextItemHandled(items[2]).ShouldBeTrue();
        context.HandledRequestContextItemCount.ShouldBe(2);
    }

    [Fact]
    public void HandleRequestContextItems_SkipsAlreadyHandledItems()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "TypeA", Value = "value1" },
            new AiRequestContextItem { Description = "TypeA", Value = "value2" }
        };
        var context = new AiRuntimeContext(items);

        // Handle first item individually
        context.HandleRequestContextItem(
            item => item.Description == "TypeA",
            _ => { });

        var handledValues = new List<string?>();

        // Act - HandleRequestContextItems should skip the already handled item
        context.HandleRequestContextItems(
            item => item.Description == "TypeA",
            item => handledValues.Add(item.Value));

        // Assert
        handledValues.Count.ShouldBe(1);
        handledValues[0].ShouldBe("value2");
    }

    [Fact]
    public void HandleUnhandledRequestContextItems_InvokesHandlerForAllUnhandled()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "Item 1", Value = "value1" },
            new AiRequestContextItem { Description = "Item 2", Value = "value2" },
            new AiRequestContextItem { Description = "Item 3", Value = "value3" }
        };
        var context = new AiRuntimeContext(items);

        // Handle first item
        context.HandleRequestContextItem(
            item => item.Description == "Item 1",
            _ => { });

        var handledValues = new List<string?>();

        // Act
        context.HandleUnhandledRequestContextItems(item => handledValues.Add(item.Value));

        // Assert
        handledValues.Count.ShouldBe(2);
        handledValues.ShouldContain("value2");
        handledValues.ShouldContain("value3");
    }

    [Fact]
    public void HandleUnhandledRequestContextItems_MarksAllItemsAsHandled()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "Item 1", Value = "value1" },
            new AiRequestContextItem { Description = "Item 2", Value = "value2" }
        };
        var context = new AiRuntimeContext(items);

        // Act
        context.HandleUnhandledRequestContextItems(_ => { });

        // Assert
        context.IsRequestContextItemHandled(items[0]).ShouldBeTrue();
        context.IsRequestContextItemHandled(items[1]).ShouldBeTrue();
        context.HandledRequestContextItemCount.ShouldBe(2);
    }

    [Fact]
    public void HandleUnhandledRequestContextItems_WhenAllHandled_DoesNotInvokeHandler()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "Item 1", Value = "value1" }
        };
        var context = new AiRuntimeContext(items);

        // Handle all items first
        context.HandleUnhandledRequestContextItems(_ => { });

        var handlerInvoked = false;

        // Act
        context.HandleUnhandledRequestContextItems(_ => handlerInvoked = true);

        // Assert
        handlerInvoked.ShouldBeFalse();
    }

    [Fact]
    public void IsRequestContextItemHandled_ReturnsTrueForHandledItem()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "Item 1", Value = "value1" }
        };
        var context = new AiRuntimeContext(items);

        context.HandleRequestContextItem(
            item => item.Description == "Item 1",
            _ => { });

        // Act & Assert
        context.IsRequestContextItemHandled(items[0]).ShouldBeTrue();
    }

    [Fact]
    public void IsRequestContextItemHandled_ReturnsFalseForUnhandledItem()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "Item 1", Value = "value1" }
        };
        var context = new AiRuntimeContext(items);

        // Act & Assert
        context.IsRequestContextItemHandled(items[0]).ShouldBeFalse();
    }

    [Fact]
    public void HandledRequestContextItemCount_ReturnsCorrectCount()
    {
        // Arrange
        var items = new[]
        {
            new AiRequestContextItem { Description = "Item 1", Value = "value1" },
            new AiRequestContextItem { Description = "Item 2", Value = "value2" },
            new AiRequestContextItem { Description = "Item 3", Value = "value3" }
        };
        var context = new AiRuntimeContext(items);

        // Act
        context.HandleRequestContextItem(item => item.Description == "Item 1", _ => { });
        context.HandleRequestContextItem(item => item.Description == "Item 2", _ => { });

        // Assert
        context.HandledRequestContextItemCount.ShouldBe(2);
    }

    [Fact]
    public void HandleRequestContextItem_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new AiRuntimeContext([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            context.HandleRequestContextItem(null!, _ => { }));
    }

    [Fact]
    public void HandleRequestContextItem_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new AiRuntimeContext([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            context.HandleRequestContextItem(_ => true, null!));
    }

    [Fact]
    public void HandleRequestContextItems_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new AiRuntimeContext([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            context.HandleRequestContextItems(null!, _ => { }));
    }

    [Fact]
    public void HandleRequestContextItems_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new AiRuntimeContext([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            context.HandleRequestContextItems(_ => true, null!));
    }

    [Fact]
    public void HandleUnhandledRequestContextItems_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new AiRuntimeContext([]);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            context.HandleUnhandledRequestContextItems(null!));
    }
}
