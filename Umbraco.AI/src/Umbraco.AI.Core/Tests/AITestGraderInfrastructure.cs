using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Default implementation of <see cref="IAITestGraderInfrastructure"/>.
/// </summary>
internal sealed class AITestGraderInfrastructure(
    IAIEditableModelSchemaBuilder schemaBuilder,
    IAIEditableModelResolver modelResolver)
    : IAITestGraderInfrastructure
{
    /// <inheritdoc />
    public IAIEditableModelSchemaBuilder SchemaBuilder { get; } = schemaBuilder;

    /// <inheritdoc />
    public IAIEditableModelResolver ModelResolver { get; } = modelResolver;
}
