import type { UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiTestRunCollectionServerDataSource } from "./test-run-collection.server.data-source.js";
import type { UaiTestRunCollectionFilterModel } from "../../types.js";

/**
 * Repository for Test Run collection operations.
 */
export class UaiTestRunCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiTestRunCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiTestRunCollectionServerDataSource(host);
    }

    async requestCollection(filter: UaiTestRunCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiTestRunCollectionRepository as api };
