import type { UaiPromptScope, UaiScopeRule } from './types.js';

/**
 * Context information available when determining if a prompt is allowed.
 */
export interface PropertyActionContext {
    /** The property editor UI alias (e.g., 'Umb.PropertyEditorUi.TextBox'). */
    propertyEditorUiAlias: string;
    /** The property alias (e.g., 'pageTitle'). */
    propertyAlias: string;
    /** The content type aliases including compositions (e.g., ['article', 'seoMixin']). */
    contentTypeAliases: string[];
}

/**
 * Determines if a prompt is allowed for the given context.
 *
 * Logic:
 * - No scope or empty allowRules = not allowed (denied by default)
 * - If any denyRule matches = not allowed
 * - If any allowRule matches = allowed
 */
export function isPromptAllowed(
    scope: UaiPromptScope | null,
    context: PropertyActionContext
): boolean {
    // No scope = not allowed anywhere (denied by default)
    if (!scope) {
        return false;
    }

    // No allow rules = not allowed anywhere
    if (scope.allowRules.length === 0) {
        return false;
    }

    // Check deny rules first (deny takes precedence)
    if (scope.denyRules.length > 0) {
        const isDenied = scope.denyRules.some((ruleSet) => matchesRule(ruleSet, context));
        if (isDenied) {
            return false;
        }
    }

    // Check allow rules (OR logic between rules)
    return scope.allowRules.some((rule) => matchesRule(rule, context));
}

/**
 * Checks if a single rule matches the context.
 * All non-null/non-empty properties must match (AND logic between properties).
 * For array properties, any value matching = that property matches (OR within array).
 */
function matchesRule(rule: UaiScopeRule, context: PropertyActionContext): boolean {
    // Check property editor UI alias
    if (rule.propertyEditorUiAliases && rule.propertyEditorUiAliases.length > 0) {
        if (!rule.propertyEditorUiAliases.includes(context.propertyEditorUiAlias)) {
            return false;
        }
    }

    // Check property alias
    if (rule.propertyAliases && rule.propertyAliases.length > 0) {
        if (!rule.propertyAliases.includes(context.propertyAlias)) {
            return false;
        }
    }

    // Check content type alias (match if any context alias is in the rule's aliases)
    if (rule.contentTypeAliases && rule.contentTypeAliases.length > 0) {
        const hasMatch = context.contentTypeAliases.some((alias) => rule.contentTypeAliases!.includes(alias));
        if (!hasMatch) {
            return false;
        }
    }

    return true;
}
