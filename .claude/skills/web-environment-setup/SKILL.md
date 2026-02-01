---
name: web-environment-setup
description: Sets up Claude Code web environment for building the Umbraco.Ai solution. Installs .NET SDK, configures proxy for NuGet, and prepares git for GitVersioning. Use this when starting a new Claude Code web session to build the project.
allowed-tools: Bash, Read
---

# Web Environment Setup

You are helping set up the Umbraco.Ai repository for building in Claude Code web environment.

## Task

Run the web environment setup script to prepare the environment for .NET development.

## Quick Start

```bash
# One-command setup
source .claude/scripts/setup-web-environment.sh

# Build
dotnet build Umbraco.Ai.sln
```

## Workflow

1. **Run the setup script**:
   ```bash
   source .claude/scripts/setup-web-environment.sh
   ```

2. **Verify the setup** by checking:
   - `dotnet --version` returns 10.x
   - Proxy environment variables are set
   - Git is not a shallow clone

3. **Test the build**:
   ```bash
   dotnet build Umbraco.Ai.sln
   ```

## Problem

Claude Code web runs in a sandboxed environment with restrictions:

1. **Proxy Authentication** - Network requests go through a JWT-authenticated proxy
2. **Package Feed Restrictions** - Only certain hosts are allowed (e.g., `api.nuget.org` but not `www.myget.org`)
3. **Shallow Git Clones** - May break Nerdbank.GitVersioning
4. **.NET SDK** - May not be pre-installed

## Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `setup-web-environment.sh` | Full environment setup | `source .claude/scripts/setup-web-environment.sh` |
| `setup-dotnet-proxy.sh` | Proxy + NuGet config only | `source .claude/scripts/setup-dotnet-proxy.sh` |
| `dotnet-with-proxy.sh` | Run dotnet with proxy | `.claude/scripts/dotnet-with-proxy.sh restore` |

## What `setup-web-environment.sh` Does

1. **Installs .NET SDK 10.0** via apt (if not present)
2. **Starts px-proxy** to handle JWT-authenticated proxy for NuGet
3. **Creates user-level NuGet.Config** that uses only `api.nuget.org` (bypasses restricted MyGet feeds)
4. **Unshallows git clone** so Nerdbank.GitVersioning can calculate version height

## Environment Detection

All scripts automatically detect if they're running in Claude Code web by checking for JWT proxy patterns:

```bash
if [[ "$HTTP_PROXY" =~ "jwt_" ]] && [[ "$HTTP_PROXY" =~ "@" ]]; then
    # Claude Code web environment - run setup
else
    # Local environment - do nothing
fi
```

| Environment | `HTTP_PROXY` value | Detection |
|-------------|-------------------|-----------|
| Claude Code web | `http://...jwt_eyJ...@host:port` | ✅ Detected |
| Local (no proxy) | *(unset)* | ❌ Not detected |
| Local (corporate proxy) | `http://proxy.corp:8080` | ❌ Not detected |

**On local machines, the scripts do nothing** - they're safe to run anywhere.

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Proxy errors | Check `/tmp/px-proxy.log` |
| NuGet restore fails | Verify proxy: `pgrep -f px` |
| GitVersioning errors | Run `git fetch --unshallow origin` |
| MyGet warnings | Safe to ignore - nuget.org is used instead |

## How It Works

```
┌─────────────────────────────────────────────────────────────────┐
│                    Claude Code Web Environment                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  HTTP_PROXY = http://...jwt_<token>@<host>:<port>              │
│       │                                                         │
│       ▼                                                         │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐       │
│  │  px-proxy   │────▶│  Sandbox    │────▶│  Allowed    │       │
│  │  :3128      │     │  Proxy      │     │  Hosts      │       │
│  └─────────────┘     └─────────────┘     └─────────────┘       │
│       ▲                                         │               │
│       │                                         ▼               │
│  ┌─────────────┐                        ┌─────────────┐        │
│  │   dotnet    │                        │ api.nuget.org│        │
│  │   restore   │                        │ (allowed)   │        │
│  └─────────────┘                        └─────────────┘        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

The px-proxy handles the JWT authentication transparently, allowing standard .NET tooling to work through the sandbox proxy.
