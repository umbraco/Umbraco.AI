using System.Reflection;

namespace Umbraco.Ai.Agent.Core.Scopes;

/// <summary>
/// Base class for AI agent scope implementations.
/// </summary>
/// <remarks>
/// <para>
/// Extend this class and apply the <see cref="AiAgentScopeAttribute"/> to define a scope.
/// The base class reads metadata from the attribute automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AiAgentScope("copilot", Icon = "icon-chat")]
/// public class CopilotScope : AiAgentScopeBase { }
/// </code>
/// </example>
public abstract class AiAgentScopeBase : IAiAgentScope
{
    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Icon { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentScopeBase"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the derived class is missing the <see cref="AiAgentScopeAttribute"/>.
    /// </exception>
    protected AiAgentScopeBase()
    {
        var attribute = GetType().GetCustomAttribute<AiAgentScopeAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"The AI agent scope '{GetType().FullName}' is missing the required {nameof(AiAgentScopeAttribute)}.");

        Id = attribute.Id;
        Icon = attribute.Icon;
    }
}
