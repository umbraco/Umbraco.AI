/**
 * Represents a single version history item.
 */
export interface UaiVersionHistoryItem {
    /** The unique identifier of this version record. */
    id: string;
    /** The ID of the entity this version belongs to. */
    entityId: string;
    /** The version number (1, 2, 3, etc.). */
    version: number;
    /** The date and time when this version was created. */
    dateCreated: string;
    /** The user ID who created this version, if available. */
    createdByUserId?: string | null;
    /** The user name who created this version, if available. */
    createdByUserName?: string | null;
    /** Optional description of what changed in this version. */
    changeDescription?: string | null;
}

/**
 * Response model for version history with pagination info.
 */
export interface UaiVersionHistoryResponse {
    /** The current version of the entity. */
    currentVersion: number;
    /** Total number of versions. */
    totalVersions: number;
    /** The versions in this page. */
    versions: UaiVersionHistoryItem[];
}

/**
 * Represents a property change between two versions.
 */
export interface UaiVersionPropertyChange {
    /** The name of the property that changed. */
    propertyName: string;
    /** The old value (from the source version). */
    oldValue?: string | null;
    /** The new value (from the target version). */
    newValue?: string | null;
}

/**
 * Response model for version comparison.
 */
export interface UaiVersionComparisonResponse {
    /** The source version number. */
    fromVersion: number;
    /** The target version number. */
    toVersion: number;
    /** The list of property changes. */
    changes: UaiVersionPropertyChange[];
}
