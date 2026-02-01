# Claude Code Scripts

Scripts for setting up the development environment in Claude Code (web and CLI).

## Web Environment Setup

When using Claude Code on the web, the sandbox environment has restrictions that require special handling for .NET development:

1. **Proxy Authentication** - Network requests go through a JWT-authenticated proxy
2. **Package Feed Restrictions** - Only certain hosts are allowed (e.g., `api.nuget.org` but not `www.myget.org`)
3. **Shallow Git Clones** - May break Nerdbank.GitVersioning
4. **.NET SDK** - May not be pre-installed

### Quick Start

Run the full setup with one command:

```bash
source .claude/scripts/setup-web-environment.sh
```

Or use the skill:
```
/web-environment-setup
```

Then build:
```bash
dotnet build Umbraco.Ai.sln
```

### Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `setup-web-environment.sh` | Full environment setup | `source .claude/scripts/setup-web-environment.sh` |
| `setup-dotnet-proxy.sh` | Proxy + NuGet config only | `source .claude/scripts/setup-dotnet-proxy.sh` |
| `dotnet-with-proxy.sh` | Run dotnet with proxy | `.claude/scripts/dotnet-with-proxy.sh restore` |

### What `setup-web-environment.sh` Does

1. **Installs .NET SDK 10.0** via apt (if not present)
2. **Starts px-proxy** to handle JWT-authenticated proxy for NuGet
3. **Creates user-level NuGet.Config** that uses only `api.nuget.org` (bypasses restricted MyGet feeds)
4. **Unshallows git clone** so Nerdbank.GitVersioning can calculate version height

### Environment Detection

All scripts automatically detect if they're running in Claude Code web by checking for JWT proxy patterns:

```bash
if [[ "$HTTP_PROXY" =~ "jwt_" ]] && [[ "$HTTP_PROXY" =~ "@" ]]; then
    # Claude Code web environment
fi
```

**On local machines**, the scripts do nothing - they're safe to run anywhere.

### Troubleshooting

| Issue | Solution |
|-------|----------|
| Proxy errors | Check `/tmp/px-proxy.log` |
| NuGet restore fails | Verify proxy: `pgrep -f px` |
| GitVersioning errors | Run `git fetch --unshallow origin` |
| MyGet warnings | Safe to ignore - nuget.org is used instead |

### How It Works

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
