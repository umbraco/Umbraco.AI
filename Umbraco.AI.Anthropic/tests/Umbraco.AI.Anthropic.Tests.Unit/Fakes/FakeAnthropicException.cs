namespace Anthropic.Fakes;

/// <summary>
/// Stand-in for the real Anthropic SDK exception types. Lives under the <c>Anthropic.*</c>
/// namespace so the classifier's namespace-prefix check picks it up, without taking a hard
/// dependency on the SDK's generated exception classes in tests.
/// </summary>
public sealed class FakeAnthropicException : Exception
{
    public FakeAnthropicException(string message, int? statusCode = null) : base(message)
    {
        StatusCode = statusCode;
    }

    public int? StatusCode { get; }
}
