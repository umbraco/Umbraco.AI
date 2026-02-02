/**
 * Result from the SearchUmbraco tool.
 * Note: Properties are camelCase to match JSON serialization from backend.
 */
export interface SearchUmbracoResult {
	success: boolean;
	results: UmbracoSearchResultItem[];
	message?: string;
}

/**
 * A single search result item from Umbraco.
 * Note: Properties are camelCase to match JSON serialization from backend.
 */
export interface UmbracoSearchResultItem {
	id: string;
	name: string;
	type: "content" | "media";
	contentType: string;
	url?: string;
	thumbnailUrl?: string;
	score: number;
	updateDate: string;
	path: string;
	metadata: Record<string, unknown>;
}
