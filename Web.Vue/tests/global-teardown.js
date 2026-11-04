import { execSync } from 'child_process';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default async function globalTeardown() {
  if (!process.env.COVERAGE) return;

  const nycOutputDir = path.join(__dirname, '..', '.nyc_output');
  const files = fs.existsSync(nycOutputDir) ? fs.readdirSync(nycOutputDir) : [];
  if (files.length === 0) {
    console.log('\nNo coverage data collected.');
    return;
  }

  console.log(`\nMerging ${files.length} coverage file(s) and generating report...`);
  execSync('npx nyc report', {
    stdio: 'inherit',
    cwd: path.join(__dirname, '..'),
  });
  console.log('\nCoverage report: coverage/index.html');
}
