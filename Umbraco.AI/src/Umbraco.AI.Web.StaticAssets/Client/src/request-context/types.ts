/**
 * Request Context Types
 *
 * Simple context item interface for passing context to AI operations.
 * Matches the backend AiRequestContextItem model.
 */

/**
 * Simple context item - matches backend AiRequestContextItem.
 * Intentionally flexible - processors on backend extract meaning.
 */
export interface UaiRequestContextItem {
	/** Human-readable description */
	description: string;
	/** The context data */
	value?: string;
}
