using System.Text.Json;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Tests.Unit.Services;

public class AITestEntityIdTests
{
    private readonly Mock<IAITestRepository> _repositoryMock = new();

    private AITestService CreateService()
        => new(
            _repositoryMock.Object,
            new Mock<IAIEntityVersionService>().Object,
            new Mock<IAITestRunner>().Object,
            new Mock<IEventAggregator>().Object);

    private void SetupSave()
        => _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AITest>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AITest t, Guid? _, CancellationToken _) => t);

    private static AITestGraderConfig[] Graders() =>
        [new AITestGraderConfig { GraderTypeId = "exact-match", Name = "Grader" }];

    [Fact]
    public async Task SaveTestAsync_WithIdConstructor_KeepsTheId()
    {
        // Arrange
        var knownId = Guid.NewGuid();
        var test = new AITest(knownId)
        {
            Alias = "known-id",
            Name = "Known Id",
            TestFeatureId = "prompt",
            TestTargetId = Guid.NewGuid(),
            TestFeatureConfig = JsonDocument.Parse("{}").RootElement,
            Graders = Graders(),
        };
        SetupSave();

        // Act
        var result = await CreateService().SaveTestAsync(test);

        // Assert
        result.Id.ShouldBe(knownId);
    }

    [Fact]
    public async Task SaveTestAsync_WithParameterlessConstructor_GeneratesAnId()
    {
        // Arrange
        var test = new AITest
        {
            Alias = "no-id",
            Name = "No Id",
            TestFeatureId = "prompt",
            TestTargetId = Guid.NewGuid(),
            TestFeatureConfig = JsonDocument.Parse("{}").RootElement,
            Graders = Graders(),
        };
        SetupSave();

        // Act
        var result = await CreateService().SaveTestAsync(test);

        // Assert
        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void IdConstructor_OnTestRunTranscriptGraderAndVariation_SetsTheId()
    {
        var id = Guid.NewGuid();

        new AITestRun(id) { TestId = Guid.NewGuid() }.Id.ShouldBe(id);
        new AITestTranscript(id) { RunId = Guid.NewGuid(), FinalOutput = JsonDocument.Parse("{}").RootElement }.Id.ShouldBe(id);
        new AITestGraderConfig(id) { GraderTypeId = "exact-match", Name = "Grader" }.Id.ShouldBe(id);
        new AITestVariation(id) { Name = "Variation" }.Id.ShouldBe(id);
    }

    [Fact]
    public void ParameterlessConstructor_OnTestRunTranscriptGraderAndVariation_GeneratesAnId()
    {
        new AITestRun { TestId = Guid.NewGuid() }.Id.ShouldNotBe(Guid.Empty);
        new AITestTranscript { RunId = Guid.NewGuid(), FinalOutput = JsonDocument.Parse("{}").RootElement }.Id.ShouldNotBe(Guid.Empty);
        new AITestGraderConfig { GraderTypeId = "exact-match", Name = "Grader" }.Id.ShouldNotBe(Guid.Empty);
        new AITestVariation { Name = "Variation" }.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void AuditLog_IdConstructor_SetsTheId_AndParameterlessGeneratesOne()
    {
        var id = Guid.NewGuid();

        new AIAuditLog(id).Id.ShouldBe(id);
        new AIAuditLog().Id.ShouldNotBe(Guid.Empty);
    }
}
