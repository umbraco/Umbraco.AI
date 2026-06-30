using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Web.Api.Common.Configuration;

/// <summary>
/// Emits scalar types that serialize as a plain string — but whose type-level
/// <see cref="System.Text.Json.Serialization.JsonConverterAttribute"/> makes them opaque to
/// Microsoft.AspNetCore.OpenApi — as <c>type: string</c> rather than an empty schema (<c>{}</c>).
/// </summary>
/// <remarks>
/// The v17 (Swashbuckle) document mapped these with <c>MapType&lt;T&gt;(() =&gt; new OpenApiSchema { Type =
/// String })</c>. Microsoft.AspNetCore.OpenApi has no equivalent, and a custom type-level converter makes
/// the type opaque, so the generated client typed these as <c>{}</c>/<c>unknown</c> — breaking callers that
/// treat them as strings. This restores the string schema.
///
/// Covers <see cref="IdOrAlias"/> (its <c>IdOrAliasJsonConverter</c> writes a string) and
/// <see cref="System.Type"/> (its <c>JsonStringTypeConverter</c> writes a string).
/// </remarks>
internal sealed class StringScalarSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        Type type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;

        if (type == typeof(IdOrAlias) || type == typeof(Type))
        {
            schema.Type = JsonSchemaType.String;
            schema.Properties?.Clear();
            schema.AllOf?.Clear();
            schema.AnyOf?.Clear();
            schema.OneOf?.Clear();
        }

        return Task.CompletedTask;
    }
}
