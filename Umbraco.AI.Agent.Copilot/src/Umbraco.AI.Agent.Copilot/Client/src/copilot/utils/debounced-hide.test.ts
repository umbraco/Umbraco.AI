import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { Subject } from "@umbraco-cms/backoffice/external/rxjs";
import { debouncedHide } from "./debounced-hide.js";

const DELAY = 200;

describe("debouncedHide", () => {
    beforeEach(() => vi.useFakeTimers());
    afterEach(() => vi.useRealTimers());

    /** Subscribe and collect every emission into an array for assertions. */
    function collect(source: Subject<boolean>) {
        const values: boolean[] = [];
        const sub = debouncedHide(source, DELAY).subscribe((v) => values.push(v));
        return { values, sub };
    }

    it("passes the rising edge through immediately (no timer wait)", () => {
        const source = new Subject<boolean>();
        const { values } = collect(source);

        source.next(true);

        expect(values).toEqual([true]); // emitted before any timer advance
    });

    it("holds the falling edge until the delay elapses", () => {
        const source = new Subject<boolean>();
        const { values } = collect(source);

        source.next(true);
        source.next(false);
        expect(values).toEqual([true]); // still true right after the false

        vi.advanceTimersByTime(DELAY - 1);
        expect(values).toEqual([true]); // not yet

        vi.advanceTimersByTime(1);
        expect(values).toEqual([true, false]); // flips at the boundary
    });

    it("cancels a pending hide when support returns within the window", () => {
        const source = new Subject<boolean>();
        const { values } = collect(source);

        source.next(true);
        source.next(false); // starts the hide timer
        vi.advanceTimersByTime(DELAY - 50);
        source.next(true); // returns before the window closes

        vi.advanceTimersByTime(DELAY); // let any stale timer fire
        expect(values).toEqual([true]); // never flipped to false
    });

    it("does not emit duplicate trues across a cancelled hide", () => {
        const source = new Subject<boolean>();
        const { values } = collect(source);

        source.next(true);
        source.next(false);
        source.next(true); // cancels hide; value is already true

        vi.advanceTimersByTime(DELAY);
        expect(values).toEqual([true]); // deduped, not [true, true]
    });

    it("emits a real hide after a genuine leave following a hop", () => {
        const source = new Subject<boolean>();
        const { values } = collect(source);

        source.next(true);
        source.next(false); // hop out
        vi.advanceTimersByTime(50);
        source.next(true); // hop into the next supported workspace
        source.next(false); // then genuinely leave
        vi.advanceTimersByTime(DELAY);

        expect(values).toEqual([true, false]);
    });

    it("clears the pending timer on unsubscribe (no late emission)", () => {
        const source = new Subject<boolean>();
        const { values, sub } = collect(source);

        source.next(true);
        source.next(false); // schedules the hide
        sub.unsubscribe();

        vi.advanceTimersByTime(DELAY * 2);
        expect(values).toEqual([true]); // unsubscribed before the hide could fire
    });

    it("shares one subscription/timer across consumers (shareReplay)", () => {
        const source = new Subject<boolean>();
        const shared$ = debouncedHide(source, DELAY);

        const a: boolean[] = [];
        const subA = shared$.subscribe((v) => a.push(v));
        source.next(true);

        // Late subscriber immediately receives the current value via shareReplay(1).
        const b: boolean[] = [];
        const subB = shared$.subscribe((v) => b.push(v));

        expect(a).toEqual([true]);
        expect(b).toEqual([true]);

        source.next(false);
        vi.advanceTimersByTime(DELAY);
        expect(a).toEqual([true, false]);
        expect(b).toEqual([true, false]);

        subA.unsubscribe();
        subB.unsubscribe();
    });
});
