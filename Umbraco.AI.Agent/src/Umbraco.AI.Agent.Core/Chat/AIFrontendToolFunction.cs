using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.Agent.Core.Chat;

/// <summary>
/// A frontend tool function that exposes the tool's parameter schema to the LLM
/// but doesn't actually execute - execution happens on the client.
/// </summary>
/// <remarks>
/// <para>
/// When this function is invoked by <see cref="FunctionInvokingChatClient"/>, it sets
/// <see cref="FunctionInvokingChatClient.CurrentContext"/>.<see cref="FunctionInvokingChatClient.FunctionInvocationContext.Terminate"/>
/// to <c>true</c>, which stops the automatic tool invocation loop.
/// </para>
/// <para>
/// This allows the controller to emit the tool call to the frontend client, where
/// the actual tool execution takes place. The run completes with an "interrupt" outcome,
/// and the client can resume with tool results.
/// </para>
/// </remarks>
public sealed class AIFrontendToolFunction : AIFunction
{
    private readonly string _name;
    private readonly string _description;
    private readonly JsonElement _jsonSchema;

    /// <summary>
    /// Gets the scope identifier for permission checks.
    /// </summary>
    public string? Scope { get; }

    /// <summary>
    /// Gets whether this tool performs destructive operations.
    /// </summary>
    public bool IsDestructive { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFrontendToolFunction"/> class.
    /// </summary>
    /// <param name="tool">The AG-UI tool definition.</param>
    /// <param name="scope">Optional scope identifier for permission checks.</param>
    /// <param name="isDestructive">Whether the tool is destructive.</param>
    public AIFrontendToolFunction(AGUITool tool, string? scope = null, bool isDestructive = false)
    {
        ArgumentNullException.ThrowIfNull(tool);

        _name = tool.Name;
        _description = tool.Description;
        _jsonSchema = BuildJsonSchema(tool.Parameters);
        Scope = scope;
        IsDestructive = isDestructive;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFrontendToolFunction"/> class.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <param name="jsonSchema">The JSON schema for the tool parameters.</param>
    /// <param name="scope">Optional scope identifier for permission checks.</param>
    /// <param name="isDestructive">Whether the tool is destructive.</param>
    public AIFrontendToolFunction(
        string name,
        string description,
        JsonElement jsonSchema,
        string? scope = null,
        bool isDestructive = false)
    {
        ArgumentNullException.ThrowIfNull(name);

        _name = name;
        _description = description ?? string.Empty;
        _jsonSchema = jsonSchema;
        Scope = scope;
        IsDestructive = isDestructive;
    }

    /// <inheritdoc />
    public override string Name => _name;

    /// <inheritdoc />
    public override string Description => _description;

    /// <inheritdoc />
    public override JsonElement JsonSchema => _jsonSchema;

    /// <inheritdoc />
    protected override ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        // Tell FunctionInvokingChatClient to stop its invocation loop.
        // This allows us to return out and emit the tool call to the frontend
        // instead of auto-executing and feeding a fake result back to the model.
        if (FunctionInvokingChatClient.CurrentContext is not null)
        {
            FunctionInvokingChatClient.CurrentContext.Terminate = true;
        }

        return ValueTask.FromResult<object?>(null);
    }

    private static JsonElement BuildJsonSchema(AGUIToolParameters parameters)
    {
        var schemaObj = new Dictionary<string, object?>
        {
            ["type"] = parameters.Type,
            ["properties"] = parameters.Properties,
        };

        if (parameters.Required?.Any() == true)
        {
            schemaObj["required"] = parameters.Required;
        }

        return JsonSerializer.SerializeToElement(schemaObj);
    }
}
