import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbPropertyActionBase, type UmbPropertyActionArgs } from "@umbraco-cms/backoffice/property-action";
import { UmbBasicState } from "@umbraco-cms/backoffice/observable-api";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UaiPartialUpdateCommand, UAI_EMPTY_GUID } from "@umbraco-ai/core";
import { AgentsService } from "../../api/sdk.gen.js";
import type { UaiAgentDetailModel } from "../types.js";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "../workspace/agent/agent-workspace.context-token.js";

/**
 * Drafts starter prompts from the agent's own `Instructions` and writes them onto the workspace
 * model. It never persists anything — the author still has to save, and can edit or undo the drafts
 * first. A failed call leaves the existing rows untouched.
 *
 * The suggestion call reads the agent's persisted instructions server-side, so it has nothing to
 * work from until the agent has been saved once, and it needs a profile to run against — either the
 * agent's own or the default chat profile. Both are surfaced through {@link disabledReasonKey}
 * rather than left to fail on click.
 */
export class UaiSuggestStarterPromptsPropertyAction extends UmbPropertyActionBase {
    #notificationContext?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;
    #localize = new UmbLocalizationController(this);

    /** Identity of the last agent/profile pair checked, so an unrelated edit does not re-ask the server. */
    #lastCheckedKey?: string;

    #disabledReasonKey = new UmbBasicState<string | undefined>(undefined);

    /**
     * Localisation key explaining why the action cannot run, or `undefined` when it can. The element
     * localises it — this is a key, not a message.
     */
    readonly disabledReasonKey = this.#disabledReasonKey.asObservable();

    constructor(host: UmbControllerHost, args: UmbPropertyActionArgs<never>) {
        super(host, args);

        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this.#notificationContext = context;
        });

        this.consumeContext(UAI_AGENT_WORKSPACE_CONTEXT, (context) => {
            if (!context) return;
            this.observe(
                context.model,
                (model) => this.#refreshAvailability(model),
                "_observeAgentForStarterSuggestions",
            );
        });
    }

    async #refreshAvailability(model?: UaiAgentDetailModel) {
        const unique = model?.unique;
        const profileId = model?.profileId ?? null;

        // The model state re-emits on every keystroke elsewhere in the editor; only the agent
        // identity and its profile can change the answer.
        const key = `${unique ?? ""}|${profileId ?? ""}`;
        if (key === this.#lastCheckedKey) return;
        this.#lastCheckedKey = key;

        if (!unique || unique === UAI_EMPTY_GUID) {
            this.#disabledReasonKey.setValue("uaiAgent_suggestStartersUnsavedAgent");
            return;
        }

        if (profileId) {
            // Known locally — no need to ask the server.
            this.#disabledReasonKey.setValue(undefined);
            return;
        }

        const { data } = await tryExecute(
            this,
            AgentsService.getSuggestStartersAvailability({ path: { agentIdOrAlias: unique } }),
        );

        // A later change already superseded this check.
        if (this.#lastCheckedKey !== key) return;

        this.#disabledReasonKey.setValue(data === true ? undefined : "uaiAgent_suggestStartersNoProfile");
    }

    override async execute() {
        const workspaceContext = await this.getContext(UAI_AGENT_WORKSPACE_CONTEXT);
        const unique = workspaceContext?.getData()?.unique;
        if (!workspaceContext || !unique || unique === UAI_EMPTY_GUID) return;

        const { data, error } = await tryExecute(
            this,
            AgentsService.suggestStarters({ path: { agentIdOrAlias: unique } }),
        );

        if (error || !data) {
            this.#notificationContext?.peek("danger", {
                data: { message: this.#localize.string("#uaiAgent_suggestStartersFailed") },
            });
            return;
        }

        workspaceContext.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>(
                { starterPrompts: data.starters.map((prompt) => ({ prompt })) },
                "starterPrompts",
            ),
        );
    }
}

export { UaiSuggestStarterPromptsPropertyAction as api };
export default UaiSuggestStarterPromptsPropertyAction;
