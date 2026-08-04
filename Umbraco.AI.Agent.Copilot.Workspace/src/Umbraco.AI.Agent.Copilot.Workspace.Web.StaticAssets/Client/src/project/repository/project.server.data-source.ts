import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ProjectsService } from "../../api/sdk.gen.js";
import { copilotWorkspaceClientReady } from "../../app.js";
import type { ProjectRequestModel } from "../../api/types.gen.js";

/**
 * Server data source for projects, wrapping the generated `ProjectsService`. Each call awaits
 * {@link copilotWorkspaceClientReady} so the client carries auth, and routes through `tryExecute`.
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

    async create(request: ProjectRequestModel) {
        await copilotWorkspaceClientReady;
        return tryExecute(this.#host, ProjectsService.create({ body: request }));
    }

    async update(id: string, request: ProjectRequestModel) {
        await copilotWorkspaceClientReady;
        return tryExecute(this.#host, ProjectsService.update({ path: { id }, body: request }));
    }

    async delete(id: string) {
        await copilotWorkspaceClientReady;
        return tryExecute(this.#host, ProjectsService.delete({ path: { id } }));
    }
}
