
import { css, html, customElement, query, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import type { UmbDropdownElement } from '@umbraco-cms/backoffice/components';

export type UaiPollingInterval = 0 | 2000 | 5000 | 10000 | 20000 | 30000;

export interface UaiPollingConfig {
    enabled: boolean;
    interval: UaiPollingInterval;
}

@customElement('uai-polling-button')
export class UaiPollingButtonElement extends UmbLitElement {
    
    @query('#polling-rate-dropdown')
    private _dropdownElement?: UmbDropdownElement;

    @state()
    private _pollingConfig: UaiPollingConfig = { enabled: false, interval: 2000 };

    #pollingIntervals: UaiPollingInterval[] = [ 2000, 5000, 10000, 20000, 30000 ];
    #pollingIntervalId: number | null = null;
    
    #togglePolling() {
        if (!this._pollingConfig.enabled) {
            // Start polling with the current interval or default to 5000ms
            const interval = this._pollingConfig.interval || this.#pollingIntervals[0];
            this.#setPolingInterval(interval);
        } else {
            // Stop polling
            this.#setPolingInterval(0);
        }
    }

    #setPolingInterval = (interval: UaiPollingInterval) => {
        
        this._pollingConfig = { 
            interval: interval > 0 ? interval : this._pollingConfig.interval, 
            enabled:  interval > 0
        }; // Trigger reactivity
        
        this.dispatchEvent(
            new CustomEvent("change", {
                detail: this._pollingConfig,
                bubbles: true,
                composed: true,
            })
        );
        
        if (this._pollingConfig.interval === 0 || !this._pollingConfig.enabled) {
            clearInterval(this.#pollingIntervalId!);
        } else {
            this.#pollingIntervalId = window.setInterval(() => {
                this.dispatchEvent(
                    new CustomEvent("interval", {
                        detail: null,
                        bubbles: true,
                        composed: true,
                    })
                );
            }, this._pollingConfig.interval);
        }
        
        this.#closePollingPopover();
    };

    #closePollingPopover() {
        if (this._dropdownElement) {
            this._dropdownElement.open = false;
        }
    }

    #getPollingLabel(): string {
        const seconds = this._pollingConfig.interval / 1000;
        return this.localize.term('uaiComponents_pollingButtonPollingActive', seconds);
    }

    #getIntervalLabel(interval: UaiPollingInterval): string {
        const seconds = interval / 1000;
        return this.localize.term('uaiComponents_pollingButtonPollingInterval', seconds);
    }

    override render() {
        return html`
			<uui-button-group>
				<uui-button label=${this.localize.term('uaiComponents_pollingButtonTogglePolling')} @click=${this.#togglePolling}>
					${this._pollingConfig.enabled
                    ? html`<uui-icon name="icon-axis-rotation" id="polling-enabled-icon"></uui-icon>${this.#getPollingLabel()}`
                    : html`<umb-localize key="uaiComponents_pollingButtonPolling">Polling</umb-localize>`}
				</uui-button>
				<umb-dropdown
					id="polling-rate-dropdown"
					compact
					label=${this.localize.term('uaiComponents_pollingButtonChoosePollingInterval')}>
					${this.#pollingIntervals.map((interval: UaiPollingInterval) =>
                        html`<uui-menu-item
								label=${this.#getIntervalLabel(interval)}
								@click-label=${() => this.#setPolingInterval(interval)}></uui-menu-item>`,
                    )}
				</umb-dropdown>
			</uui-button-group>
		`;
    }

    static override styles = [
        css`
			#polling-enabled-icon {
				margin-right: var(--uui-size-space-3);
				margin-bottom: 1px;
				-webkit-animation: rotate-center 0.8s ease-in-out infinite both;
				animation: rotate-center 0.8s ease-in-out infinite both;
			}

			@-webkit-keyframes rotate-center {
				0% {
					-webkit-transform: rotate(0);
					transform: rotate(0);
				}
				100% {
					-webkit-transform: rotate(360deg);
					transform: rotate(360deg);
				}
			}
			@keyframes rotate-center {
				0% {
					-webkit-transform: rotate(0);
					transform: rotate(0);
				}
				100% {
					-webkit-transform: rotate(360deg);
					transform: rotate(360deg);
				}
			}
		`,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        'uai-polling-button': UaiPollingButtonElement;
    }
}
