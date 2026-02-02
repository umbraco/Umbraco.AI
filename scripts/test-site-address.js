#!/usr/bin/env node
// Test script for site address endpoint via named pipe
import http from 'http';
import { execSync } from 'child_process';

function getUniqueIdentifier() {
  const sanitize = (name) => name.replace(/[^a-zA-Z0-9\-_.]/g, '') || 'default';

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
const pipeName = `umbraco.demosite.${identifier}`;
const socketPath = process.platform === 'win32'
  ? `\\\\.\\pipe\\${pipeName}`
  : `/tmp/${pipeName}`;

console.log(`Testing site address endpoint via named pipe: ${pipeName}`);

http.get({ socketPath, path: '/site-address' }, (res) => {
  let data = '';
  res.setEncoding('utf8');
  res.on('data', (chunk) => data += chunk);
  res.on('end', () => {
    if (res.statusCode === 200) {
      console.log(`\nSite address: ${data}`);
      console.log('\nSuccess! Demo site is running.');
    } else {
      console.error(`\nError: HTTP ${res.statusCode} ${res.statusMessage}`);
      console.error(data);
    }
  });
}).on('error', (err) => {
  console.error(`\nError: ${err.message}`);
  console.error('Make sure the demo site is running with: /demo-site-management start');
});
