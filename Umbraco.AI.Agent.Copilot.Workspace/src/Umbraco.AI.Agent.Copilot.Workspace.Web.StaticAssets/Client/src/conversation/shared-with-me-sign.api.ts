import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import { UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import type { UmbEntitySignApi, UmbEntitySignApiArgs } from "@umbraco-cms/backoffice/entity-sign";
import { UAI_SHARED_CONVERSATION_ROW_CONTEXT } from "./shared-with-me-row.context.js";

/**
 * Entity-sign api for the "shared with me" badge (AC2). Unlike the owner-side "Is Shared" sign (a fixed
 * `meta.label` string), this badge's tooltip is per-row ("Shared by <owner>"), so it exposes a reactive
 * `label` observable instead of a static `getLabel()`. `createExtensionElementWithApi` constructs this
 * api with the sign element itself as `host`, which — once rendered inside `<umb-entity-sign-bundle>` —
 * is a real DOM descendant of the row, so `consumeContext` reaches the per-row
 * {@link UaiSharedConversationRowContext} provided by the tree item via normal context-request bubbling.
 */
export class UaiSharedWithMeSignApi extends UmbControllerBase implements UmbEntitySignApi {
    #label = new UmbStringState("");
    #localize = new UmbLocalizationController(this);

    readonly label = this.#label.asObservable();

    constructor(host: UmbControllerHost, _args: UmbEntitySignApiArgs) {
        super(host);
        this.consumeContext(UAI_SHARED_CONVERSATION_ROW_CONTEXT, (context) => {
            this.observe(context?.ownerDisplayName, (owner) => {
                this.#label.setValue(this.#localize.term("uaiCopilotWorkspace_sharedByOwner", owner ?? ""));
            });
        });
    }
}

export { UaiSharedWithMeSignApi as api };
