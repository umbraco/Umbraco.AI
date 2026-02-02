---
name: demo-site-management
description: Manages the Umbraco.AI demo site for development. Handles starting with DemoSite-Claude profile, port discovery via named pipes, and OpenAPI client generation. Use when starting, stopping, or checking the demo site, or when generating OpenAPI clients for frontend development.
argument-hint: [start|stop|generate-client|status|restart|open]
allowed-tools: Bash, Read, TaskOutput, TaskStop
---

# Demo Site Management

Manage the Umbraco.AI demo site with automatic port discovery via named pipes.

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
   - Try querying site address endpoint via named pipe (see "Query site address via named pipe" section)
   - Check if background tasks exist with "DemoSite" in description
   - If running, report and exit
2. If not running, start in background: `cd demo/Umbraco.AI.DemoSite && dotnet run --launch-profile DemoSite-Claude`
3. Wait 15-20 seconds for startup
4. Query site address endpoint via named pipe to get port and pipe name (see "Query site address via named pipe" section)
5. Report:
   - Task ID for later stopping (save this for future commands)
   - Port number (from site address endpoint)
   - Pipe name (format: umbraco.demosite.{branch-or-worktree})
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
   - Suggest checking with `/demo-site-management status`

4. Verify shutdown:
   - Try connecting to the last known port (should fail)
   - Check if background task is gone

5. Report results:
   - Success: "Demo site stopped (task ID: {id})"
   - Failure: "Could not find running demo site"
   - Note: Pipes are automatically cleaned up when process exits

### For "generate-client"
1. Check if site is running:
   - Try querying site address endpoint via named pipe (see "Query site address via named pipe" section)
   - Check if any background bash tasks are related to DemoSite
2. If not running, report error with suggestion: "Demo site not running. Start it with `/demo-site-management start`"
3. Run: `npm run generate-client` (runs all three packages concurrently)
4. Monitor output for:
   - "Using named pipe: umbraco.demosite.{identifier}" (should appear 3 times)
   - "✓ TypeScript client generated successfully" (should appear 3 times)
   - No errors (EPIPE, connection refused, etc.)
5. Report summary:
   - Success/failure for each package (core, prompt, agent)
   - Pipe name used
   - Whether concurrent connections worked (no errors)

### For "status"
Use multi-method detection to determine site status:

1. **Query site address endpoint**: Try querying via named pipe (see "Query site address via named pipe" section)
   - If successful, site is running and you have port info
   - If fails, continue to other methods

2. **Check background tasks**: Look for tasks with "DemoSite" or "demo-site" in name/output
   - If found, extract task ID

3. **Determine git context**:
   - Run `git rev-parse --git-dir` to check if worktree
   - If worktree, extract name from `.git/worktrees/{name}`
   - Otherwise use `git branch --show-current`
   - If no git, identifier is "default"

4. **Report comprehensive status**:
   - Running: yes/no (based on site address endpoint response)
   - Task ID: if background task found
   - Port: from site address endpoint
   - Pipe name: `umbraco.demosite.{identifier}`
   - Git context: branch name, worktree name, or "not in git repo"
   - Suggestion: How to start if not running, or how to connect if running

### For "restart"
Execute stop operation, wait 3 seconds, then execute start operation.

### For "open"
1. Check if demo site is running and get port info:
   - Query site address endpoint via named pipe (see "Query site address via named pipe" section)
   - If fails, report error: "Demo site not running. Start it with `/demo-site-management start`"
2. Launch default browser with discovered URL:
   - Windows: `powershell.exe -Command "Start-Process 'https://127.0.0.1:<port>'"`
   - Linux: `xdg-open https://127.0.0.1:<port>`
   - macOS: `open https://127.0.0.1:<port>`
3. Report:
   - Browser launched
   - URL opened
   - Note about certificate warning (self-signed HTTPS)
   - Credentials reminder: admin@example.com / password1234

## Query Site Address via Named Pipe

The demo site exposes a `/site-address` endpoint that returns port and pipe information as JSON.
Query it via HTTP over named pipes without needing to know the port:

**Using Node.js** (recommended, cross-platform):
```javascript
import http from 'http';
import { execSync } from 'child_process';

// Get pipe name from git context
function getIdentifier() {
  try {
    const gitDir = execSync('git rev-parse --git-dir', { encoding: 'utf-8' }).trim();
    if (gitDir.includes('worktrees')) {
      return gitDir.split(/[\\\/]/).find((p, i, arr) => arr[i-1] === 'worktrees') || 'default';
    }
    return execSync('git branch --show-current', { encoding: 'utf-8' }).trim() || 'default';
  } catch { return 'default'; }
}

const identifier = getIdentifier().replace(/[^a-zA-Z0-9\-_.]/g, '') || 'default';
const pipeName = `umbraco.demosite.${identifier}`;
const socketPath = process.platform === 'win32' ? `\\\\.\\pipe\\${pipeName}` : `/tmp/${pipeName}`;

const address = await new Promise((resolve, reject) => {
  http.get({ socketPath, path: '/site-address' }, (res) => {
    let body = '';
    res.on('data', chunk => body += chunk);
    res.on('end', () => res.statusCode === 200 ? resolve(body) : reject(new Error(`HTTP ${res.statusCode}`)));
  }).on('error', reject);
});

// address = "https://127.0.0.1:44355"
```

**Using curl** (PowerShell on Windows):
```powershell
$identifier = (git branch --show-current).Trim() -replace '[^a-zA-Z0-9\-_]', ''
$pipeName = "umbraco.demosite.$identifier"
curl.exe --unix-socket "//./pipe/$pipeName" http://localhost/site-address
```

**Response format:** Plain text HTTPS address
```
https://127.0.0.1:44355
```

## Detection Helper Commands

Use these commands for reliable cross-platform detection:

**Check for background tasks:**
```bash
# Look for tasks with DemoSite in output (path varies by platform)
# Use /tasks command or check TaskOutput for running demo site tasks
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

## Port Discovery Details

The demo site uses HTTP over named pipes for automatic port discovery:
- **Pipe naming**: `umbraco.demosite.<identifier>`
- **Identifier logic**:
  - Worktree: extracted from `.git/worktrees/<name>`
  - Main repo: current branch name
  - No git: `default`
- **Site address endpoint**: `/site-address` (returns HTTPS address as plain text)
- **HTTP transport**: Kestrel listens on both named pipe and HTTP/HTTPS
- **Concurrent support**: Multiple clients can connect simultaneously
- **Implementation**: `demo/Umbraco.AI.DemoSite/Composers/NamedPipeListenerComposer.cs`

## Common Issues

### Pipe not found
- Demo site not running or still starting up
- Solution: Wait 15-20 seconds after start, or check status with `/demo-site-management status`

### Connection refused
- Pipe doesn't exist or connection failed
- Check that site is running: `/demo-site-management status`
- Verify pipe name matches git context (branch/worktree)

### Multiple instances conflict
- Each worktree/branch gets unique pipe name
- Main branch: `umbraco.demosite.<branch-name>`
- Worktree: `umbraco.demosite.<worktree-name>`
- No git: `umbraco.demosite.default`

## Success Criteria

**After start**: Report task ID, pipe name, port, and URL
**After stop**: Confirm process stopped successfully
**After generate-client**: Show success for all three packages (core, prompt, agent)
**After status**: Show running state, port, and pipe connection details
