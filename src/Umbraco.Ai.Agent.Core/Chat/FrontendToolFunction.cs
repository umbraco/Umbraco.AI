using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agui.Models;

namespace Umbraco.Ai.Agent.Core.Chat;

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
public sealed class FrontendToolFunction : AIFunction
{
    private readonly string _name;
    private readonly string _description;
    private readonly JsonElement _jsonSchema;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontendToolFunction"/> class.
    /// </summary>
    /// <param name="tool">The AG-UI tool definition.</param>
    public FrontendToolFunction(AguiTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        _name = tool.Name;
        _description = tool.Description;
        _jsonSchema = BuildJsonSchema(tool.Parameters);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontendToolFunction"/> class.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <param name="jsonSchema">The JSON schema for the tool parameters.</param>
    public FrontendToolFunction(string name, string description, JsonElement jsonSchema)
    {
        ArgumentNullException.ThrowIfNull(name);

        _name = name;
        _description = description ?? string.Empty;
        _jsonSchema = jsonSchema;
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

    private static JsonElement BuildJsonSchema(AguiToolParameters parameters)
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
