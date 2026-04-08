using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.SpeechToText;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.AuditLog.Middleware;

internal sealed class AIAuditingSpeechToTextClient : AIBoundSpeechToTextClientBase
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIAuditLogService _auditLogService;
    private readonly IAIAuditLogFactory _auditLogFactory;
    private readonly IOptionsMonitor<AIAuditLogOptions> _auditLogOptions;

    public AIAuditingSpeechToTextClient(
        ISpeechToTextClient innerClient,
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIAuditLogService auditLogService,
        IAIAuditLogFactory auditLogFactory,
        IOptionsMonitor<AIAuditLogOptions> auditLogOptions)
        : base(innerClient)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _auditLogService = auditLogService;
        _auditLogFactory = auditLogFactory;
        _auditLogOptions = auditLogOptions;
    }

    public override async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var (auditScope, auditLog) = await StartAuditLogAsync(options, cancellationToken);

        // Enrich the ambient Activity with Umbraco-specific tags (independent of audit logging)
        AIActivityEnricher.EnrichCurrentActivity(auditLog, _runtimeContextAccessor);

        var auditPrompt = auditLog is not null
            ? new AIAuditPrompt { Data = BuildPromptData(options), Capability = AICapability.SpeechToText }
            : null;

        try
        {
            var response = await base.GetTextAsync(audioSpeechStream, options, cancellationToken);

            // Complete audit-log (if exists)
            // Use CancellationToken.None so the status update is always persisted,
            // even if the original request was cancelled (e.g. client disconnected)
            if (auditLog is not null)
            {
                await _auditLogService.QueueCompleteAuditLogAsync(
                    auditLog,
                    auditPrompt,
                    new AIAuditResponse
                    {
                        Data = response.Text,
                    },
                    CancellationToken.None);
            }

            return response;
        }
        catch (Exception ex)
        {
            if (auditLog is not null)
            {
                // Use CancellationToken.None so the failure status is always persisted,
                // even if the original request was cancelled (e.g. client disconnected)
                await _auditLogService.QueueRecordAuditLogFailureAsync(
                    auditLog, auditPrompt, ex, CancellationToken.None);
            }

            throw;
        }
        finally
        {
            auditScope?.Dispose();
        }
    }

    public override async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (auditScope, auditLog) = await StartAuditLogAsync(options, cancellationToken);

        // Enrich the ambient Activity with Umbraco-specific tags (independent of audit logging)
        AIActivityEnricher.EnrichCurrentActivity(auditLog, _runtimeContextAccessor);

        var auditPrompt = auditLog is not null
            ? new AIAuditPrompt { Data = BuildPromptData(options), Capability = AICapability.SpeechToText }
            : null;

        IAsyncEnumerable<SpeechToTextResponseUpdate> stream;

        try
        {
            stream = base.GetStreamingTextAsync(audioSpeechStream, options, cancellationToken);
        }
        catch (Exception ex)
        {
            if (auditLog is not null)
            {
                await _auditLogService.QueueRecordAuditLogFailureAsync(
                    auditLog, auditPrompt, ex, cancellationToken);
            }

            auditScope?.Dispose();
            throw;
        }

        // Stream updates - wrap in error-capturing enumerator to handle
        // exceptions thrown during iteration
        await foreach (var update in WrapStreamWithErrorCapture(
            stream, auditLog, auditPrompt, auditScope, cancellationToken))
        {
            yield return update;
        }

        // Mark audit-log as completed (only reached if no exception during streaming)
        // Use CancellationToken.None so the status update is always persisted,
        // even if the original request was cancelled (e.g. client disconnected)
        if (auditLog is not null)
        {
            var trackingClient = InnerClient.GetService<AITrackingSpeechToTextClient>();

            await _auditLogService.QueueCompleteAuditLogAsync(
                auditLog,
                auditPrompt,
                new AIAuditResponse
                {
                    Data = trackingClient?.LastTranscriptionText,
                },
                CancellationToken.None);
        }

        auditScope?.Dispose();
    }

    /// <summary>
    /// Wraps an async stream to capture exceptions during iteration
    /// and record them as audit log failures. This is needed because C# does not
    /// allow yield return inside try-catch blocks.
    /// </summary>
    private async IAsyncEnumerable<SpeechToTextResponseUpdate> WrapStreamWithErrorCapture(
        IAsyncEnumerable<SpeechToTextResponseUpdate> stream,
        AIAuditLog? auditLog,
        AIAuditPrompt? auditPrompt,
        AIAuditScope? auditScope,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            SpeechToTextResponseUpdate current;
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    yield break;
                }

                current = enumerator.Current;
            }
            catch (Exception ex)
            {
                if (auditLog is not null)
                {
                    // Use CancellationToken.None so the failure status is always persisted,
                    // even if the original request was cancelled (e.g. client disconnected)
                    await _auditLogService.QueueRecordAuditLogFailureAsync(
                        auditLog, auditPrompt, ex, CancellationToken.None);
                }

                auditScope?.Dispose();
                throw;
            }

            yield return current;
        }
    }

    /// <summary>
    /// Creates audit log and scope if audit logging is enabled and runtime context is available.
    /// </summary>
    private async Task<(AIAuditScope? Scope, AIAuditLog? AuditLog)> StartAuditLogAsync(
        SpeechToTextOptions? options,
        CancellationToken cancellationToken)
    {
        if (!_auditLogOptions.CurrentValue.Enabled || _runtimeContextAccessor.Context is null)
        {
            return (null, null);
        }

        var auditLogContext = AIAuditContext.ExtractFromRuntimeContext(
            AICapability.SpeechToText,
            _runtimeContextAccessor.Context,
            BuildPromptData(options));

        // Extract metadata from RuntimeContext if present
        Dictionary<string, string>? metadata = null;
        var context = _runtimeContextAccessor.Context;
        if (context?.TryGetValue<string[]>(Constants.ContextKeys.LogKeys, out var logKeys) == true)
        {
            metadata = logKeys.ToDictionary(
                key => key,
                key => context.GetValue<object?>(key)?.ToString() ?? string.Empty);
        }

        var auditLog = _auditLogFactory.Create(
            auditLogContext,
            metadata,
            parentId: AIAuditScope.Current?.AuditLogId);

        var auditScope = AIAuditScope.Begin(auditLog.Id);

        // Capture TraceId from ambient Activity (created by OpenTelemetry middleware)
        auditLog.TraceId = Activity.Current?.TraceId.ToString();

        await _auditLogService.QueueStartAuditLogAsync(auditLog, ct: cancellationToken);

        return (auditScope, auditLog);
    }

    /// <summary>
    /// Builds a descriptive prompt data object for audit logging.
    /// Since STT doesn't have text prompts, we capture the options metadata.
    /// </summary>
    private static object? BuildPromptData(SpeechToTextOptions? options)
    {
        if (options is null)
        {
            return "speech-to-text transcription";
        }

        return new
        {
            Type = "speech-to-text",
            options.ModelId,
            options.SpeechLanguage,
        };
    }
}
