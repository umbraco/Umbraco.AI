using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Umbraco.AI.Agent.Core.Agents;
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
/// <para>
/// The base class handles deserialization of the raw <see cref="JsonElement"/> settings
/// into the strongly-typed <typeparamref name="TSettings"/> object, providing a typed
/// <see cref="BuildWorkflowAsync(AIAgent, TSettings, CancellationToken)"/> for implementations to override.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AIAgentWorkflow("sequential-pipeline", "Sequential Pipeline")]
/// public class SequentialPipelineWorkflow : AIAgentWorkflowBase&lt;SequentialPipelineSettings&gt;
/// {
///     public SequentialPipelineWorkflow(IAIEditableModelSchemaBuilder schemaBuilder)
///         : base(schemaBuilder) { }
///
///     public override Task&lt;Workflow&gt; BuildWorkflowAsync(
///         AIAgent agent, SequentialPipelineSettings settings, CancellationToken ct)
///     {
///         // Build your workflow using typed settings
///     }
/// }
/// </code>
/// </example>
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
    protected override AIEditableModelSchema? GetSettingsSchema()
        => _schemaBuilder.BuildForType<TSettings>(Id);

    /// <summary>
    /// Deserializes settings and delegates to the typed <see cref="BuildWorkflowAsync(AIAgent, TSettings, CancellationToken)"/>.
    /// </summary>
    protected override Task<Workflow> BuildWorkflowAsync(AIAgent agent, JsonElement? settings, CancellationToken cancellationToken)
    {
        if (!settings.HasValue)
        {
            throw new ArgumentException($"Settings are required for workflow '{Id}' but were not provided.");
        }

        var typedSettings =
            settings.Value.Deserialize<TSettings>(Umbraco.AI.Core.Constants.DefaultJsonSerializerOptions)!;

        return BuildWorkflowAsync(agent, typedSettings, cancellationToken);
    }

    /// <summary>
    /// Builds a MAF workflow from the agent definition and strongly-typed settings.
    /// </summary>
    /// <param name="agent">The agent definition containing shared properties like ProfileId.</param>
    /// <param name="settings">The deserialized workflow-specific settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MAF <see cref="Workflow"/> that can be executed or converted to an AIAgent via <c>AsAIAgent</c>.</returns>
    protected abstract Task<Workflow> BuildWorkflowAsync(AIAgent agent, TSettings settings, CancellationToken cancellationToken);
}
