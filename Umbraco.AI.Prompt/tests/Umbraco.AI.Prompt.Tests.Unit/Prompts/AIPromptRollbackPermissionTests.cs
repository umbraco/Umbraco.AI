using System.Reflection;
using System.Runtime.CompilerServices;

using Shouldly;

using Umbraco.AI.Core.Security;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Core.Security;

using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

public class AIPromptRollbackPermissionTests
{
    [Fact]
    public void EveryAdapterInThisPackage_DeclaresAKnownRollbackPermission()
    {
        // Given every versionable entity adapter in Prompt.Core, found by reflection
        var adapterTypes = typeof(AIPromptVersionableEntityAdapter).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IAIVersionableEntityAdapter).IsAssignableFrom(t))
            .ToList();
        var knownVerbs = new[] { typeof(AIPermissions), typeof(AIPromptPermissions) }
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        // When  each is asked which right its rollback needs (the property is constant, so no constructor is run)
        var declared = adapterTypes.ToDictionary(
            t => t.Name,
            t => ((IAIVersionableEntityAdapter)RuntimeHelpers.GetUninitializedObject(t)).RollbackPermission);

        // Then  there are adapters, and each declares one of the AIPermissions verbs
        declared.ShouldNotBeEmpty();
        declared.Where(d => d.Value is null || !knownVerbs.Contains(d.Value)).ShouldBeEmpty();
    }
}
