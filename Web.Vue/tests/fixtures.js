import { expect, test as base } from '@playwright/test';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const coverageDir = path.join(__dirname, '..', '.nyc_output');

export const test = base.extend({
  page: async ({ page }, use, testInfo) => {
    await use(page);

    if (!process.env.COVERAGE) return;

    const coverage = await page.evaluate(() => window.__coverage__ ?? null).catch(() => null);
    if (!coverage) return;

    fs.mkdirSync(coverageDir, { recursive: true });
    const safeName = testInfo.title.replace(/[^a-z0-9]/gi, '_').toLowerCase().slice(0, 60);
    const file = path.join(coverageDir, `${safeName}_w${testInfo.workerIndex}_${Date.now()}.json`);
    fs.writeFileSync(file, JSON.stringify(coverage));
  },
});

export { expect };
