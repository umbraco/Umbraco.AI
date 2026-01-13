import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiAuditLogCollectionServerDataSource } from "./audit-collection.server.data-source.ts";

/**
 * Repository for AuditLog collection operations.
 */
export class UaiAuditLogCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiAuditLogCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiAuditLogCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiAuditLogCollectionRepository as api };
