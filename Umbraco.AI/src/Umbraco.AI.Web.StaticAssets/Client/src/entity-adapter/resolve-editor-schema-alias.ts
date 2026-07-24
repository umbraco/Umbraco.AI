import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbDataTypeItemRepository } from "@umbraco-cms/backoffice/data-type";

/**
 * Resolves the property editor schema alias for a property value being applied.
 *
 * Value preparers are matched by editor schema alias. The alias is normally taken from the existing
 * value entry, but a property that currently has NO value (an empty field) has no such entry — so we
 * fall back to resolving it from the property's data type via the data-type item repository. Without
 * this, preparers (e.g. rich-text / date-time) would be skipped when writing into an empty field.
 *
 * @param host The controller host (the workspace context) used to instantiate the repository.
 * @param existingEditorAlias The editor alias from the existing value entry, if any.
 * @param dataTypeUnique The property's data type unique, used as the fallback lookup key.
 * @returns The editor schema alias, or `undefined` when it cannot be resolved.
 */
export async function resolveEditorSchemaAlias(
    host: UmbControllerHost,
    existingEditorAlias: string | undefined,
    dataTypeUnique: string | undefined,
): Promise<string | undefined> {
    if (existingEditorAlias) {
        return existingEditorAlias;
    }

    if (!dataTypeUnique) {
        return undefined;
    }

    try {
        const repository = new UmbDataTypeItemRepository(host);
        const { data } = await repository.requestItems([dataTypeUnique]);
        return data?.[0]?.propertyEditorSchemaAlias;
    } catch {
        // A lookup failure is non-fatal — fall back to no preparer (raw value passthrough).
        return undefined;
    }
}
