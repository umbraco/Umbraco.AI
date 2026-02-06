/**
 * Event emitted when an item is selected.
 * @public
 */
export class UaiSelectedEvent extends Event {
    public static readonly TYPE = "selected";
    public unique: string | null;
    public item: any;
    public constructor(unique: string | null, item?: any, args?: EventInit) {
        // mimics the native change event
        super(UaiSelectedEvent.TYPE, { bubbles: true, composed: false, cancelable: false, ...args });
        this.unique = unique;
        this.item = item;
    }
}
