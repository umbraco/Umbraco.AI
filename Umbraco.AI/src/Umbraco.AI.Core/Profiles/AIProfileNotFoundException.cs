using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Exception thrown when a required AI profile is not configured or cannot be found.
/// </summary>
public sealed class AIProfileNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProfileNotFoundException"/> class.
    /// </summary>
    /// <param name="capability">The capability for which no profile was found.</param>
    /// <param name="message">The exception message.</param>
    public AIProfileNotFoundException(AICapability capability, string message)
        : base(message)
    {
        Capability = capability;
    }

    /// <summary>
    /// Gets the capability for which no profile was found.
    /// </summary>
    public AICapability Capability { get; }
}
