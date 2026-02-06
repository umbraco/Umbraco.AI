import { noChange } from "@umbraco-cms/backoffice/external/lit";
import { AsyncDirective } from "lit/async-directive.js";
import type { Observable, Subscription } from "rxjs";

export abstract class UaiObserveDirectiveBase<T> extends AsyncDirective {
    observable: Observable<T | undefined> | undefined;
    subscription: Subscription | undefined;

    formatValue: (val: T) => unknown = (v) => v;

    // When the observable changes, unsubscribe to the old one and
    // subscribe to the new one
    renderObservable(observable: Observable<T | undefined>) {
        if (this.observable !== observable) {
            this.subscription?.unsubscribe();
            this.observable = observable;
            if (this.isConnected) {
                this.subscribe(observable);
            }
        }
        return noChange;
    }

    // Subscribes to the observable, calling the directive's asynchronous
    // setValue API each time the value changes
    subscribe(observable: Observable<T | undefined>) {
        this.subscription = observable.subscribe((v: T | undefined) => {
            if (v) {
                this.setValue(this.formatValue(v));
            }
        });
    }

    // When the directive is disconnected from the DOM, unsubscribe to ensure
    // the directive instance can be garbage collected
    disconnected() {
        this.subscription?.unsubscribe();
    }

    // If the subtree the directive is in was disconnected and subsequently
    // re-connected, re-subscribe to make the directive operable again
    reconnected() {
        if (this.observable) {
            this.subscribe(this.observable);
        }
    }
}
