// GP-18 (AC3, AC4)
import { describe, expect, it } from "vitest";
import { isUserGroupEditable } from "./user-group-settings-list.utils.js";

describe("isUserGroupEditable", () => {
    it("treats every group as editable when no editable groups are set", () => {
        expect(isUserGroupEditable("a", undefined)).toBe(true);
    });

    it("treats a listed group as editable", () => {
        expect(isUserGroupEditable("a", ["a", "b"])).toBe(true);
    });

    it("treats an unlisted group as not editable", () => {
        expect(isUserGroupEditable("c", ["a", "b"])).toBe(false);
    });

    it("treats no group as editable when the list is empty", () => {
        expect(isUserGroupEditable("a", [])).toBe(false);
    });

    it("compares ids ignoring case", () => {
        expect(isUserGroupEditable("ABC", ["abc"])).toBe(true);
    });
});
