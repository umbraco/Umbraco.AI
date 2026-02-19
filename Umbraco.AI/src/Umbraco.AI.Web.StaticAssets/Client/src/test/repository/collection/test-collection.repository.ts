import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiTestCollectionServerDataSource } from "./test-collection.server.data-source.js";

/**
 * Repository for Test collection operations.
 */
export class UaiTestCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiTestCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiTestCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiTestCollectionRepository as api };
