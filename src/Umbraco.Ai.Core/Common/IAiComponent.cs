namespace Umbraco.Ai.Core.Common;

/// <summary>
/// Defines a contract for AI components within the Umbraco AI ecosystem.
/// </summary>
public interface IAiComponent
{
    /// <summary>
    /// The unique id of this AI component.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// The name of this AI component.
    /// </summary>
    string Name { get; }
}