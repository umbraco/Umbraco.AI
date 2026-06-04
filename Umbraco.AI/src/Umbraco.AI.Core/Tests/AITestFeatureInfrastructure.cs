using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Default implementation of <see cref="IAITestFeatureInfrastructure"/>.
/// </summary>
internal sealed class AITestFeatureInfrastructure(
    AITestContextResolver contextResolver,
    IAIEditableModelSchemaBuilder schemaBuilder,
    IAIEditableModelResolver modelResolver)
    : IAITestFeatureInfrastructure
{
    /// <inheritdoc />
    public AITestContextResolver ContextResolver { get; } = contextResolver;

    /// <inheritdoc />
    public IAIEditableModelSchemaBuilder SchemaBuilder { get; } = schemaBuilder;

    /// <inheritdoc />
    public IAIEditableModelResolver ModelResolver { get; } = modelResolver;
}
