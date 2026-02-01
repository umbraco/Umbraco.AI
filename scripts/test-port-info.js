#!/usr/bin/env node
// Test script for port info endpoint via named pipe
import http from 'http';
import { execSync } from 'child_process';

function getUniqueIdentifier() {
  const sanitize = (name) => name.replace(/[^a-zA-Z0-9\-_]/g, '') || 'default';

  try {
    const gitDir = execSync('git rev-parse --git-dir', { encoding: 'utf-8' }).trim();

    // Check if this is a worktree
    if (gitDir.includes('worktrees')) {
      const parts = gitDir.split(/[\\\/]/);
      const worktreeIndex = parts.findIndex(p => p === 'worktrees');
      if (worktreeIndex >= 0 && worktreeIndex + 1 < parts.length) {
        return sanitize(parts[worktreeIndex + 1]);
      }
    }

    // Main worktree - use branch name
    const branch = execSync('git branch --show-current', { encoding: 'utf-8' }).trim();
    return sanitize(branch || 'default');
  } catch {
    return 'default';
  }
}

const identifier = getUniqueIdentifier();
const pipeName = `umbraco-ai-demo-${identifier}`;
const socketPath = process.platform === 'win32'
  ? `\\\\.\\pipe\\${pipeName}`
  : `/tmp/${pipeName}`;

console.log(`Testing port info endpoint via named pipe: ${pipeName}`);

http.get({ socketPath, path: '/port-info' }, (res) => {
  let data = '';
  res.setEncoding('utf8');
  res.on('data', (chunk) => data += chunk);
  res.on('end', () => {
    if (res.statusCode === 200) {
      const portInfo = JSON.parse(data);
      console.log('\nPort Info:', JSON.stringify(portInfo, null, 2));
      console.log(`\nSuccess! Demo site is running on port ${portInfo.port}`);
    } else {
      console.error(`\nError: HTTP ${res.statusCode} ${res.statusMessage}`);
      console.error(data);
    }
  });
}).on('error', (err) => {
  console.error(`\nError: ${err.message}`);
  console.error('Make sure the demo site is running with: /demo-site-management start');
});
