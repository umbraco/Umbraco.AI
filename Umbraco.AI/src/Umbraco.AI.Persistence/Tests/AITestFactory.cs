using System.Text.Json;
using System.Text.Json.Nodes;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AITest"/> domain models and <see cref="AITestEntity"/> database entities.
/// Handles encryption/decryption of sensitive config fields during the mapping process.
/// </summary>
internal sealed class AITestFactory : IAITestFactory
{
    private readonly IAIEditableModelSerializer _serializer;
    private readonly AITestFeatureCollection _testFeatures;
    private readonly AITestGraderCollection _graders;

    public AITestFactory(
        IAIEditableModelSerializer serializer,
        AITestFeatureCollection testFeatures,
        AITestGraderCollection graders)
    {
        _serializer = serializer;
        _testFeatures = testFeatures;
        _graders = graders;
    }

    /// <inheritdoc />
    public AITest BuildDomain(AITestEntity entity)
    {
        IReadOnlyList<string> tags = Array.Empty<string>();
        if (!string.IsNullOrEmpty(entity.Tags))
        {
            tags = entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        IReadOnlyList<AITestGraderConfig> graders = Array.Empty<AITestGraderConfig>();
        if (!string.IsNullOrEmpty(entity.GradersJson))
        {
            graders = DeserializeGraders(entity.GradersJson);
        }

        JsonElement? testFeatureConfig = null;
        if (!string.IsNullOrEmpty(entity.TestFeatureConfigJson))
        {
            // Deserialize with automatic decryption of sensitive fields
            testFeatureConfig = _serializer.Deserialize(entity.TestFeatureConfigJson);
        }

        return new AITest
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Description = entity.Description,
            TestFeatureId = entity.TestFeatureId,
            TestTargetId = entity.TestTargetId,
            TestFeatureConfig = testFeatureConfig,
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

    /// <inheritdoc />
    public AITestEntity BuildEntity(AITest test)
    {
        return new AITestEntity
        {
            Id = test.Id,
            Alias = test.Alias,
            Name = test.Name,
            Description = test.Description,
            TestFeatureId = test.TestFeatureId,
            TestTargetId = test.TestTargetId,
            TestFeatureConfigJson = SerializeTestFeatureConfig(test),
            GradersJson = SerializeGraders(test.Graders, test),
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

    /// <inheritdoc />
    public void UpdateEntity(AITestEntity entity, AITest test)
    {
        entity.Alias = test.Alias;
        entity.Name = test.Name;
        entity.Description = test.Description;
        entity.TestFeatureId = test.TestFeatureId;
        entity.TestTargetId = test.TestTargetId;
        entity.TestFeatureConfigJson = SerializeTestFeatureConfig(test);
        entity.GradersJson = SerializeGraders(test.Graders, test);
        entity.RunCount = test.RunCount;
        entity.Tags = test.Tags.Count > 0 ? string.Join(',', test.Tags) : null;
        entity.IsActive = test.IsActive;
        entity.BaselineRunId = test.BaselineRunId;
        entity.Version = test.Version;
        entity.DateModified = test.DateModified;
        entity.ModifiedByUserId = test.ModifiedByUserId;
    }

    /// <summary>
    /// Serializes the test feature config with automatic encryption of sensitive fields.
    /// </summary>
    private string SerializeTestFeatureConfig(AITest test)
    {
        if (!test.TestFeatureConfig.HasValue)
        {
            return "{}";
        }

        var schema = GetTestFeatureConfigSchema(test.TestFeatureId);
        return _serializer.Serialize(test.TestFeatureConfig.Value, schema) ?? "{}";
    }

    /// <summary>
    /// Serializes the graders list to a JSON string for database storage.
    /// Each grader's Config (JsonElement) is stored as a "ConfigJson" string in the DB format,
    /// with sensitive fields encrypted using the grader's schema.
    /// </summary>
    private string? SerializeGraders(IReadOnlyList<AITestGraderConfig> graderConfigs, AITest test)
    {
        if (graderConfigs.Count == 0)
        {
            return null;
        }

        var array = new JsonArray();

        foreach (var graderConfig in graderConfigs)
        {
            // Encrypt config if schema is available
            string? configJson = null;
            if (graderConfig.Config is { } configElement)
            {
                var schema = GetGraderConfigSchema(graderConfig.GraderTypeId);
                configJson = schema != null
                    ? _serializer.Serialize(configElement, schema)
                    : configElement.GetRawText();
            }

            var graderObj = new JsonObject
            {
                [nameof(AITestGraderConfig.Id)] = graderConfig.Id,
                [nameof(AITestGraderConfig.GraderTypeId)] = graderConfig.GraderTypeId,
                [nameof(AITestGraderConfig.Name)] = graderConfig.Name,
                [nameof(AITestGraderConfig.Description)] = graderConfig.Description,
                ["ConfigJson"] = configJson,
                [nameof(AITestGraderConfig.Negate)] = graderConfig.Negate,
                [nameof(AITestGraderConfig.Severity)] = (int)graderConfig.Severity,
                [nameof(AITestGraderConfig.Weight)] = graderConfig.Weight
            };

            array.Add(graderObj);
        }

        return array.ToJsonString();
    }

    /// <summary>
    /// Deserializes the graders list from database JSON, decrypting each grader's config.
    /// The DB format stores config as a "ConfigJson" string which is converted to JsonElement.
    /// </summary>
    private IReadOnlyList<AITestGraderConfig> DeserializeGraders(string gradersJson)
    {
        using var doc = JsonDocument.Parse(gradersJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<AITestGraderConfig>();
        }

        var result = new List<AITestGraderConfig>();

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            JsonElement? config = null;

            if (element.TryGetProperty("ConfigJson", out var configJsonProp) &&
                configJsonProp.ValueKind == JsonValueKind.String)
            {
                var configJsonStr = configJsonProp.GetString();
                if (!string.IsNullOrWhiteSpace(configJsonStr))
                {
                    // Decrypt sensitive fields
                    var decrypted = _serializer.Deserialize(configJsonStr);
                    config = decrypted;
                }
            }

            var grader = new AITestGraderConfig
            {
                Id = element.GetProperty(nameof(AITestGraderConfig.Id)).GetGuid(),
                GraderTypeId = element.GetProperty(nameof(AITestGraderConfig.GraderTypeId)).GetString()!,
                Name = element.GetProperty(nameof(AITestGraderConfig.Name)).GetString()!,
                Description = element.TryGetProperty(nameof(AITestGraderConfig.Description), out var descProp) && descProp.ValueKind == JsonValueKind.String
                    ? descProp.GetString()
                    : null,
                Config = config,
                Negate = element.TryGetProperty(nameof(AITestGraderConfig.Negate), out var negateProp) && negateProp.GetBoolean(),
                Severity = element.TryGetProperty(nameof(AITestGraderConfig.Severity), out var severityProp)
                    ? (AITestGraderSeverity)severityProp.GetInt32()
                    : AITestGraderSeverity.Error,
                Weight = element.TryGetProperty(nameof(AITestGraderConfig.Weight), out var weightProp)
                    ? weightProp.GetDouble()
                    : 1.0
            };

            result.Add(grader);
        }

        return result;
    }

    private AIEditableModelSchema? GetTestFeatureConfigSchema(string testFeatureId)
    {
        var feature = _testFeatures.GetById(testFeatureId);
        return feature?.GetConfigSchema();
    }

    private AIEditableModelSchema? GetGraderConfigSchema(string graderTypeId)
    {
        var grader = _graders.GetById(graderTypeId);
        return grader?.GetConfigSchema();
    }
}
