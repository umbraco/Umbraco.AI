import { cpSync, rmSync } from "fs";

if (process.env.NPM_OUTPUT_DIR) {
    const srcDir = "./artifacts";
    const outputDir = `${process.env.NPM_OUTPUT_DIR}`;

    rmSync(outputDir, { recursive: true, force: true });
    cpSync(srcDir, outputDir, { recursive: true });

    console.log("--- Copied npm package to build output directory successfully. ---");
}
