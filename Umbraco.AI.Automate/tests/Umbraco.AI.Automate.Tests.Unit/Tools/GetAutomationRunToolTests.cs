using Moq;
using Shouldly;
using Umbraco.AI.Automate.Tools;
using Umbraco.AI.Core.Tools;
using Umbraco.Automate.Core.Runs;
using Xunit;

namespace Umbraco.AI.Automate.Tests.Unit.Tools;

public class GetAutomationRunToolTests
{
    private readonly Mock<IAutomationRunService> _runServiceMock = new();

    private static readonly Guid TestRunId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public async Task ExecuteAsync_WithExistingRun_ReturnsStatus()
    {
        // Arrange
        var run = new AutomationRun
        {
            AutomationId = Guid.NewGuid(),
            AutomationVersion = 1,
            WorkspaceId = Guid.NewGuid(),
            ServiceAccountKey = Guid.NewGuid(),
            InitiatedBy = "ai-agent",
            Status = AutomationRunStatus.Completed,
        };
        typeof(AutomationRun).GetProperty(nameof(AutomationRun.Id))!.SetValue(run, TestRunId);

        _runServiceMock
            .Setup(s => s.GetRunAsync(TestRunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var tool = new GetAutomationRunTool(_runServiceMock.Object);

        // Act
        var result = await ((IAITool)tool).ExecuteAsync(new GetAutomationRunArgs(TestRunId));

        // Assert
        var typedResult = result.ShouldBeOfType<GetAutomationRunResult>();
        typedResult.Success.ShouldBeTrue();
        typedResult.Status.ShouldBe("Completed");
        typedResult.RunId.ShouldBe(TestRunId);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyRunId_ReturnsError()
    {
        // Arrange
        var tool = new GetAutomationRunTool(_runServiceMock.Object);

        // Act
        var result = await ((IAITool)tool).ExecuteAsync(new GetAutomationRunArgs(Guid.Empty));

        // Assert
        var typedResult = result.ShouldBeOfType<GetAutomationRunResult>();
        typedResult.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentRun_ReturnsNotFound()
    {
        // Arrange
        _runServiceMock
            .Setup(s => s.GetRunAsync(TestRunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AutomationRun?)null);

        var tool = new GetAutomationRunTool(_runServiceMock.Object);

        // Act
        var result = await ((IAITool)tool).ExecuteAsync(new GetAutomationRunArgs(TestRunId));

        // Assert
        var typedResult = result.ShouldBeOfType<GetAutomationRunResult>();
        typedResult.Success.ShouldBeFalse();
        typedResult.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithStepRuns_ReturnsStepDetails()
    {
        // Arrange
        var run = new AutomationRun
        {
            AutomationId = Guid.NewGuid(),
            AutomationVersion = 1,
            WorkspaceId = Guid.NewGuid(),
            ServiceAccountKey = Guid.NewGuid(),
            InitiatedBy = "ai-agent",
            Status = AutomationRunStatus.Running,
            StepRuns =
            [
                new StepRun
                {
                    RunId = TestRunId,
                    StepId = Guid.NewGuid(),
                    ActionAlias = "umbracoAutomate.sendEmail",
                    Status = StepRunStatus.Completed,
                    Duration = TimeSpan.FromSeconds(2.5),
                },
                new StepRun
                {
                    RunId = TestRunId,
                    StepId = Guid.NewGuid(),
                    ActionAlias = "umbracoAI.runAgent",
                    Status = StepRunStatus.Running,
                },
            ],
        };
        typeof(AutomationRun).GetProperty(nameof(AutomationRun.Id))!.SetValue(run, TestRunId);

        _runServiceMock
            .Setup(s => s.GetRunAsync(TestRunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var tool = new GetAutomationRunTool(_runServiceMock.Object);

        // Act
        var result = await ((IAITool)tool).ExecuteAsync(new GetAutomationRunArgs(TestRunId));

        // Assert
        var typedResult = result.ShouldBeOfType<GetAutomationRunResult>();
        typedResult.Steps.Count.ShouldBe(2);
        typedResult.Steps[0].ActionAlias.ShouldBe("umbracoAutomate.sendEmail");
        typedResult.Steps[0].Status.ShouldBe("Completed");
        typedResult.Steps[0].DurationSeconds.ShouldBe(2.5);
        typedResult.Steps[1].Status.ShouldBe("Running");
    }
}
