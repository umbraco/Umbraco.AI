import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiPromptCollectionServerDataSource } from "./prompt-collection.server.data-source.js";

/**
 * Repository for Prompt collection operations.
 */
export class UaiPromptCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiPromptCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiPromptCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiPromptCollectionRepository as api };
