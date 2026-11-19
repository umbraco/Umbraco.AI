// GP-1 (AC1, AC4)
//
// STAGED SPEC: moves to the path below docs/plans/granular-permissions/specs/ with its task.
import { describe, expect, it } from "vitest";

describe("Feature: AI rights on the user group screen", () => {
    describe("Scenario: the core permission manifests are registered", () => {
        it.skip("pending: T10: registers 9 entityUserPermission manifests", () => {
            // Given the core permissions manifests
            // When  they are read
            // Then  there are 9 entityUserPermission manifests (AC1)
            expect(true).toBe(true);
        });
        it.skip("pending: T10: puts Manage connections under Configuration", () => {
            // Given the Manage connections manifest
            // When  its meta is read
            // Then  its group is Configuration (AC1)
            expect(true).toBe(true);
        });
        it.skip("pending: T10: puts View logs under Monitoring", () => {
            // Given the View logs manifest
            // When  its meta is read
            // Then  its group is Monitoring (AC1)
            expect(true).toBe(true);
        });
    });
    describe("Scenario: only core is installed", () => {
        it.skip("pending: T10: has no Manage agents manifest in core", () => {
            // Given the core permissions manifests
            // When  they are searched for Uai.Agent.Manage
            // Then  none is found (AC4)
            expect(true).toBe(true);
        });
    });
});
