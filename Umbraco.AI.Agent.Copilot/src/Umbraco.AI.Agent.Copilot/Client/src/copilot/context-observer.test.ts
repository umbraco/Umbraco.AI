import { describe, it, expect, beforeEach, afterEach } from "vitest";
import type { Subscription } from "@umbraco-cms/backoffice/external/rxjs";
import { createSectionObservable } from "./context-observer.js";

/**
 * The observable shares a single history-patching source across subscribers, ref-counted at the
 * module level. Every test must fully unsubscribe (see afterEach) so the ref count returns to zero
 * and the history patch is restored before the next test.
 */
describe("createSectionObservable", () => {
    let subs: Subscription[] = [];

    /** Subscribe, track the subscription for teardown, and collect emissions. */
    function collect() {
        const values: (string | null)[] = [];
        const sub = createSectionObservable().subscribe((v) => values.push(v));
        subs.push(sub);
        return values;
    }

    beforeEach(() => {
        // Deterministic starting URL, set out-of-band (before any subscription patches history).
        window.history.replaceState(null, "", "/section/content");
    });

    afterEach(() => {
        subs.forEach((s) => s.unsubscribe());
        subs = [];
    });

    it("emits the current section on subscribe", () => {
        const values = collect();
        expect(values).toEqual(["content"]);
    });

    it("emits null when the path has no section segment", () => {
        window.history.replaceState(null, "", "/umbraco/login");
        const values = collect();
        expect(values).toEqual([null]);
    });

    it("emits the new section on pushState navigation", () => {
        const values = collect();
        history.pushState(null, "", "/section/media/workspace/media/edit/123");
        expect(values).toEqual(["content", "media"]);
    });

    it("emits on replaceState navigation", () => {
        const values = collect();
        history.replaceState(null, "", "/section/settings");
        expect(values).toEqual(["content", "settings"]);
    });

    it("dedupes when navigation stays within the same section", () => {
        const values = collect();
        history.pushState(null, "", "/section/content/workspace/document/edit/1");
        history.pushState(null, "", "/section/content/workspace/document/edit/2");
        expect(values).toEqual(["content"]); // distinctUntilChanged collapses same-section moves
    });

    it("re-reads the section on browser popstate (back/forward)", () => {
        const values = collect();
        // Change the URL out-of-band (not via the patched history methods), then fire popstate as a
        // real back/forward would.
        window.location.href = "https://localhost/section/media";
        window.dispatchEvent(new PopStateEvent("popstate"));
        expect(values).toEqual(["content", "media"]);
    });

    it("patches history.pushState once regardless of subscriber count", () => {
        const original = history.pushState;

        collect();
        const afterFirst = history.pushState;
        expect(afterFirst).not.toBe(original); // patched on first subscribe

        collect();
        expect(history.pushState).toBe(afterFirst); // second subscriber does not re-patch
    });

    it("restores history.pushState/replaceState when the last subscriber leaves", () => {
        const originalPush = history.pushState;
        const originalReplace = history.replaceState;

        const sub1 = createSectionObservable().subscribe();
        const sub2 = createSectionObservable().subscribe();
        expect(history.pushState).not.toBe(originalPush); // patched while subscribed

        sub1.unsubscribe();
        expect(history.pushState).not.toBe(originalPush); // still one subscriber -> still patched

        sub2.unsubscribe();
        expect(history.pushState).toBe(originalPush); // ref count hit zero -> restored
        expect(history.replaceState).toBe(originalReplace);
    });

    it("re-patches after a full teardown and re-subscribe", () => {
        const originalPush = history.pushState;

        const sub1 = createSectionObservable().subscribe();
        sub1.unsubscribe();
        expect(history.pushState).toBe(originalPush); // torn down

        const values = collect(); // fresh subscribe re-patches and re-reads the URL
        expect(history.pushState).not.toBe(originalPush);
        expect(values).toEqual(["content"]);
    });
});
