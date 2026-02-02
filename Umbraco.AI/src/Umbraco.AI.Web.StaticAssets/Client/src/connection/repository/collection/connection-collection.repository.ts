import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiConnectionCollectionServerDataSource } from "./connection-collection.server.data-source.js";

/**
 * Repository for Connection collection operations.
 */
export class UaiConnectionCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiConnectionCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiConnectionCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiConnectionCollectionRepository as api };
