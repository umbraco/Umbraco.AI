using Umbraco.Ai.Core.Tests;

namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AiTest"/> domain models and <see cref="AiTestEntity"/> database entities.
/// </summary>
internal static class AiTestFactory
{
    /// <summary>
    /// Creates an <see cref="AiTest"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiTest BuildDomain(AiTestEntity entity)
    {
        IReadOnlyList<string> tags = Array.Empty<string>();
        if (!string.IsNullOrEmpty(entity.Tags))
        {
            tags = entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return new AiTest
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            TestTypeId = entity.TestTypeId,
            Target = new AiTestTarget
            {
                TargetId = entity.TargetId,
                IsAlias = entity.TargetIsAlias
            },
            TestCase = new AiTestCase
            {
                TestCaseJson = entity.TestCaseJson
            },
            Graders = entity.Graders.Select(AiTestGraderFactory.BuildDomain).ToList(),
            RunCount = entity.RunCount,
            Tags = tags,
            IsEnabled = entity.IsEnabled,
            BaselineRunId = entity.BaselineRunId,
            Version = entity.Version,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId
        };
    }

    /// <summary>
    /// Creates an <see cref="AiTestEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="test">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiTestEntity BuildEntity(AiTest test)
    {
        return new AiTestEntity
        {
            Id = test.Id,
            Alias = test.Alias,
            Name = test.Name,
            Description = test.Description,
            TestTypeId = test.TestTypeId,
            TargetId = test.Target.TargetId,
            TargetIsAlias = test.Target.IsAlias,
            TestCaseJson = test.TestCase.TestCaseJson,
            RunCount = test.RunCount,
            Tags = test.Tags.Any() ? string.Join(',', test.Tags) : null,
            IsEnabled = test.IsEnabled,
            BaselineRunId = test.BaselineRunId,
            Version = test.Version,
            DateCreated = test.DateCreated,
            DateModified = test.DateModified,
            CreatedByUserId = test.CreatedByUserId,
            ModifiedByUserId = test.ModifiedByUserId,
            Graders = test.Graders.Select(AiTestGraderFactory.BuildEntity).ToList()
        };
    }

    /// <summary>
    /// Updates an existing <see cref="AiTestEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="test">The domain model with updated values.</param>
    public static void UpdateEntity(AiTestEntity entity, AiTest test)
    {
        entity.Alias = test.Alias;
        entity.Name = test.Name;
        entity.Description = test.Description;
        entity.TestTypeId = test.TestTypeId;
        entity.TargetId = test.Target.TargetId;
        entity.TargetIsAlias = test.Target.IsAlias;
        entity.TestCaseJson = test.TestCase.TestCaseJson;
        entity.RunCount = test.RunCount;
        entity.Tags = test.Tags.Any() ? string.Join(',', test.Tags) : null;
        entity.IsEnabled = test.IsEnabled;
        entity.BaselineRunId = test.BaselineRunId;
        entity.Version = test.Version;
        entity.DateModified = test.DateModified;
        entity.ModifiedByUserId = test.ModifiedByUserId;
        // DateCreated and CreatedByUserId are intentionally not updated

        // Update graders - remove old, add new
        entity.Graders.Clear();
        foreach (var grader in test.Graders)
        {
            entity.Graders.Add(AiTestGraderFactory.BuildEntity(grader));
        }
    }
}
