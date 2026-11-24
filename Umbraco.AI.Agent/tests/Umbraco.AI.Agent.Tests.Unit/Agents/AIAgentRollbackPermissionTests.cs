using System.Reflection;
using System.Runtime.CompilerServices;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Security;
using Umbraco.AI.Core.Versioning;

using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

public class AIAgentRollbackPermissionTests
{
    [Fact]
    public void EveryAdapterInThisPackage_DeclaresAKnownRollbackPermission()
    {
        // Given every versionable entity adapter in Agent.Core, found by reflection
        var adapterTypes = typeof(AIAgentVersionableEntityAdapter).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IAIVersionableEntityAdapter).IsAssignableFrom(t))
            .ToList();
        var knownVerbs = typeof(AIPermissions).GetFields(BindingFlags.Public | BindingFlags.Static)
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
