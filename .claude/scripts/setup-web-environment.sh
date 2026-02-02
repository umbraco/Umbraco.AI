#!/bin/bash
# Full setup script for Claude Code web environment
# This script prepares the environment for building the Umbraco.AI solution.
#
# What it does:
# 1. Installs .NET SDK 10.0 if not present (via apt)
# 2. Unshallows git clone for Nerdbank.GitVersioning
# 3. Sets up proxy for NuGet package restoration
#
# Usage: source .claude/scripts/setup-web-environment.sh
#
# This script only runs setup steps when in Claude Code web environment,
# so it's safe to source in any environment.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Check if we're in Claude Code web environment
is_claude_web_environment() {
    [[ "$HTTP_PROXY" =~ "jwt_" ]] && [[ "$HTTP_PROXY" =~ "@" ]]
}

# Install .NET SDK if not present
install_dotnet_sdk() {
    if command -v dotnet &> /dev/null; then
        echo ".NET SDK already installed: $(dotnet --version)"
        return 0
    fi

    echo "Installing .NET SDK 10.0..."

    # Check if apt is available
    if ! command -v apt-get &> /dev/null; then
        echo "Warning: apt-get not available, cannot install .NET SDK automatically"
        echo "Please install .NET SDK 10.0 manually"
        return 1
    fi

    # Update package list and install .NET SDK
    apt-get update -qq
    apt-get install -y -qq dotnet-sdk-10.0

    if command -v dotnet &> /dev/null; then
        echo ".NET SDK installed successfully: $(dotnet --version)"
    else
        echo "Error: .NET SDK installation failed"
        return 1
    fi
}

# Unshallow git clone if needed (for Nerdbank.GitVersioning)
unshallow_git_if_needed() {
    # Check if we're in a git repo
    if ! git rev-parse --is-inside-work-tree &> /dev/null; then
        echo "Not in a git repository, skipping unshallow"
        return 0
    fi

    # Check if it's a shallow clone
    if [[ -f "$(git rev-parse --git-dir)/shallow" ]]; then
        echo "Detected shallow clone, fetching full history for GitVersioning..."
        git fetch --unshallow origin 2>/dev/null || git fetch --depth=100 origin 2>/dev/null || true
        echo "Git history fetched"
    else
        echo "Git repository has full history"
    fi
}

# Main setup
if is_claude_web_environment; then
    echo "=== Claude Code Web Environment Setup ==="
    echo ""

    # Step 1: Install .NET SDK
    echo "[1/3] Checking .NET SDK..."
    install_dotnet_sdk
    echo ""

    # Step 2: Setup proxy (this also configures NuGet)
    echo "[2/3] Setting up proxy and NuGet configuration..."
    source "$SCRIPT_DIR/setup-dotnet-proxy.sh"
    echo ""

    # Step 3: Unshallow git
    echo "[3/3] Checking git repository..."
    unshallow_git_if_needed
    echo ""

    echo "=== Setup Complete ==="
    echo ""
    echo "You can now build with: dotnet build Umbraco.AI.sln"
else
    echo "Not in Claude Code web environment - no setup needed"
    echo "For local development, use the standard setup: /repo-setup"
fi
