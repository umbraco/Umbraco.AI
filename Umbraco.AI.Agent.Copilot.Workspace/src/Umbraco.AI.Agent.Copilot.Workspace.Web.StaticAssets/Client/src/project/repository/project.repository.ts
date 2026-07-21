import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiProjectServerDataSource } from "./project.server.data-source.js";
import type { ProjectRequestModel } from "../../api/types.gen.js";

/** Repository for project collection + detail + mutation operations. */
export class UaiProjectRepository extends UmbRepositoryBase {
    #source: UaiProjectServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#source = new UaiProjectServerDataSource(host);
    }

    async requestCollection() {
        return this.#source.getCollection();
    }

    async requestById(id: string) {
        return this.#source.getById(id);
    }

    async create(request: ProjectRequestModel) {
        return this.#source.create(request);
    }

    async update(id: string, request: ProjectRequestModel) {
        return this.#source.update(id, request);
    }

    async delete(id: string) {
        return this.#source.delete(id);
    }
}
