import type { UaiContextResourceTypeDetailModel, UaiContextResourceTypeItemModel } from "./types.js";
import { ContextResourceTypeResponseModel } from "../api";

export const UaiContextResourceTypeTypeMapper = {
    toItemModel(response: ContextResourceTypeResponseModel): UaiContextResourceTypeItemModel {
        return {
            id: response.id,
            name: response.name,
            description: response.description,
            icon: response.icon,
        };
    },

    toDetailModel(response: ContextResourceTypeResponseModel): UaiContextResourceTypeDetailModel {
        return {
            id: response.id,
            name: response.name,
            description: response.description,
            icon: response.icon,
        };
    },
};
