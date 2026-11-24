// GP-12 (AC2): the AI section's prompt list asks for restricted prompts too
import { beforeEach, describe, expect, it, vi } from "vitest";

const getAllPrompts = vi.hoisted(() => vi.fn());

vi.mock("../../../api/index.js", () => ({ PromptsService: { getAllPrompts } }));
vi.mock("@umbraco-cms/backoffice/resources", () => ({
    tryExecute: async (_host: unknown, promise: Promise<unknown>) => ({ data: await promise }),
}));
vi.mock("../../type-mapper.js", () => ({ UaiPromptTypeMapper: { toItemModel: (item: unknown) => item } }));

import { UaiPromptCollectionServerDataSource } from "./prompt-collection.server.data-source.js";

describe("Feature: the prompt collection data source", () => {
    beforeEach(() => {
        getAllPrompts.mockReset();
        getAllPrompts.mockResolvedValue({ items: [], total: 0 });
    });

    it("asks for restricted prompts", async () => {
        await new UaiPromptCollectionServerDataSource({} as never).getCollection({ skip: 0, take: 10 });
        expect(getAllPrompts.mock.calls[0][0].query.includeRestricted).toBe(true);
    });

    it("still passes the paging and filter through", async () => {
        await new UaiPromptCollectionServerDataSource({} as never).getCollection({ filter: "x", skip: 5, take: 10 });
        expect(getAllPrompts.mock.calls[0][0].query).toMatchObject({ filter: "x", skip: 5, take: 10 });
    });
});
