#!/usr/bin/env node
// Test script: confirms the demo site is up at this worktree's assigned port
import https from "https";
import { getPort } from "worktree-dev-port";

let port;
try {
    port = getPort();
} catch (error) {
    console.error(`\nError: ${error.message}`);
    process.exit(1);
}

console.log(`Testing demo site at https://127.0.0.1:${port}`);

https
    .get({ hostname: "127.0.0.1", port, path: "/", rejectUnauthorized: false }, (res) => {
        if (res.statusCode) {
            console.log(`\nSite address: https://127.0.0.1:${port}`);
            console.log("\nSuccess! Demo site is running.");
        }
        res.resume();
    })
    .on("error", (err) => {
        console.error(`\nError: ${err.message}`);
        console.error("Make sure the demo site is running with: /demo-site-management start");
    });
