#!/bin/sh
# Custom merge driver for release-manifest.json
# Preserves the file on release/hotfix branches, uses default merge elsewhere
#
# Arguments: %O %A %B %L %P
#   %O = ancestor's version
#   %A = current version
#   %B = other branch's version
#   %L = conflict marker size
#   %P = file path

current_branch=$(git rev-parse --abbrev-ref HEAD)

# If on release or hotfix branch, keep our version (exit 0 = successful merge, no changes)
if echo "$current_branch" | grep -qE '^(release|hotfix)/'; then
    # Keep our version (%A) by doing nothing - it's already in place
    exit 0
fi

# On other branches, use default merge behavior
# If the file was deleted in their branch (%B is empty or doesn't exist), delete it
if [ ! -s "$3" ]; then
    # File was deleted in the incoming branch, delete ours too
    rm -f "$2"
    exit 0
fi

# Otherwise, attempt a standard merge
cat "$3" > "$2"
exit 0
