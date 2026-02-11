/**
 * Formats a timestamp for display, showing relative time for recent entries
 * and absolute time for older entries.
 *
 * This function handles both UTC timestamps (with "Z" suffix) and timestamps
 * without timezone information by treating them as UTC.
 *
 * @param timestamp ISO 8601 timestamp string
 * @returns Formatted timestamp string
 */
export function formatTimestamp(timestamp: string): string {
    // Ensure the timestamp is treated as UTC if it doesn't have a timezone indicator
    const normalizedTimestamp = timestamp.endsWith("Z") || timestamp.includes("+") || timestamp.includes("-")
        ? timestamp
        : `${timestamp}Z`;

    const date = new Date(normalizedTimestamp);

    // Validate the parsed date
    if (isNaN(date.getTime())) {
        return "Invalid date";
    }

    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;

    // For older dates, show the local date and time
    return date.toLocaleDateString() + " " + date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}
