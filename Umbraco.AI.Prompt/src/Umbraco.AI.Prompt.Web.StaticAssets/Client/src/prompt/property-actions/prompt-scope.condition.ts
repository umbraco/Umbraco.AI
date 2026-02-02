import { UMB_PROPERTY_CONTEXT } from '@umbraco-cms/backoffice/property';
import { UMB_PROPERTY_STRUCTURE_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/content-type';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import type { UmbConditionConfigBase, UmbConditionControllerArguments, UmbExtensionCondition } from '@umbraco-cms/backoffice/extension-api';
import { UmbConditionBase } from '@umbraco-cms/backoffice/extension-registry';
import { isPromptAllowed, type PropertyActionContext } from './prompt-scope-matcher.js';
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
 * Condition that determines if a prompt is allowed based on its scope configuration.
 */
export class UaiPromptScopeCondition
    extends UmbConditionBase<UaiPromptScopeConditionConfig>
    implements UmbExtensionCondition
{
    #propertyEditorUiAlias: string | null = null;
    #propertyAlias: string | null = null;
    #contentTypeAliases: string[] = [];

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

        // Get content type context for content type alias
        this.consumeContext(UMB_PROPERTY_STRUCTURE_WORKSPACE_CONTEXT, (context) => {
            if (!context) {
                this.#contentTypeAliases = [];
                this.#updatePermitted();
                return;
            }

            this.observe(
                context.structure.contentTypeAliases,
                (aliases) => {
                    this.#contentTypeAliases = aliases ?? [];
                    this.#updatePermitted();
                },
                ContentTypeSymbol
            );
        });
    }

    #updatePermitted(): void {
        // If we don't have the basic context yet, don't allow
        if (!this.#propertyEditorUiAlias || !this.#propertyAlias) {
            this.permitted = false;
            return;
        }

        const context: PropertyActionContext = {
            propertyEditorUiAlias: this.#propertyEditorUiAlias,
            propertyAlias: this.#propertyAlias,
            contentTypeAliases: this.#contentTypeAliases,
        };

        this.permitted = isPromptAllowed(this.config.scope, context);
    }
}

export { UaiPromptScopeCondition as api };
