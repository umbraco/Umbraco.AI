using System.Text.Json;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AITest"/> domain models and <see cref="AITestEntity"/> database entities.
/// Handles serialization/deserialization of test case data based on the test feature's schema.
/// </summary>
internal sealed class AITestFactory
{
    private readonly IAIEditableModelSerializer _serializer;
    private readonly AITestFeatureCollection _testFeatures;

    public AITestFactory(
        IAIEditableModelSerializer serializer,
        AITestFeatureCollection testFeatures)
    {
        _serializer = serializer;
        _testFeatures = testFeatures;
    }

    /// <summary>
    /// Creates an <see cref="AITest"/> domain model from a database entity.
    /// </summary>
    public AITest BuildDomain(AITestEntity entity)
    {
        IReadOnlyList<string> tags = Array.Empty<string>();
        if (!string.IsNullOrEmpty(entity.Tags))
        {
            tags = entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        IReadOnlyList<AITestGrader> graders = Array.Empty<AITestGrader>();
        if (!string.IsNullOrEmpty(entity.GradersJson))
        {
            graders = (IReadOnlyList<AITestGrader>?)JsonSerializer.Deserialize<List<AITestGrader>>(entity.GradersJson) ?? Array.Empty<AITestGrader>();
        }

        object? testCase = null;
        if (!string.IsNullOrEmpty(entity.TestCaseJson))
        {
            // Deserialize test case using schema-based deserialization
            testCase = _serializer.Deserialize(entity.TestCaseJson);
        }

        return new AITest
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            TestFeatureId = entity.TestFeatureId,
            TestTargetId = entity.TestTargetId,
            TestCase = testCase,
            Graders = graders,
            RunCount = entity.RunCount,
            Tags = tags,
            IsActive = entity.IsActive,
            BaselineRunId = entity.BaselineRunId,
            Version = entity.Version,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId
        };
    }

    /// <summary>
    /// Creates an <see cref="AITestEntity"/> database entity from a domain model.
    /// </summary>
    public AITestEntity BuildEntity(AITest test)
    {
        var schema = GetSchemaForTestFeature(test.TestFeatureId);

        return new AITestEntity
        {
            Id = test.Id,
            Alias = test.Alias,
            Name = test.Name,
            Description = test.Description,
            TestFeatureId = test.TestFeatureId,
            TestTargetId = test.TestTargetId,
            TestCaseJson = _serializer.Serialize(test.TestCase, schema) ?? "{}",
            GradersJson = test.Graders.Count > 0 ? JsonSerializer.Serialize(test.Graders) : null,
            RunCount = test.RunCount,
            Tags = test.Tags.Count > 0 ? string.Join(',', test.Tags) : null,
            IsActive = test.IsActive,
            BaselineRunId = test.BaselineRunId,
            Version = test.Version,
            DateCreated = test.DateCreated,
            DateModified = test.DateModified,
            CreatedByUserId = test.CreatedByUserId,
            ModifiedByUserId = test.ModifiedByUserId
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AITestEntity"/> with values from a domain model.
    /// </summary>
    public void UpdateEntity(AITestEntity entity, AITest test)
    {
        var schema = GetSchemaForTestFeature(test.TestFeatureId);

        entity.Alias = test.Alias;
        entity.Name = test.Name;
        entity.Description = test.Description;
        entity.TestFeatureId = test.TestFeatureId;
        entity.TestTargetId = test.TestTargetId;
        entity.TestCaseJson = _serializer.Serialize(test.TestCase, schema) ?? "{}";
        entity.GradersJson = test.Graders.Count > 0 ? JsonSerializer.Serialize(test.Graders) : null;
        entity.RunCount = test.RunCount;
        entity.Tags = test.Tags.Count > 0 ? string.Join(',', test.Tags) : null;
        entity.IsActive = test.IsActive;
        entity.BaselineRunId = test.BaselineRunId;
        entity.Version = test.Version;
        entity.DateModified = test.DateModified;
        entity.ModifiedByUserId = test.ModifiedByUserId;
    }

    private AIEditableModelSchema? GetSchemaForTestFeature(string testFeatureId)
    {
        var testFeature = _testFeatures.FirstOrDefault(f => f.Id == testFeatureId);
        return testFeature?.GetTestCaseSchema();
    }
}
