const NEAR_BOTTOM_THRESHOLD_PX = 50;

/**
 * True when the container is close enough to the bottom that new content
 * should keep it pinned there, rather than jumping a user who scrolled up.
 */
export function isNearBottom(
    container: { scrollTop: number; scrollHeight: number; clientHeight: number },
    threshold = NEAR_BOTTOM_THRESHOLD_PX,
): boolean {
    return container.scrollHeight - container.scrollTop - container.clientHeight <= threshold;
}
