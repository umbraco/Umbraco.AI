using System.Reflection;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Web.Api.Management.Capability.Controllers;

namespace Umbraco.AI.Tests.Unit.Api.Management.Capability;

/// <summary>
/// Guards the shape of <see cref="EnabledCapabilitiesController.ImplementedCapabilities"/>, a hand-maintained
/// set, against every capability-kind marker interface actually declared in Umbraco.AI.Core (see
/// <see cref="IAICapability"/> and its siblings).
/// </summary>
/// <remarks>
/// Written structurally, over whatever capability interfaces the assembly happens to contain, so adding a
/// capability (a new <c>IAI*Capability</c> interface) fails this test until <c>ImplementedCapabilities</c> is
/// updated to include it — rather than the capability silently missing from
/// <c>GET .../capability/enabled</c>, which fails nothing on its own since a missing entry there just isn't
/// listed. Mirrors <c>CapabilitySettingsSurfaceTests</c> in <c>Providers/</c>.
/// </remarks>
public class ImplementedCapabilitiesSurfaceTests
{
    private static readonly Assembly CoreAssembly = typeof(IAICapability).Assembly;

    public static TheoryData<Type> CapabilityInterfaces()
    {
        var data = new TheoryData<Type>();
        foreach (var type in CoreAssembly.GetTypes()
            .Where(t => t.IsInterface
                && t.IsPublic
                && !t.IsGenericType
                && t != typeof(IAICapability)
                && typeof(IAICapability).IsAssignableFrom(t))
            .OrderBy(t => t.Name))
        {
            data.Add(type);
        }

        return data;
    }

    [Fact]
    public void CapabilityInterfaces_AreDiscovered()
    {
        // A reflection query that silently matches nothing would make every other test here vacuous.
        CapabilityInterfaces().Count.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Theory]
    [MemberData(nameof(CapabilityInterfaces))]
    public void EveryCapability_IsInTheImplementedSet(Type capabilityInterface)
    {
        var kind = KindOf(capabilityInterface);

        EnabledCapabilitiesController.ImplementedCapabilities.ShouldContain(
            kind,
            $"{capabilityInterface.Name} maps to AICapability.{kind}, but "
                + "EnabledCapabilitiesController.ImplementedCapabilities does not list it, so it can never "
                + "appear in GET .../capability/enabled.");
    }

    /// <summary>
    /// The <see cref="AICapability"/> value a capability-kind marker interface represents. Switches on the
    /// interface's name rather than referencing the type itself — some of these interfaces are
    /// <c>[Experimental]</c> — mirroring <c>CapabilitySettingsSurfaceTests.ClientNoun</c>.
    /// </summary>
    private static AICapability KindOf(Type capabilityInterface) => capabilityInterface.Name switch
    {
        "IAIChatCapability" => AICapability.Chat,
        "IAIEmbeddingCapability" => AICapability.Embedding,
        "IAISpeechToTextCapability" => AICapability.SpeechToText,
        "IAIImageGeneratorCapability" => AICapability.ImageGeneration,
        "IAIDecisionCapability" => AICapability.Decision,
        _ => throw new NotSupportedException(
            $"{capabilityInterface.Name} is new here. Add its AICapability mapping above, and make sure "
                + "EnabledCapabilitiesController.ImplementedCapabilities lists that AICapability value if the "
                + "capability is actually shipped."),
    };
}
