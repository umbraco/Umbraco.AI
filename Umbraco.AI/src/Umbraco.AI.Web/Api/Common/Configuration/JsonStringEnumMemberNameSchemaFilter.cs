using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Umbraco.AI.Web.Api.Common.Configuration;

/// <summary>
/// Rewrites string-enum schema values to honor <see cref="JsonStringEnumMemberNameAttribute"/>,
/// so the generated OpenAPI document (and downstream TypeScript clients) advertise the actual
/// wire values rather than the .NET member names.
/// </summary>
/// <remarks>
/// Swashbuckle derives string-enum schema values from the .NET enum member names and ignores
/// <c>[JsonStringEnumMemberName]</c>, even though the runtime (<see cref="JsonStringEnumConverter{TEnum}"/>)
/// serializes and deserializes using the attribute values. The result is a schema that lists values
/// the server will reject: under ASP.NET's case-sensitive Web JSON binding, sending the advertised
/// <c>"Resolved"</c> for an enum whose wire value is <c>"resolved"</c> yields a 400. This filter closes
/// that gap by replacing each string-enum schema's values with the member-name overrides.
///
/// Only string enums are touched: integer enums (no <see cref="JsonStringEnumConverter{TEnum}"/>)
/// produce numeric <c>enum</c> entries and are left alone, and <c>[Flags]</c> string enums produce no
/// <c>enum</c> array (the value is an open-ended set) so there is nothing to rewrite. The wire value is
/// the attribute name when present, otherwise the member name — matching the converter's behavior when
/// no naming policy is configured.
/// </remarks>
internal sealed class JsonStringEnumMemberNameSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        // Only the concrete schema is mutable; the interface exposes Enum read-only.
        if (schema is OpenApiSchema concrete)
        {
            RewriteEnumValues(concrete, context.Type);
        }
    }

    /// <summary>
    /// Replaces a string-enum schema's values with the <see cref="JsonStringEnumMemberNameAttribute"/>
    /// wire values. No-op for non-enum types, integer enums, and enums without an <c>enum</c> array.
    /// </summary>
    internal static void RewriteEnumValues(OpenApiSchema schema, Type type)
    {
        // Only string enums carry an enum value list to correct.
        if (schema.Enum is not { Count: > 0 } values)
        {
            return;
        }

        Type enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (enumType.IsEnum == false)
        {
            return;
        }

        // Integer enums serialize as numbers; leave their numeric enum entries untouched.
        if (values.Any(value => value is JsonValue node && node.GetValueKind() == JsonValueKind.String) == false)
        {
            return;
        }

        // Mutate the existing list in place rather than reassigning schema.Enum: Swashbuckle/
        // Microsoft.OpenApi retain the original list reference, so a reassignment is silently
        // discarded (the original list is what gets serialized).
        FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        values.Clear();
        foreach (FieldInfo field in fields)
        {
            var memberName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? field.Name;
            values.Add(JsonValue.Create(memberName));
        }
    }
}
