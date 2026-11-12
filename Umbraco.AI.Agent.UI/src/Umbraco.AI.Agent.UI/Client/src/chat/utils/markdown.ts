import { DOMPurify } from "@umbraco-cms/backoffice/external/dompurify";
import { marked } from "@umbraco-cms/backoffice/external/marked";

/** Attribute on the "show image" button that holds the hidden image's URL. */
export const UAI_HIDDEN_IMAGE_SRC_ATTRIBUTE = "data-uai-image-src";

export interface UaiChatMarkdownOptions {
    /** Outside image URLs the user has chosen to show. */
    shownImageUrls?: ReadonlySet<string>;
    /** Label for the button that stands in for a hidden outside image. */
    hiddenImageLabel?: (host: string) => string;
}

// Only the elements and attributes markdown produces. Nothing here can load a URL by itself
// except `img`, which is handled below.
const ALLOWED_TAGS = [
    "a",
    "b",
    "blockquote",
    "br",
    "code",
    "del",
    "div",
    "em",
    "h1",
    "h2",
    "h3",
    "h4",
    "h5",
    "h6",
    "hr",
    "i",
    "img",
    "input",
    "li",
    "ol",
    "p",
    "pre",
    "s",
    "span",
    "strong",
    "sub",
    "sup",
    "table",
    "tbody",
    "td",
    "th",
    "thead",
    "tr",
    "ul",
];
const ALLOWED_ATTR = [
    "align",
    "alt",
    "checked",
    "class",
    "colspan",
    "disabled",
    "href",
    "rowspan",
    "src",
    "start",
    "title",
    "type",
];

/**
 * Converts chat message markdown to HTML that is safe to render.
 *
 * Message text can include content that came from tools and other sources, so the HTML is
 * sanitized. Images from this site are shown. Images from anywhere else are replaced by a
 * button, so nothing is loaded from an outside server until the user chooses to show it.
 * Attached images are rendered separately, not through markdown.
 */
export function renderChatMarkdown(markdown: string, options: UaiChatMarkdownOptions = {}): string {
    const html = marked.parse(markdown, { async: false });
    const fragment = DOMPurify.sanitize(html, {
        ALLOWED_TAGS,
        ALLOWED_ATTR,
        RETURN_DOM_FRAGMENT: true,
    });

    // Markdown only produces checkbox inputs (task lists). Anything else, such as an image input,
    // could load a URL.
    fragment.querySelectorAll("input").forEach((input) => {
        if (input.getAttribute("type") !== "checkbox") {
            input.remove();
        }
    });

    fragment.querySelectorAll("img").forEach((img) => processImage(img, options));

    const container = document.createElement("div");
    container.append(fragment);
    return container.innerHTML;
}

function processImage(img: HTMLImageElement, options: UaiChatMarkdownOptions) {
    const url = toUrl(img.getAttribute("src"));
    if (!url) {
        img.remove();
        return;
    }

    if (isSameSiteImage(url) || options.shownImageUrls?.has(url.href)) {
        img.setAttribute("referrerpolicy", "no-referrer");
        img.setAttribute("loading", "lazy");
        return;
    }

    if (url.protocol !== "https:" && url.protocol !== "http:") {
        img.remove();
        return;
    }

    const button = document.createElement("uui-button");
    button.setAttribute("look", "outline");
    button.setAttribute("compact", "");
    button.setAttribute(UAI_HIDDEN_IMAGE_SRC_ATTRIBUTE, url.href);
    const label = options.hiddenImageLabel?.(url.host) ?? `Show image from ${url.host}`;
    button.setAttribute("label", label);
    button.textContent = label;
    img.replaceWith(button);
}

function toUrl(src: string | null): URL | null {
    if (!src) {
        return null;
    }
    try {
        return new URL(src, window.location.href);
    } catch {
        return null;
    }
}

function isSameSiteImage(url: URL): boolean {
    // A data: image is inline, so it loads nothing from anywhere.
    return url.origin === window.location.origin || /^data:image\//i.test(url.href);
}
