/**
 * Synthetic property-editor-UI alias for the agent's "Starter prompts" property.
 *
 * The agent editor is a workspace view, not a document-type property, so no real property editor UI
 * exists here. `umb-property-action-menu` matches actions solely on the string it is handed via
 * `.propertyEditorUiAlias`, so a namespaced alias of our own gives that one property its own action
 * menu without pulling in the document-type property stack.
 */
export const UAI_AGENT_STARTER_PROMPTS_PROPERTY_EDITOR_UI_ALIAS = "Uai.PropertyEditorUi.AgentStarterPrompts";

export const UAI_SUGGEST_STARTER_PROMPTS_PROPERTY_ACTION_ALIAS =
    "UmbracoAIAgent.PropertyAction.SuggestStarterPrompts";
