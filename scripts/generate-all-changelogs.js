// scripts/generate-all-changelogs.js
const { getProducts, generateChangelog } = require("./generate-changelog");
const path = require("path");

async function generateAllChangelogs() {
    const rootDir = process.cwd();
    const products = getProducts(rootDir);
    const productNames = Object.keys(products);

    console.log(`📦 Found ${productNames.length} products`);
    console.log("");

    for (const product of productNames) {
        console.log(`Generating changelog for ${product}...`);
        try {
            await generateChangelog(product, null, { unreleased: true, rootDir });
            console.log(`✅ ${product} done\n`);
        } catch (err) {
            console.error(`❌ ${product} failed:`, err.message, "\n");
        }
    }

    console.log("🎉 All changelogs generated!");
}

generateAllChangelogs().catch((err) => {
    console.error("❌ Error:", err.message);
    process.exit(1);
});
