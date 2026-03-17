import type { UmbCollectionFilterModel, UmbCollectionRepository } from "@umbraco-cms/backoffice/collection";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UaiGuardrailCollectionServerDataSource } from "./guardrail-collection.server.data-source.js";

/**
 * Repository for Guardrail collection operations.
 */
export class UaiGuardrailCollectionRepository extends UmbRepositoryBase implements UmbCollectionRepository {
    #collectionSource: UaiGuardrailCollectionServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#collectionSource = new UaiGuardrailCollectionServerDataSource(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        return this.#collectionSource.getCollection(filter);
    }
}

export { UaiGuardrailCollectionRepository as api };
