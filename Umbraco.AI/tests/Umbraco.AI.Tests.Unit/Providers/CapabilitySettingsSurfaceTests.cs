using System.Reflection;
using Umbraco.AI.Core.Providers;

#pragma warning disable UMBRACOAI_IMAGEGEN // Names the experimental image capability in the surface guard

namespace Umbraco.AI.Tests.Unit.Providers;

/// <summary>
/// Guards the shape of the provider extension points rather than any behaviour: every capability a provider
/// can implement must offer the same way to declare its own settings.
/// </summary>
/// <remarks>
/// Written structurally, over whatever capability interfaces the assembly happens to contain, so adding a
/// capability fails this test until its settings surface exists. A hand-maintained list would simply be
/// updated in the same commit that forgot the surface.
/// <para>
/// Driven from the interfaces rather than the <c>AICapability</c> enum on purpose: the enum carries reserved
/// values (<c>Media</c>, <c>Moderation</c>) that no provider can implement yet, and a guard that fails on
/// planned-but-absent capabilities would have to be suppressed to stay green.
/// </para>
/// </remarks>
public class CapabilitySettingsSurfaceTests
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
    public void EveryCapability_HasATwoParameterBase(Type capabilityInterface)
    {
        var twoParameterBase = FindTwoParameterBase(capabilityInterface);

        twoParameterBase.ShouldNotBeNull(
            $"{capabilityInterface.Name} has no two-parameter base, so a provider cannot declare capability "
            + "settings for it. Add AI...CapabilityBase<TSettings, TCapabilitySettings> mirroring the chat one.");
    }

    [Theory]
    [MemberData(nameof(CapabilityInterfaces))]
    public void EveryTwoParameterBase_DeclaresItsSettingsType(Type capabilityInterface)
    {
        var twoParameterBase = FindTwoParameterBase(capabilityInterface).ShouldNotBeNull();

        // Sealed, so a derived provider cannot report a settings type that differs from the one the base
        // applies — which is what keeps the generated schema and the applied values in agreement.
        var property = twoParameterBase.GetProperty(nameof(IAICapability.CapabilitySettingsType));
        property.ShouldNotBeNull();
        property.GetMethod!.IsFinal.ShouldBeTrue(
            $"{twoParameterBase.Name} should seal CapabilitySettingsType.");
    }

    [Theory]
    [MemberData(nameof(CapabilityInterfaces))]
    public void EveryTwoParameterBase_HasAnApplyHook(Type capabilityInterface)
    {
        var twoParameterBase = FindTwoParameterBase(capabilityInterface).ShouldNotBeNull();

        // The hook is the whole point: without it the settings would be resolved and then dropped, which is
        // the failure mode CapabilitySettingsRoundTripTests covers behaviourally.
        var apply = twoParameterBase
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .SingleOrDefault(m => m.Name == "ApplyCapabilitySettings");

        apply.ShouldNotBeNull($"{twoParameterBase.Name} should declare an ApplyCapabilitySettings hook.");
        apply.IsAbstract.ShouldBeTrue("The hook should be abstract, so implementing it is not optional.");

        // (settings, modelId, options) — the model is what lets a provider gate a setting it cannot send.
        var parameters = apply.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[1].ParameterType.ShouldBe(typeof(string));
    }

    [Theory]
    [MemberData(nameof(CapabilityInterfaces))]
    public void EveryCapability_EnforcesDeclarationsAtRequestTime(Type capabilityInterface)
    {
        // A declaration that only reaches the editor is the trap this series exists to close: the field
        // disappears, and the value still goes out on a profile saved before a model change. So every
        // capability that can declare unsupported core settings must also have a core decorator that strips
        // them, and the bases must reference it.
        var enforcer = CoreAssembly.GetTypes()
            .SingleOrDefault(t => t.Name.StartsWith("DeclaredSettings", StringComparison.Ordinal)
                && t.Name.Contains(ClientNoun(capabilityInterface), StringComparison.Ordinal));

        enforcer.ShouldNotBeNull(
            $"{capabilityInterface.Name} has no DeclaredSettings… decorator, so a declaration it makes would "
            + "be shown to the editor and ignored on the request.");
    }

    /// <summary>
    /// The noun a capability's client type uses, which the enforcing decorator is named after.
    /// </summary>
    private static string ClientNoun(Type capabilityInterface) => capabilityInterface.Name switch
    {
        nameof(IAIChatCapability) => "ChatClient",
        nameof(IAIEmbeddingCapability) => "EmbeddingGenerator",
        nameof(IAISpeechToTextCapability) => "SpeechToTextClient",
        nameof(IAIImageGeneratorCapability) => "ImageGenerator",
        _ => throw new NotSupportedException(
            $"{capabilityInterface.Name} is new here. Add its client noun, and make sure it has a "
            + "DeclaredSettings… decorator installed by its base."),
    };

    private static Type? FindTwoParameterBase(Type capabilityInterface)
        => CoreAssembly.GetTypes()
            .SingleOrDefault(t => t.IsAbstract
                && t.IsPublic
                && t.IsGenericTypeDefinition
                && t.GetGenericArguments().Length == 2
                && t.GetInterfaces().Contains(capabilityInterface));
}
