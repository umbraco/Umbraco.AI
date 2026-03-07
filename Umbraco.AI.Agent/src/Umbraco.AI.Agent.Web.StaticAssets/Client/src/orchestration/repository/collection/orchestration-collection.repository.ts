import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiOrchestrationCollectionServerDataSource } from "./orchestration-collection.server.data-source.js";

/**
 * Repository for Orchestration collection operations.
 */
export class UaiOrchestrationCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiOrchestrationCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiOrchestrationCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiOrchestrationCollectionRepository as api };
