using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Umbraco.AI.Web.Api.Common.Configuration;

/// <summary>
/// Renames Microsoft.AspNetCore.OpenApi's mangled schema names for derived polymorphic types
/// (e.g. <c>AgentConfigModelStandardAgentConfigModel</c>) back to the simple derived type name
/// (<c>StandardAgentConfigModel</c>).
/// </summary>
/// <remarks>
/// Microsoft.AspNetCore.OpenApi names derived polymorphic schemas as
/// <c>{baseSchemaId}{derivedTypeName}</c>. <see cref="OpenApiOptions.CreateSchemaReferenceId"/>
/// is only consulted for the base type — it isn't called independently for derived types, so it
/// cannot influence their names. See dotnet/aspnetcore#58332.
///
/// This transformer detects polymorphic schemas by their <c>discriminator.mapping</c> entries and
/// shortens any derived schema name whose key begins with the base schema name. It updates the
/// references in <c>anyOf</c>, <c>oneOf</c>, <c>allOf</c>, and the discriminator mapping so the
/// document stays internally consistent. Without this transformer, regenerated TypeScript clients
/// see renamed types across the v17 → v18 migration.
/// </remarks>
internal sealed class PreservePolymorphicSchemaNamesTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (document.Components?.Schemas is not { Count: > 0 } schemas)
        {
            return Task.CompletedTask;
        }

        var renames = BuildRenameMap(schemas);
        if (renames.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach ((var oldKey, var newKey) in renames)
        {
            if (schemas.ContainsKey(newKey))
            {
                // Collision — leave the framework name in place rather than silently overwrite.
                continue;
            }

            schemas[newKey] = schemas[oldKey];
            schemas.Remove(oldKey);
        }

        foreach (IOpenApiSchema schema in schemas.Values)
        {
            UpdateSchemaReferences(schema, renames, document);
        }

        return Task.CompletedTask;
    }

    private static Dictionary<string, string> BuildRenameMap(IDictionary<string, IOpenApiSchema> schemas)
    {
        var renames = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach ((var baseName, IOpenApiSchema baseSchema) in schemas)
        {
            if (baseSchema.Discriminator?.Mapping is not { Count: > 0 } mapping)
            {
                continue;
            }

            foreach (OpenApiSchemaReference derivedRef in mapping.Values.OfType<OpenApiSchemaReference>())
            {
                var derivedKey = derivedRef.Reference?.Id;
                if (string.IsNullOrEmpty(derivedKey))
                {
                    continue;
                }

                if (derivedKey.StartsWith(baseName, StringComparison.Ordinal) is false || derivedKey.Length <= baseName.Length)
                {
                    continue;
                }

                renames[derivedKey] = derivedKey[baseName.Length..];
            }
        }

        return renames;
    }

    private static void UpdateSchemaReferences(IOpenApiSchema schema, IReadOnlyDictionary<string, string> renames, OpenApiDocument document)
    {
        UpdateRefList(schema.AnyOf, renames, document);
        UpdateRefList(schema.OneOf, renames, document);
        UpdateRefList(schema.AllOf, renames, document);

        if (schema.Discriminator?.Mapping is { Count: > 0 } mapping)
        {
            foreach (var key in mapping.Keys.ToList())
            {
                if (mapping[key] is OpenApiSchemaReference current &&
                    current.Reference?.Id is { } id &&
                    renames.TryGetValue(id, out var newName))
                {
                    mapping[key] = new OpenApiSchemaReference(newName, document);
                }
            }
        }
    }

    private static void UpdateRefList(IList<IOpenApiSchema>? list, IReadOnlyDictionary<string, string> renames, OpenApiDocument document)
    {
        if (list is null)
        {
            return;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is OpenApiSchemaReference reference &&
                reference.Reference?.Id is { } id &&
                renames.TryGetValue(id, out var newName))
            {
                list[i] = new OpenApiSchemaReference(newName, document);
            }
        }
    }
}
