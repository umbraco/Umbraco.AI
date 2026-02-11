import http from "http";
import { execSync } from "child_process";

function getIdentifier() {
    try {
        const gitDir = execSync("git rev-parse --git-dir", { encoding: "utf-8" }).trim();
        if (gitDir.includes("worktrees")) {
            const parts = gitDir.split(/[\\\/]/);
            const idx = parts.indexOf("worktrees");
            return parts[idx + 1] || "default";
        }
        return execSync("git branch --show-current", { encoding: "utf-8" }).trim() || "default";
    } catch {
        return "default";
    }
}

const identifier = getIdentifier().replace(/[^a-zA-Z0-9\-_.]/g, "") || "default";
const pipeName = `umbraco.demosite.${identifier}`;
const socketPath = process.platform === "win32" ? `\\\\.\\pipe\\${pipeName}` : `/tmp/${pipeName}`;

const req = http.get({ socketPath, path: "/site-address", timeout: 2000 }, (res) => {
    let body = "";
    res.on("data", chunk => body += chunk);
    res.on("end", () => {
        if (res.statusCode === 200) {
            console.log(JSON.stringify({ running: true, address: body.trim(), pipeName, identifier }));
        } else {
            console.log(JSON.stringify({ running: false, error: `HTTP ${res.statusCode}` }));
        }
    });
});

req.on("error", (err) => {
    console.log(JSON.stringify({ running: false, error: err.message, pipeName, identifier }));
});

req.on("timeout", () => {
    req.destroy();
    console.log(JSON.stringify({ running: false, error: "timeout" }));
});
