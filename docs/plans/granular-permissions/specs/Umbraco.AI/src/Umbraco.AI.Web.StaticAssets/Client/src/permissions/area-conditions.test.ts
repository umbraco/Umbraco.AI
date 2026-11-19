// GP-5 (AC1, AC3-AC6), GP-4 (AC6)
//
// STAGED SPEC: moves to the path below docs/plans/granular-permissions/specs/ with its task.
import { describe, expect, it } from "vitest";

describe("Feature: areas need their right in the backoffice", () => {
    describe("Scenario: each area's manifests carry its fallback condition", () => {
        it.skip("pending: T11: conditions the Connections menu item on Uai.Connection.Manage", () => {
            // Given the connection menu item manifest
            // When  its conditions are read
            // Then  it has Umb.Condition.UserPermission.Fallback with Uai.Connection.Manage (AC3)
            expect(true).toBe(true);
        });
        it.skip("pending: T11: conditions connection create and delete entity actions", () => {
            // Given the connection entity action manifests (each)
            // When  their conditions are read
            // Then  each has the fallback condition (AC4)
            expect(true).toBe(true);
        });
        it.skip("pending: T11: conditions the connections collection", () => {
            // Given the connections collection manifest
            // When  its conditions are read
            // Then  it has the fallback condition (AC5)
            expect(true).toBe(true);
        });
        it.skip("pending: T11: conditions the connection workspace", () => {
            // Given the connection workspace manifest
            // When  its conditions are read
            // Then  it has the fallback condition (AC6)
            expect(true).toBe(true);
        });
        it.skip("pending: T11: conditions the Contexts save action on Uai.Context.Manage", () => {
            // Given the context workspace save action manifest
            // When  its conditions are read
            // Then  it has the fallback condition with Uai.Context.Manage (AC1)
            expect(true).toBe(true);
        });
        it.skip("pending: T12: conditions the Audit log menu item on Uai.AuditLog.View", () => {
            // Given the audit log menu item manifest
            // When  its conditions are read
            // Then  it has the fallback condition with Uai.AuditLog.View (GP-4 AC6)
            expect(true).toBe(true);
        });
    });
});
