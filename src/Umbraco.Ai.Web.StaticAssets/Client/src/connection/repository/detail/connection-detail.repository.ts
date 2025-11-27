import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiConnectionDetailServerDataSource } from "./connection-detail.server.data-source.js";
import { UAI_CONNECTION_DETAIL_STORE_CONTEXT } from "./connection-detail.store.js";
import type { UaiConnectionDetailModel } from "../../types.js";

/**
 * Repository for Connection detail CRUD operations.
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 */
export class UaiConnectionDetailRepository extends UmbDetailRepositoryBase<UaiConnectionDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiConnectionDetailServerDataSource, UAI_CONNECTION_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UaiConnectionDetailModel) {
        return super.create(model, null);
    }
}

export { UaiConnectionDetailRepository as api };
