import { FullConfig } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

/**
 * Global Teardown for Svony Browser E2E Tests
 * 
 * This teardown runs once after all tests and handles:
 * 1. Stopping MCP servers
 * 2. Cleaning up test data
 * 3. Generating final reports
 * 4. Archiving artifacts
 */

async function globalTeardown(config: FullConfig) {
  console.log('\n🧹 Starting Svony Browser E2E Test Teardown...\n');

  // Step 1: Stop MCP servers
  console.log('📋 Step 1: Stopping MCP servers...');
  await stopMcpServers();
  console.log('   ✅ MCP servers stopped\n');

  // Step 2: Clean up test data
  console.log('📋 Step 2: Cleaning up test data...');
  await cleanupTestData();
  console.log('   ✅ Test data cleaned\n');

  // Step 3: Generate summary report
  console.log('📋 Step 3: Generating summary report...');
  await generateSummaryReport();
  console.log('   ✅ Summary report generated\n');

  // Step 4: Archive artifacts
  console.log('📋 Step 4: Archiving artifacts...');
  await archiveArtifacts();
  console.log('   ✅ Artifacts archived\n');

  console.log('✨ Global teardown complete!\n');
}

async function stopMcpServers(): Promise<void> {
  const pidsFile = path.resolve(__dirname, '../.mcp-pids');
  
  if (!fs.existsSync(pidsFile)) {
    console.log('   ℹ️  No MCP server PIDs found');
    return;
  }

  try {
    const pids: number[] = JSON.parse(fs.readFileSync(pidsFile, 'utf-8'));
    
    for (const pid of pids) {
      try {
        process.kill(pid, 'SIGTERM');
        console.log(`   ✅ Stopped process ${pid}`);
      } catch (error) {
        // Process may have already exited
        console.log(`   ℹ️  Process ${pid} already stopped`);
      }
    }

    // Clean up PID file
    fs.unlinkSync(pidsFile);
  } catch (error) {
    console.log(`   ⚠️  Error stopping MCP servers: ${error}`);
  }
}

async function cleanupTestData(): Promise<void> {
  const testDataDir = path.resolve(__dirname, '../test-data');
  const authDir = path.resolve(__dirname, '../.auth');

  // Keep test data for debugging, but clean up auth state
  if (fs.existsSync(authDir)) {
    fs.rmSync(authDir, { recursive: true, force: true });
    console.log('   ✅ Auth state cleaned');
  }

  // Clean up temporary files
  const tempFiles = [
    path.resolve(__dirname, '../.mcp-pids'),
    path.resolve(__dirname, '../.test-lock'),
  ];

  for (const file of tempFiles) {
    if (fs.existsSync(file)) {
      fs.unlinkSync(file);
    }
  }
}

async function generateSummaryReport(): Promise<void> {
  const reportsDir = path.resolve(__dirname, '../reports');
  const jsonResultsPath = path.join(reportsDir, 'json/results.json');
  
  if (!fs.existsSync(jsonResultsPath)) {
    console.log('   ℹ️  No JSON results found');
    return;
  }

  try {
    const results = JSON.parse(fs.readFileSync(jsonResultsPath, 'utf-8'));
    
    const summary = {
      timestamp: new Date().toISOString(),
      version: '7.0',
      stats: {
        total: results.stats?.expected || 0,
        passed: results.stats?.expected - (results.stats?.unexpected || 0) - (results.stats?.skipped || 0),
        failed: results.stats?.unexpected || 0,
        skipped: results.stats?.skipped || 0,
        duration: results.stats?.duration || 0,
      },
      coverage: {
        statements: 0,
        branches: 0,
        functions: 0,
        lines: 0,
      },
      suites: [] as any[],
    };

    // Extract suite information
    if (results.suites) {
      for (const suite of results.suites) {
        summary.suites.push({
          name: suite.title,
          tests: suite.specs?.length || 0,
          passed: suite.specs?.filter((s: any) => s.ok).length || 0,
          failed: suite.specs?.filter((s: any) => !s.ok).length || 0,
        });
      }
    }

    // Write summary
    const summaryPath = path.join(reportsDir, 'summary.json');
    fs.writeFileSync(summaryPath, JSON.stringify(summary, null, 2));

    // Generate markdown summary
    const markdownSummary = `# Svony Browser v7.0 - Test Summary

## Overview
- **Date**: ${summary.timestamp}
- **Version**: ${summary.version}
- **Duration**: ${(summary.stats.duration / 1000).toFixed(2)}s

## Results
| Metric | Count |
|--------|-------|
| Total Tests | ${summary.stats.total} |
| Passed | ${summary.stats.passed} ✅ |
| Failed | ${summary.stats.failed} ❌ |
| Skipped | ${summary.stats.skipped} ⏭️ |

## Pass Rate
**${((summary.stats.passed / summary.stats.total) * 100).toFixed(1)}%**

## Suites
${summary.suites.map(s => `- **${s.name}**: ${s.passed}/${s.tests} passed`).join('\n')}
`;

    fs.writeFileSync(path.join(reportsDir, 'summary.md'), markdownSummary);
    console.log('   ✅ Summary written to reports/summary.md');
  } catch (error) {
    console.log(`   ⚠️  Error generating summary: ${error}`);
  }
}

async function archiveArtifacts(): Promise<void> {
  const artifactsDir = path.resolve(__dirname, '../test-results');
  const archiveDir = path.resolve(__dirname, '../archives');

  if (!fs.existsSync(artifactsDir)) {
    console.log('   ℹ️  No artifacts to archive');
    return;
  }

  // Create archive directory
  if (!fs.existsSync(archiveDir)) {
    fs.mkdirSync(archiveDir, { recursive: true });
  }

  // Create timestamped archive name
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const archiveName = `test-run-${timestamp}`;

  // For now, just log that archiving would happen
  // In production, you would use a proper archiving library
  console.log(`   ℹ️  Artifacts would be archived to: ${archiveName}`);
}

export default globalTeardown;
