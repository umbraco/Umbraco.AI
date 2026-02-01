#!/bin/bash
# Setup proxy for .NET/NuGet in Claude Code web environment
# This script detects if we're in the Claude Code web environment and sets up
# px-proxy to handle the JWT-authenticated proxy for .NET tools.
#
# It also configures NuGet to use only nuget.org since MyGet and other feeds
# may not be in the sandbox allowlist.

set -e

# Function to configure NuGet for sandbox environment
configure_nuget_for_sandbox() {
    # Create user-level NuGet config that restricts to nuget.org only
    # This works around MyGet/other feeds not being in the sandbox allowlist
    local NUGET_CONFIG_DIR="$HOME/.nuget/NuGet"
    local NUGET_CONFIG="$NUGET_CONFIG_DIR/NuGet.Config"

    # Only create if it doesn't exist (don't overwrite user config)
    if [[ ! -f "$NUGET_CONFIG" ]]; then
        mkdir -p "$NUGET_CONFIG_DIR"
        cat > "$NUGET_CONFIG" << 'NUGETEOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
NUGETEOF
        echo "Created NuGet config using nuget.org only (sandbox mode)"
    else
        echo "User NuGet config already exists, skipping sandbox configuration"
    fi
}

# Check if we're in Claude Code web environment (look for JWT proxy pattern)
if [[ "$HTTP_PROXY" =~ "jwt_" ]] && [[ "$HTTP_PROXY" =~ "@" ]]; then
    echo "Detected Claude Code web environment with JWT proxy"

    # Configure NuGet to use only allowed sources
    configure_nuget_for_sandbox

    # Check if px-proxy is already running
    if pgrep -f "px.*--port=3128" > /dev/null 2>&1; then
        echo "px-proxy already running on port 3128"
        export http_proxy="http://127.0.0.1:3128"
        export https_proxy="http://127.0.0.1:3128"
        export HTTP_PROXY="http://127.0.0.1:3128"
        export HTTPS_PROXY="http://127.0.0.1:3128"
        # Don't exit - let the script continue (important when sourced)
        return 0 2>/dev/null || true
    fi

    # Check if px-proxy is installed
    if ! command -v px &> /dev/null; then
        echo "Installing px-proxy..."
        pip install --quiet px-proxy 2>/dev/null || pip install --quiet --user px-proxy 2>/dev/null
    fi

    # Extract upstream proxy details
    PROXY_URL="$HTTP_PROXY"
    UPSTREAM_HOST=$(echo "$PROXY_URL" | sed -E 's|http://([^:]+:[^@]+@)?([^/]+).*|\2|')
    CREDS=$(echo "$PROXY_URL" | sed -E 's|http://([^@]+)@.*|\1|')
    UPSTREAM_USER=$(echo "$CREDS" | cut -d: -f1)
    UPSTREAM_PASS=$(echo "$CREDS" | cut -d: -f2-)

    echo "Starting px-proxy for upstream: $UPSTREAM_HOST"

    # Start px-proxy in background
    export PX_PASSWORD="$UPSTREAM_PASS"
    nohup px --server="$UPSTREAM_HOST" --username="$UPSTREAM_USER" --auth=BASIC --port=3128 > /tmp/px-proxy.log 2>&1 &

    # Wait for px-proxy to start
    sleep 2

    # Verify px-proxy is running
    if pgrep -f "px.*--port=3128" > /dev/null 2>&1; then
        echo "px-proxy started successfully"

        # Export local proxy for .NET
        export http_proxy="http://127.0.0.1:3128"
        export https_proxy="http://127.0.0.1:3128"
        export HTTP_PROXY="http://127.0.0.1:3128"
        export HTTPS_PROXY="http://127.0.0.1:3128"

        echo "Proxy environment configured for .NET"
    else
        echo "Warning: px-proxy failed to start. Check /tmp/px-proxy.log"
        return 1 2>/dev/null || exit 1
    fi
else
    echo "Not in Claude Code web environment - no proxy setup needed"
fi

# Return success (for sourcing)
return 0 2>/dev/null || true
