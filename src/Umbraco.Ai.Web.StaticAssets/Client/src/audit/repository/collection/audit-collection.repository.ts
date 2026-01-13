import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiAuditCollectionServerDataSource } from "./audit-collection.server.data-source.ts";

/**
 * Repository for Audit collection operations.
 */
export class UaiAuditCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiAuditCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiAuditCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiAuditCollectionRepository as api };
