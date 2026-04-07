using System.Diagnostics.CodeAnalysis;
using Microsoft.Agents.AI;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Wraps an <see cref="AgentResponse"/> with a typed structured output result.
/// </summary>
/// <typeparam name="T">The type of the structured output.</typeparam>
public sealed class AIStructuredAgentResponse<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIStructuredAgentResponse{T}"/> class.
    /// </summary>
    /// <param name="inner">The underlying agent response.</param>
    /// <param name="result">The deserialized structured output, or default if deserialization failed.</param>
    internal AIStructuredAgentResponse(AgentResponse inner, T? result)
    {
        Inner = inner;
        Result = result;
    }

    /// <summary>
    /// Gets the underlying agent response.
    /// </summary>
    public AgentResponse Inner { get; }

    /// <summary>
    /// Gets the deserialized structured output result.
    /// May be <c>default</c> if the provider did not honor the structured output schema.
    /// Use <see cref="TryGetResult"/> for safe access.
    /// </summary>
    public T? Result { get; }

    /// <summary>
    /// Attempts to get the structured output result.
    /// </summary>
    /// <param name="result">When this method returns, contains the result if available.</param>
    /// <returns><c>true</c> if a non-null result is available; otherwise, <c>false</c>.</returns>
    public bool TryGetResult([NotNullWhen(true)] out T? result)
    {
        result = Result;
        return result is not null;
    }
}
