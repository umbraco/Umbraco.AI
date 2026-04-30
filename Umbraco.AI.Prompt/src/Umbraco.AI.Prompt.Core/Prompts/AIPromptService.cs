using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.InlineChat;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;
using AIValueChange = Umbraco.AI.Core.EntityAdapter.AIValueChange;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Service implementation for prompt management operations.
/// </summary>
internal sealed class AIPromptService : IAIPromptService
{
    private readonly IAIPromptRepository _repository;
    private readonly IAIEntityVersionService _versionService;
    private readonly IAIChatService _chatService;
    private readonly IAIPromptTemplateService _templateService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AIToolCollection _tools;
    private readonly IAIFunctionFactory _functionFactory;
    private readonly IAIRuntimeContextScopeProvider _runtimeContextScopeProvider;
    private readonly AIRuntimeContextContributorCollection _contextContributors;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;
    private readonly IEventAggregator _eventAggregator;

    public AIPromptService(
        IAIPromptRepository repository,
        IAIEntityVersionService versionService,
        IAIChatService chatService,
        IAIPromptTemplateService templateService,
        IServiceScopeFactory serviceScopeFactory,
        AIToolCollection tools,
        IAIFunctionFactory functionFactory,
        IAIRuntimeContextScopeProvider runtimeContextScopeProvider,
        AIRuntimeContextContributorCollection contextContributors,
        IEventAggregator eventAggregator,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _versionService = versionService;
        _chatService = chatService;
        _templateService = templateService;
        _serviceScopeFactory = serviceScopeFactory;
        _tools = tools;
        _functionFactory = functionFactory;
        _runtimeContextScopeProvider = runtimeContextScopeProvider;
        _contextContributors = contextContributors;
        _eventAggregator = eventAggregator;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public Task<AIPrompt?> GetPromptAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AIPrompt?> GetPromptByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AIPrompt>> GetPromptsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<(IEnumerable<AIPrompt> Items, int Total)> GetPromptsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, profileId, cancellationToken);

    /// <inheritdoc />
    public async Task<AIPrompt> SavePromptAsync(AIPrompt prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Instructions);

        // Validate option count
        if (prompt.OptionCount < 0)
        {
            throw new ArgumentException("OptionCount must be >= 0", nameof(prompt.OptionCount));
        }

