import type {
    KnowledgeSetResponseModel,
    KnowledgeSetDetailResponseModel,
    KnowledgeSetItemContentResponseModel,
} from "../api";
import { UAI_KNOWLEDGE_SET_ENTITY_TYPE } from "./constants.js";
import type {
    UaiKnowledgeSetDetailModel,
    UaiKnowledgeSetItemContentModel,
    UaiKnowledgeSetItemModel,
} from "./types.js";

export const UaiKnowledgeSetTypeMapper = {
    toItemModel(response: KnowledgeSetResponseModel): UaiKnowledgeSetItemModel {
        return {
            unique: response.id,
            entityType: UAI_KNOWLEDGE_SET_ENTITY_TYPE,
            name: response.name,
            description: response.description ?? null,
            icon: response.icon ?? null,
            itemCount: response.itemCount ?? 0,
        };
    },

    toDetailModel(response: KnowledgeSetDetailResponseModel): UaiKnowledgeSetDetailModel {
        return {
            unique: response.id,
            entityType: UAI_KNOWLEDGE_SET_ENTITY_TYPE,
            name: response.name,
            description: response.description ?? null,
            icon: response.icon ?? null,
            items: (response.items ?? []).map((item) => ({
                key: item.key,
                name: item.name,
                description: item.description ?? null,
            })),
        };
    },

    toItemContentModel(response: KnowledgeSetItemContentResponseModel): UaiKnowledgeSetItemContentModel {
        return {
            key: response.key,
            content: response.content,
        };
    },
};
