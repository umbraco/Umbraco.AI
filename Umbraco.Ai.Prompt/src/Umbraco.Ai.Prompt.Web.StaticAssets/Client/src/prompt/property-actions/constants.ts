/**
 * Property Editor UI aliases for text-based editors that support prompt insertion.
 */
export const TEXT_BASED_PROPERTY_EDITOR_UIS = [
    'Umb.PropertyEditorUi.TextBox',
    'Umb.PropertyEditorUi.TextArea',
    'Umb.PropertyEditorUi.Tiptap',          // Rich Text Editor (replaced TinyMCE in v16+)
    'Umb.PropertyEditorUi.MarkdownEditor',
] as const;

/**
 * Alias prefix for dynamically registered prompt property actions.
 */
export const UAI_PROMPT_PROPERTY_ACTION_PREFIX = 'UmbracoAiPrompt.PropertyAction';

/**
 * Alias for the prompt scope condition.
 */
export const UAI_PROMPT_SCOPE_CONDITION_ALIAS = 'Uai.Condition.PromptScope';
