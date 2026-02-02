import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiAgentCollectionServerDataSource } from "./agent-collection.server.data-source.js";

/**
 * Repository for Agent collection operations.
 */
export class UaiAgentCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiAgentCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiAgentCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiAgentCollectionRepository as api };
