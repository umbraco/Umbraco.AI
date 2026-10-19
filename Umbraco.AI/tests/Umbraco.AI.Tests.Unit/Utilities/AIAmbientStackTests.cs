using Umbraco.AI.Core.Utilities;

namespace Umbraco.AI.Tests.Unit.Utilities;

/// <summary>
/// The shared per-flow current value behind the runtime context and the resolved context. Its behaviour through
/// those stores is covered by their own tests; these cover what is specific to the shared type.
/// </summary>
public class AIAmbientStackTests
{
    private readonly AIAmbientStack<string> _stack = new();

    [Fact]
    public void Push_MakesTheValueCurrent_AndDisposingRestoresTheOneItReplaced()
    {
        using (_stack.Push("outer"))
        {
            using (_stack.Push("inner"))
            {
                _stack.Current.ShouldBe("inner");
            }

            _stack.Current.ShouldBe("outer");
        }

        _stack.Current.ShouldBeNull();
    }

    [Fact]
    public void Dispose_Twice_OnlyRestoresOnce()
    {
        using var outer = _stack.Push("outer");
        var inner = _stack.Push("inner");
        inner.Dispose();
        using var later = _stack.Push("later");

        inner.Dispose();

        _stack.Current.ShouldBe("later");
    }

    [Fact]
    public void Stacks_AreIndependent()
    {
        var other = new AIAmbientStack<string>();

        using var _ = _stack.Push("mine");

        other.Current.ShouldBeNull();
    }

    [Fact]
    public void Enter_WithAnEntryFromAnotherStack_OrAnotherHandle_DoesNothing()
    {
        var other = new AIAmbientStack<string>();
        using var foreign = other.Push("theirs");

        _stack.Enter(foreign).ShouldBeNull();
        _stack.Enter(new MemoryStream()).ShouldBeNull();
        _stack.Enter(null).ShouldBeNull();
        _stack.Current.ShouldBeNull();
    }

    [Fact]
    public void Enter_MakesTheEntryCurrent_AndRestoresAfter()
    {
        var entry = _stack.Push("entered");
        entry.Dispose();
        using var _ = _stack.Push("current");

        using (_stack.Enter(entry))
        {
            _stack.Current.ShouldBe("entered");
        }

        _stack.Current.ShouldBe("current");
    }
}
