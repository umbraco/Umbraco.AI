// GP-7 (AC6), GP-8 (AC3, AC5, AC6)
import { describe, expect, it } from "vitest";
import {
    addUserGroupIds,
    canAddUserGroups,
    canRemoveUserGroup,
    canUseRestrictedItem,
    removeUserGroupId,
} from "./user-group-access.utils.js";

describe("Feature: who can change an item's access", () => {
    it("an item with no groups can be used by anyone", () => {
        expect(canUseRestrictedItem([], ["a"], false)).toBe(true);
    });
    it("a user in one of the groups can use the item", () => {
        expect(canUseRestrictedItem(["A", "b"], ["a"], false)).toBe(true);
    });
    it("a user in none of the groups can't use the item", () => {
        expect(canUseRestrictedItem(["b"], ["a"], false)).toBe(false);
    });
    it("an item limited to a deleted group can't be used by a non-admin", () => {
        expect(canUseRestrictedItem(["deleted"], ["a"], false)).toBe(false);
    });
    it("an admin can always use the item", () => {
        expect(canUseRestrictedItem(["b"], [], true)).toBe(true);
    });
});

describe("Feature: who can remove a group from the list", () => {
    it("an admin can remove any group, even the last one", () => {
        expect(canRemoveUserGroup("x", ["x"], [], true)).toBe(true);
    });
    it("a non-admin can remove one of their own groups when others remain", () => {
        expect(canRemoveUserGroup("A", ["a", "b"], ["a"], false)).toBe(true);
    });
    it("a non-admin can't remove a group they aren't in", () => {
        expect(canRemoveUserGroup("b", ["a", "b"], ["a"], false)).toBe(false);
    });
    it("a non-admin can't remove the last group", () => {
        expect(canRemoveUserGroup("a", ["a"], ["a"], false)).toBe(false);
    });
});

describe("Feature: editing the list", () => {
    it("adding skips groups already in the list", () => {
        expect(addUserGroupIds(["a"], ["A", "b"])).toEqual(["a", "b"]);
    });
    it("removing drops only that group", () => {
        expect(removeUserGroupId(["a", "b"], "A")).toEqual(["b"]);
    });
});

describe("Feature: whether there is any group to choose", () => {
    it("an admin (no restriction) can add a group", () => {
        expect(canAddUserGroups(undefined)).toBe(true);
    });
    it("a user in some groups can add one", () => {
        expect(canAddUserGroups(["a"])).toBe(true);
    });
    it("a user in no group has nothing to add", () => {
        expect(canAddUserGroups([])).toBe(false);
    });
});
