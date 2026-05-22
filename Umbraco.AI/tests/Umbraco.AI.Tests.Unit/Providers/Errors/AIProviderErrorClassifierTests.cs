using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Tests.Unit.Providers.Errors;

public class AIProviderErrorClassifierTests
{
    [Fact]
    public void Classify_NoClassifiers_FallsBackToUnknown()
    {
        var composite = CreateClassifier();

        var result = composite.Classify(new InvalidOperationException("nothing recognises me"));

        result.Category.ShouldBe(AIProviderErrorCategory.Unknown);
        result.UserMessage.ShouldNotBeNullOrWhiteSpace();
        result.ProviderCode.ShouldBeNull();
    }

    [Fact]
    public void Classify_FirstMatchingClassifierWins()
    {
        var first = new StubClassifier(_ => new AIProviderErrorInfo(
            AIProviderErrorCategory.RateLimited, "first wins", "first", "raw"));
        var second = new StubClassifier(_ => new AIProviderErrorInfo(
            AIProviderErrorCategory.Authentication, "second", "second", "raw"));
        var composite = CreateClassifier(first, second);

        var result = composite.Classify(new Exception());

        result.UserMessage.ShouldBe("first wins");
        result.Category.ShouldBe(AIProviderErrorCategory.RateLimited);
    }

    [Fact]
    public void Classify_FallsThroughNullReturningClassifiers()
    {
        var nullClassifier = new StubClassifier(_ => null);
        var matching = new StubClassifier(_ => new AIProviderErrorInfo(
            AIProviderErrorCategory.Transient, "second classifier matched", null, "raw"));
        var composite = CreateClassifier(nullClassifier, matching);

        var result = composite.Classify(new Exception());

        result.Category.ShouldBe(AIProviderErrorCategory.Transient);
        result.UserMessage.ShouldBe("second classifier matched");
    }

    private static AIProviderErrorClassifier CreateClassifier(params IAIProviderErrorClassifier[] classifiers)
        => new(new AIProviderErrorClassifierCollection(() => classifiers));

    private sealed class StubClassifier : IAIProviderErrorClassifier
    {
        private readonly Func<Exception, AIProviderErrorInfo?> _classify;
        public StubClassifier(Func<Exception, AIProviderErrorInfo?> classify) => _classify = classify;
        public AIProviderErrorInfo? Classify(Exception exception) => _classify(exception);
    }
}
