// Shared loader for feature-design mockups. Copy into <plan-folder>/mockups/ beside the screens.
//
// - UMBRACO_VERSION is the major the project targets (the newest when it targets more than one).
//   It picks the backoffice look from LOOKS: 17 has square 3px corners and a pink "current"
//   colour, 18 uses UUI's rounder defaults. _mockup.css keys off html[data-umbraco].
// - LOOKS lists only the majors whose look changed. Any other major, a beta included, uses the
//   newest look below it, since most majors look like the one before. Add a major here, and
//   its rules to _mockup.css, only when its backoffice looks different.
// - Loads UUI 2.x, the major Umbraco 17.6+ and 18 ship. For Umbraco 17.5 or older (UUI 1.x),
//   change UUI_VERSION to '1' and the stylesheet links in each page to uui-css@1.
// - Swaps <uui-icon name="icon-*"> for the real backoffice icon, so mockups use the same icon
//   names the code will.

const UMBRACO_VERSION = '18';
const UUI_VERSION = '2';

// Each look, and the backoffice release its icons load from. Pinned, because the CDN's
// major ranges (@19) skip betas and RCs.
const LOOKS = { 17: '17.6.1', 18: '18.1.0' };

// A ?umbraco=17 or ?umbraco=18 query string overrides the major, to compare looks.
const major = Number(new URLSearchParams(location.search).get('umbraco') ?? UMBRACO_VERSION);
const known = Object.keys(LOOKS).map(Number);
const look = Math.max(...known.filter((m) => m <= major), Math.min(...known));
const BACKOFFICE_VERSION = LOOKS[look];
document.documentElement.dataset.umbraco = String(look);

const ICON_BASE = `https://cdn.jsdelivr.net/npm/@umbraco-cms/backoffice@${BACKOFFICE_VERSION}/dist-cms/packages/core/icon-registry/icons`;

await import(`https://cdn.jsdelivr.net/npm/@umbraco-ui/uui@${UUI_VERSION}/+esm`);

const iconCache = new Map();
function loadIcon(name) {
	if (!iconCache.has(name)) {
		iconCache.set(
			name,
			import(`${ICON_BASE}/${name}.js`).then((m) => m.default).catch(() => null),
		);
	}
	return iconCache.get(name);
}

async function swapIcons(root) {
	const icons = root.querySelectorAll('uui-icon[name^="icon-"]');
	await Promise.all(
		[...icons].map(async (el) => {
			const svg = await loadIcon(el.getAttribute('name'));
			if (!svg) return console.warn(`No backoffice icon named ${el.getAttribute('name')}`);
			// A uui-icon that already failed a name lookup stays on its fallback, so swap in a
			// fresh one carrying the same attributes (slot, class, style) and the real SVG.
			const fresh = document.createElement('uui-icon');
			for (const { name, value } of el.attributes) if (name !== 'name') fresh.setAttribute(name, value);
			fresh.svg = svg;
			el.replaceWith(fresh);
		}),
	);
}

await swapIcons(document);
// Let open-on-load elements (toasts, popovers) finish animating in before a screenshot.
await new Promise((resolve) => setTimeout(resolve, 1000));
document.documentElement.dataset.mockupReady = 'true';
