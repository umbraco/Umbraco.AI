// GP-9 (AC2): the AI section's agent list asks for restricted agents too
import { beforeEach, describe, expect, it, vi } from "vitest";

const getAllAgents = vi.hoisted(() => vi.fn());

vi.mock("../../../api/index.js", () => ({ AgentsService: { getAllAgents } }));
vi.mock("@umbraco-cms/backoffice/resources", () => ({
    tryExecute: async (_host: unknown, promise: Promise<unknown>) => ({ data: await promise }),
}));
vi.mock("../../type-mapper.js", () => ({ UaiAgentTypeMapper: { toItemModel: (item: unknown) => item } }));

import { UaiAgentCollectionServerDataSource } from "./agent-collection.server.data-source.js";

describe("Feature: the agent collection data source", () => {
    beforeEach(() => {
        getAllAgents.mockReset();
        getAllAgents.mockResolvedValue({ items: [], total: 0 });
    });

    it("asks for restricted agents", async () => {
        await new UaiAgentCollectionServerDataSource({} as never).getCollection({ skip: 0, take: 10 });
        expect(getAllAgents.mock.calls[0][0].query.includeRestricted).toBe(true);
    });

    it("still passes the paging and filter through", async () => {
        await new UaiAgentCollectionServerDataSource({} as never).getCollection({ filter: "x", skip: 5, take: 10 });
        expect(getAllAgents.mock.calls[0][0].query).toMatchObject({ filter: "x", skip: 5, take: 10 });
    });
});
