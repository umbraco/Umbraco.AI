// GP-18 (AC3, AC4)
//
// STAGED SPEC: moves to the path below docs/plans/granular-permissions/specs/ with its task.
import { describe, expect, it } from "vitest";

describe("Feature: user group settings list limited to editable groups", () => {
    describe("Scenario: editableUserGroupIds is set to [editors, writers]", () => {
        it.skip("pending: T47: makes the editors row editable", () => {
            // Given a list with rows for editors and translators
            // When  row editability is worked out
            // Then  the editors row is editable
            expect(true).toBe(true);
        });
        it.skip("pending: T47: makes the translators row read-only", () => {
            // Given the same list
            // When  row editability is worked out
            // Then  the translators row is read-only (AC4)
            expect(true).toBe(true);
        });
        it.skip("pending: T47: limits the Add picker to editors and writers", () => {
            // Given the same list
            // When  the Add picker's filter is applied to all groups
            // Then  only editors and writers pass (AC3)
            expect(true).toBe(true);
        });
    });
    describe("Scenario: editableUserGroupIds is unset", () => {
        it.skip("pending: T47: keeps every row editable", () => {
            // Given a list with rows for editors and translators and no editableUserGroupIds
            // When  row editability is worked out
            // Then  every row is editable
            expect(true).toBe(true);
        });
    });
});
