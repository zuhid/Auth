import { expect, test } from './fixtures.js';
import { LoginPage } from './pages/LoginPage.js';
import { RegisterPage } from './pages/RegisterPage.js';
import { waitForVerifyPath } from './helpers/mail.js';


test('theme is saved to DB and restored after re-login', async ({ page, request }) => {
  const email = `test_theme_${Date.now()}_${Math.random().toString(36).slice(2, 6)}@example.com`;
  const password = 'Secure123!';

  // Step 1: Register
  const registerPage = new RegisterPage(page);
  await registerPage.goto();
  await registerPage.fill({ email, password, firstName: 'Theme', lastName: 'Test', phone: '5551234567' });
  const [registerResponse] = await Promise.all([
    page.waitForResponse(r => r.url().toLowerCase().includes('/account/register') && r.request().method() === 'POST'),
    registerPage.register(),
  ]);
  expect(registerResponse.ok(), 'Register API call failed').toBeTruthy();

  // Step 2: Verify email
  const verifyPath = await waitForVerifyPath(request, email);
  await page.goto(verifyPath);
  await expect(page.locator('.card')).toContainText('successfully verified');

  // Step 3: First login
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.fill({ email, password });
  await Promise.all([
    page.waitForResponse(r => r.url().toLowerCase().includes('/account/login') && r.request().method() === 'POST'),
    loginPage.login(),
  ]);
  await expect(page).toHaveURL(/\/account\/account\//);

  // Step 4: Switch theme to 'dark' and confirm the API saves it
  await page.locator('#user-menu-btn').click();
  const [updateResponse] = await Promise.all([
    page.waitForResponse(r => r.url().toLowerCase().includes('/account/updatetheme') && r.request().method() === 'POST'),
    page.locator('#theme-switcher').selectOption('dark'),
  ]);
  expect(updateResponse.ok(), 'UpdateTheme API call failed').toBeTruthy();
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');

  // Step 5: Logout (this also clears localStorage including app-theme)
  await page.locator('#logout').click();
  await expect(page).toHaveURL(/\/account\/login/);

  // Step 6: Navigate to login (fresh module load, no theme in localStorage)
  await loginPage.goto();

  // Step 7: Log back in
  await loginPage.fill({ email, password });
  await Promise.all([
    page.waitForResponse(r => r.url().toLowerCase().includes('/account/login') && r.request().method() === 'POST'),
    loginPage.login(),
  ]);
  await expect(page).toHaveURL(/\/account\/account\//);

  // Step 8: Verify the theme from DB was applied
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  await page.locator('#user-menu-btn').click();
  await expect(page.locator('#theme-switcher')).toHaveValue('dark');
});
