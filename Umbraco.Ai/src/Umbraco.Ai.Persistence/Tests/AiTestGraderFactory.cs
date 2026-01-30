using Umbraco.Ai.Core.Tests;

namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AiTestGrader"/> domain models and <see cref="AiTestGraderEntity"/> database entities.
/// </summary>
internal static class AiTestGraderFactory
{
    /// <summary>
    /// Creates an <see cref="AiTestGrader"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiTestGrader BuildDomain(AiTestGraderEntity entity)
    {
        return new AiTestGrader
        {
            Id = entity.Id,
            TestId = entity.TestId,
            GraderTypeId = entity.GraderTypeId,
            Name = entity.Name,
            Description = entity.Description,
            ConfigJson = entity.ConfigJson,
            Negate = entity.Negate,
            Severity = (AiTestGraderSeverity)entity.Severity,
            Weight = entity.Weight,
            SortOrder = entity.SortOrder
        };
    }

    /// <summary>
    /// Creates an <see cref="AiTestGraderEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="grader">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiTestGraderEntity BuildEntity(AiTestGrader grader)
    {
        return new AiTestGraderEntity
        {
            Id = grader.Id,
            TestId = grader.TestId,
            GraderTypeId = grader.GraderTypeId,
            Name = grader.Name,
            Description = grader.Description,
            ConfigJson = grader.ConfigJson,
            Negate = grader.Negate,
            Severity = (int)grader.Severity,
            Weight = grader.Weight,
            SortOrder = grader.SortOrder
        };
    }
}
