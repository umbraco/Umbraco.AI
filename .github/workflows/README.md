# GitHub Actions Workflows

This directory contains automated workflows for the Umbraco.AI repository.

## Workflows

### 🏷️ Auto Labeler (`auto-labeler.yml`)
Automatically applies component labels to issues based on the selected component in the issue template.

### 🔄 Sync to Azure DevOps (`sync-to-azure-devops.yml`)
Syncs GitHub issues to Azure DevOps backlog when labeled with `state/sprint-candidate`.

**Configuration Required:**
- **Secret:** `AZURE_DEVOPS_PAT` - Personal Access Token for Azure DevOps API
  - Organization: `umbraco`
  - Project: `D-Team Tracker`
  - Team: `AI Team`
  - Required permissions: Work Items (Read, Write, & Manage)

**How it works:**
1. Triggers when an issue is labeled with `state/sprint-candidate`
2. Creates a work item in Azure DevOps (D-Team Tracker / AI Team):
   - **Bug** if the GitHub issue has a `bug` label
   - **User Story** otherwise
3. Tags the work item with `Umbraco AI`
4. Links the GitHub issue to the work item
5. Posts a comment on the GitHub issue with the work item link

**To set up the PAT:**
1. Go to Azure DevOps → User Settings → Personal Access Tokens
2. Create a new token with:
   - Organization: `umbraco`
   - Scopes: `Work Items (Read, Write, & Manage)`
3. Add the token as a repository secret named `AZURE_DEVOPS_PAT`

### 🤖 Claude Code Review (`claude-code-review.yml`)
Automated code review using Claude AI on pull requests.

### 🔧 Claude (`claude.yml`)
Claude Code integration workflow.

## Adding New Workflows

When creating new workflows:
1. Use descriptive emoji prefixes in workflow names (🏷️ 🔄 🤖 ✅ 🧪)
2. Document required secrets and permissions
3. Add error handling and logging
4. Update this README with workflow description
