import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ProjectsService } from "../../api/sdk.gen.js";
import { copilotWorkspaceClientReady } from "../../app.js";

/**
 * Server data source for projects. Phase 5 only needs the list (to resolve
 * project names for grouping the conversation sidebar); full project CRUD is
 * Phase 6.
 */
export class UaiProjectServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    async getCollection() {
        await copilotWorkspaceClientReady;
        return tryExecute(this.#host, ProjectsService.getAll());
    }

    async getById(id: string) {
        await copilotWorkspaceClientReady;
        return tryExecute(this.#host, ProjectsService.getById({ path: { id } }));
    }
}
