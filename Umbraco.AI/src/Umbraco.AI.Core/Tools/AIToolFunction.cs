using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Umbraco.AI.Core.Tools;

/// <summary>
/// <see cref="AIFunction"/> implementation for typed <see cref="IAITool"/> instances that exposes
/// the arguments record's properties as top-level function parameters in the generated JSON schema.
/// </summary>
/// <remarks>
/// The MEAI default <see cref="AIFunctionFactory"/> delegate path used previously produced a schema
/// with a single top-level parameter (named after the delegate parameter, i.e. <c>args</c>) whose
/// value was the <typeparamref name="TArgs"/> object. While OpenAI and Anthropic providers populated
/// that nested object correctly, Google Gemini's function-calling implementation does not reliably
/// fill nested object parameters and instead emits <c>{"args": null}</c>, which caused every typed
/// tool invocation to fail.
///
/// Generating the schema directly from <typeparamref name="TArgs"/> places each property at the top
/// level, which all providers handle consistently.
/// </remarks>
/// <typeparam name="TArgs">The typed arguments record for the tool.</typeparam>
internal sealed class AIToolFunction<TArgs> : AIFunction where TArgs : class
{
    private static readonly JsonSerializerOptions _serializerOptions = Constants.DefaultJsonSerializerOptions;

    private readonly IAITool _tool;
    private readonly string _name;
    private readonly string _description;
    private readonly JsonElement _schema;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AIToolFunction{TArgs}"/>.
    /// </summary>
    /// <param name="tool">The tool being wrapped.</param>
    /// <param name="name">The function name (tool id).</param>
    /// <param name="description">The function description.</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostic logging of tool invocation failures.</param>
    public AIToolFunction(IAITool tool, string name, string description, ILoggerFactory? loggerFactory = null)
    {
        _tool = tool;
        _name = name;
        _description = description;
        _schema = AIJsonUtilities.CreateJsonSchema(
            type: typeof(TArgs),
            description: null,
            hasDefaultValue: false,
            defaultValue: null,
            serializerOptions: _serializerOptions,
            inferenceOptions: null);
        _logger = loggerFactory?.CreateLogger($"Umbraco.AI.Tools.{name}") ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public override string Name => _name;

    /// <inheritdoc />
    public override string Description => _description;

    /// <inheritdoc />
    public override JsonElement JsonSchema => _schema;

    /// <inheritdoc />
    public override JsonSerializerOptions JsonSerializerOptions => _serializerOptions;

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        // AIFunctionArguments is a dictionary whose keys match the top-level properties of TArgs
        // (since the schema we exposed flattened TArgs). Round-trip through JSON to bind to TArgs.
        var dict = arguments.ToDictionary(kv => kv.Key, kv => kv.Value);
        var argsElement = JsonSerializer.SerializeToElement(dict, _serializerOptions);

        TArgs? typedArgs;
        try
        {
            typedArgs = argsElement.Deserialize<TArgs>(_serializerOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                $"Invalid arguments for tool '{_name}'. " +
                $"Expected type: {typeof(TArgs).Name}. " +
                $"JSON: {argsElement.GetRawText()}. " +
                $"Error: {ex.Message}",
                ex);
        }

        if (typedArgs is null)
        {
            throw new ArgumentException(
                $"Failed to deserialize arguments to {typeof(TArgs).Name} for tool '{_name}'. " +
                $"JSON: {argsElement.GetRawText()}");
        }

        try
        {
            return await _tool.ExecuteAsync(typedArgs, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Don't swallow cancellation — let MEAI surface it.
            throw;
        }
        catch (Exception ex)
        {
            // MEAI's FunctionInvokingChatClient catches function exceptions and produces a
            // sanitised ErrorContent that the AG-UI trace renders as '[unknown:ErrorContent]'
            // with no message — by the time the user sees it, the actual cause has been lost.
            // Log here so the underlying exception always lands in our trace, then return a
            // structured error result so the LLM (and the rendered tool call) sees enough to
            // diagnose or self-correct rather than a blind error.
            _logger.LogError(
                ex,
                "Tool '{ToolName}' threw {ExceptionType} while executing. Args JSON: {ArgsJson}",
                _name,
                ex.GetType().Name,
                argsElement.GetRawText());

            return new ToolInvocationError(
                Success: false,
                ToolName: _name,
                ErrorType: ex.GetType().Name,
                Message: ex.Message);
        }
    }

    /// <summary>
    /// Structured error payload returned in place of a thrown exception so the chat trace and
    /// the LLM both see a diagnosable failure rather than the opaque '[unknown:ErrorContent]'
    /// MEAI produces when a tool throws.
    /// </summary>
    internal sealed record ToolInvocationError(bool Success, string ToolName, string ErrorType, string Message);
}
