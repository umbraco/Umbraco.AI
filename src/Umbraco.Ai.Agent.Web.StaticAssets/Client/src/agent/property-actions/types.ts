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
export interface UAiAgentScope {
    /** Rules that define where the prompt is allowed (OR between rules). */
    allowRules: UaiScopeRule[];
    /** Rules that define where the prompt is denied (OR between rules). */
    denyRules: UaiScopeRule[];
}

/**
 * Model for prompt registration - contains only the data needed for property action registration.
 */
export interface UAiAgentRegistrationModel {
    unique: string;
    alias: string;
    name: string;
    description: string | null;
    content: string;
    profileId: string | null;
    scope: UAiAgentScope | null;
}

/**
 * Meta data passed to the prompt property action.
 * Simplified to only include data needed for property action display and execution.
 */
export interface UAiAgentPropertyActionMeta {
    icon: string;
    label: string;
    promptUnique: string;
    promptDescription: string | null;
    Agentscope: UAiAgentScope | null;
}

/**
 * Data passed to the prompt preview modal.
 * Contains entity context for server-side execution.
 */
export interface UAiAgentPreviewModalData {
    promptUnique: string;
    promptName: string;
    promptDescription: string | null;
    // Entity context for execution (required for scope validation)
    entityId: string;
    entityType: string;
    propertyAlias: string;
    culture?: string;
    segment?: string;
}

/**
 * Value returned from the prompt preview modal.
 */
export interface UAiAgentPreviewModalValue {
    action: 'insert' | 'copy' | 'cancel';
    content?: string;
}
