using System.Reflection;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tools.Scopes;

/// <summary>
/// Base class for tool scope implementations.
/// </summary>
/// <remarks>
/// <para>
/// Extend this class and apply the <see cref="AIToolScopeAttribute"/> to define a scope.
/// The base class reads metadata from the attribute automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AIToolScope("content-read", Icon = "icon-folder", Domain = "Content")]
/// public class ContentReadScope : AIToolScopeBase { }
/// </code>
/// </example>
public abstract class AIToolScopeBase : IAIToolScope, IDiscoverable
{
    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Icon { get; }

    /// <inheritdoc />
    public bool IsDestructive { get; }

    /// <inheritdoc />
    public string Domain { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> ForEntityTypes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIToolScopeBase"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the derived class is missing the <see cref="AIToolScopeAttribute"/>.
    /// </exception>
    protected AIToolScopeBase()
    {
        var attribute = GetType().GetCustomAttribute<AIToolScopeAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"The AI tool scope '{GetType().FullName}' is missing the required {nameof(AIToolScopeAttribute)}.");

        Id = attribute.Id;
        Icon = attribute.Icon;
        IsDestructive = attribute.IsDestructive;
        Domain = attribute.Domain;
        ForEntityTypes = attribute.ForEntityTypes ?? Array.Empty<string>();
    }
}
