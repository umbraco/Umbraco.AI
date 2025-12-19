import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UAiAgentCollectionServerDataSource } from "./agent-collection.server.data-source.js";

/**
 * Repository for Agent collection operations.
 */
export class UAiAgentCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UAiAgentCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UAiAgentCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UAiAgentCollectionRepository as api };
