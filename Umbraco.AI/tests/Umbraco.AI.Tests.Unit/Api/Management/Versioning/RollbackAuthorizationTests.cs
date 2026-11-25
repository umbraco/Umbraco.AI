// GP-3 (AC1-AC4); PLAN guess 1

using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

using Umbraco.AI.Core.Security;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Web.Api.Management.Versioning.Controllers;
using Umbraco.Cms.Core.Mapping;

using static Umbraco.AI.Tests.Unit.Api.Management.AIAuthorizationTestHelper;

namespace Umbraco.AI.Tests.Unit.Api.Management.Versioning;

/// <summary>
/// Rollback needs the right the entity type's adapter declares. The controller runs with the
/// real policies and handlers; only the version adapters are mocked.
/// </summary>
public class RollbackAuthorizationTests
{
    // The verb an add-on such as Umbraco.AI.Agent declares; core only sees it as an adapter's rollback right
    private const string AgentManageVerb = "Uai.Agent.Manage";

    private static Mock<IAIVersionableEntityAdapter> CreateAdapter(string entityTypeName, string? rollbackPermission)
    {
        var adapter = new Mock<IAIVersionableEntityAdapter>();
        adapter.SetupGet(a => a.EntityTypeName).Returns(entityTypeName);
        adapter.SetupGet(a => a.RollbackPermission).Returns(rollbackPermission);
        adapter.Setup(a => a.RollbackAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return adapter;
    }

    private static EntityVersionHistoryController CreateController(
        Mock<IAIVersionableEntityAdapter> adapter,
        string[] rights)
    {
        var adapters = new AIVersionableEntityAdapterCollection(() => [adapter.Object]);
        return new EntityVersionHistoryController(
            Mock.Of<IAIEntityVersionService>(),
            adapters,
            Mock.Of<IUmbracoMapper>(),
            CreateAuthorizationService(hasSection: true, verbs: rights))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    private static int? StatusOf(IActionResult result) => (result as IStatusCodeActionResult)?.StatusCode;

    #region Happy path

    [Fact]
    public async Task ProfileRollback_WithManageProfiles_Returns200()
    {
        // Given an AI section user with Uai.Profile.Manage and a profile with an older version
        var adapter = CreateAdapter("Profile", AIPermissions.ManageProfiles);
        var controller = CreateController(adapter, [AIPermissions.ManageProfiles]);

        // When  they POST versions/{profile type}/{id}/{version}/rollback
        var result = await controller.RollbackToVersion("profile", Guid.NewGuid(), 1);

        // Then  the request succeeds (AC1); the action answers 204 No Content, as it always has
        result.ShouldBeOfType<NoContentResult>();
        adapter.Verify(a => a.RollbackAsync(It.IsAny<Guid>(), 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task ProfileRollback_WithoutManageProfiles_Returns403()
    {
        // Given an AI section user with every AI right except Uai.Profile.Manage
        var controller = CreateController(
            CreateAdapter("Profile", AIPermissions.ManageProfiles),
            AllVerbs.Where(v => v != AIPermissions.ManageProfiles).ToArray());

        // When  they roll back a profile
        var result = await controller.RollbackToVersion("profile", Guid.NewGuid(), 1);

        // Then  the response is 403 (AC2)
        StatusOf(result).ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task ProfileRollback_WithoutManageProfiles_LeavesCurrentVersion()
    {
        // Given an AI section user without Uai.Profile.Manage
        var adapter = CreateAdapter("Profile", AIPermissions.ManageProfiles);
        var controller = CreateController(adapter, []);

        // When  they roll back a profile
        await controller.RollbackToVersion("profile", Guid.NewGuid(), 1);

        // Then  the profile's current version is unchanged: the adapter never rolled it back (AC3)
        adapter.Verify(
            a => a.RollbackAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AgentRollback_WithOnlyManageProfiles_Returns403()
    {
        // Given an AI section user with Uai.Profile.Manage but not Uai.Agent.Manage
        var controller = CreateController(CreateAdapter("Agent", AgentManageVerb), [AIPermissions.ManageProfiles]);

        // When  they roll back an agent
        var result = await controller.RollbackToVersion("agent", Guid.NewGuid(), 1);

        // Then  the response is 403 (AC4)
        StatusOf(result).ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Rollback_WhenAdapterThrowsPermissionDenied_Returns403()
    {
        // Given an AI section user with Uai.Agent.Manage and an agent adapter whose rollback throws AIPermissionDeniedException
        var adapter = CreateAdapter("Agent", AgentManageVerb);
        adapter.Setup(a => a.RollbackAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIPermissionDeniedException("Not allowed to change this."));
        var controller = CreateController(adapter, [AgentManageVerb]);

        // When  they roll the agent back
        var result = await controller.RollbackToVersion("agent", Guid.NewGuid(), 1);

        // Then  the response is 403 (GP-8 AC8, GP-18 AC8)
        StatusOf(result).ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task UnmappedEntityTypeRollback_Returns403()
    {
        // Given an AI section user with every AI right and an adapter that declares no rollback right
        var adapter = CreateAdapter("Widget", null);
        var controller = CreateController(adapter, AllVerbs);

        // When  they roll it back
        var result = await controller.RollbackToVersion("widget", Guid.NewGuid(), 1);

        // Then  the response is 403 (PLAN guess 1)
        StatusOf(result).ShouldBe(StatusCodes.Status403Forbidden);
        adapter.Verify(
            a => a.RollbackAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Adapters declare their right

    [Fact]
    public void EveryCoreAdapter_DeclaresAKnownRollbackPermission()
    {
        // Given every versionable entity adapter in the core assembly, found by reflection
        var adapterTypes = typeof(IAIVersionableEntityAdapter).Assembly.GetTypes()
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

    #endregion
}
