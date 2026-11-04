import { expect, test } from './fixtures.js';
import { LoginPage } from './pages/LoginPage.js';
import { RegisterPage } from './pages/RegisterPage.js';
import { UsersPage } from './pages/UsersPage.js';
import { random } from './random.js';
import { API_BASE, loginAsAdmin } from './helpers/admin.js';
import { waitForVerifyPath } from './helpers/mail.js';


async function registerVerifyAndLogin(page, request) {
  const email = `test_${Date.now()}_${Math.random().toString(36).slice(2, 6)}@example.com`;
  const user = { email, password: 'Secure123!', firstName: 'Test', lastName: 'User', phone: random.phone() };


  const registerPage = new RegisterPage(page);
  await registerPage.goto();
  await registerPage.fill(user);
  const [registerResponse] = await Promise.all([
    page.waitForResponse(r => r.url().toLowerCase().includes('/account/register') && r.request().method() === 'POST'),
    registerPage.register(),
  ]);
  expect(registerResponse.ok()).toBeTruthy();

  const verifyPath = await waitForVerifyPath(request, email);
  await page.goto(verifyPath);
  await expect(page.locator('.card')).toContainText('successfully verified');

  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.fill({ email: user.email, password: user.password });
  await loginPage.login();
  await expect(page).toHaveURL(/\/account\/account\//);

  return { email, password: user.password };
}

test.describe('Users page — access control', () => {
  test('Users link is not visible in nav for non-admin', async ({ page, request }) => {
    await registerVerifyAndLogin(page, request);
    await expect(page.locator('a[href="/users"]')).not.toBeVisible();
  });

  test('Navigating to /users as non-admin shows no data (API returns 403)', async ({ page, request }) => {
    await registerVerifyAndLogin(page, request);
    const [usersResponse] = await Promise.all([
      page.waitForResponse(r => r.url().toLowerCase().includes(`${API_BASE}/user`.toLowerCase()) && r.request().method() === 'GET'),
      page.goto('/users'),
    ]);
    expect(usersResponse.status()).toBe(403);
    // The table renders a single "No results." placeholder row when empty
    await expect(page.locator('tbody tr')).toHaveCount(1);
    await expect(page.locator('tbody td.empty')).toContainText('No results.');
  });
});

test.describe('Users page — admin CRUD', () => {
  test('Admin can see Users link in nav after login with admin token', async ({ page, request }) => {
    const token = await loginAsAdmin(request);

    await page.goto('/account/login');
    await page.evaluate((t) => localStorage.setItem('token', t), token);
    await page.goto('/dashboard');

    await expect(page.locator('a[href="/users"]')).toBeVisible();
  });

  test('Admin can add, edit, and deactivate users', async ({ page, request }) => {
    const token = await loginAsAdmin(request);

    await page.goto('/account/login');
    await page.evaluate((t) => localStorage.setItem('token', t), token);

    const usersPage = new UsersPage(page);
    await Promise.all([
      page.waitForResponse(r => r.url().toLowerCase().includes(`${API_BASE}/user`.toLowerCase()) && r.request().method() === 'GET'),
      usersPage.goto(),
    ]);

    // Add a new user
    const newEmail = `playwright_${Date.now()}@example.com`;
    await usersPage.addUserButton.click();
    await usersPage.fillAddForm({ email: newEmail, firstName: 'Play', lastName: 'Wright' });

    const [createResponse] = await Promise.all([
      page.waitForResponse(r => new URL(r.url()).pathname.toLowerCase() === '/user' && r.request().method() === 'POST'),
      usersPage.saveButton.click(),
    ]);
    expect(createResponse.ok()).toBeTruthy();

    // Verify the new user row appears (filter first: the table is paginated)
    await usersPage.search(newEmail);
    await expect(usersPage.userRow(newEmail)).toBeVisible();

    // Get the created user's ID from the row (POST /User returns no body)
    const userId = await usersPage.userIdFor(newEmail);

    // Edit the user
    await usersPage.editButton(userId).click();
    await usersPage.firstNameInput.fill('Updated');
    const [updateResponse] = await Promise.all([
      page.waitForResponse(r => new URL(r.url()).pathname.toLowerCase() === `/user/${userId}` && r.request().method() === 'PUT'),
      usersPage.saveButton.click(),
    ]);
    expect(updateResponse.ok()).toBeTruthy();

    // Deactivate the user
    const [deactivateResponse] = await Promise.all([
      page.waitForResponse(r => new URL(r.url()).pathname.toLowerCase() === `/user/${userId}/deactivate` && r.request().method() === 'POST'),
      usersPage.toggleButton(userId).click(),
    ]);
    expect(deactivateResponse.ok()).toBeTruthy();

    // Status badge should now show Inactive
    await expect(usersPage.userRow(newEmail).locator('.status-badge')).toContainText('Inactive');

    // Reactivate
    const [activateResponse] = await Promise.all([
      page.waitForResponse(r => new URL(r.url()).pathname.toLowerCase() === `/user/${userId}/activate` && r.request().method() === 'POST'),
      usersPage.toggleButton(userId).click(),
    ]);
    expect(activateResponse.ok()).toBeTruthy();
    await expect(usersPage.userRow(newEmail).locator('.status-badge')).toContainText('Active');
  });
});
