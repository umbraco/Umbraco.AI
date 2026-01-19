/**
 * Result from the SearchUmbraco tool.
 */
export interface SearchUmbracoResult {
	Success: boolean;
	Results: UmbracoSearchResultItem[];
	Message?: string;
}

/**
 * A single search result item from Umbraco.
 */
export interface UmbracoSearchResultItem {
	Id: string;
	Name: string;
	Type: "content" | "media";
	ContentType: string;
	Url?: string;
	ThumbnailUrl?: string;
	Score: number;
	UpdateDate: string;
	Path: string;
	Metadata: Record<string, unknown>;
}
