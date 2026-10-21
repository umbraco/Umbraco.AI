// EX-7 — Extract from TypeScript (AC2, AC5)
//
// Public shape (mirrors UaiDecisionController):
//   controller.extract(request: UaiExtractionRequest, options?: { profileIdOrAlias?, signal? })
//     → Promise<{ data?: UaiExtractionResult; error?: unknown }>
//   UaiExtractionRequest { state: string; referenceDate?: string; fields: UaiExtractionField[] }
//   UaiExtractionField   { kind: "text"|"number"|"date"|"time"|"datetime"|"duration"|"list"; id; instructions; ... }
//   UaiExtractionResult  { answers: Record<string, UaiExtractionAnswer>; modelId?; usage? }
// The repository receives one request object carrying state, fields, referenceDate,
// profileIdOrAlias and signal, like UaiDecisionRepository.ask.
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

// Stub at the real boundary: the repository the public controller delegates to.
const extract = vi.fn();
vi.mock("../repository/extraction.repository.js", () => ({
    UaiExtractionRepository: class {
        extract = extract;
    },
}));

import { UaiExtractionController } from "./extraction.controller.js";
import type { UaiExtractionRequest, UaiExtractionResult } from "../types.js";

function createHost(): UmbControllerHost {
    const element = document.createElement("div");
    document.body.appendChild(element);
    const host = new UmbElementControllerHost(element);
    host.hostConnected();
    return host;
}

const orderRequest: UaiExtractionRequest = {
    state: "Order A-88412. I paid 701,10 kr. The mug and teapot were broken.",
    fields: [
        { kind: "number", id: "amount", instructions: "Amount paid", decimals: 2 },
        { kind: "list", id: "damaged", instructions: "Damaged products", itemType: "text" },
    ],
};

describe("Feature: UaiExtractionController", () => {
    describe("Scenario: a number and a list field are extracted", () => {
        let result: { data?: UaiExtractionResult; error?: unknown };

        beforeEach(async () => {
            extract.mockReset();
            extract.mockResolvedValue({
                data: {
                    answers: {
                        amount: { kind: "number", value: 701.1, isFound: true },
                        damaged: { kind: "list", values: ["mug", "teapot"], spans: [], isFound: true },
                    },
                    modelId: "@cf/cloudflare/clairic",
                },
            });
            result = await new UaiExtractionController(createHost()).extract(orderRequest);
        });

        it("returns the number answer's value as a number", () => {
            expect(result.data?.answers.amount).toMatchObject({ kind: "number", value: 701.1 });
        });

        it("returns the list answer's values as an array", () => {
            expect(result.data?.answers.damaged).toMatchObject({ kind: "list", values: ["mug", "teapot"] });
        });

        it("returns the model id", () => {
            expect(result.data?.modelId).toBe("@cf/cloudflare/clairic");
        });
    });

    describe("Scenario: options are passed alongside the request", () => {
        beforeEach(async () => {
            extract.mockReset();
            extract.mockResolvedValue({ data: { answers: {} } });
            await new UaiExtractionController(createHost()).extract(
                { ...orderRequest, referenceDate: "2026-10-20" },
                { profileIdOrAlias: "cloudflare-clairic", signal: new AbortController().signal },
            );
        });

        it("forwards the state to the repository", () => {
            expect(extract.mock.calls[0][0].state).toBe(orderRequest.state);
        });

        it("forwards the fields to the repository", () => {
            expect(extract.mock.calls[0][0].fields).toEqual(orderRequest.fields);
        });

        it("forwards the reference date to the repository", () => {
            expect(extract.mock.calls[0][0].referenceDate).toBe("2026-10-20");
        });

        it("forwards the profile id or alias to the repository", () => {
            expect(extract.mock.calls[0][0].profileIdOrAlias).toBe("cloudflare-clairic");
        });

        it("forwards the abort signal to the repository", () => {
            expect(extract.mock.calls[0][0].signal).toBeInstanceOf(AbortSignal);
        });
    });

    describe("Sad path", () => {
        describe("Scenario: the server returns 400", () => {
            let result: { data?: UaiExtractionResult; error?: unknown };

            beforeEach(async () => {
                extract.mockReset();
                extract.mockResolvedValue({ error: { status: 400, title: "Duplicate field id 'orderId'." } });
                result = await new UaiExtractionController(createHost()).extract(orderRequest);
            });

            it("resolves with an error and no answers instead of throwing", () => {
                expect(result).toEqual({ error: { status: 400, title: "Duplicate field id 'orderId'." } });
            });
        });

        describe("Scenario: the server returns 404 because Extraction is disabled", () => {
            let result: { data?: UaiExtractionResult; error?: unknown };

            beforeEach(async () => {
                extract.mockReset();
                extract.mockResolvedValue({ error: { status: 404 } });
                result = await new UaiExtractionController(createHost()).extract(orderRequest);
            });

            it("resolves with an error and no answers instead of throwing", () => {
                expect(result).toEqual({ error: { status: 404 } });
            });
        });
    });
});
