import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiKnowledgeSetCollectionServerDataSource } from "./knowledge-set-collection.server.data-source.js";

/**
 * Repository for Knowledge Set collection operations.
 */
export class UaiKnowledgeSetCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiKnowledgeSetCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiKnowledgeSetCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiKnowledgeSetCollectionRepository as api };
