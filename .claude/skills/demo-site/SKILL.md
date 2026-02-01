---
name: demo-site
description: Manages the Umbraco.Ai demo site for development. Handles starting with DemoSite-Claude profile, port discovery via named pipes, and OpenAPI client generation. Use when starting, stopping, or checking the demo site, or when generating OpenAPI clients for frontend development.
argument-hint: [start|stop|generate-client|status|restart|open]
allowed-tools: Bash, Read, TaskOutput, TaskStop
---

# Demo Site Management

Manage the Umbraco.Ai demo site with automatic port discovery via named pipes.

## Command: $ARGUMENTS

Execute the requested demo site operation.

### Available commands

- **start**: Start demo site with DemoSite-Claude profile on dynamic port
- **stop**: Stop the running demo site
- **generate-client**: Generate OpenAPI clients (starts site if needed)
- **status**: Check if site is running and show port/pipe info
- **restart**: Stop and restart the demo site
- **open**: Open the demo site in default browser (discovers port automatically)

## Current Environment

- Working directory: !`pwd`
- Git branch: !`git branch --show-current 2>/dev/null || echo "not in git repo"`
- Background tasks: !`echo "Check with /tasks command for active background tasks"`

## Implementation Guide

### For "start"
1. Check if already running using multi-method detection:
   - Try connecting to common ports (44355, 5000-65535 range is too broad, skip)
   - Check if background tasks exist with "DemoSite" in description
   - If running, report and exit
2. If not running, start in background: `cd demo/Umbraco.Ai.DemoSite && dotnet run --launch-profile DemoSite-Claude`
3. Wait 15-20 seconds for startup
4. Read the task output to find the port number (look for "Now listening on: https://127.0.0.1:<port>")
5. Make initial request to trigger middleware using the discovered port: `curl -k https://127.0.0.1:<port>`
6. Wait 2 seconds, then read task output again to find the pipe name (look for "Port discovery pipe: umbraco-ai-demo-port-<identifier>")
7. Report:
   - Task ID for later stopping (save this for future commands)
   - Port number
   - Pipe name (format: umbraco-ai-demo-port-{branch-or-worktree})
   - Site URL

### For "stop"
1. Find background tasks related to demo site:
   - Look for tasks with "DemoSite" or "demo-site" in name
   - Extract task ID from task list

2. If task found:
   - Use TaskStop with the task ID to stop gracefully
   - Wait 2-3 seconds for cleanup

3. If no task found:
   - Report that no running demo site was found
   - Suggest checking with `/demo-site status`

4. Verify shutdown:
   - Try connecting to the last known port (should fail)
   - Check if background task is gone

5. Report results:
   - Success: "Demo site stopped (task ID: {id})"
   - Failure: "Could not find running demo site"
   - Note: Pipes are automatically cleaned up when process exits

### For "generate-client"
1. Check if site is running using multi-method detection:
   - Look for background task output files in temp directory
   - Try connecting to expected ports (check 44355 first, common default)
   - Check if any background bash tasks are related to DemoSite
2. If not running, report error with suggestion: "Demo site not running. Start it with `/demo-site start`"
3. If running but just started, ensure middleware is initialized:
   - Wait 2 seconds if site was just started
   - Middleware only starts on first HTTP request
4. Run: `npm run generate-client` (runs all three packages concurrently)
5. Monitor output for:
   - "Connected to demo site named pipe" (should appear 3 times)
   - "Discovered demo site on port X via named pipe" (should appear 3 times)
   - "✓ TypeScript client generated successfully" (should appear 3 times)
   - Any EPIPE errors (should be none)
6. Report summary:
   - Success/failure for each package (core, prompt, agent)
   - Port discovered via pipe
   - Whether concurrent connections worked (no EPIPE errors)
   - Whether banner was suppressed (no ASCII art visible)

### For "status"
Use multi-method detection to determine site status:

1. **Check background tasks**: Look for tasks with "DemoSite" or "demo-site" in name/output
   - If found, extract task ID and read output for port info

2. **Check port connectivity**: Try connecting to common ports
   - Try 44355 (default): `curl -k --connect-timeout 1 https://localhost:44355 2>&1`
   - If background task found with port, try that port

3. **Check named pipe** (Windows-specific, informational only):
   - Expected pipe name based on git: `umbraco-ai-demo-port-{branch-or-worktree}`
   - Note: Enumerating pipes from Bash on Windows is difficult

4. **Determine git context**:
   - Run `git rev-parse --git-dir` to check if worktree
   - If worktree, extract name from `.git/worktrees/{name}`
   - Otherwise use `git branch --show-current`
   - If no git, identifier is "default"

5. **Report comprehensive status**:
   - Running: yes/no (based on task or port connectivity)
   - Task ID: if background task found
   - Port: if discoverable from task output or connectivity test
   - Expected pipe name: `umbraco-ai-demo-port-{identifier}`
   - Git context: branch name, worktree name, or "not in git repo"
   - Suggestion: How to start if not running, or how to connect if running

### For "restart"
Execute stop operation, wait 3 seconds, then execute start operation.

### For "open"
1. Check if demo site is running using status detection methods
2. If not running, report error: "Demo site not running. Start it with `/demo-site start`"
3. If running, discover the port:
   - Look for background task output file
   - Extract port from "Now listening on: https://127.0.0.1:<port>" line
   - Or try common ports (55209, 44355, etc.)
4. Launch default browser with discovered URL:
   - Windows: `powershell.exe -Command "Start-Process 'https://127.0.0.1:<port>'"`
   - Linux: `xdg-open https://127.0.0.1:<port>`
   - macOS: `open https://127.0.0.1:<port>`
5. Report:
   - Browser launched
   - URL opened
   - Note about certificate warning (self-signed HTTPS)
   - Credentials reminder: admin@example.com / password1234

## Detection Helper Commands

Use these commands for reliable cross-platform detection:

**Check for background tasks:**
```bash
# Look for tasks with DemoSite in output (path varies by platform)
# Use /tasks command or check TaskOutput for running demo site tasks
```

**Test port connectivity:**
```bash
# Quick connection test (1 second timeout)
curl -k --connect-timeout 1 https://localhost:44355 2>&1 | head -1
```

**Determine git identifier:**
```bash
# Get worktree or branch name
git_dir=$(git rev-parse --git-dir 2>/dev/null)
if [[ "$git_dir" == *"worktrees"* ]]; then
  echo "worktree: $(basename $(dirname $git_dir))"
else
  echo "branch: $(git branch --show-current 2>/dev/null || echo 'not-in-git')"
fi
```

**Read task output for port:**
```bash
# Extract port from task output
grep "Now listening on:" /path/to/task.output | grep -oP 'https://[^:]+:\K[0-9]+'
```

## Port Discovery Details

The demo site uses named pipes for automatic port discovery:
- **Pipe naming**: `umbraco-ai-demo-port-<identifier>`
- **Identifier logic**:
  - Worktree: extracted from `.git/worktrees/<name>`
  - Main repo: current branch name
  - No git: `default`
- **Middleware**: Starts on first HTTP request (InvokeAsync in pipeline)
- **Concurrent support**: Multiple generate-client instances can connect simultaneously
- **Implementation**: `demo/Umbraco.Ai.DemoSite/Middleware/PortDiscoveryMiddleware.cs`

## Common Issues

### Pipe not found
- Middleware only starts on first HTTP request
- Solution: Make a curl request to the site to trigger it

### Generate-client falls back to 44355
- Pipe doesn't exist or connection failed
- Check that site is running and middleware initialized
- Verify pipe name matches git context

### Multiple instances conflict
- Each worktree/branch gets unique pipe name
- Main branch: `umbraco-ai-demo-port-<branch-name>`
- Worktree: `umbraco-ai-demo-port-<worktree-name>`
- No git: `umbraco-ai-demo-port-default`

## Success Criteria

**After start**: Report task ID, pipe name, port, and URL
**After stop**: Confirm process stopped successfully
**After generate-client**: Show success for all three packages (core, prompt, agent)
**After status**: Show running state, port, and pipe connection details
