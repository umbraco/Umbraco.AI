/**
 * Represents a tool scope item.
 * @public
 */
export interface UaiToolScope {
    id: string;
    icon: string;
    isDestructive: boolean;
    domain: string;
}

/**
 * Represents a tool item.
 * @public
 */
export interface UaiToolItem {
    id: string;
    name: string;
    description: string;
    scopeId: string;
    isDestructive: boolean;
    tags: string[];
}
