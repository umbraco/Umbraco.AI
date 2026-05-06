using System.ComponentModel;
using System.Text.Json.Nodes;

using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the GetPropertyValueSchema tool.
/// </summary>
/// <param name="DataTypeKey">
/// The GUID key of the data type whose value schema should be returned. The key is surfaced as
/// <c>DataTypeKey</c> on each property in <c>get_content_type_schema</c> results.
/// </param>
public record GetPropertyValueSchemaArgs(
    [property: Description("The GUID key of the data type whose value schema is wanted. Use the DataTypeKey value from get_content_type_schema results.")]
    Guid DataTypeKey);

/// <summary>
/// Tool that returns the JSON Schema describing the value shape that a single data type accepts on write.
/// Use this when <c>get_content_type_schema</c> did not include a schema for a property and a focused
/// lookup is required (or for nested element-type data types reached through a block editor).
/// </summary>
[AITool("get_property_value_schema", "Get Property Value Schema", ScopeId = ContentReadScope.ScopeId)]
public class GetPropertyValueSchemaTool : AIToolBase<GetPropertyValueSchemaArgs>
{
    private readonly IPropertyEditorSchemaService _propertyEditorSchemaService;
    private readonly IPublishedContentTypeCache _publishedContentTypeCache;

    /// <summary>
    /// Initializes a new instance of <see cref="GetPropertyValueSchemaTool"/>.
    /// </summary>
    public GetPropertyValueSchemaTool(
        IPropertyEditorSchemaService propertyEditorSchemaService,
        IPublishedContentTypeCache publishedContentTypeCache)
    {
        _propertyEditorSchemaService = propertyEditorSchemaService;
        _publishedContentTypeCache = publishedContentTypeCache;
    }

    /// <inheritdoc />
    public override string Description =>
        "Returns the JSON Schema (draft 2020-12) for the value a property data type accepts on write. " +
        "REQUIRED before calling set_value when the matching property's ValueSchema was not embedded in a " +
        "prior get_content_type_schema response, or when constructing a value for a nested element-type data type " +
        "reached through a block editor (block list / block grid). " +
        "Do not guess the input shape from formatted values shown elsewhere — always confirm against the schema. " +
        "Provide the DataTypeKey (GUID) returned by get_content_type_schema.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(GetPropertyValueSchemaArgs args, CancellationToken cancellationToken = default)
    {
        if (args.DataTypeKey == Guid.Empty)
        {
            return new GetPropertyValueSchemaResult(
                false, args.DataTypeKey, null, null, "DataTypeKey cannot be empty.");
        }

        Attempt<PropertyValueSchema, PropertyEditorSchemaOperationStatus> attempt =
            await _propertyEditorSchemaService.GetSchemaAsync(args.DataTypeKey);

        if (!attempt.Success)
        {
            string message = attempt.Status switch
            {
                PropertyEditorSchemaOperationStatus.DataTypeNotFound =>
                    $"Data type '{args.DataTypeKey}' was not found.",
                PropertyEditorSchemaOperationStatus.SchemaNotSupported =>
                    $"The property editor for data type '{args.DataTypeKey}' does not expose a value schema.",
                _ => $"Schema lookup failed with status '{attempt.Status}'.",
            };

            return new GetPropertyValueSchemaResult(
                false, args.DataTypeKey, null, null, message);
        }

        var enrichedSchema = BlockSchemaEnricher.Enrich(
            attempt.Result?.JsonSchema,
            _publishedContentTypeCache,
            _propertyEditorSchemaService);

        return new GetPropertyValueSchemaResult(
            true,
            args.DataTypeKey,
            attempt.Result?.ValueType?.FullName,
            enrichedSchema,
            null);
    }
}

/// <summary>
/// Result of the get property value schema tool.
/// </summary>
/// <param name="Success">Whether a schema was returned.</param>
/// <param name="DataTypeKey">The GUID key the schema was requested for (echoed back).</param>
/// <param name="ValueClrTypeName">
/// The fully-qualified CLR type name the editor accepts as input, when available.
/// </param>
/// <param name="ValueSchema">JSON Schema describing the value structure, when available.</param>
/// <param name="Message">Optional message (typically populated when <see cref="Success"/> is <c>false</c>).</param>
public record GetPropertyValueSchemaResult(
    bool Success,
    Guid DataTypeKey,
    string? ValueClrTypeName,
    JsonObject? ValueSchema,
    string? Message);
