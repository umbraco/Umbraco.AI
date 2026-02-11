using System.Reflection;

namespace Umbraco.AI.Agent.Core.Scopes;

/// <summary>
/// Base class for AI agent scope implementations.
/// </summary>
/// <remarks>
/// <para>
/// Extend this class and apply the <see cref="AIAgentScopeAttribute"/> to define a scope.
/// The base class reads metadata from the attribute automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AIAgentScope("copilot", Icon = "icon-chat")]
/// public class CopilotScope : AIAgentScopeBase { }
/// </code>
/// </example>
public abstract class AIAgentScopeBase : IAIAgentScope
{
    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Icon { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedContextDimensions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentScopeBase"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the derived class is missing the <see cref="AIAgentScopeAttribute"/>.
    /// </exception>
    protected AIAgentScopeBase()
    {
        var attribute = GetType().GetCustomAttribute<AIAgentScopeAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"The AI agent scope '{GetType().FullName}' is missing the required {nameof(AIAgentScopeAttribute)}.");

        Id = attribute.Id;
        Icon = attribute.Icon;
        SupportedContextDimensions = attribute.SupportedContextDimensions ?? Array.Empty<string>();
    }
}
