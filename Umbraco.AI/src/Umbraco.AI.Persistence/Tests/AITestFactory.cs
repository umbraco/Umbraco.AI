using System.Text.Json;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AITest"/> domain models and <see cref="AITestEntity"/> database entities.
/// </summary>
internal static class AITestFactory
{
    /// <summary>
    /// Creates an <see cref="AITest"/> domain model from a database entity.
    /// </summary>
    public static AITest BuildDomain(AITestEntity entity)
    {
        IReadOnlyList<string> tags = Array.Empty<string>();
        if (!string.IsNullOrEmpty(entity.Tags))
        {
            tags = entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        IReadOnlyList<AITestGraderConfig> graders = Array.Empty<AITestGraderConfig>();
        if (!string.IsNullOrEmpty(entity.GradersJson))
        {
            graders = (IReadOnlyList<AITestGraderConfig>?)JsonSerializer.Deserialize<List<AITestGraderConfig>>(entity.GradersJson) ?? Array.Empty<AITestGraderConfig>();
        }

        JsonElement? testCase = null;
        if (!string.IsNullOrEmpty(entity.TestCaseJson))
        {
            // Parse JSON to JsonElement - test features will deserialize to their specific types
            testCase = JsonSerializer.Deserialize<JsonElement>(entity.TestCaseJson);
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
    public static AITestEntity BuildEntity(AITest test)
    {
        return new AITestEntity
        {
            Id = test.Id,
            Alias = test.Alias,
            Name = test.Name,
            Description = test.Description,
            TestFeatureId = test.TestFeatureId,
            TestTargetId = test.TestTargetId,
            TestCaseJson = test.TestCase.HasValue
                ? JsonSerializer.Serialize(test.TestCase.Value)
                : "{}",
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
    public static void UpdateEntity(AITestEntity entity, AITest test)
    {
        entity.Alias = test.Alias;
        entity.Name = test.Name;
        entity.Description = test.Description;
        entity.TestFeatureId = test.TestFeatureId;
        entity.TestTargetId = test.TestTargetId;
        entity.TestCaseJson = test.TestCase.HasValue
            ? JsonSerializer.Serialize(test.TestCase.Value)
            : "{}";
        entity.GradersJson = test.Graders.Count > 0 ? JsonSerializer.Serialize(test.Graders) : null;
        entity.RunCount = test.RunCount;
        entity.Tags = test.Tags.Count > 0 ? string.Join(',', test.Tags) : null;
        entity.IsActive = test.IsActive;
        entity.BaselineRunId = test.BaselineRunId;
        entity.Version = test.Version;
        entity.DateModified = test.DateModified;
        entity.ModifiedByUserId = test.ModifiedByUserId;
    }
}
