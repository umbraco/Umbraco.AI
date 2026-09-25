using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for checking <see cref="AIRequiresCapabilityAttribute"/> declarations.
/// </summary>
public static class AIRequiresCapabilityExtensions
{
    /// <summary>
    /// Determines whether every capability the instance's type declares via
    /// <see cref="AIRequiresCapabilityAttribute"/> is currently enabled.
    /// </summary>
    /// <param name="instance">The instance to inspect (e.g. a guardrail evaluator or test grader).</param>
    /// <param name="experimentalFeatures">Resolves whether a given capability is enabled.</param>
    /// <returns>
    /// <c>true</c> when the type has no <see cref="AIRequiresCapabilityAttribute"/>, or when every
    /// capability it lists is enabled; otherwise <c>false</c>.
    /// </returns>
    public static bool AreRequiredCapabilitiesEnabled(this object instance, IAIExperimentalFeatures experimentalFeatures)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(experimentalFeatures);

        return instance.GetType().AreRequiredCapabilitiesEnabled(experimentalFeatures);
    }

    /// <summary>
    /// Determines whether every capability a type declares via <see cref="AIRequiresCapabilityAttribute"/>
    /// is currently enabled.
    /// </summary>
    /// <param name="type">The type to inspect (e.g. a guardrail evaluator or test grader type).</param>
    /// <param name="experimentalFeatures">Resolves whether a given capability is enabled.</param>
    /// <returns>
    /// <c>true</c> when the type has no <see cref="AIRequiresCapabilityAttribute"/>, or when every
    /// capability it lists is enabled; otherwise <c>false</c>.
    /// </returns>
    public static bool AreRequiredCapabilitiesEnabled(this Type type, IAIExperimentalFeatures experimentalFeatures)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(experimentalFeatures);

        var requiredCapabilities = type
            .GetCustomAttributes(typeof(AIRequiresCapabilityAttribute), inherit: true)
            .Cast<AIRequiresCapabilityAttribute>();

        return requiredCapabilities.All(attribute => experimentalFeatures.IsCapabilityEnabled(attribute.Capability));
    }
}
