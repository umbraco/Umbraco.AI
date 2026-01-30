using Umbraco.Ai.Core.Tests;

namespace Umbraco.Ai.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AiTestGraderResult"/> domain models and <see cref="AiTestGraderResultEntity"/> database entities.
/// </summary>
internal static class AiTestGraderResultFactory
{
    /// <summary>
    /// Creates an <see cref="AiTestGraderResult"/> domain model from a database entity.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    public static AiTestGraderResult BuildDomain(AiTestGraderResultEntity entity)
    {
        return new AiTestGraderResult
        {
            Id = entity.Id,
            RunId = entity.RunId,
            GraderId = entity.GraderId,
            Passed = entity.Passed,
            Score = entity.Score,
            ActualValue = entity.ActualValue,
            ExpectedValue = entity.ExpectedValue,
            FailureMessage = entity.FailureMessage,
            MetadataJson = entity.MetadataJson
        };
    }

    /// <summary>
    /// Creates an <see cref="AiTestGraderResultEntity"/> database entity from a domain model.
    /// </summary>
    /// <param name="result">The domain model.</param>
    /// <returns>The database entity.</returns>
    public static AiTestGraderResultEntity BuildEntity(AiTestGraderResult result)
    {
        return new AiTestGraderResultEntity
        {
            Id = result.Id,
            RunId = result.RunId,
            GraderId = result.GraderId,
            Passed = result.Passed,
            Score = result.Score,
            ActualValue = result.ActualValue,
            ExpectedValue = result.ExpectedValue,
            FailureMessage = result.FailureMessage,
            MetadataJson = result.MetadataJson
        };
    }
}
