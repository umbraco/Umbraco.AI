import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiProjectServerDataSource } from "./project.server.data-source.js";

/** Repository for project read operations (Phase 5 uses the list only). */
export class UaiProjectRepository extends UmbRepositoryBase {
    #source: UaiProjectServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#source = new UaiProjectServerDataSource(host);
    }

    async requestCollection() {
        return this.#source.getCollection();
    }
}
