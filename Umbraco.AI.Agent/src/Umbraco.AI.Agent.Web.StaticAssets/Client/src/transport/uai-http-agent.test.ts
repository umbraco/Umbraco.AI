import { afterEach, describe, expect, it, vi } from "vitest";
import { AgentsService } from "../api/index.js";
import { UaiHttpAgent } from "./uai-http-agent.js";

function createAgent() {
    return new UaiHttpAgent({
        agentId: "agent-1",
        runner: (body, signal, requestOptions) =>
            AgentsService.streamAgentAGUI({
                path: { agentIdOrAlias: "agent-1" },
                body,
                signal,
                ...requestOptions,
                // Not in the SDK's Options type, but read by the generated SSE client: keep backoff short.
                ...({
                    sseSleepFn: async () => {},
                } as object),
            }) as never,
    });
}

const input = { threadId: "t", runId: "r", messages: [], tools: [], context: [], state: {}, forwardedProps: {} };

function run(agent: UaiHttpAgent) {
    return new Promise<{ error?: Error; completed: boolean }>((resolve) => {
        agent.run(input as never).subscribe({
            error: (error) => resolve({ error, completed: false }),
            complete: () => resolve({ completed: true }),
        });
    });
}

describe("UaiHttpAgent stream refusal", () => {
    afterEach(() => vi.unstubAllGlobals());

    it("does not retry a 403 and ends the run with the ProblemDetails detail", async () => {
        const agent = createAgent();
        let calls = 0;
        const fetchMock = vi.fn(async () => {
            // Safety valve so the pre-fix endless retry fails the assertion instead of hanging.
            if (++calls > 5) agent.abortRun();
            return new Response(
                JSON.stringify({
                    title: "Agent not available",
                    detail: "The specified agent is not available to you.",
                }),
                { status: 403, headers: { "Content-Type": "application/problem+json" } },
            );
        });
        vi.stubGlobal("fetch", fetchMock);

        const result = await run(agent);
        expect(fetchMock).toHaveBeenCalledTimes(1);
        expect(result.error?.message).toBe("The specified agent is not available to you.");
    });

    it("retries a 5xx instead of ending the run", async () => {
        const fetchMock = vi
            .fn()
            .mockResolvedValueOnce(new Response("boom", { status: 503 }))
            .mockResolvedValueOnce(
                new Response('data: {"type":"RUN_FINISHED"}\n\n', {
                    status: 200,
                    headers: { "Content-Type": "text/event-stream" },
                }),
            );
        vi.stubGlobal("fetch", fetchMock);

        const result = await run(createAgent());

        expect(fetchMock).toHaveBeenCalledTimes(2);
        expect(result.completed).toBe(true);
    });
});
