using System.Runtime.CompilerServices;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Tests.Unit.RuntimeContext;

public class AIRuntimeContextScopeProviderTests
{
    private readonly AIRuntimeContextScopeProvider _provider = new();

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
        _provider.Context.ShouldBeSameAs(scope.Context);
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
    public void Dispose_OutOfOrder_SkipsTheDisposedScope()
    {
        // Arrange
        var scope1 = _provider.CreateScope();
        var scope2 = _provider.CreateScope();

        // Act: dispose the outer scope first.
        Should.NotThrow(() => scope1.Dispose());

        // Assert: the inner scope stays current, then nothing is, as the outer scope is already disposed.
        _provider.Context.ShouldBeSameAs(scope2.Context);
        scope2.ParentContext.ShouldBeNull();
        scope2.Depth.ShouldBe(1);

        scope2.Dispose();
        _provider.Context.ShouldBeNull();
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
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        using var outerScope = _provider.CreateScope();
        var scope = _provider.CreateScope();

        // Act & Assert
        Should.NotThrow(() =>
        {
            scope.Dispose();
            scope.Dispose();
            scope.Dispose();
        });
        _provider.Context.ShouldBeSameAs(outerScope.Context);
    }

    [Fact]
    public async Task Context_FlowsIntoAwaitedCalls()
    {
        using var scope = _provider.CreateScope();

        var seen = await ReadContextAfterYieldAsync();

        seen.ShouldBeSameAs(scope.Context);
    }

    [Fact]
    public async Task ParallelFlows_EachSeeOnlyTheirOwnContext()
    {
        // Arrange: #524 - two AI calls started together from the same call.
        using var outerScope = _provider.CreateScope();
        var bothCreated = new Barrier(2);

        async Task<bool> RunAsync()
        {
            await Task.Yield();
            using var scope = _provider.CreateScope();
            bothCreated.SignalAndWait(TimeSpan.FromSeconds(5));
            await Task.Delay(10);
            var ownBeforeDispose = ReferenceEquals(_provider.Context, scope.Context);
            scope.Dispose();
            return ownBeforeDispose && ReferenceEquals(_provider.Context, outerScope.Context);
        }

        // Act
        var results = await Task.WhenAll(Task.Run(RunAsync), Task.Run(RunAsync));

        // Assert
        results.ShouldAllBe(ok => ok);
        _provider.Context.ShouldBeSameAs(outerScope.Context);
    }

    [Fact]
    public async Task ParallelFlows_WithNoOuterScope_DoNotSeeEachOther()
    {
        // Two top-level calls started together in one request, before any context exists.
        var bothCreated = new Barrier(2);

        async Task<bool> RunAsync(string value)
        {
            await Task.Yield();
            using var scope = _provider.CreateScope();
            scope.Context.SetValue("call", value);
            bothCreated.SignalAndWait(TimeSpan.FromSeconds(5));
            await Task.Delay(10);
            return _provider.Context?.GetValue<string>("call") == value;
        }

        var results = await Task.WhenAll(Task.Run(() => RunAsync("a")), Task.Run(() => RunAsync("b")));

        results.ShouldAllBe(ok => ok);
        _provider.Context.ShouldBeNull();
    }

    [Fact]
    public async Task ScopeCreatedInAChildTask_IsNotCurrentForTheParent()
    {
        using var outerScope = _provider.CreateScope();

        await Task.Run(() =>
        {
            var inner = _provider.CreateScope();
            _provider.Context.ShouldBeSameAs(inner.Context);

            // Left undisposed on purpose: it must still not leak out of the child task.
        });

        _provider.Context.ShouldBeSameAs(outerScope.Context);
    }

    [Fact]
    public async Task WorkStartedInAScope_AfterTheScopeIsDisposed_SeesTheNearestLiveScope()
    {
        // e.g. a background task started inside a call that outlives it.
        using var outerScope = _provider.CreateScope();
        var innerScope = _provider.CreateScope();
        var release = new TaskCompletionSource();
        var background = Task.Run(async () =>
        {
            await release.Task;
            return _provider.Context;
        });

        innerScope.Dispose();
        release.SetResult();

        (await background).ShouldBeSameAs(outerScope.Context);
    }

    [Fact]
    public async Task ScopeCreatedInsideAnIterator_IsNotCurrentForLaterStepsOfTheStreamItRuns()
    {
        // Documents why scope-creating iterators run their inner stream with WithRuntimeContext.
        var seen = new List<AIRuntimeContext?>();

        await foreach (var _ in CreatesScopeThenStreams(Steps(3, seen), wrap: false))
        {
        }

        seen[0].ShouldNotBeNull();
        seen.Skip(1).ShouldAllBe(context => context == null);
    }

