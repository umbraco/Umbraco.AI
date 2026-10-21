// EX-7 — Extract from TypeScript (AC2-AC5)
//
// Stubs the generated SDK. ASSUMPTION: T10's regenerated client exposes `ExtractionService.extract`;
// rename here if the generator names it differently (as the Decision data source spec notes).
import { beforeEach, describe, expect, it, vi } from "vitest";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

// Stub at the real boundary: the generated OpenAPI SDK call.
const sdkExtract = vi.fn();
vi.mock("../../api/sdk.gen.js", () => ({
    ExtractionService: { extract: (...args: unknown[]) => sdkExtract(...args) },
}));
vi.mock("@umbraco-cms/backoffice/resources", () => ({
    tryExecute: async (_host: unknown, promise: Promise<unknown>) => promise,
}));

import { UaiExtractionServerDataSource } from "./extraction.server.data-source.js";

function createHost(): UmbControllerHost {
    const element = document.createElement("div");
    document.body.appendChild(element);
    const host = new UmbElementControllerHost(element);
    host.hostConnected();
    return host;
}

type ExtractResult = Awaited<ReturnType<UaiExtractionServerDataSource["extract"]>>;

const state = "Order A-88412 was delivered on 3 October. I paid 701,10 kr. The mug and teapot were broken.";

