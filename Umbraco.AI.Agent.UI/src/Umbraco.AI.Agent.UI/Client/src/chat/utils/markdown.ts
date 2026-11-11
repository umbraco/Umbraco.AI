import { DOMPurify } from "@umbraco-cms/backoffice/external/dompurify";
import { marked } from "@umbraco-cms/backoffice/external/marked";

/**
 * Converts chat message markdown to HTML that is safe to render.
 *
 * Message text can include content that came from tools and other sources, so the HTML is
 * sanitized. Images and inline styles are removed as well, since both can load remote URLs
 * without any user interaction. Attached images are rendered separately, not through markdown.
 */
export function renderChatMarkdown(markdown: string): string {
    const html = marked.parse(markdown, { async: false });
    return DOMPurify.sanitize(html, {
        USE_PROFILES: { html: true },
        FORBID_TAGS: ["img", "style"],
        FORBID_ATTR: ["style"],
    });
}
