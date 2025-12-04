/**
 * A single scope rule that determines where a prompt can appear.
 * All non-null properties use AND logic between them.
 * Values within each array use OR logic.
 */
export interface UaiScopeRule {
    /** Property Editor UI aliases to match (OR within array). Null/empty = any. */
    propertyEditorUiAliases: string[] | null;
    /** Property aliases to match (OR within array). Null/empty = any. */
    propertyAliases: string[] | null;
    /** Document type aliases to match (OR within array). Null/empty = any. */
    documentTypeAliases: string[] | null;
}

/**
 * Scope configuration defining where a prompt appears.
 */
export interface UaiPromptScope {
    /** Rules that define where the prompt should appear (OR between rules). */
    includeRules: UaiScopeRule[];
    /** Rules that define where the prompt should NOT appear (OR between rules). */
    excludeRules: UaiScopeRule[];
}

/**
 * Model for prompt registration - contains only the data needed for property action registration.
 */
export interface UaiPromptRegistrationModel {
    unique: string;
    alias: string;
    name: string;
    description: string | null;
    content: string;
    profileId: string | null;
    scope: UaiPromptScope | null;
}

/**
 * Meta data passed to the prompt property action.
 */
export interface UaiPromptPropertyActionMeta {
    icon: string;
    label: string;
    promptUnique: string;
    promptAlias: string;
    promptContent: string;
    promptDescription: string | null;
    promptProfileId: string | null;
    promptScope: UaiPromptScope | null;
}

/**
 * Data passed to the prompt preview modal.
 */
export interface UaiPromptPreviewModalData {
    promptName: string;
    promptDescription: string | null;
    promptContent: string;
    promptProfileId: string | null;
}

/**
 * Value returned from the prompt preview modal.
 */
export interface UaiPromptPreviewModalValue {
    action: 'insert' | 'copy' | 'cancel';
    content?: string;
}
