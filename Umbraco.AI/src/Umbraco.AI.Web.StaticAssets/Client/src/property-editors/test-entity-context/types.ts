/**
 * Response model for a registered entity type from the API.
 */
export interface TestEntityTypeModel {
    entityType: string;
    name: string;
    icon?: string | null;
    hasSubTypes: boolean;
}

/**
 * Response model for an entity sub-type from the API.
 */
export interface TestEntitySubTypeModel {
    alias: string;
    name: string;
    icon?: string | null;
    description?: string | null;
}

/**
 * The entity context configuration stored as the property editor value.
 */
export interface EntityContextValue {
    entityType: string;
    entitySubType?: string | null;
    mockEntity?: Record<string, unknown> | null;
}
