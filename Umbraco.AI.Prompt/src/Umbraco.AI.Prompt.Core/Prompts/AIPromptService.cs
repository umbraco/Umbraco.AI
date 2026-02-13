using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using AIPropertyChange = Umbraco.AI.Core.EntityAdapter.AIPropertyChange;
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
    public Task<PagedModel<AIPrompt>> GetPromptsPagedAsync(
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

        // Save version snapshot of existing entity before update
        var existing = await _repository.GetByIdAsync(prompt.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        return await _repository.SaveAsync(prompt, userId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeletePromptAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> PromptAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => _repository.AliasExistsAsync(alias, excludeId, cancellationToken);

    /// <inheritdoc />
    public async Task<AIPromptExecutionResult> ExecutePromptAsync(
        Guid promptId,
        AIPromptExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Get the prompt
        var prompt = await GetPromptAsync(promptId, cancellationToken)
            ?? throw new InvalidOperationException($"Prompt {promptId} not found");

        // 2. Validate scope - ensure prompt is allowed to run for this context
        // Use a scope to resolve the scoped IAIPromptScopeValidator
        using var scope = _serviceScopeFactory.CreateScope();
        var scopeValidator = scope.ServiceProvider.GetRequiredService<IAIPromptScopeValidator>();
        var scopeValidation = await scopeValidator.ValidateAsync(prompt, request, cancellationToken);
        if (!scopeValidation.IsAllowed)
        {
            throw new InvalidOperationException(
                $"Prompt execution denied: {scopeValidation.DenialReason}");
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

        // Set prompt metadata in runtime context for auditing and telemetry
        runtimeContext.SetValue(Constants.MetadataKeys.PromptId, prompt.Id);
        runtimeContext.SetValue(Constants.MetadataKeys.PromptAlias, prompt.Alias);
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureType, "prompt");
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureId, prompt.Id);
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureAlias, prompt.Alias);
        runtimeContext.SetValue(CoreConstants.ContextKeys.FeatureVersion, prompt.Version);

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

        // 7.5. Inject format instructions for multiple options
        if (prompt.OptionCount >= 2)
        {
            var formatInstructions = $"""
                IMPORTANT: Return your response as a JSON object with an "options" array containing {prompt.OptionCount} options.
                Each option must have:
                - "label": A short title (2-5 words)
                - "value": The actual content
                - "description": Optional brief explanation

                Example format:
                ```json
                {{
                  "options": [
                    {{
                      "label": "Formal Tone",
                      "value": "The content in formal style...",
                      "description": "Professional and business-appropriate"
                    }},
                    {{
                      "label": "Casual Style",
                      "value": "The content in casual style...",
                      "description": "Friendly and conversational"
                    }}
                  ]
                }}
                ```

                Generate exactly {prompt.OptionCount} distinct options for the user to choose from.
                """;

            messages.Insert(0, new ChatMessage(ChatRole.System, formatInstructions));
        }

        // 8. Create ChatOptions with PromptId for context resolution, feature tracking, and system tools
        var chatOptions = new ChatOptions
        {
            Tools = _tools.ToSystemToolFunctions(_functionFactory).Cast<AITool>().ToList(),
            ToolMode = ChatToolMode.Auto
        };

        // 10. Execute via chat service
        var response = prompt.ProfileId.HasValue
            ? await _chatService.GetChatResponseAsync(prompt.ProfileId.Value, messages, chatOptions, cancellationToken)
            : await _chatService.GetChatResponseAsync(messages, chatOptions, cancellationToken);

        var responseText = response.Text ?? string.Empty;

        // 11. Build ResultOptions based on option count
        return prompt.OptionCount switch
        {
            0 => new AIPromptExecutionResult
            {
                Content = responseText,
                Usage = response.Usage,
                ResultOptions = [] // Empty array for informational
            },

            1 => new AIPromptExecutionResult
            {
                Content = responseText,
                Usage = response.Usage,
                ResultOptions = [
                    new AIPromptExecutionResult.AIPromptResultOption
                    {
                        Label = "Result",
                        DisplayValue = responseText,
                        Description = null,
                        PropertyChange = new AIPropertyChange
                        {
                            Alias = request.PropertyAlias,
                            Value = responseText,
                            Culture = request.Culture,
                            Segment = request.Segment
                        }
                    }
                ]
            },

            >= 2 => await ParseMultipleResultResponseWithRetryAsync(
                prompt,
                messages,
                chatOptions,
                responseText,
                response.Usage,
                request,
                cancellationToken),

            _ => throw new InvalidOperationException($"Invalid option count: {prompt.OptionCount}")
        };
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
                ResultOptions = parseResult.Options!
            };
        }

        // If parsing failed and we haven't exceeded retries, try again with enhanced instructions
        if (retryCount < MaxRetries)
        {
            var enhancedInstructions = $"""
                CRITICAL: Your previous response was not in the correct format.
                You MUST return ONLY a valid JSON object with this exact structure:

                {{
                  "options": [
                    {{
                      "label": "Option 1 Title",
                      "value": "The actual content for option 1",
                      "description": "Brief explanation"
                    }},
                    {{
                      "label": "Option 2 Title",
                      "value": "The actual content for option 2",
                      "description": "Brief explanation"
                    }}
                  ]
                }}

                Do NOT include any explanatory text before or after the JSON.
                Wrap the JSON in a ```json code block.
                Provide {prompt.OptionCount} distinct options.

                Parse error from previous attempt: {parseResult.Error}
                """;

            // Remove old format instructions and add enhanced ones
            messages.RemoveAt(0); // Remove old system message
            messages.Insert(0, new ChatMessage(ChatRole.System, enhancedInstructions));

            // Retry execution
            var retryResponse = prompt.ProfileId.HasValue
                ? await _chatService.GetChatResponseAsync(prompt.ProfileId.Value, messages, chatOptions, cancellationToken)
                : await _chatService.GetChatResponseAsync(messages, chatOptions, cancellationToken);

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
                    PropertyChange = new AIPropertyChange
                    {
                        Alias = request.PropertyAlias,
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
