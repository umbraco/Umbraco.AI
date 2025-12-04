import { UMB_PROPERTY_CONTEXT } from '@umbraco-cms/backoffice/property';
import { UMB_PROPERTY_STRUCTURE_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/content-type';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import type { UmbConditionConfigBase, UmbConditionControllerArguments, UmbExtensionCondition } from '@umbraco-cms/backoffice/extension-api';
import { UmbConditionBase } from '@umbraco-cms/backoffice/extension-registry';
import { shouldShowPrompt, type PropertyActionContext } from './prompt-scope-matcher.js';
import type { UaiPromptScope } from './types.js';

/**
 * Condition configuration for prompt scope filtering.
 */
export interface UaiPromptScopeConditionConfig extends UmbConditionConfigBase {
    scope: UaiPromptScope | null;
}

const PropertyContextSymbol = Symbol();
const ContentTypeSymbol = Symbol();

/**
 * Condition that determines if a prompt should appear based on its scope configuration.
 */
export class UaiPromptScopeCondition
    extends UmbConditionBase<UaiPromptScopeConditionConfig>
    implements UmbExtensionCondition
{
    #propertyEditorUiAlias: string | null = null;
    #propertyAlias: string | null = null;
    #documentTypeAliases: string[] = [];

    constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UaiPromptScopeConditionConfig>) {
        super(host, args);

        // Get property context for property editor UI alias and property alias
        this.consumeContext(UMB_PROPERTY_CONTEXT, (context) => {
            if (!context) {
                this.#propertyEditorUiAlias = null;
                this.#propertyAlias = null;
                this.#updatePermitted();
                return;
            }

            // Observe the editor manifest for property editor UI alias
            this.observe(
                context.editorManifest,
                (manifest) => {
                    this.#propertyEditorUiAlias = manifest?.alias ?? null;
                    this.#updatePermitted();
                },
                PropertyContextSymbol
            );

            // Get the property alias
            this.#propertyAlias = context.getAlias() ?? null;
            this.#updatePermitted();
        });

        // Get content type context for document type alias
        this.consumeContext(UMB_PROPERTY_STRUCTURE_WORKSPACE_CONTEXT, (context) => {
            if (!context) {
                this.#documentTypeAliases = [];
                this.#updatePermitted();
                return;
            }

            this.observe(
                context.structure.contentTypeAliases,
                (aliases) => {
                    this.#documentTypeAliases = aliases ?? [];
                    this.#updatePermitted();
                },
                ContentTypeSymbol
            );
        });
    }

    #updatePermitted(): void {
        // If we don't have the basic context yet, don't show
        if (!this.#propertyEditorUiAlias || !this.#propertyAlias) {
            this.permitted = false;
            return;
        }

        const context: PropertyActionContext = {
            propertyEditorUiAlias: this.#propertyEditorUiAlias,
            propertyAlias: this.#propertyAlias,
            // Use the first content type alias (typically the main document type)
            documentTypeAlias: this.#documentTypeAliases.length > 0 ? this.#documentTypeAliases[0] : null,
        };

        this.permitted = shouldShowPrompt(this.config.scope, context);
    }
}

export { UaiPromptScopeCondition as api };
