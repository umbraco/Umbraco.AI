import { customElement, property, css, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiAgentToolStatus, UaiAgentToolElementProps } from "../uai-agent-tool.extension.js";
import type { WeatherData } from "./show-weather.api.js";

/**
 * Weather icons mapped to conditions.
 */
const WEATHER_ICONS: Record<WeatherData["condition"], string> = {
    sunny: "icon-sunny",
    cloudy: "icon-cloud",
    rainy: "icon-rate",
    snowy: "icon-snowflake",
    stormy: "icon-flash",
};

/**
 * Custom element for rendering weather data.
 * Demonstrates Generative UI pattern for AI tool results.
 */
@customElement("uai-tool-weather")
export class UaiToolWeatherElement extends UmbLitElement implements UaiAgentToolElementProps {
    @property({ type: Object })
    args: Record<string, unknown> = {};

    @property({ type: String })
    status: UaiAgentToolStatus = "pending";

    @property({ type: Object })
    result?: WeatherData;

    @state()
    private _expanded = false;

    #getWeatherIcon(): string {
        if (!this.result?.condition) return "icon-cloud";
        return WEATHER_ICONS[this.result.condition] || "icon-cloud";
    }

    #getConditionColor(): string {
        if (!this.result?.condition) return "var(--uui-color-text)";

        const colors: Record<WeatherData["condition"], string> = {
            sunny: "#f59e0b",
            cloudy: "#6b7280",
            rainy: "#3b82f6",
            snowy: "#06b6d4",
            stormy: "#8b5cf6",
        };
        return colors[this.result.condition];
    }

    #renderLoading() {
        const location = (this.args.location as string) || "your location";
        return html`
            <div class="weather-card loading">
                <div class="weather-loading">
                    <uui-loader></uui-loader>
                    <span>Getting weather for ${location}...</span>
                </div>
            </div>
        `;
    }

    #renderWeather() {
        if (!this.result) return html``;

        return html`
            <div class="weather-card" style="--condition-color: ${this.#getConditionColor()}">
                <div class="weather-main">
                    <div class="weather-icon">
                        <uui-icon name=${this.#getWeatherIcon()}></uui-icon>
                    </div>
                    <div class="weather-info">
                        <div class="weather-location">${this.result.location}</div>
                        <div class="weather-temp">
                            ${this.result.temperature}°${this.result.unit === "celsius" ? "C" : "F"}
                        </div>
                        <div class="weather-condition">${this.result.condition}</div>
                    </div>
                </div>

                <div class="weather-description">${this.result.description}</div>

                <button class="weather-toggle" @click=${() => (this._expanded = !this._expanded)}>
                    ${this._expanded ? "Hide details" : "Show details"}
                    <uui-icon name=${this._expanded ? "icon-arrow-up" : "icon-arrow-down"}></uui-icon>
                </button>

                ${this._expanded
                    ? html`
                          <div class="weather-details">
                              <div class="weather-detail">
                                  <uui-icon name="icon-rate"></uui-icon>
                                  <span>Humidity: ${this.result.humidity}%</span>
                              </div>
                              <div class="weather-detail">
                                  <uui-icon name="icon-navigation"></uui-icon>
                                  <span>Wind: ${this.result.windSpeed} km/h</span>
                              </div>
                          </div>
                      `
                    : ""}
            </div>
        `;
    }

    #renderError() {
        return html`
            <div class="weather-card error">
                <uui-icon name="icon-alert"></uui-icon>
                <span>Failed to get weather data</span>
            </div>
        `;
    }

    override render() {
        if (this.status === "error") {
            return this.#renderError();
        }

        if (this.status === "pending" || this.status === "executing" || this.status === "streaming") {
            return this.#renderLoading();
        }

        return this.#renderWeather();
    }

    static override styles = css`
        :host {
            display: block;
        }

        .weather-card {
            background: linear-gradient(135deg, var(--uui-color-surface-alt) 0%, var(--uui-color-surface) 100%);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            padding: var(--uui-size-space-4);
            min-width: 250px;
            max-width: 320px;
        }

        .weather-card.loading {
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100px;
        }

        .weather-card.error {
            background: var(--uui-color-danger-standalone);
            color: var(--uui-color-danger-contrast);
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
        }

        .weather-loading {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: var(--uui-size-space-2);
            color: var(--uui-color-text-alt);
        }

        .weather-main {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-4);
        }

        .weather-icon {
            font-size: 48px;
            color: var(--condition-color, var(--uui-color-text));
        }

        .weather-info {
            flex: 1;
        }

        .weather-location {
            font-size: var(--uui-type-default-size);
            font-weight: 600;
            color: var(--uui-color-text);
        }

        .weather-temp {
            font-size: 28px;
            font-weight: 700;
            color: var(--uui-color-text);
            line-height: 1.2;
        }

        .weather-condition {
            font-size: var(--uui-type-small-size);
            color: var(--condition-color, var(--uui-color-text-alt));
            text-transform: capitalize;
        }

        .weather-description {
            margin-top: var(--uui-size-space-3);
            font-size: var(--uui-type-small-size);
            color: var(--uui-color-text-alt);
            line-height: 1.4;
        }

        .weather-toggle {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-1);
            margin-top: var(--uui-size-space-3);
            padding: 0;
            background: none;
            border: none;
            color: var(--uui-color-interactive);
            cursor: pointer;
            font-size: var(--uui-type-small-size);
        }

        .weather-toggle:hover {
            color: var(--uui-color-interactive-emphasis);
        }

        .weather-toggle uui-icon {
            font-size: 12px;
        }

        .weather-details {
            margin-top: var(--uui-size-space-3);
            padding-top: var(--uui-size-space-3);
            border-top: 1px solid var(--uui-color-border);
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
        }

        .weather-detail {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
            font-size: var(--uui-type-small-size);
            color: var(--uui-color-text-alt);
        }

        .weather-detail uui-icon {
            font-size: 14px;
            color: var(--uui-color-text-alt);
        }
    `;
}

export default UaiToolWeatherElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-tool-weather": UaiToolWeatherElement;
    }
}
