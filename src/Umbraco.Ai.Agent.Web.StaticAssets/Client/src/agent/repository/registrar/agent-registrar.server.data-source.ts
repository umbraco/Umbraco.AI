import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AgentsService } from "../../../api/index.js";
import type { PromptItemResponseModel } from "../../../api/types.gen.js";
import type { UAiAgentRegistrationModel } from "../../property-actions/types.js";

/**
 * Server data source for fetching Agents for property action registration.
 * Only returns active Agents with minimal data needed for registration.
 */
export class UAiAgentRegistrarServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets all active Agents for registration as property actions.
     */
    async getActiveAgents(): Promise<{ data?: UAiAgentRegistrationModel[]; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            AgentsService.getAllAgents({
                query: {
                    skip: 0,
                    take: 1000, // Fetch all Agents - registration happens once
                },
            })
        );

        if (error || !data) {
            return { error };
        }

        // Filter to only active Agents and fetch full details for content
        const activeAgents = data.items.filter((item: PromptItemResponseModel) => item.isActive);

        // We need to fetch full details for each prompt to get the content
        const promptDetails = await Promise.all(
            activeAgents.map(async (item: PromptItemResponseModel) => {
                const { data: detail, error: detailError } = await tryExecute(
                    this.#host,
                    AgentsService.getPromptByIdOrAlias({
                        path: { promptIdOrAlias: item.id },
                    })
                );

                if (detailError || !detail) {
                    return null;
                }

                return {
                    unique: detail.id,
                    alias: detail.alias,
                    name: detail.name,
                    description: detail.description ?? null,
                    content: detail.content,
                    profileId: detail.profileId ?? null,
                    scope: detail.scope ? {
                        allowRules: detail.scope.allowRules?.map(r => ({
                            propertyEditorUiAliases: r.propertyEditorUiAliases ?? null,
                            propertyAliases: r.propertyAliases ?? null,
                            contentTypeAliases: r.contentTypeAliases ?? null,
                        })) ?? [],
                        denyRules: detail.scope.denyRules?.map(r => ({
                            propertyEditorUiAliases: r.propertyEditorUiAliases ?? null,
                            propertyAliases: r.propertyAliases ?? null,
                            contentTypeAliases: r.contentTypeAliases ?? null,
                        })) ?? [],
                    } : null,
                } satisfies UAiAgentRegistrationModel;
            })
        );

        return {
            data: promptDetails.filter((p): p is UAiAgentRegistrationModel => p !== null),
        };
    }
}
