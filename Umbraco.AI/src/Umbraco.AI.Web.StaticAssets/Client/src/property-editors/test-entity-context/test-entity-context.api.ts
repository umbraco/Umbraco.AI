import { client } from "../../api/client.gen.js";
import type { TestEntityTypeModel, TestEntitySubTypeModel } from "./types.js";

/**
 * Fetches registered entity types from the API.
 * TODO: Replace with generated SDK after OpenAPI client regeneration.
 */
export async function fetchEntityTypes(): Promise<TestEntityTypeModel[]> {
    const { data } = await client.get<TestEntityTypeModel[]>({
        url: "/umbraco/ai/management/api/v1/tests/entity-types",
    });
    return data ?? [];
}

/**
 * Fetches sub-types for a specific entity type from the API.
 * TODO: Replace with generated SDK after OpenAPI client regeneration.
 */
export async function fetchEntitySubTypes(entityType: string): Promise<TestEntitySubTypeModel[]> {
    const { data } = await client.get<TestEntitySubTypeModel[]>({
        url: `/umbraco/ai/management/api/v1/tests/entity-types/${encodeURIComponent(entityType)}/sub-types`,
    });
    return data ?? [];
}
