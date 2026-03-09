using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Agent.Core.Workflows;

/// <summary>
/// Base class for AI agent workflow implementations with typed settings.
/// </summary>
/// <typeparam name="TSettings">The type of settings object required by this workflow.</typeparam>
/// <remarks>
/// <para>
/// Extend this class when your workflow requires custom settings that should be
/// configurable in the backoffice. The settings type will be used to generate
/// a dynamic form via <see cref="IAIEditableModelSchemaBuilder"/>.
/// </para>
/// </remarks>
public abstract class AIAgentWorkflowBase<TSettings> : AIAgentWorkflowBase
    where TSettings : class, new()
{
    private readonly IAIEditableModelSchemaBuilder _schemaBuilder;

    /// <inheritdoc />
    public override Type? SettingsType => typeof(TSettings);

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentWorkflowBase{TSettings}"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder for generating settings UI.</param>
    protected AIAgentWorkflowBase(IAIEditableModelSchemaBuilder schemaBuilder)
    {
        _schemaBuilder = schemaBuilder;
    }

    /// <inheritdoc />
    public override AIEditableModelSchema? GetSettingsSchema()
        => _schemaBuilder.BuildForType<TSettings>(Id);
}
