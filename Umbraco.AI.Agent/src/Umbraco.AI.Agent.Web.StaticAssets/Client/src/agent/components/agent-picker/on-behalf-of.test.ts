import { describe, expect, it } from "vitest";
import { buildAutomateWorkspaceOnBehalfOf } from "./on-behalf-of.js";

describe("buildAutomateWorkspaceOnBehalfOf", () => {
    it("prefixes the workspace id with the automate-workspace kind", () => {
        expect(buildAutomateWorkspaceOnBehalfOf("abc-123")).toBe("automate-workspace:abc-123");
    });

    it("returns undefined when no workspace is set", () => {
        expect(buildAutomateWorkspaceOnBehalfOf(undefined)).toBeUndefined();
    });

    it("returns undefined for an empty workspace id", () => {
        expect(buildAutomateWorkspaceOnBehalfOf("")).toBeUndefined();
    });
});
