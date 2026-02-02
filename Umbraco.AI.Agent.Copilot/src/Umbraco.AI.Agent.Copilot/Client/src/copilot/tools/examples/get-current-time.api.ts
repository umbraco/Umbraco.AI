import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UaiAgentToolApi } from "../uai-agent-tool.extension.js";

/**
 * Example frontend tool: Get Current Time
 * Returns the current date and time in various formats.
 */
export default class GetCurrentTimeApi extends UmbControllerBase implements UaiAgentToolApi {
  async execute(args: Record<string, unknown>): Promise<string> {
    const now = new Date();
    const format = (args.format as string) || "locale";

    switch (format) {
      case "iso":
        return JSON.stringify({
          format: "iso",
          value: now.toISOString(),
        });
      case "unix":
        return JSON.stringify({
          format: "unix",
          value: Math.floor(now.getTime() / 1000),
        });
      case "locale":
      default:
        return JSON.stringify({
          format: "locale",
          date: now.toLocaleDateString(),
          time: now.toLocaleTimeString(),
          full: now.toLocaleString(),
          timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        });
    }
  }
}
