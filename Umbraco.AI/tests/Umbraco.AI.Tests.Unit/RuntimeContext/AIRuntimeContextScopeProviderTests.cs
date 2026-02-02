using Microsoft.AspNetCore.Http;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Tests.Unit.RuntimeContext;

public class AIRuntimeContextScopeProviderTests
{
    private readonly DefaultHttpContext _httpContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AIRuntimeContextScopeProvider _provider;

    public AIRuntimeContextScopeProviderTests()
    {
        _httpContext = new DefaultHttpContext();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);
        _provider = new AIRuntimeContextScopeProvider(_httpContextAccessorMock.Object);
    }

    [Fact]
    public void CreateScope_WhenNoExistingScope_CreatesNewContext()
    {
        // Act
        using var scope = _provider.CreateScope();

        // Assert
        scope.ShouldNotBeNull();
        scope.Context.ShouldNotBeNull();
        scope.ParentContext.ShouldBeNull();
        scope.Depth.ShouldBe(1);
    }

    [Fact]
    public void CreateScope_WhenNested_CreatesNewIsolatedContext()
    {
        // Arrange
        using var outerScope = _provider.CreateScope();
        var outerContext = outerScope.Context;

        // Act
        using var innerScope = _provider.CreateScope();

        // Assert
        innerScope.Context.ShouldNotBeSameAs(outerContext);
        innerScope.ParentContext.ShouldBeSameAs(outerContext);
        innerScope.Depth.ShouldBe(2);
    }

    [Fact]
    public void Dispose_RestoresPreviousContext()
    {
        // Arrange
        using var outerScope = _provider.CreateScope();
        var outerContext = outerScope.Context;

        // Act
        var innerScope = _provider.CreateScope();
        _provider.Context.ShouldNotBeSameAs(outerContext);
        innerScope.Dispose();

        // Assert
        _provider.Context.ShouldBeSameAs(outerContext);
    }

    [Fact]
    public void CreateScope_DeepNesting_WorksCorrectly()
    {
        // Arrange & Act
        using var scope1 = _provider.CreateScope();
        using var scope2 = _provider.CreateScope();
        using var scope3 = _provider.CreateScope();

        // Assert
        scope1.Depth.ShouldBe(1);
        scope1.ParentContext.ShouldBeNull();

        scope2.Depth.ShouldBe(2);
        scope2.ParentContext.ShouldBeSameAs(scope1.Context);

        scope3.Depth.ShouldBe(3);
        scope3.ParentContext.ShouldBeSameAs(scope2.Context);

        _provider.Context.ShouldBeSameAs(scope3.Context);
    }

    [Fact]
    public void Dispose_AllScopes_ContextBecomesNull()
    {
        // Arrange
        var scope1 = _provider.CreateScope();
        var scope2 = _provider.CreateScope();

        // Act
        scope2.Dispose();
        scope1.Dispose();

        // Assert
        _provider.Context.ShouldBeNull();
    }

    [Fact]
    public void Dispose_OutOfOrder_DoesNotThrow()
    {
        // Arrange
        var scope1 = _provider.CreateScope();
        var scope2 = _provider.CreateScope();

        // Act - Dispose outer scope first (out of order)
        // This is a no-op since scope2 is at the top of the stack
        Should.NotThrow(() => scope1.Dispose());

        // Assert - Inner context should still be accessible
        _provider.Context.ShouldBeSameAs(scope2.Context);

        // Dispose inner scope
        scope2.Dispose();

        // Note: The context is now scope1's context since scope1's dispose was a no-op.
        // This is expected behavior for out-of-order disposal - it's handled gracefully
        // (no crash) but the stack doesn't perfectly clean up.
        _provider.Context.ShouldBeSameAs(scope1.Context);
    }

    [Fact]
    public void CreateScope_NoHttpContext_ReturnsDetachedScope()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        using var scope = _provider.CreateScope();

        // Assert
        scope.ShouldNotBeNull();
        scope.Context.ShouldNotBeNull();
        scope.ParentContext.ShouldBeNull();
        scope.Depth.ShouldBe(1);
    }

    [Fact]
    public void CreateScope_WithItems_PopulatesContext()
    {
        // Arrange
        var items = new[]
        {
            new AIRequestContextItem { Description = "Description 1", Value = "value1" },
            new AIRequestContextItem { Description = "Description 2", Value = "value2" }
        };

        // Act
        using var scope = _provider.CreateScope(items);

        // Assert
        scope.Context.RequestContextItems.Count.ShouldBe(2);
        scope.Context.RequestContextItems[0].Description.ShouldBe("Description 1");
        scope.Context.RequestContextItems[1].Description.ShouldBe("Description 2");
    }

    [Fact]
    public void NestedScope_DataChanges_DoNotAffectParent()
    {
        // Arrange
        using var outerScope = _provider.CreateScope();
        outerScope.Context.SetValue("test", "outer-value");

        // Act
        using var innerScope = _provider.CreateScope();
        innerScope.Context.SetValue("test", "inner-value");

        // Assert
        outerScope.Context.GetValue<string>("test").ShouldBe("outer-value");
        innerScope.Context.GetValue<string>("test").ShouldBe("inner-value");
    }

    [Fact]
    public void Context_ReturnsCurrentScopeContext()
    {
        // Arrange
        using var scope1 = _provider.CreateScope();
        _provider.Context.ShouldBeSameAs(scope1.Context);

        using var scope2 = _provider.CreateScope();
        _provider.Context.ShouldBeSameAs(scope2.Context);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var scope = _provider.CreateScope();

        // Act & Assert
        Should.NotThrow(() =>
        {
            scope.Dispose();
            scope.Dispose();
            scope.Dispose();
        });
    }

    [Fact]
    public void DetachedScope_Dispose_DoesNotThrow()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var scope = _provider.CreateScope();

        // Act & Assert
        Should.NotThrow(() => scope.Dispose());
    }
}
