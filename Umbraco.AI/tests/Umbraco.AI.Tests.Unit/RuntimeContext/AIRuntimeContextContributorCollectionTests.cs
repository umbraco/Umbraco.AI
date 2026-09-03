using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Tests.Unit.RuntimeContext;

public class AIRuntimeContextContributorCollectionTests
{
    [Fact]
    public async Task PopulateAsync_WithSyncOnlyContributor_InvokesContributeViaDefault()
    {
        // Arrange — a contributor that only implements the original sync Contribute,
        // exactly like every contributor in this codebase today. PopulateAsync must
        // still invoke it via the interface's default ContributeAsync wrapper.
        var contributor = new SyncOnlyContributor();
        var collection = new AIRuntimeContextContributorCollection(() => [contributor]);
        var context = new AIRuntimeContext([]);

        // Act
        await collection.PopulateAsync(context);

        // Assert
        contributor.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task PopulateAsync_WithMultipleContributors_InvokesAllInOrder()
    {
        // Arrange
        var callOrder = new List<int>();
        var first = new SyncOnlyContributor(() => callOrder.Add(1));
        var second = new SyncOnlyContributor(() => callOrder.Add(2));
        var collection = new AIRuntimeContextContributorCollection(() => [first, second]);
        var context = new AIRuntimeContext([]);

        // Act
        await collection.PopulateAsync(context);

        // Assert
        callOrder.ShouldBe([1, 2]);
    }

    [Fact]
    public void Populate_StillWorksUnchanged()
    {
        // Arrange — the original sync method must keep working exactly as before.
        var contributor = new SyncOnlyContributor();
        var collection = new AIRuntimeContextContributorCollection(() => [contributor]);
        var context = new AIRuntimeContext([]);

        // Act
        collection.Populate(context);

        // Assert
        contributor.WasCalled.ShouldBeTrue();
    }

    private sealed class SyncOnlyContributor : IAIRuntimeContextContributor
    {
        private readonly Action? _onContribute;

        public SyncOnlyContributor(Action? onContribute = null)
        {
            _onContribute = onContribute;
        }

        public bool WasCalled { get; private set; }

        public void Contribute(AIRuntimeContext context)
        {
            WasCalled = true;
            _onContribute?.Invoke();
        }
    }
}
