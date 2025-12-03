import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbPropertyActionBase, type UmbPropertyActionArgs } from '@umbraco-cms/backoffice/property-action';
import { UMB_PROPERTY_CONTEXT } from '@umbraco-cms/backoffice/property';
import { umbOpenModal } from '@umbraco-cms/backoffice/modal';
import { UAI_PROMPT_PREVIEW_MODAL } from './prompt-preview-modal.token.js';
import type { UaiPromptPropertyActionMeta } from './types.js';

/**
 * Property action that opens a modal to preview and insert prompt content.
 */
export class UaiPromptInsertPropertyAction extends UmbPropertyActionBase<UaiPromptPropertyActionMeta> {
    #propertyContext?: typeof UMB_PROPERTY_CONTEXT.TYPE;
    #init: Promise<unknown>;

    constructor(host: UmbControllerHost, args: UmbPropertyActionArgs<UaiPromptPropertyActionMeta>) {
        super(host, args);

        this.#init = this.consumeContext(UMB_PROPERTY_CONTEXT, (context) => {
            this.#propertyContext = context;
        }).asPromise({ preventTimeout: true });
    }

    override async execute() {
        await this.#init;

        if (!this.#propertyContext) {
            throw new Error('Property context is not available');
        }

        const meta = this.args.meta;
        if (!meta) {
            throw new Error('Property action meta is not available');
        }

        try {
            const result = await umbOpenModal(this, UAI_PROMPT_PREVIEW_MODAL, {
                data: {
                    promptName: meta.label,
                    promptDescription: meta.promptDescription,
                    promptContent: meta.promptContent,
                },
            });

            if (result.action === 'insert' && result.content) {
                this.#propertyContext.setValue(result.content);
            }
        } catch {
            // Modal was rejected/cancelled - do nothing
        }
    }
}

export { UaiPromptInsertPropertyAction as api };
