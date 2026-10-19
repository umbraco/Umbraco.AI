namespace Umbraco.AI.Core.Models;

/// <summary>
/// Marks a type as depending on an <see cref="AICapability"/> being enabled.
/// </summary>
/// <remarks>
/// <para>
/// Apply once per required capability (<c>AllowMultiple = true</c>). Checked via
/// <see cref="Extensions.AIRequiresCapabilityExtensions.AreRequiredCapabilitiesEnabled(Type, Core.Settings.IAIExperimentalFeatures)"/>,
/// which reads every attribute on the type and requires all of them to be enabled.
/// </para>
/// <para>
/// "Enabled" reflects <see cref="Core.Settings.IAIExperimentalFeatures.IsCapabilityEnabled"/>: a
/// non-experimental capability always reports enabled, so the attribute only has a practical effect
/// for capabilities that are still experimental (gated behind a feature flag, default off). It does
/// not check that a profile exists for the capability — that's a separate, runtime concern.
/// </para>
/// <para>
/// This attribute is a declaration, not an enforcement mechanism. Consumers read it to decide whether
/// to offer the type at all — the Management API's guardrail evaluator and test grader listing
/// endpoints are the current examples. It does <b>not</b> stop the type from executing: an item stays
/// in its own collection, so something that already references it by id still resolves even while the
/// required capability is disabled. An implementation must itself check
/// <see cref="Core.Settings.IAIExperimentalFeatures.IsCapabilityEnabled"/> at the point it runs and
/// fail safe (e.g. return a flagged/failed result) when the capability is off. That runtime check —
/// not this attribute — is what makes the type fail safe instead of running with a capability that
/// isn't available.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
// Inherited: a subclass shouldn't be able to silently drop a base type's requirement by omission.
// Combined with AllowMultiple, the base type's attributes and the subclass's own attributes both
// apply — a subclass only ever adds requirements, it can't remove one it didn't declare.
public sealed class AIRequiresCapabilityAttribute : Attribute
{
    /// <summary>
    /// Gets the capability this type requires to be enabled.
    /// </summary>
    public AICapability Capability { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIRequiresCapabilityAttribute"/> class.
    /// </summary>
    /// <param name="capability">The capability this type requires to be enabled.</param>
    public AIRequiresCapabilityAttribute(AICapability capability)
    {
        Capability = capability;
    }
}
