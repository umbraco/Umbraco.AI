import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UaiAgentToolApi } from "../uai-agent-tool.extension.js";

/**
 * Weather data returned by the tool.
 */
export interface WeatherData {
  location: string;
  temperature: number;
  unit: "celsius" | "fahrenheit";
  condition: "sunny" | "cloudy" | "rainy" | "snowy" | "stormy";
  humidity: number;
  windSpeed: number;
  description: string;
}

/**
 * Example frontend tool: Show Weather
 * Returns weather data for a location (mock data for demo).
 */
export default class ShowWeatherApi extends UmbControllerBase implements UaiAgentToolApi {
  async execute(args: Record<string, unknown>): Promise<WeatherData> {
    const location = (args.location as string) || "London";

    // Simulate API delay
    await new Promise((resolve) => setTimeout(resolve, 500));

    // Mock weather data based on location
    const weatherConditions: Array<WeatherData["condition"]> = [
      "sunny",
      "cloudy",
      "rainy",
      "snowy",
      "stormy",
    ];

    // Generate pseudo-random but consistent data based on location
    const hash = location.split("").reduce((acc, char) => acc + char.charCodeAt(0), 0);
    const conditionIndex = hash % weatherConditions.length;
    const baseTemp = 10 + (hash % 25);

    return {
      location,
      temperature: baseTemp,
      unit: "celsius",
      condition: weatherConditions[conditionIndex],
      humidity: 40 + (hash % 50),
      windSpeed: 5 + (hash % 20),
      description: this.#getDescription(weatherConditions[conditionIndex], baseTemp),
    };
  }

  #getDescription(condition: WeatherData["condition"], temp: number): string {
    const descriptions: Record<WeatherData["condition"], string> = {
      sunny: `Clear skies with ${temp}°C. A beautiful day!`,
      cloudy: `Overcast with ${temp}°C. Might clear up later.`,
      rainy: `Rainy conditions at ${temp}°C. Don't forget your umbrella!`,
      snowy: `Snowy weather at ${temp}°C. Bundle up warm!`,
      stormy: `Stormy conditions at ${temp}°C. Best to stay indoors.`,
    };
    return descriptions[condition];
  }
}
