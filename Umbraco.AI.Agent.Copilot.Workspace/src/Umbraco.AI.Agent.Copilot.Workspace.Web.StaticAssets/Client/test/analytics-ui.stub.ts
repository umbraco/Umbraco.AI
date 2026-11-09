// Test-only stand-in for `@umbraco/analytics-ui`. The npm package ships types only; the backoffice
// loads its code at runtime through the import map. Core's broad barrel (aliased in vitest.config.ts)
// reaches the usage dashboard, which imports these names, so they must exist for the module graph to
// load. No Workspace test renders the dashboard, so nothing here is ever called.
export const UAUI_TABLE_CELL = {};
export class UauiTableServerController {}
export const uauiFormatDateRange = () => "";
export const uauiGroupByOptions = () => [];
export const uauiPresetRange = () => undefined;