describe("Feature: extraction server data source", () => {
    describe("Scenario: a request with every field kind is sent", () => {
        beforeEach(async () => {
            sdkExtract.mockReset();
            sdkExtract.mockResolvedValue({ data: { answers: {} } });
            await new UaiExtractionServerDataSource(createHost()).extract({
                state,
                referenceDate: "2026-10-20",
                profileIdOrAlias: "cloudflare-clairic",
                fields: [
                    { kind: "text", id: "orderId", instructions: "The order number" },
                    { kind: "number", id: "amount", instructions: "Amount paid", decimals: 2, integer: false, unit: "DKK" },
                    { kind: "date", id: "deliveredOn", instructions: "Delivery date" },
                    { kind: "time", id: "deliveredAt", instructions: "Delivery time" },
                    { kind: "datetime", id: "orderedAt", instructions: "Order placed" },
                    { kind: "duration", id: "returnWindow", instructions: "Return window", unit: "days" },
                    { kind: "list", id: "damaged", instructions: "Damaged products", itemType: "text" },
                ],
            });
        });

        it.skip("sends each field's kind as the API's $type", () => {
            expect(sdkExtract.mock.calls[0][0].body.fields.map((f: { $type: string }) => f.$type)).toEqual([
                "text",
                "number",
                "date",
                "time",
                "datetime",
                "duration",
                "list",
            ]);
        });

        it.skip("sends a datetime field with $type datetime", () => {
            expect(sdkExtract.mock.calls[0][0].body.fields[4].$type).toBe("datetime");
        });

        it.skip("doesn't send kind on the wire", () => {
            expect(sdkExtract.mock.calls[0][0].body.fields[0]).not.toHaveProperty("kind");
        });

        it.skip("sends the field id", () => {
            expect(sdkExtract.mock.calls[0][0].body.fields[0].id).toBe("orderId");
        });

        it.skip("sends the number field's options", () => {
            expect(sdkExtract.mock.calls[0][0].body.fields[1]).toMatchObject({ decimals: 2, integer: false, unit: "DKK" });
        });

        it.skip("sends the list field's item type", () => {
            expect(sdkExtract.mock.calls[0][0].body.fields[6].itemType).toBe("text");
        });

        it.skip("sends the state at the top level of the body", () => {
            expect(sdkExtract.mock.calls[0][0].body.state).toBe(state);
        });

        it.skip("sends the reference date", () => {
            expect(sdkExtract.mock.calls[0][0].body.referenceDate).toBe("2026-10-20");
        });

        it.skip("forwards the profile id or alias", () => {
            expect(sdkExtract.mock.calls[0][0].body.profileIdOrAlias).toBe("cloudflare-clairic");
        });
    });

    describe("Scenario: a response with a number and a list answer comes back", () => {
        let result: ExtractResult;

        beforeEach(async () => {
            sdkExtract.mockReset();
            sdkExtract.mockResolvedValue({
                data: {
                    answers: {
                        amount: { $type: "number", value: 701.1, confidence: 0.9999, nullProbability: 0.00002, isFound: true },
                        damaged: {
                            $type: "list",
                            values: ["mug", "teapot"],
                            spans: [
                                { start: 64, end: 67 },
                                { start: 72, end: 78 },
                            ],
                            confidence: 0.99,
                            isFound: true,
                        },
                    },
                    modelId: "@cf/cloudflare/clairic",
                    usage: { inputTokenCount: 612, outputTokenCount: 0, totalTokenCount: 612 },
                },
            });
            result = await new UaiExtractionServerDataSource(createHost()).extract({
                state,
                fields: [
                    { kind: "number", id: "amount", instructions: "Amount paid" },
                    { kind: "list", id: "damaged", instructions: "Damaged products" },
                ],
            });
        });

        it.skip("maps the API's $type back to kind", () => {
            expect(result.data?.answers.amount?.kind).toBe("number");
        });

        it.skip("gives the number answer a numeric value", () => {
            expect(typeof (result.data?.answers.amount as { value?: unknown } | undefined)?.value).toBe("number");
        });

        it.skip("gives the list answer a values array", () => {
            expect((result.data?.answers.damaged as { values?: unknown } | undefined)?.values).toEqual(["mug", "teapot"]);
        });

        it.skip("keeps the list answer's spans in the same order as its values", () => {
            expect((result.data?.answers.damaged as { spans?: unknown } | undefined)?.spans).toEqual([
                { start: 64, end: 67 },
                { start: 72, end: 78 },
            ]);
        });

        it.skip("keeps the confidence", () => {
            expect(result.data?.answers.amount).toMatchObject({ confidence: 0.9999 });
        });

        it.skip("doesn't leak the generated model's $type onto the answer", () => {
            expect(result.data?.answers.amount).not.toHaveProperty("$type");
        });

        it.skip("keeps the model id", () => {
            expect(result.data?.modelId).toBe("@cf/cloudflare/clairic");
        });
    });

    describe("Scenario: answer keys use capitals", () => {
        let result: ExtractResult;

        beforeEach(async () => {
            sdkExtract.mockReset();
            sdkExtract.mockResolvedValue({
                data: { answers: { OrderID: { $type: "text", value: "A-88412", isFound: true } } },
            });
            result = await new UaiExtractionServerDataSource(createHost()).extract({
                state,
                fields: [{ kind: "text", id: "OrderID", instructions: "The order number" }],
            });
        });

        it.skip("keeps the answer key exactly as sent", () => {
            expect(Object.keys(result.data?.answers ?? {})).toEqual(["OrderID"]);
        });
    });

    describe("Scenario: a value isn't in the content", () => {
        let result: ExtractResult;

        beforeEach(async () => {
            sdkExtract.mockReset();
            sdkExtract.mockResolvedValue({
                data: { answers: { invoiceNo: { $type: "text", value: null, isFound: false } } },
            });
            result = await new UaiExtractionServerDataSource(createHost()).extract({
                state,
                fields: [{ kind: "text", id: "invoiceNo", instructions: "The invoice number" }],
            });
        });

        it.skip("keeps the null value", () => {
            expect((result.data?.answers.invoiceNo as { value?: unknown } | undefined)?.value).toBeNull();
        });
    });

    describe("Sad path", () => {
        describe("Scenario: the server returns 400", () => {
            let result: ExtractResult;

            beforeEach(async () => {
                sdkExtract.mockReset();
                sdkExtract.mockResolvedValue({ error: { status: 400 } });
                result = await new UaiExtractionServerDataSource(createHost()).extract({
                    state,
                    fields: [{ kind: "text", id: "orderId", instructions: "The order number" }],
                });
            });

            it.skip("returns the error and no answers", () => {
                expect(result).toEqual({ error: { status: 400 } });
            });
        });

        describe("Scenario: an answer comes back with an unrecognized $type", () => {
            let result: ExtractResult;

            beforeEach(async () => {
                sdkExtract.mockReset();
                sdkExtract.mockResolvedValue({
                    data: { answers: { guess: { $type: "estimate", value: 3, isFound: true } } },
                });
                result = await new UaiExtractionServerDataSource(createHost()).extract({
                    state,
                    fields: [{ kind: "number", id: "guess", instructions: "A guess" }],
                });
            });

            it.skip("returns an explicit error instead of silently dropping the answer", () => {
                expect(result).toEqual({ error: expect.any(Error) });
            });
        });
    });
});
