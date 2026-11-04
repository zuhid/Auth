import { expect, test } from './fixtures.js';
import { API_BASE, loginAsAdmin } from './helpers/admin.js';
import { waitForVerifyPath } from './helpers/mail.js';

test.describe('Dashboard — user status pie chart', () => {
  test('Admin sees pie chart with active/inactive counts on dashboard', async ({ page, request }) => {
    const token = await loginAsAdmin(request);

    await page.goto('/account/login');
    await page.evaluate((t) => localStorage.setItem('token', t), token);

    const [statsResponse] = await Promise.all([
      page.waitForResponse(r => new URL(r.url()).pathname.toLowerCase() === '/dashboard/stats' && r.request().method() === 'GET'),
      page.goto('/dashboard'),
    ]);

    expect(statsResponse.ok()).toBeTruthy();
    const stats = await statsResponse.json();
    expect(typeof stats.active).toBe('number');
    expect(typeof stats.inactive).toBe('number');

    // Pie chart SVG should be visible
    const svg = page.locator('#user_status svg');
    await expect(svg).toBeVisible();

    // At least one pie slice path should be rendered (there is always at least 1 user)
    const paths = svg.locator('path');
    await expect(paths).not.toHaveCount(0);

    // Legend entries should show the correct counts
    const legend = page.locator('#user_status .legend');
    await expect(legend).toContainText(`Active`);
    await expect(legend).toContainText(`Inactive`);
    await expect(legend).toContainText(String(stats.active));
    await expect(legend).toContainText(String(stats.inactive));
  });

  test('Non-admin also sees dashboard (stats call returns 403, chart shows no data)', async ({ page, request }) => {
    // Log in via the register flow used in other specs
    const email = `dash_test_${Date.now()}@example.com`;
    const password = 'Secure123!';

    // Register
    const registerRes = await request.post(`${API_BASE}/account/register`, {
      data: { email, password, firstName: 'Dash', lastName: 'Test', phone: '5550001234' },
      headers: { 'Content-Type': 'application/json' },
    });
    expect(registerRes.ok()).toBeTruthy();

    const verifyPath = await waitForVerifyPath(request, email);
    await page.goto(verifyPath);
    await expect(page.locator('.card')).toContainText('successfully verified');

    // Login
    const loginRes = await request.post(`${API_BASE}/account/login`, {
      data: { email, password },
      headers: { 'Content-Type': 'application/json' },
    });
    const { token } = await loginRes.json();
    await page.goto('/account/login');
    await page.evaluate((t) => localStorage.setItem('token', t), token);

    // Non-admin navigating to /dashboard — stats returns 403, chart shows "No data"
    const [statsResponse] = await Promise.all([
      page.waitForResponse(r => new URL(r.url()).pathname.toLowerCase() === '/dashboard/stats' && r.request().method() === 'GET'),
      page.goto('/dashboard'),
    ]);
    expect(statsResponse.status()).toBe(403);

    await expect(page.locator('#user_status svg text')).toContainText('No data');
  });
});