    [Fact]
    public async Task WithRuntimeContext_MakesTheScopeCurrentForEveryStepAndTheDisposal()
    {
        var seen = new List<AIRuntimeContext?>();
        AIRuntimeContext? seenAtDisposal = null;
        AIRuntimeContext? created = null;

        await foreach (var context in CreatesScopeThenStreams(Steps(3, seen, c => seenAtDisposal = c), wrap: true))
        {
            created ??= context;
        }

        created.ShouldNotBeNull();
        seen.Count.ShouldBe(3);
        seen.ShouldAllBe(context => ReferenceEquals(context, created));
        seenAtDisposal.ShouldBeSameAs(created);
        _provider.Context.ShouldBeNull();
    }

    [Fact]
    public async Task WithRuntimeContext_DoesNotChangeTheConsumersContext()
    {
        using var consumerScope = _provider.CreateScope();

        await foreach (var _ in CreatesScopeThenStreams(Steps(2, []), wrap: true))
        {
            _provider.Context.ShouldBeSameAs(consumerScope.Context);
        }

        _provider.Context.ShouldBeSameAs(consumerScope.Context);
    }

    [Fact]
    public async Task WithRuntimeContext_TwoStreamsReadInTurn_EachStepSeesItsOwnScope()
    {
        // Two streamed calls read alternately by one consumer.
        var seenA = new List<AIRuntimeContext?>();
        var seenB = new List<AIRuntimeContext?>();
        await using var a = CreatesScopeThenStreams(Steps(3, seenA), wrap: true).GetAsyncEnumerator();
        await using var b = CreatesScopeThenStreams(Steps(3, seenB), wrap: true).GetAsyncEnumerator();

        while (await a.MoveNextAsync() & await b.MoveNextAsync())
        {
        }

        seenA.Distinct().Count().ShouldBe(1);
        seenB.Distinct().Count().ShouldBe(1);
        seenA[0].ShouldNotBeNull();
        seenB[0].ShouldNotBeNull();
        seenA[0].ShouldNotBeSameAs(seenB[0]);
    }

    [Fact]
    public void WithRuntimeContext_WithNoScope_ReturnsTheStreamAsIs()
    {
        var source = Steps(1, []);

        source.WithRuntimeContext(null).ShouldBeSameAs(source);
    }

    [Fact]
    public void Enter_ADisposedScope_DoesNothing()
    {
        using var outerScope = _provider.CreateScope();
        var disposed = _provider.CreateScope();
        disposed.Dispose();

        using (AIRuntimeContextScopeProvider.Enter(disposed))
        {
            _provider.Context.ShouldBeSameAs(outerScope.Context);
        }
    }

    [Fact]
    public async Task ManyParallelFlows_NeverSeeAnotherFlowsContext()
    {
        // Stress: 200 flows, each opening three scopes in turn under the root, with awaits between, all at once.
        using var rootScope = _provider.CreateScope();
        var failures = 0;

        async Task RunAsync(int flow)
        {
            await Task.Yield();
            for (var round = 0; round < 3; round++)
            {
                using var scope = _provider.CreateScope();
                scope.Context.SetValue("flow", flow);
                await Task.Delay(Random.Shared.Next(0, 3));
                if (_provider.Context?.GetValue<int>("flow") != flow || scope.Depth != 2)
                {
                    Interlocked.Increment(ref failures);
                }

                await NestedAsync(flow);
            }

            if (!ReferenceEquals(_provider.Context, rootScope.Context))
            {
                Interlocked.Increment(ref failures);
            }
        }

        async Task NestedAsync(int flow)
        {
            await Task.Yield();
            if (_provider.Context?.GetValue<int>("flow") != flow)
            {
                Interlocked.Increment(ref failures);
            }
        }

        await Task.WhenAll(Enumerable.Range(0, 200).Select(i => Task.Run(() => RunAsync(i))));

        failures.ShouldBe(0);
        _provider.Context.ShouldBeSameAs(rootScope.Context);
    }

    private async Task<AIRuntimeContext?> ReadContextAfterYieldAsync()
    {
        await Task.Yield();
        return _provider.Context;
    }

    /// <summary>Creates a scope, then runs <paramref name="inner"/>, like a scoped streaming client.</summary>
    private async IAsyncEnumerable<AIRuntimeContext> CreatesScopeThenStreams(
        IAsyncEnumerable<int> inner,
        bool wrap,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var scope = _provider.CreateScope();
        var stream = wrap ? inner.WithRuntimeContext(scope) : inner;
        await foreach (var _ in stream.WithCancellation(cancellationToken))
        {
            yield return scope.Context;
        }
    }

    /// <summary>A stream that records the current context at each step, and optionally when disposed.</summary>
    private async IAsyncEnumerable<int> Steps(int count, List<AIRuntimeContext?> seen, Action<AIRuntimeContext?>? onDispose = null)
    {
        try
        {
            for (var i = 0; i < count; i++)
            {
                await Task.Yield();
                seen.Add(_provider.Context);
                yield return i;
            }
        }
        finally
        {
            onDispose?.Invoke(_provider.Context);
        }
    }
}
