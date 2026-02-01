---
name: web-environment-setup
description: Sets up Claude Code web environment for building the Umbraco.Ai solution. Installs .NET SDK, configures proxy for NuGet, and prepares git for GitVersioning. Use this when starting a new Claude Code web session to build the project.
allowed-tools: Bash, Read
---

# Web Environment Setup

You are helping set up the Umbraco.Ai repository for building in Claude Code web environment.

## Task

Run the web environment setup script to prepare the environment for .NET development.

## Workflow

1. **Run the setup script**:
   ```bash
   source /home/user/Umbraco.Ai/.claude/scripts/setup-web-environment.sh
   ```

2. **Verify the setup** by checking:
   - `dotnet --version` returns 10.x
   - Proxy environment variables are set
   - Git is not a shallow clone

3. **Test the build**:
   ```bash
   dotnet build Umbraco.Ai.sln
   ```

## What the Script Does

The setup script (`setup-web-environment.sh`) automatically:

1. **Installs .NET SDK 10.0** via apt if not present
2. **Configures px-proxy** to handle JWT-authenticated proxy for NuGet
3. **Creates user-level NuGet.Config** using only nuget.org (MyGet may not be accessible in sandbox)
4. **Unshallows git clone** so Nerdbank.GitVersioning can calculate version height

## Environment Detection

The script automatically detects if running in Claude Code web environment by checking for JWT proxy patterns in `HTTP_PROXY`. If not in web environment, it skips all setup steps, making it safe to run anywhere.

## Troubleshooting

- **Proxy issues**: Check `/tmp/px-proxy.log` for proxy errors
- **NuGet restore fails**: Verify proxy is running with `pgrep -f px`
- **GitVersioning errors**: Run `git fetch --unshallow origin` manually
- **Build warnings about MyGet**: These can be ignored; nuget.org is used instead
