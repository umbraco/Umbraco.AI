import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiTraceCollectionServerDataSource } from "./trace-collection.server.data-source.js";

/**
 * Repository for Trace collection operations.
 */
export class UaiTraceCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiTraceCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiTraceCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiTraceCollectionRepository as api };
