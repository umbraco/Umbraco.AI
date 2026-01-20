/**
 * UI mode for prompt property actions.
 * - 'modal': Shows a centered dialog (default)
 * - 'panel': Shows a slide-in side panel
 */
export type UaiPromptUiMode = 'modal' | 'panel';

/**
 * A single scope rule that determines where a prompt can run.
 * All non-null properties use AND logic between them.
 * Values within each array use OR logic.
 */
export interface UaiScopeRule {
    /** Property Editor UI aliases to match (OR within array). Null/empty = any. */
    propertyEditorUiAliases: string[] | null;
    /** Property aliases to match (OR within array). Null/empty = any. */
    propertyAliases: string[] | null;
    /** Content type aliases to match (OR within array). Null/empty = any. */
    contentTypeAliases: string[] | null;
}

/**
 * Scope configuration defining where a prompt can run.
 */
export interface UaiPromptScope {
    /** Rules that define where the prompt is allowed (OR between rules). */
    allowRules: UaiScopeRule[];
    /** Rules that define where the prompt is denied (OR between rules). */
    denyRules: UaiScopeRule[];
}

/**
 * Model for prompt registration - contains only the data needed for property action registration.
 */
export interface UaiPromptRegistrationModel {
    unique: string;
    alias: string;
    name: string;
    description: string | null;
    instructions: string;
    profileId: string | null;
    scope: UaiPromptScope | null;
    /** UI mode for the property action. Defaults to 'modal'. */
    uiMode?: UaiPromptUiMode;
}

/**
 * Meta data passed to the prompt property action.
 * Simplified to only include data needed for property action display and execution.
 */
export interface UaiPromptPropertyActionMeta {
    icon: string;
    label: string;
    promptUnique: string;
    promptDescription: string | null;
    promptScope: UaiPromptScope | null;
    /** UI mode for the property action. Defaults to 'modal'. */
    uiMode?: UaiPromptUiMode;
}

/**
 * Context item for passing data to AI operations.
 * Matches backend AiRequestContextItem.
 */
export interface UaiPromptContextItem {
    /** Human-readable description */
    description: string;
    /** The context data (any JSON-serializable value) */
    value?: string;
}

/**
 * Data passed to the prompt preview modal.
 * Contains entity context for server-side execution.
 */
export interface UaiPromptPreviewModalData {
    promptUnique: string;
    promptName: string;
    promptDescription: string | null;
    // Entity context for execution (required for scope validation)
    entityId: string;
    entityType: string;
    propertyAlias: string;
    culture?: string;
    segment?: string;
    /** Serialized entity context for AI operations */
    context?: UaiPromptContextItem[];
}

/**
 * Property change to be applied to the entity.
 * Matches core UaiPropertyChange type.
 */
export interface UaiPromptPropertyChange {
    /** The property alias. */
    alias: string;
    /** The new value to set. */
    value: unknown;
    /** The culture for variant content. */
    culture?: string;
    /** The segment for segmented content. */
    segment?: string;
}

/**
 * Value returned from the prompt preview modal.
 */
export interface UaiPromptPreviewModalValue {
    action: 'insert' | 'copy' | 'cancel';
    content?: string;
    /** Property changes to apply to the entity. */
    propertyChanges?: UaiPromptPropertyChange[];
}
