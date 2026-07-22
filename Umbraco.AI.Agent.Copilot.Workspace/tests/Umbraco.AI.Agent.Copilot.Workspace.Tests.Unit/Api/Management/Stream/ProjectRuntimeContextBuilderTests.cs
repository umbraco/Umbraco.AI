using Shouldly;
using Umbraco.AI.Agent.Conversations.Core;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Copilot.Workspace.Web.Api.Management.Stream;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;
using Xunit;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Api.Management.Stream;

/// <summary>
/// Tests for <see cref="ProjectRuntimeContextBuilder"/> — the map from a project to the runtime-context
/// properties that inject its framing, instructions, resources, and referenced contexts into a run.
/// </summary>
public class ProjectRuntimeContextBuilderTests
{
    private const string TextResourceTypeId = "text";

    [Fact]
    public void Build_WithEverything_EmitsContextIdsAndFramingThenInstructionsThenResources()
    {
        // Arrange
        var contextA = Guid.NewGuid();
        var contextB = Guid.NewGuid();
        var onDemandId = Guid.NewGuid();
        var alwaysId = Guid.NewGuid();
        var project = new AIProject
        {
            Id = Guid.NewGuid(),
            Name = "Docs",
            Description = "The documentation project.",
            Instructions = "Answer in British English.",
            ContextIds = [contextA, contextB],
            Resources =
            [
                new AIAttachedResource { Id = alwaysId, ResourceTypeId = "text", Name = "Always fact", SortOrder = 1, InjectionMode = AIContextResourceInjectionMode.Always },
                new AIAttachedResource { Id = onDemandId, ResourceTypeId = "file", Name = "Big doc", SortOrder = 0, InjectionMode = AIContextResourceInjectionMode.OnDemand },
            ],
        };

        // Act
        var properties = ProjectRuntimeContextBuilder.Build(project);

        // Assert
        properties.ShouldNotBeNull();

        properties!.ContainsKey(CoreConstants.ContextKeys.AdditionalContextIds).ShouldBeTrue();
        var contextIds = properties[CoreConstants.ContextKeys.AdditionalContextIds].ShouldBeAssignableTo<IReadOnlyList<Guid>>();
        contextIds!.ShouldBe([contextA, contextB]);

        var resources = properties[CoreConstants.ContextKeys.AdditionalResources]
            .ShouldBeAssignableTo<IReadOnlyList<AIContextResolverResource>>();
        resources!.Count.ShouldBe(4);

        // 1) Framing — text, Always, content mentions the name and description.
        var framing = resources[0];
        framing.ResourceTypeId.ShouldBe(TextResourceTypeId);
        framing.InjectionMode.ShouldBe(AIContextResourceInjectionMode.Always);
        var framingContent = framing.Settings.ShouldBeOfType<TextResourceSettings>().Content!;
        framingContent.ShouldContain("Docs");
        framingContent.ShouldContain("The documentation project.");

        // 2) Instructions — text, Always, verbatim instructions.
        var instructions = resources[1];
        instructions.ResourceTypeId.ShouldBe(TextResourceTypeId);
        instructions.InjectionMode.ShouldBe(AIContextResourceInjectionMode.Always);
        instructions.Name.ShouldBe("Project instructions");
        instructions.Settings.ShouldBeOfType<TextResourceSettings>().Content.ShouldBe("Answer in British English.");

        // 3) The project's own resources, in SortOrder (OnDemand sort 0 before Always sort 1),
        //    with their injection modes preserved.
        resources[2].Id.ShouldBe(onDemandId);
        resources[2].InjectionMode.ShouldBe(AIContextResourceInjectionMode.OnDemand);
        resources[3].Id.ShouldBe(alwaysId);
        resources[3].InjectionMode.ShouldBe(AIContextResourceInjectionMode.Always);
    }

    [Fact]
    public void Build_NameOnly_StillEmitsFraming_AndNoContextIds()
    {
        // Arrange — a project with only a name (no instructions, resources, or contexts).
        var project = new AIProject { Id = Guid.NewGuid(), Name = "Marketing" };

        // Act
        var properties = ProjectRuntimeContextBuilder.Build(project);

        // Assert
        properties.ShouldNotBeNull();
        properties!.ContainsKey(CoreConstants.ContextKeys.AdditionalContextIds).ShouldBeFalse();

        var resources = properties[CoreConstants.ContextKeys.AdditionalResources]
            .ShouldBeAssignableTo<IReadOnlyList<AIContextResolverResource>>();
        resources!.Count.ShouldBe(1);
        resources[0].ResourceTypeId.ShouldBe(TextResourceTypeId);
        resources[0].Settings.ShouldBeOfType<TextResourceSettings>().Content!.ShouldContain("Marketing");
    }

    [Fact]
    public void Build_EmptyProject_ReturnsNull()
    {
        // Arrange — nothing to contribute (no name, instructions, resources, or contexts).
        var project = new AIProject { Id = Guid.NewGuid(), Name = string.Empty };

        // Act / Assert
        ProjectRuntimeContextBuilder.Build(project).ShouldBeNull();
    }
}
