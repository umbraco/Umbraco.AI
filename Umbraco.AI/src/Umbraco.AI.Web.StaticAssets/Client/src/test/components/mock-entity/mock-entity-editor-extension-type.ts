/**
 * Mock Entity Editor Extension Type
 *
 * Defines a custom extension type for pluggable mock entity editors.
 * Third parties can register editors for specific entity types.
 */

import type { ManifestBase } from "@umbraco-cms/backoffice/extension-api";

export const UAI_TEST_MOCK_ENTITY_EDITOR_EXTENSION_TYPE = "uaiTestMockEntityEditor";

export interface ManifestTestMockEntityEditor extends ManifestBase {
    type: typeof UAI_TEST_MOCK_ENTITY_EDITOR_EXTENSION_TYPE;
    /** Entity types this editor handles (e.g., ["document", "media", "member"]) */
    forEntityTypes: string[];
    /** Lazy loader for the editor element */
    element: () => Promise<any>;
}
