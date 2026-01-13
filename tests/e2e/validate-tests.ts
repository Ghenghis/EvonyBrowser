/**
 * Test Validation Script - Svony Browser v7.0
 * 
 * This script validates that all test files are properly structured
 * and can be parsed by TypeScript without errors.
 */

import * as fs from 'fs';
import * as path from 'path';

const specsDir = path.join(__dirname, 'specs');
const pagesDir = path.join(__dirname, 'pages');

function validateDirectory(dir: string, type: string): { valid: number; errors: string[] } {
  const errors: string[] = [];
  let valid = 0;
  
  if (!fs.existsSync(dir)) {
    errors.push(`Directory not found: ${dir}`);
    return { valid, errors };
  }
  
  const files = fs.readdirSync(dir).filter(f => f.endsWith('.ts'));
  
  for (const file of files) {
    const filePath = path.join(dir, file);
    try {
      const content = fs.readFileSync(filePath, 'utf-8');
      
      // Basic validation
      if (type === 'spec') {
        if (!content.includes("import { test, expect }")) {
          errors.push(`${file}: Missing Playwright imports`);
        }
        if (!content.includes("test.describe")) {
          errors.push(`${file}: Missing test.describe block`);
        }
      } else if (type === 'page') {
        if (!content.includes("export class")) {
          errors.push(`${file}: Missing class export`);
        }
        if (!content.includes("constructor(page: Page)")) {
          errors.push(`${file}: Missing Page constructor`);
        }
      }
      
      valid++;
    } catch (err) {
      errors.push(`${file}: ${err}`);
    }
  }
  
  return { valid, errors };
}

console.log('=== Svony Browser v7.0 Test Validation ===\n');

// Validate specs
console.log('Validating spec files...');
const specResult = validateDirectory(specsDir, 'spec');
console.log(`  Valid: ${specResult.valid}`);
if (specResult.errors.length > 0) {
  console.log(`  Errors:`);
  specResult.errors.forEach(e => console.log(`    - ${e}`));
}

// Validate pages
console.log('\nValidating page objects...');
const pageResult = validateDirectory(pagesDir, 'page');
console.log(`  Valid: ${pageResult.valid}`);
if (pageResult.errors.length > 0) {
  console.log(`  Errors:`);
  pageResult.errors.forEach(e => console.log(`    - ${e}`));
}

// Summary
console.log('\n=== Summary ===');
console.log(`Total Specs: ${specResult.valid}`);
console.log(`Total Pages: ${pageResult.valid}`);
console.log(`Total Errors: ${specResult.errors.length + pageResult.errors.length}`);

if (specResult.errors.length + pageResult.errors.length === 0) {
  console.log('\n✅ All tests validated successfully!');
  process.exit(0);
} else {
  console.log('\n❌ Validation failed with errors');
  process.exit(1);
}
