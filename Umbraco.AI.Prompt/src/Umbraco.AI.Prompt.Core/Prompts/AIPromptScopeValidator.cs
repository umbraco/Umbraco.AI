using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Validates whether a prompt execution is allowed based on its scope configuration.
/// </summary>
internal sealed class AIPromptScopeValidator : IAIPromptScopeValidator
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IMemberTypeService _memberTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly ILogger<AIPromptScopeValidator> _logger;

    public AIPromptScopeValidator(
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService,
        IMemberTypeService memberTypeService,
        IDataTypeService dataTypeService,
        ILogger<AIPromptScopeValidator> logger)
    {
        _contentTypeService = contentTypeService;
        _mediaTypeService = mediaTypeService;
        _memberTypeService = memberTypeService;
        _dataTypeService = dataTypeService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AIPromptScopeValidationResult> ValidateAsync(
        AIPrompt prompt,
        AIPromptExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        // If no scope is defined, prompt is not allowed anywhere
        if (prompt.Scope is null)
        {
            return AIPromptScopeValidationResult.Denied("Prompt has no scope defined.");
        }

        // If no allow rules are defined, prompt is not allowed anywhere
        if (prompt.Scope.AllowRules.Count == 0)
        {
            return AIPromptScopeValidationResult.Denied("Prompt has no allow rules defined.");
        }

        // Build the resolved context from the request and actual content type
        var resolvedContext = await ResolveContextAsync(request, cancellationToken);

        // Check deny rules first (deny takes precedence)
        foreach (var denyRule in prompt.Scope.DenyRules)
        {
            if (MatchesRule(denyRule, resolvedContext))
            {
                return AIPromptScopeValidationResult.Denied("Prompt execution denied by deny rule.");
            }
        }

        // Check if any allow rule matches
        foreach (var allowRule in prompt.Scope.AllowRules)
        {
            if (MatchesRule(allowRule, resolvedContext))
            {
                return AIPromptScopeValidationResult.Allowed();
            }
        }

        return AIPromptScopeValidationResult.Denied("No allow rule matched the execution context.");
    }

    /// <summary>
    /// Resolves the execution context using the content type alias provided by the frontend.
    /// The frontend resolves the content type alias from the workspace context (for documents/media)
    /// or from the block element manager's structure (for blocks).
    /// </summary>
    private async Task<ResolvedScopeContext> ResolveContextAsync(
        AIPromptExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var context = new ResolvedScopeContext
        {
            ContentTypeAlias = request.ContentTypeAlias,
            PropertyAlias = request.PropertyAlias,
        };

        // Resolve the property editor UI alias from the content type
        context.PropertyEditorUiAlias = await ResolvePropertyEditorUiAliasAsync(
            request.ContentTypeAlias,
            request.EntityType,
            request.PropertyAlias,
            cancellationToken);

        return context;
    }

    /// <summary>
    /// Resolves the property editor UI alias for a given content type alias and property alias.
    /// Uses the entity type to determine which content type service to query.
    /// The UI alias is stored on the DataType, not the PropertyType.
    /// </summary>
    private async Task<string?> ResolvePropertyEditorUiAliasAsync(
        string contentTypeAlias,
        string entityType,
        string propertyAlias,
        CancellationToken cancellationToken)
    {
        IContentTypeBase? contentType = entityType.ToLowerInvariant() switch
        {
            "document" or "block" => _contentTypeService.Get(contentTypeAlias),
            "media" => _mediaTypeService.Get(contentTypeAlias),
            "member" => _memberTypeService.Get(contentTypeAlias),
            _ => _contentTypeService.Get(contentTypeAlias), // Default fallback
        };

        var propertyType = contentType is IContentTypeComposition compositionContentType
            ? compositionContentType.CompositionPropertyTypes.FirstOrDefault(
                pt => pt.Alias.Equals(propertyAlias, StringComparison.OrdinalIgnoreCase))
            : contentType?.PropertyTypes.FirstOrDefault(
                pt => pt.Alias.Equals(propertyAlias, StringComparison.OrdinalIgnoreCase));

        if (propertyType is null)
        {
            return null;
        }

        // Get the data type to access the EditorUiAlias
        var dataType = await _dataTypeService.GetAsync(propertyType.DataTypeKey);

        return dataType?.EditorUiAlias;
    }

    /// <summary>
    /// Checks if a single scope rule matches the resolved context.
    /// All non-null/non-empty properties must match (AND logic between properties).
    /// For array properties, any value matching = that property matches (OR within array).
    /// </summary>
    private static bool MatchesRule(AIPromptScopeRule rule, ResolvedScopeContext context)
    {
        // Check property editor UI alias
        if (rule.PropertyEditorUiAliases is { Count: > 0 })
        {
            if (string.IsNullOrEmpty(context.PropertyEditorUiAlias))
            {
                return false;
            }

            if (!rule.PropertyEditorUiAliases.Contains(context.PropertyEditorUiAlias, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check property alias
        if (rule.PropertyAliases is { Count: > 0 })
        {
            if (string.IsNullOrEmpty(context.PropertyAlias))
            {
                return false;
            }

            if (!rule.PropertyAliases.Contains(context.PropertyAlias, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check content type alias
        if (rule.ContentTypeAliases is { Count: > 0 })
        {
            if (string.IsNullOrEmpty(context.ContentTypeAlias))
            {
                return false;
            }

            if (!rule.ContentTypeAliases.Contains(context.ContentTypeAlias, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Internal context resolved from the request and actual content type.
    /// </summary>
    private sealed class ResolvedScopeContext
    {
        public string? ContentTypeAlias { get; set; }
        public string? PropertyAlias { get; set; }
        public string? PropertyEditorUiAlias { get; set; }
    }
}

/// <summary>
/// Result of scope validation.
/// </summary>
public sealed class AIPromptScopeValidationResult
{
    /// <summary>
    /// Whether execution is allowed.
    /// </summary>
    public bool IsAllowed { get; private init; }

    /// <summary>
    /// Reason for denial, if applicable.
    /// </summary>
    public string? DenialReason { get; private init; }

    private AIPromptScopeValidationResult() { }

    /// <summary>
    /// Creates an allowed result.
    /// </summary>
    public static AIPromptScopeValidationResult Allowed() => new() { IsAllowed = true };

    /// <summary>
    /// Creates a denied result with a reason.
    /// </summary>
    public static AIPromptScopeValidationResult Denied(string reason) => new()
    {
        IsAllowed = false,
        DenialReason = reason
    };
}