        // Generate new ID if needed
        if (prompt.Id == Guid.Empty)
        {
            prompt.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(prompt.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != prompt.Id)
        {
            throw new InvalidOperationException($"A prompt with alias '{prompt.Alias}' already exists.");
        }

        // Update timestamp
        prompt.DateModified = DateTime.UtcNow;

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Publish saving notification (before save)
        var messages = new EventMessages();
        var savingNotification = new AIPromptSavingNotification(prompt, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);

        // Check if cancelled
        if (savingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Prompt save cancelled: {errorMessages}");
        }

        // Save version snapshot of existing entity before update
        var existing = await _repository.GetByIdAsync(prompt.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        // Perform save
        var savedPrompt = await _repository.SaveAsync(prompt, userId, cancellationToken);

        // Publish saved notification (after save)
        var savedNotification = new AIPromptSavedNotification(savedPrompt, messages)
            .WithStateFrom(savingNotification);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return savedPrompt;
    }

    /// <inheritdoc />
    public async Task<bool> DeletePromptAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Publish deleting notification (before delete)
        var messages = new EventMessages();
        var deletingNotification = new AIPromptDeletingNotification(id, messages);
        await _eventAggregator.PublishAsync(deletingNotification, cancellationToken);

        // Check if cancelled
        if (deletingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Prompt delete cancelled: {errorMessages}");
        }

        // Perform delete
        var result = await _repository.DeleteAsync(id, cancellationToken);

        // Publish deleted notification (after delete)
        var deletedNotification = new AIPromptDeletedNotification(id, messages)
            .WithStateFrom(deletingNotification);
        await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public Task<bool> PromptsExistWithProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
        => _repository.ExistsWithProfileIdAsync(profileId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> PromptAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => _repository.AliasExistsAsync(alias, excludeId, cancellationToken);

    /// <inheritdoc />
    public Task<AIPromptExecutionResult> ExecutePromptAsync(
        Guid promptId,
        AIPromptExecutionRequest request,
        CancellationToken cancellationToken = default)
        => ExecutePromptAsync(promptId, request, new AIPromptExecutionOptions(), cancellationToken);

    /// <inheritdoc />
    public async Task<AIPromptExecutionResult> ExecutePromptAsync(
        Guid promptId,
        AIPromptExecutionRequest request,
        AIPromptExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        // 1. Get the prompt
        var prompt = await GetPromptAsync(promptId, cancellationToken)
            ?? throw new InvalidOperationException($"Prompt {promptId} not found");

        // 2. Validate scope - ensure prompt is allowed to run for this context
        if (options.ValidateScope)
        {
            // Use a scope to resolve the scoped IAIPromptScopeValidator
            using var scope = _serviceScopeFactory.CreateScope();
            var scopeValidator = scope.ServiceProvider.GetRequiredService<IAIPromptScopeValidator>();
            var scopeValidation = await scopeValidator.ValidateAsync(prompt, request, cancellationToken);
            if (!scopeValidation.IsAllowed)
            {
                throw new InvalidOperationException(
                    $"Prompt execution denied: {scopeValidation.DenialReason}");
            }
        }

        // Publish executing notification (before execution)
        var eventMessages = new EventMessages();
        var executingNotification = new AIPromptExecutingNotification(prompt, request, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        // Check if cancelled
        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Prompt execution cancelled: {errorMessages}");
        }

        // Create a runtime context scope for this execution
        using var runtimeContextScope = _runtimeContextScopeProvider.CreateScope(request.Context ?? []);
        var runtimeContext = runtimeContextScope.Context;

        // 3. Process context items through registered contributors
        _contextContributors.Populate(runtimeContext);

        // If no EntityId was set by contributors, use request.EntityId as fallback
        if (!runtimeContext.Data.ContainsKey(CoreConstants.ContextKeys.EntityId) && request.EntityId != Guid.Empty)
        {
            runtimeContext.SetValue(CoreConstants.ContextKeys.EntityId, request.EntityId);
        }

        // If no EntityType was set by contributors, use request.EntityType as fallback.
        // Downstream resolvers (e.g. ContentContextResolver) rely on this to route to
        // the correct published cache — without it, media-scoped prompts would query
        // the document cache with a media key and fail to materialise the node.
        if (!runtimeContext.Data.ContainsKey(CoreConstants.ContextKeys.EntityType) && !string.IsNullOrEmpty(request.EntityType))
        {
            runtimeContext.SetValue(CoreConstants.ContextKeys.EntityType, request.EntityType);
        }

        // If no ElementId was set by contributors, use request.ElementId as fallback
        if (!runtimeContext.Data.ContainsKey(CoreConstants.ContextKeys.ElementId) && request.ElementId.HasValue && request.ElementId.Value != Guid.Empty)
        {
            runtimeContext.SetValue(CoreConstants.ContextKeys.ElementId, request.ElementId.Value);
        }

        if (!runtimeContext.Data.ContainsKey(CoreConstants.ContextKeys.ElementType) && !string.IsNullOrEmpty(request.ElementType))
        {
            runtimeContext.SetValue(CoreConstants.ContextKeys.ElementType, request.ElementType);
        }

        // Set prompt metadata in runtime context for auditing and telemetry
        runtimeContext.SetValue(Constants.MetadataKeys.PromptId, prompt.Id);
        runtimeContext.SetValue(Constants.MetadataKeys.PromptAlias, prompt.Alias);
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureType, "prompt");
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureId, prompt.Id);
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureAlias, prompt.Alias);
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureVersion, prompt.Version);

        // Set context IDs override in runtime context — full replace (all context resolvers suppress
        // themselves when this core key is set; ProfileContextResolver then surfaces the override set).
        if (options.ContextIdsOverride is not null)
        {
            runtimeContext.SetValue(CoreConstants.ContextKeys.ContextIdsOverride, options.ContextIdsOverride);
        }

        // Set guardrail IDs override in runtime context for guardrail resolvers to pick up
        if (options.GuardrailIdsOverride is not null)
        {
            runtimeContext.SetValue(CoreConstants.ContextKeys.GuardrailIdsOverride, options.GuardrailIdsOverride);
        }

        // 4. Build template context from basic request info + processor results
        var templateContext = BuildExecutionContext(request);
        foreach (var (key, value) in runtimeContext.Variables)
        {
            templateContext[key] = value;
        }
        templateContext["currentValue"] = templateContext.TryGetValue(request.PropertyAlias, out var propValue)
            ? propValue
            : null;

        // 5. Process template variables (returns multimodal content list)
        var contents = await _templateService.ProcessTemplateAsync(prompt.Instructions, templateContext, cancellationToken);

        // 6. Build chat messages with multimodal content
        List<ChatMessage> messages = [new(ChatRole.User, contents.ToList())];

        // 7. Inject system message from context processors (only if IncludeEntityContext is allowed)
        if (prompt.IncludeEntityContext && runtimeContext?.SystemMessageParts.Count > 0)
        {
            var contextContent = string.Join("\n\n", runtimeContext.SystemMessageParts);

            // Insert new system message at the beginning
            messages.Insert(0, new ChatMessage(ChatRole.System, contextContent));
        }

        // 7.5. Inject format instructions based on option count.
        // Inserted AFTER context messages so they appear closest to the user message,
        // which makes LLMs more likely to follow them.
        if (prompt.OptionCount == 1)
        {
            var formatInstructions = """
                IMPORTANT: Return ONLY the requested content value.
                Do NOT include any preamble, introduction, or explanation before the value.
                Do NOT include any closing remarks, suggestions, or follow-up text after the value.
                Do NOT wrap the value in markdown formatting unless the content itself requires it.
                """;

            messages.Add(new ChatMessage(ChatRole.System, formatInstructions));
        }
        else if (prompt.OptionCount >= 2)
        {
            var formatInstructions = $$"""
                IMPORTANT: Return your response as a JSON object with an "options" array containing {{prompt.OptionCount}} options.
                Each option must have:
                - "label": A short title (2-5 words)
                - "value": The actual content
                - "description": Optional brief explanation

                Generate exactly {{prompt.OptionCount}} distinct options for the user to choose from.
                """;

            messages.Add(new ChatMessage(ChatRole.System, formatInstructions));
        }

        // 8. Create ChatOptions with PromptId for context resolution, feature tracking, and system tools
        var chatOptions = new ChatOptions
        {
            Tools = _tools.ToSystemToolFunctions(_functionFactory).Cast<AITool>().ToList(),
            ToolMode = ChatToolMode.Auto
        };

        var profileId = options.ProfileIdOverride ?? prompt.ProfileId;
        void ConfigureChat(AIChatBuilder chat)
        {
            chat.WithAlias($"prompt-{prompt.Alias}")
                .WithChatOptions(chatOptions)
                .AsPassThrough();
            if (profileId.HasValue)
            {
                chat.WithProfile(profileId.Value);
            }
        }

        // 10. Execute and build result based on option count.
        // For OptionCount 1 and 2+, use structured output via GetStructuredResponseAsync<T>
        // which delegates to M.E.AI's structured output extensions (schema, ResponseFormat, deserialization).
        AIPromptExecutionResult result;

        switch (prompt.OptionCount)
        {
            case 0:
            {
                var response = await _chatService.GetChatResponseAsync(ConfigureChat, messages, cancellationToken);
                result = new AIPromptExecutionResult
                {
                    Content = response.Text ?? string.Empty,
                    Usage = response.Usage,
                    Messages = messages,
                    ResultOptions = [] // Empty array for informational
                };
                break;
            }

            case 1:
            {
                var response = await _chatService.GetChatResponseAsync(chat =>
                {
                    ConfigureChat(chat);
                    chat.WithOutputSchema(AIOutputSchema.FromType<SingleValueResponse>());
                }, messages, cancellationToken);
                var responseText = response.Text ?? string.Empty;

                var displayValue = response.TryGetResult<SingleValueResponse>(out var parsed) ? parsed.Value : responseText;
                result = new AIPromptExecutionResult
                {
                    Content = responseText,
                    Usage = response.Usage,
                    Messages = messages,
                    ResultOptions =
                    [
                        new AIPromptExecutionResult.AIPromptResultOption
                        {
                            Label = "Result",
                            DisplayValue = displayValue,
                            Description = null,
                            ValueChange = new AIValueChange
                            {
                                Path = request.PropertyAlias,
                                Value = displayValue,
                                Culture = request.Culture,
                                Segment = request.Segment
                            }
                        }
                    ]
                };
                break;
            }

            case >= 2:
            {
                var response = await _chatService.GetChatResponseAsync(chat =>
                {
                    ConfigureChat(chat);
                    chat.WithOutputSchema(AIOutputSchema.FromType<MultiOptionResponse>());
                }, messages, cancellationToken);
                var responseText = response.Text ?? string.Empty;

                if (response.TryGetResult<MultiOptionResponse>(out var parsed) && parsed.Options is { Count: > 0 })
                {
                    result = new AIPromptExecutionResult
                    {
                        Content = responseText,
                        Usage = response.Usage,
                        Messages = messages,
                        ResultOptions = parsed.Options.Select(option => new AIPromptExecutionResult.AIPromptResultOption
                        {
                            Label = option.Label,
                            DisplayValue = option.Value,
                            Description = option.Description,
                            ValueChange = new AIValueChange
                            {
                                Path = request.PropertyAlias,
                                Value = option.Value,
                                Culture = request.Culture,
                                Segment = request.Segment
                            }
                        }).ToList()
                    };
                }
                else
                {
                    // Structured output not honored — fall back to retry-based parsing
                    result = await ParseMultipleResultResponseWithRetryAsync(
                        prompt, messages, chatOptions, responseText,
                        response.Usage, request, cancellationToken);
                }
                break;
            }

            default:
                throw new InvalidOperationException($"Invalid option count: {prompt.OptionCount}");
        }

        // Publish executed notification (after execution)
        var executedNotification = new AIPromptExecutedNotification(prompt, request, result, eventMessages)
            .WithStateFrom(executingNotification);
        await _eventAggregator.PublishAsync(executedNotification, cancellationToken);

        return result;
    }

    /// <summary>
    /// Builds the execution context dictionary from the request.
    /// </summary>
    private static Dictionary<string, object?> BuildExecutionContext(AIPromptExecutionRequest request)
    {
        var context = new Dictionary<string, object?>
        {
            ["entityId"] = request.EntityId.ToString(),
            ["entityType"] = request.EntityType,
            ["propertyAlias"] = request.PropertyAlias,
        };

        if (request.ElementId.HasValue)
        {
            context["elementId"] = request.ElementId.Value.ToString();
        }

        if (!string.IsNullOrEmpty(request.ElementType))
        {
            context["elementType"] = request.ElementType;
        }

        if (!string.IsNullOrEmpty(request.Culture))
        {
            context["culture"] = request.Culture;
        }

        if (!string.IsNullOrEmpty(request.Segment))
        {
            context["segment"] = request.Segment;
        }

        return context;
    }

    /// <summary>
    /// Parses multiple-option response with retry logic if JSON parsing fails.
    /// Retries up to 2 times with enhanced format instructions before failing.
    /// Accepts whatever option count the AI returns (no strict count validation).
    /// </summary>
    private async Task<AIPromptExecutionResult> ParseMultipleResultResponseWithRetryAsync(
        AIPrompt prompt,
        IList<ChatMessage> messages,
        ChatOptions chatOptions,
        string responseText,
        UsageDetails? usage,
        AIPromptExecutionRequest request,
        CancellationToken cancellationToken,
        int retryCount = 0)
    {
        const int MaxRetries = 2;

        // Try to parse the response
        var parseResult = TryParseMultipleResultResponse(responseText, request);

        if (parseResult.Success)
        {
            return new AIPromptExecutionResult
            {
                Content = responseText,
                Usage = usage,
                Messages = messages.ToList(),
                ResultOptions = parseResult.Options!
            };
        }

        // If parsing failed and we haven't exceeded retries, try again with enhanced instructions
        if (retryCount < MaxRetries)
        {
            var enhancedInstructions = $$"""
                CRITICAL: Your previous response was not in the correct format.
                You MUST return ONLY a valid JSON object with this exact structure:

                {
                  "options": [
                    {
                      "label": "Option 1 Title",
                      "value": "The actual content for option 1",
                      "description": "Brief explanation"
                    },
                    {
                      "label": "Option 2 Title",
                      "value": "The actual content for option 2",
                      "description": "Brief explanation"
                    }
                  ]
                }

                Do NOT include any explanatory text before or after the JSON.
                Wrap the JSON in a ```json code block.
                Provide {{prompt.OptionCount}} distinct options.

                Parse error from previous attempt: {{parseResult.Error}}
                """;

            // Remove old format instructions and add enhanced ones
            messages.RemoveAt(0); // Remove old system message
            messages.Insert(0, new ChatMessage(ChatRole.System, enhancedInstructions));

            // Retry execution
            var retryResponse = await _chatService.GetChatResponseAsync(chat =>
            {
                chat.WithAlias($"prompt-{prompt.Alias}-retry")
                    .WithChatOptions(chatOptions)
                    .AsPassThrough();
                if (prompt.ProfileId.HasValue)
                {
                    chat.WithProfile(prompt.ProfileId.Value);
                }
            }, messages, cancellationToken);

            var retryText = retryResponse.Text ?? string.Empty;
            var combinedUsage = CombineUsage(usage, retryResponse.Usage);

            return await ParseMultipleResultResponseWithRetryAsync(
                prompt,
                messages,
                chatOptions,
                retryText,
                combinedUsage,
                request,
                cancellationToken,
                retryCount + 1);
        }

        // Max retries exceeded, return error with raw response
        return new AIPromptExecutionResult
        {
            Content = $"❌ Failed to parse multiple options after {MaxRetries} attempts.\n\n**Error:** {parseResult.Error}\n\n**Raw response:**\n\n{responseText}",
            Usage = usage,
            Messages = messages.ToList(),
            ResultOptions = [] // Empty array on error
        };
    }

    /// <summary>
    /// Attempts to parse a multiple-option response containing JSON options.
    /// Returns success flag, parsed options with property changes, or error message.
    /// Accepts whatever count the AI returns (no strict count validation).
    /// </summary>
    private static (bool Success, IReadOnlyList<AIPromptExecutionResult.AIPromptResultOption>? Options, string? Error)
        TryParseMultipleResultResponse(string responseText, AIPromptExecutionRequest request)
    {
        try
        {
            // Extract JSON from markdown code block if present
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                responseText,
                @"```json\s*\n(.*?)\n```",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            var jsonText = jsonMatch.Success ? jsonMatch.Groups[1].Value.Trim() : responseText.Trim();

            // Parse JSON
            using var doc = System.Text.Json.JsonDocument.Parse(jsonText);

            if (!doc.RootElement.TryGetProperty("options", out var optionsArray))
            {
                return (false, null, "JSON missing 'options' array property");
            }

            var options = new List<AIPromptExecutionResult.AIPromptResultOption>();
            foreach (var optionElement in optionsArray.EnumerateArray())
            {
                if (!optionElement.TryGetProperty("label", out var labelProp) ||
                    !optionElement.TryGetProperty("value", out var valueProp))
                {
                    return (false, null, "Option missing required 'label' or 'value' property");
                }

                var displayValue = valueProp.GetString() ?? string.Empty;
                var description = optionElement.TryGetProperty("description", out var desc)
                    ? desc.GetString()
                    : null;

                options.Add(new AIPromptExecutionResult.AIPromptResultOption
                {
                    Label = labelProp.GetString() ?? "Option",
                    DisplayValue = displayValue,
                    Description = description,
                    ValueChange = new AIValueChange
                    {
                        Path = request.PropertyAlias,
                        Value = displayValue,
                        Culture = request.Culture,
                        Segment = request.Segment
                    }
                });
            }

            if (options.Count == 0)
            {
                return (false, null, "Options array is empty");
            }

            // NOTE: We accept whatever count the AI returns - no strict validation against prompt.OptionCount

            return (true, options, null);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return (false, null, $"Invalid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, null, $"Parsing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Combines usage details from multiple API calls (for retry scenarios).
    /// </summary>
    private static UsageDetails? CombineUsage(UsageDetails? usage1, UsageDetails? usage2)
    {
        if (usage1 == null) return usage2;
        if (usage2 == null) return usage1;

        return new UsageDetails
        {
            InputTokenCount = (usage1.InputTokenCount ?? 0) + (usage2.InputTokenCount ?? 0),
            OutputTokenCount = (usage1.OutputTokenCount ?? 0) + (usage2.OutputTokenCount ?? 0),
            TotalTokenCount = (usage1.TotalTokenCount ?? 0) + (usage2.TotalTokenCount ?? 0)
        };
    }
}
