using System.Reflection;

namespace Umbraco.AI.Agent.Core.Surfaces;

/// <summary>
/// Base class for AI agent surface implementations.
/// </summary>
/// <remarks>
/// <para>
/// Extend this class and apply the <see cref="AIAgentSurfaceAttribute"/> to define a surface.
/// The base class reads metadata from the attribute automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AIAgentSurface("copilot", Icon = "icon-chat")]
/// public class CopilotSurface : AIAgentSurfaceBase { }
/// </code>
/// </example>
public abstract class AIAgentSurfaceBase : IAIAgentSurface
{
    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Icon { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedScopeDimensions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentSurfaceBase"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the derived class is missing the <see cref="AIAgentSurfaceAttribute"/>.
    /// </exception>
    protected AIAgentSurfaceBase()
    {
        var attribute = GetType().GetCustomAttribute<AIAgentSurfaceAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"The AI agent surface '{GetType().FullName}' is missing the required {nameof(AIAgentSurfaceAttribute)}.");

        Id = attribute.Id;
        Icon = attribute.Icon;
        SupportedScopeDimensions = attribute.SupportedScopeDimensions ?? [];
    }
}
