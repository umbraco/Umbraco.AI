using System.Text.Json;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Versionable entity adapter for AI tests.
/// </summary>
internal sealed class AITestVersionableEntityAdapter : AIVersionableEntityAdapterBase<AITest>
{
    private readonly IAITestService _testService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AITestVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="testService">The test service for rollback operations.</param>
    public AITestVersionableEntityAdapter(IAITestService testService)
    {
        _testService = testService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AITest entity)
    {
        var snapshot = new
        {
            entity.Id,
            entity.Alias,
            entity.Name,
            entity.Description,
            entity.TestFeatureId,
            entity.TestTargetId,
            TestCase = entity.TestCase?.ToString(),
            Graders = entity.Graders.Select(g => new
            {
                g.Id,
                g.GraderTypeId,
                g.Name,
                g.Description,
                g.ConfigJson,
                g.Negate,
                Severity = (int)g.Severity,
                g.Weight
            }).ToArray(),
            entity.RunCount,
            Tags = entity.Tags.Count > 0 ? string.Join(',', entity.Tags) : null,
            entity.IsActive,
            entity.BaselineRunId,
            entity.Version,
            entity.DateCreated,
            entity.DateModified,
            entity.CreatedByUserId,
            entity.ModifiedByUserId
        };

        return JsonSerializer.Serialize(snapshot, Constants.DefaultJsonSerializerOptions);
    }

    /// <inheritdoc />
    protected override AITest? RestoreFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            IReadOnlyList<string> tags = Array.Empty<string>();
            if (root.TryGetProperty("tags", out var tagsElement) &&
                tagsElement.ValueKind == JsonValueKind.String)
            {
                var tagsString = tagsElement.GetString();
                if (!string.IsNullOrEmpty(tagsString))
                {
                    tags = tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
            }

            JsonElement? testCase = null;
            if (root.TryGetProperty("testCase", out var testCaseElement) &&
                testCaseElement.ValueKind == JsonValueKind.String)
            {
                var testCaseJson = testCaseElement.GetString();
                if (!string.IsNullOrEmpty(testCaseJson))
                {
                    testCase = JsonDocument.Parse(testCaseJson).RootElement;
                }
            }

            var graders = new List<AITestGraderConfig>();
            if (root.TryGetProperty("graders", out var gradersElement) &&
                gradersElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var graderElement in gradersElement.EnumerateArray())
                {
                    var grader = new AITestGraderConfig
                    {
                        Id = graderElement.GetProperty("id").GetGuid(),
                        GraderTypeId = graderElement.GetProperty("graderTypeId").GetString()!,
                        Name = graderElement.GetProperty("name").GetString()!,
                        Description = graderElement.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String
                            ? descProp.GetString()
                            : null,
                        ConfigJson = graderElement.TryGetProperty("configJson", out var configProp) && configProp.ValueKind == JsonValueKind.String
                            ? configProp.GetString()
                            : null,
                        Negate = graderElement.TryGetProperty("negate", out var negateProp) && negateProp.GetBoolean(),
                        Severity = graderElement.TryGetProperty("severity", out var severityProp)
                            ? (AITestGraderSeverity)severityProp.GetInt32()
                            : AITestGraderSeverity.Error,
                        Weight = graderElement.TryGetProperty("weight", out var weightProp)
                            ? weightProp.GetDouble()
                            : 1.0
                    };

                    graders.Add(grader);
                }
            }

            return new AITest
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                Description = root.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String
                    ? desc.GetString()
                    : null,
                TestFeatureId = root.GetProperty("testFeatureId").GetString()!,
                TestTargetId = root.GetProperty("testTargetId").GetGuid(),
                TestCase = testCase,
                Graders = graders,
                RunCount = root.GetProperty("runCount").GetInt32(),
                Tags = tags,
                IsActive = root.TryGetProperty("isActive", out var isActive) && isActive.GetBoolean(),
                BaselineRunId = root.TryGetProperty("baselineRunId", out var baseline) && baseline.ValueKind != JsonValueKind.Null
                    ? baseline.GetGuid()
                    : null,
                Version = root.GetProperty("version").GetInt32(),
                DateCreated = root.GetProperty("dateCreated").GetDateTime(),
                DateModified = root.GetProperty("dateModified").GetDateTime(),
                CreatedByUserId = root.TryGetProperty("createdByUserId", out var cbu) && cbu.ValueKind != JsonValueKind.Null && cbu.TryGetGuid(out var cbuGuid)
                    ? cbuGuid : null,
                ModifiedByUserId = root.TryGetProperty("modifiedByUserId", out var mbu) && mbu.ValueKind != JsonValueKind.Null && mbu.TryGetGuid(out var mbuGuid)
                    ? mbuGuid : null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyList<AIValueChange> CompareVersions(AITest from, AITest to)
    {
        var changes = new List<AIValueChange>();

        if (from.Alias != to.Alias)
        {
            changes.Add(new AIValueChange("Alias", from.Alias, to.Alias));
        }

        if (from.Name != to.Name)
        {
            changes.Add(new AIValueChange("Name", from.Name, to.Name));
        }

        if (from.Description != to.Description)
        {
            changes.Add(new AIValueChange("Description", from.Description ?? "null", to.Description ?? "null"));
        }

        if (from.TestFeatureId != to.TestFeatureId)
        {
            changes.Add(new AIValueChange("TestFeatureId", from.TestFeatureId, to.TestFeatureId));
        }

        if (from.TestTargetId != to.TestTargetId)
        {
            changes.Add(new AIValueChange("TestTargetId", from.TestTargetId.ToString(), to.TestTargetId.ToString()));
        }

        // Compare test case JSON
        var fromTestCase = from.TestCase?.ToString() ?? string.Empty;
        var toTestCase = to.TestCase?.ToString() ?? string.Empty;
        if (fromTestCase != toTestCase)
        {
            changes.Add(new AIValueChange(
                "TestCase",
                AIJsonComparer.TruncateValue(fromTestCase),
                AIJsonComparer.TruncateValue(toTestCase)));
        }

        // Compare graders
        var fromGraders = string.Join(",", from.Graders.Select(g => $"{g.GraderTypeId}:{g.Name}"));
        var toGraders = string.Join(",", to.Graders.Select(g => $"{g.GraderTypeId}:{g.Name}"));
        if (fromGraders != toGraders)
        {
            changes.Add(new AIValueChange("Graders", fromGraders, toGraders));
        }

        if (from.RunCount != to.RunCount)
        {
            changes.Add(new AIValueChange("RunCount", from.RunCount.ToString(), to.RunCount.ToString()));
        }

        // Compare tags
        var fromTags = string.Join(",", from.Tags);
        var toTags = string.Join(",", to.Tags);
        if (fromTags != toTags)
        {
            changes.Add(new AIValueChange("Tags", fromTags, toTags));
        }

        if (from.IsActive != to.IsActive)
        {
            changes.Add(new AIValueChange("IsActive", from.IsActive.ToString(), to.IsActive.ToString()));
        }

        if (from.BaselineRunId != to.BaselineRunId)
        {
            changes.Add(new AIValueChange(
                "BaselineRunId",
                from.BaselineRunId?.ToString() ?? "null",
                to.BaselineRunId?.ToString() ?? "null"));
        }

        return changes;
    }

    /// <inheritdoc />
    public override Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
        => _testService.RollbackTestAsync(entityId, version, cancellationToken);

    /// <inheritdoc />
    protected override Task<AITest?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _testService.GetTestAsync(entityId, cancellationToken);
}
