namespace Umbraco.AI.Core.Models;

/// <summary>
/// Marks a guardrail evaluator or test grader as depending on an <see cref="AICapability"/>.
/// </summary>
/// <remarks>
/// <para>
/// Apply once per required capability (<c>AllowMultiple = true</c>). Checked via
/// <see cref="Extensions.AIRequiresCapabilityExtensions.AreRequiredCapabilitiesEnabled(Type, Core.Settings.IAIExperimentalFeatures)"/>,
/// which reads every attribute on the type and requires all of them to be enabled.
/// </para>
/// <para>
/// This attribute only controls whether the item is offered in the Management API's evaluator/grader
/// listing endpoints — it does <b>not</b> stop the item from executing. The item stays in its own
/// collection (evaluators, graders), so a rule or test that already references it by id still
/// resolves even while the required capability is disabled. An evaluator or grader implementation
/// must itself check <see cref="Core.Settings.IAIExperimentalFeatures.IsCapabilityEnabled"/> in its own
/// <c>EvaluateAsync</c>/<c>GradeAsync</c> and return a flagged/failed result when the capability is
/// off. That runtime check — not this attribute — is what makes the item fail safe instead of running
/// with a capability that isn't available.
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
