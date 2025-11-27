import { UmbDetailRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiProfileDetailServerDataSource } from "./profile-detail.server.data-source.js";
import { UAI_PROFILE_DETAIL_STORE_CONTEXT } from "./profile-detail.store.js";
import type { UaiProfileDetailModel } from "../../types.js";

/**
 * Repository for Profile detail CRUD operations.
 * Uses UmbDetailRepositoryBase for consistent CMS patterns.
 */
export class UaiProfileDetailRepository extends UmbDetailRepositoryBase<UaiProfileDetailModel> {
    constructor(host: UmbControllerHost) {
        super(host, UaiProfileDetailServerDataSource, UAI_PROFILE_DETAIL_STORE_CONTEXT);
    }

    override async create(model: UaiProfileDetailModel) {
        return super.create(model, null);
    }
}

export { UaiProfileDetailRepository as api };
