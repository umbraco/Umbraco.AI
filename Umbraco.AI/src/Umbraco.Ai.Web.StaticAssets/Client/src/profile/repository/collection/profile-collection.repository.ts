import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiProfileCollectionServerDataSource } from "./profile-collection.server.data-source.js";

/**
 * Repository for Profile collection operations.
 */
export class UaiProfileCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiProfileCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiProfileCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiProfileCollectionRepository as api };
