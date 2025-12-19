using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Validates whether a agent execution is allowed based on its scope configuration.
/// </summary>
internal sealed class AiAgentScopeValidator : IAiAgentScopeValidator
{
    private readonly IEntityService _entityService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IMemberTypeService _memberTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly ILogger<AiAgentScopeValidator> _logger;

    public AiAgentScopeValidator(
        IEntityService entityService,
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService,
        IMemberTypeService memberTypeService,
        IDataTypeService dataTypeService,
        ILogger<AiAgentScopeValidator> logger)
    {
        _entityService = entityService;
        _contentTypeService = contentTypeService;
        _mediaTypeService = mediaTypeService;
        _memberTypeService = memberTypeService;
        _dataTypeService = dataTypeService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiAgentScopeValidationResult> ValidateAsync(
        AiAgent agent,
        AiAgentExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        // If no scope is defined, agent is not allowed anywhere
        if (agent.Scope is null)
        {
            return AiAgentScopeValidationResult.Denied("Agent has no scope defined.");
        }

        // If no allow rules are defined, agent is not allowed anywhere
        if (agent.Scope.AllowRules.Count == 0)
        {
            return AiAgentScopeValidationResult.Denied("Agent has no allow rules defined.");
        }

        // Build the resolved context from the request and actual content item
        var resolvedContext = await ResolveContextAsync(request, cancellationToken);

        // Check deny rules first (deny takes precedence)
        foreach (var denyRule in agent.Scope.DenyRules)
        {
            if (MatchesRule(denyRule, resolvedContext))
            {
                return AiAgentScopeValidationResult.Denied("Agent execution denied by deny rule.");
            }
        }

        // Check if any allow rule matches
        foreach (var allowRule in agent.Scope.AllowRules)
        {
            if (MatchesRule(allowRule, resolvedContext))
            {
                return AiAgentScopeValidationResult.Allowed();
            }
        }

        return AiAgentScopeValidationResult.Denied("No allow rule matched the execution context.");
    }

    /// <summary>
    /// Resolves the execution context by looking up the actual entity.
    /// This ensures the scope rules are validated against the real content type and property configuration.
    /// </summary>
    private async Task<ResolvedScopeContext> ResolveContextAsync(
        AiAgentExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var context = new ResolvedScopeContext
        {
            PropertyAlias = request.PropertyAlias
        };

        var entityInfo = ResolveEntityInfo(request.EntityId, request.EntityType);
        if (entityInfo is null)
        {
            return context;
        }

        // Use the actual content type alias from the entity
        context.ContentTypeAlias = entityInfo.Value.ContentTypeAlias;

        // Resolve the property editor UI alias
        context.PropertyEditorUiAlias = await ResolvePropertyEditorUiAliasAsync(
            entityInfo.Value.ContentTypeAlias,
            entityInfo.Value.ObjectType,
            request.PropertyAlias,
            cancellationToken);

        return context;
    }

    /// <summary>
    /// Resolves entity information (content type alias and object type) from the entity ID and type.
    /// Uses IEntityService with the object type to get the full content entity with ContentTypeAlias populated.
    /// </summary>
    private (string ContentTypeAlias, UmbracoObjectTypes ObjectType)? ResolveEntityInfo(Guid entityId, string entityType)
    {
        // Map the entity type string to UmbracoObjectTypes
        var objectType = entityType.ToLowerInvariant() switch
        {
            "document" => UmbracoObjectTypes.Document,
            "media" => UmbracoObjectTypes.Media,
            "member" => UmbracoObjectTypes.Member,
            _ => UmbracoObjectTypes.Unknown
        };

        if (objectType == UmbracoObjectTypes.Unknown)
        {
            return null;
        }

        // Use the overload that takes object type - this returns the proper entity type
        // with ContentTypeAlias populated (DocumentEntitySlim, MediaEntitySlim, etc.)
        var entity = _entityService.Get(entityId, objectType);
        if (entity is not IContentEntitySlim contentEntity)
        {
            return null;
        }

        return (contentEntity.ContentTypeAlias, objectType);
    }

    /// <summary>
    /// Resolves the property editor UI alias for a given content type, object type, and property alias.
    /// The UI alias is stored on the DataType, not the PropertyType.
    /// </summary>
    private async Task<string?> ResolvePropertyEditorUiAliasAsync(
        string contentTypeAlias,
        UmbracoObjectTypes objectType,
        string propertyAlias,
        CancellationToken cancellationToken)
    {
        IContentTypeBase? contentType = objectType switch
        {
            UmbracoObjectTypes.Document => _contentTypeService.Get(contentTypeAlias),
            UmbracoObjectTypes.Media => _mediaTypeService.Get(contentTypeAlias),
            UmbracoObjectTypes.Member => _memberTypeService.Get(contentTypeAlias),
            _ => null
        };

        var propertyType = contentType?.PropertyTypes.FirstOrDefault(
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
    private static bool MatchesRule(AiAgentScopeRule rule, ResolvedScopeContext context)
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
    /// Internal context resolved from the request and actual content item.
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
public sealed class AiAgentScopeValidationResult
{
    /// <summary>
    /// Whether execution is allowed.
    /// </summary>
    public bool IsAllowed { get; private init; }

    /// <summary>
    /// Reason for denial, if applicable.
    /// </summary>
    public string? DenialReason { get; private init; }

    private AiAgentScopeValidationResult() { }

    /// <summary>
    /// Creates an allowed result.
    /// </summary>
    public static AiAgentScopeValidationResult Allowed() => new() { IsAllowed = true };

    /// <summary>
    /// Creates a denied result with a reason.
    /// </summary>
    public static AiAgentScopeValidationResult Denied(string reason) => new()
    {
        IsAllowed = false,
        DenialReason = reason
    };
}
