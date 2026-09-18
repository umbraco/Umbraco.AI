---
name: demo-site-management
description: Manages the Umbraco.AI demo site for development. Handles starting with DemoSite-Claude profile, per-worktree port lookup via git config, and OpenAPI client generation. Use when starting, stopping, or checking the demo site, or when generating OpenAPI clients for frontend development.
argument-hint: [start|stop|generate-client|status|restart|open]
---

# Demo Site Management

Manage the Umbraco.AI demo site. Each worktree gets its own stable dev port, assigned once by [Umbraco.Community.WorktreeDevPort](https://github.com/mattbrailsford/Umbraco.Community.WorktreeDevPort) and stored in that worktree's own git config — no named pipe, socket, or discovery endpoint to query.

## Command: $ARGUMENTS

Execute the requested demo site operation.

### Available commands

- **start**: Start demo site with DemoSite-Claude profile
- **stop**: Stop the running demo site
- **generate-client**: Generate OpenAPI clients (starts site if needed)
- **status**: Check if site is running and show its port
- **restart**: Stop and restart the demo site
- **open**: Open the demo site in default browser

## Current Environment

- Working directory: !`pwd`
- Git branch: !`git branch --show-current 2>/dev/null || echo "not in git repo"`
- Background tasks: !`echo "Check with /tasks command for active background tasks"`

## Implementation Guide

### For "start"

1. Check if already running using multi-method detection:
    - Try reading the port (see "Get the worktree's dev port" section) and connecting to it
    - Check if background tasks exist with "DemoSite" in description
    - If running, report and exit
2. Detect demo site path:
    - Read `Directory.Packages.props` and extract the major from the `Umbraco.Cms.Core` lower bound (e.g. `[18.0.0, …)` → `18`)
    - Demo site path: `demos/v{major}/Umbraco.AI.DemoSite`
3. If not running, start in background: `cd demos/v{major}/Umbraco.AI.DemoSite && dotnet run --launch-profile DemoSite-Claude`
4. Wait 15-20 seconds for startup (the package picks a free port on first run in this worktree, or reuses the one it already picked)
5. Read the port (see "Get the worktree's dev port" section)
6. Report:
    - Task ID for later stopping (save this for future commands)
    - Port number
    - Site URL (`https://127.0.0.1:<port>`)

### For "stop"

1. Find background tasks related to demo site:
    - Look for tasks with "DemoSite" or "demo-site" in name
    - Extract task ID from task list

2. If task found:
    - Use TaskStop with the task ID to stop gracefully
    - Wait 2-3 seconds for cleanup

3. If no task found:
    - Report that no running demo site was found
    - Suggest checking with `/demo-site-management status`

4. Verify shutdown:
    - Try connecting to the last known port (should fail)
    - Check if background task is gone

5. Report results:
    - Success: "Demo site stopped (task ID: {id})"
    - Failure: "Could not find running demo site"
    - Note: the assigned port is remembered in git config and reused on the next start — nothing to clean up

### For "generate-client"

1. Check if site is running:
    - Try reading the port (see "Get the worktree's dev port" section) and connecting to it
    - Check if any background bash tasks are related to DemoSite
2. If not running, report error with suggestion: "Demo site not running. Start it with `/demo-site-management start`"
3. Run: `npm run generate-client` (runs all three packages concurrently)
4. Monitor output for:
    - "Using port <port> for this worktree" (should appear 3 times)
    - "✓ TypeScript client generated successfully" (should appear 3 times)
    - No errors (connection refused, certificate errors, etc.)
5. Report summary:
    - Success/failure for each package (core, prompt, agent)
    - Port used
    - Whether concurrent connections worked (no errors)

### For "status"

Use multi-method detection to determine site status:

1. **Read the port and probe it**: see "Get the worktree's dev port" section
    - If a port is set and reachable, site is running
    - If no port is set yet, the site has never been started in this worktree
    - If a port is set but unreachable, the site isn't currently running

2. **Check background tasks**: Look for tasks with "DemoSite" or "demo-site" in name/output
    - If found, extract task ID

3. **Report comprehensive status**:
    - Running: yes/no
    - Task ID: if background task found
    - Port: from git config (if set)
    - Git context: branch name, worktree name, or "not in git repo"
    - Suggestion: How to start if not running, or how to connect if running

### For "restart"

Execute stop operation, wait 3 seconds, then execute start operation.

### For "open"

1. Check if demo site is running and get its port (see "Get the worktree's dev port" section)
    - If no port is set or it's unreachable, report error: "Demo site not running. Start it with `/demo-site-management start`"
2. Launch default browser with discovered URL:
    - Windows: `powershell.exe -Command "Start-Process 'https://127.0.0.1:<port>'"`
    - Linux: `xdg-open https://127.0.0.1:<port>`
    - macOS: `open https://127.0.0.1:<port>`
3. Report:
    - Browser launched
    - URL opened
    - Note about certificate warning (self-signed HTTPS)
    - Credentials reminder: admin@example.com / password1234

## Get the Worktree's Dev Port

The demo site's port is assigned once (by `Umbraco.Community.WorktreeDevPort` on first run) and stored in this worktree's own git config — a plain read, no server round-trip needed to discover it:

```bash
git config --worktree --get worktreedevport.port
```

Empty/no output means the site has never been started in this worktree yet. A value means that's the port to use — probe `https://127.0.0.1:<port>` to confirm the site is actually up right now (the config value persists across restarts, so its presence alone doesn't mean the process is currently running).

This works identically whether you're in the main checkout or a linked worktree — git scopes `--worktree` config to whichever one you're currently in.

## Common Issues

### No port set yet

- The demo site has never been started in this worktree
- Solution: `/demo-site-management start`

### Port set but connection refused

- The value is stale from a previous run; the process isn't currently up
- Check with `/demo-site-management status`, then start it if needed — the same port will be reused

### Multiple worktrees

- Each worktree gets its own port automatically, with no collisions (a free port is verified before being assigned)
- Removing a worktree (`git worktree remove`) removes its saved port with it — nothing to clean up by hand

## Success Criteria

**After start**: Report task ID, port, and URL
**After stop**: Confirm process stopped successfully
**After generate-client**: Show success for all three packages (core, prompt, agent)
**After status**: Show running state and port
