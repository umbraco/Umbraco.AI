import type { UaiPromptScope, UaiScopeRule } from './types.js';

/**
 * Context information available when determining if a prompt should appear.
 */
export interface PropertyActionContext {
    /** The property editor UI alias (e.g., 'Umb.PropertyEditorUi.TextBox'). */
    propertyEditorUiAlias: string;
    /** The property alias (e.g., 'pageTitle'). */
    propertyAlias: string;
    /** The document type alias (e.g., 'article'). May be null if not in a document context. */
    documentTypeAlias: string | null;
}

/**
 * Determines if a prompt should appear for the given context.
 *
 * Logic:
 * - No scope or empty includeRules = don't show (scoped by default)
 * - If any excludeRule matches = don't show
 * - If any includeRule matches = show
 */
export function shouldShowPrompt(
    scope: UaiPromptScope | null,
    context: PropertyActionContext
): boolean {
    // No scope = doesn't appear anywhere (scoped by default)
    if (!scope) {
        return false;
    }

    // No include rules = doesn't appear anywhere
    if (scope.includeRules.length === 0) {
        return false;
    }

    // Check exclusions first (exclusions take precedence)
    if (scope.excludeRules.length > 0) {
        const isExcluded = scope.excludeRules.some((ruleSet) => matchesRule(ruleSet, context));
        if (isExcluded) {
            return false;
        }
    }

    // Check inclusions (OR logic between rules)
    return scope.includeRules.some((rule) => matchesRule(rule, context));
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

    // Check document type alias
    if (rule.documentTypeAliases && rule.documentTypeAliases.length > 0) {
        if (!context.documentTypeAlias || !rule.documentTypeAliases.includes(context.documentTypeAlias)) {
            return false;
        }
    }

    return true;
}
