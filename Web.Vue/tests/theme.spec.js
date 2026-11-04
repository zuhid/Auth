import { expect, test } from './fixtures.js';

// Navigate to the app, set up localStorage, then go to an authenticated route.
// The router only checks !!storage.token, so any truthy string bypasses the guard.
async function goToAuthenticatedPage(page) {
  await page.goto('/account/login');
  await page.evaluate(() => {
    localStorage.removeItem('app-theme');
    localStorage.setItem('token', 'fake.jwt.token');
  });
  await page.goto('/dashboard');
}

async function openUserDropdown(page) {
  await page.locator('#user-menu-btn').click();
}

test.describe('Theme switcher', () => {
  test.beforeEach(async ({ page }) => {
    await goToAuthenticatedPage(page);
  });

  test('user menu button is visible in the navbar', async ({ page }) => {
    await expect(page.locator('#user-menu-btn')).toBeVisible();
  });

  test('theme switcher is visible after opening user menu', async ({ page }) => {
    await openUserDropdown(page);
    await expect(page.locator('#theme-switcher')).toBeVisible();
  });

  test('defaults to light theme', async ({ page }) => {
    await openUserDropdown(page);
    await expect(page.locator('#theme-switcher')).toHaveValue('light');
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
  });

  test('can switch to dark theme', async ({ page }) => {
    await openUserDropdown(page);
    await page.locator('#theme-switcher').selectOption('dark');
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
    const stored = await page.evaluate(() => localStorage.getItem('app-theme'));
    expect(stored).toBe('dark');
  });

  test('can switch to light theme', async ({ page }) => {
    await openUserDropdown(page);
    await page.locator('#theme-switcher').selectOption('light');
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
    const stored = await page.evaluate(() => localStorage.getItem('app-theme'));
    expect(stored).toBe('light');
  });

  test('can cycle through every theme', async ({ page }) => {
    await openUserDropdown(page);
    const switcher = page.locator('#theme-switcher');

    await switcher.selectOption('dark');
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');

    await switcher.selectOption('light');
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
  });

  test('offers exactly the themes the API accepts', async ({ page }) => {
    await openUserDropdown(page);
    await expect(page.locator('#theme-switcher option')).toHaveText(['Light', 'Dark']);
  });

  test('falls back to the default when a retired theme is stored', async ({ page }) => {
    await page.evaluate(() => localStorage.setItem('app-theme', 'material'));
    await page.reload();
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
    await openUserDropdown(page);
    await expect(page.locator('#theme-switcher')).toHaveValue('light');
  });

  test('theme choice persists across page reload', async ({ page }) => {
    await openUserDropdown(page);
    await page.locator('#theme-switcher').selectOption('dark');
    await page.reload();
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
    await openUserDropdown(page);
    await expect(page.locator('#theme-switcher')).toHaveValue('dark');
  });

  test('logout button is inside the user menu', async ({ page }) => {
    await openUserDropdown(page);
    await expect(page.locator('#logout')).toBeVisible();
  });

  test('dropdown closes when clicking outside', async ({ page }) => {
    await openUserDropdown(page);
    await expect(page.locator('#theme-switcher')).toBeVisible();
    await page.locator('.navbar-brand').click();
    await expect(page.locator('#theme-switcher')).not.toBeVisible();
  });
});
