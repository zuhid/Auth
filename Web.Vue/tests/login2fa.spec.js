import { expect, test } from './fixtures.js';
import { AccountPage } from './pages/AccountPage.js';
import { LoginPage } from './pages/LoginPage.js';
import { RegisterPage } from './pages/RegisterPage.js';
import { random } from './random.js';
import { computeTotp } from './totp.js';
import { waitForEmailCode, waitForSmsCode, waitForVerifyPath } from './helpers/mail.js';


// Register, verify email, login, enable 2FA, then clear token to simulate logout.
// Returns { email, password, sharedKey } so the caller can re-login with 2FA.
async function registerEnableAndLogout(page, request) {
  const email = `test_${Date.now()}_${Math.random().toString(36).slice(2, 6)}@example.com`;
  const password = 'Secure123!';
  const user = { email, password, firstName: 'Test', lastName: 'User', phone: random.phone() };


  const registerPage = new RegisterPage(page);
  await registerPage.goto();
  await registerPage.fill(user);
  const [registerResponse] = await Promise.all([
    page.waitForResponse(r => r.url().toLowerCase().includes('/account/register') && r.request().method() === 'POST'),
    registerPage.register(),
  ]);
  expect(registerResponse.ok(), 'Register API call failed').toBeTruthy();

  const verifyPath = await waitForVerifyPath(request, email);
  await page.goto(verifyPath);
  await expect(page.locator('.card')).toContainText('successfully verified');

  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.fill({ email, password });
  await loginPage.login();
  await expect(page).toHaveURL(/\/account\/account\//);

  const accountPage = new AccountPage(page);
  await accountPage.enableAuthenticatorButton.click();
  const sharedKey = await accountPage.getSharedKey();
  const setupCode = computeTotp(sharedKey);
  await accountPage.verificationCodeInput.fill(setupCode);
  await accountPage.verifyAndEnableButton.click();
  await expect(accountPage.twoFactorEnabledMessage).toBeVisible();

  await page.evaluate(() => localStorage.clear());

  return { email, password, sharedKey, phone: user.phone };
}

test.describe('Navigation and logout', () => {
  test('logout button clears session and redirects to login', async ({ page, request }) => {
    const email = `test_${Date.now()}_${Math.random().toString(36).slice(2, 6)}@example.com`;
    const password = 'Secure123!';
    const user = { email, password, firstName: 'Nav', lastName: 'Test', phone: random.phone() };


    const registerPage = new RegisterPage(page);
    await registerPage.goto();
    await registerPage.fill(user);
    await Promise.all([
      page.waitForResponse(r => r.url().toLowerCase().includes('/account/register') && r.request().method() === 'POST'),
      registerPage.register(),
    ]);

    const verifyPath = await waitForVerifyPath(request, email);
    await page.goto(verifyPath);
    await expect(page.locator('.card')).toContainText('successfully verified');

    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.fill({ email, password });
    await loginPage.login();
    await expect(page).toHaveURL(/\/account\/account\//);

    // Logout lives in the user dropdown — open it first
    await page.locator('#user-menu-btn').click();
    const logoutButton = page.locator('#logout');
    await expect(logoutButton).toBeVisible();
    await logoutButton.click();

    await expect(page).toHaveURL(/\/account\/login/);
    const token = await page.evaluate(() => localStorage.getItem('token'));
    expect(token).toBeNull();
  });
});

test.describe('2FA Login', () => {
  test('provider selection appears after login when 2FA is enabled', async ({ page, request }) => {
    const { email, password } = await registerEnableAndLogout(page, request);

    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.fill({ email, password });

    const [loginResponse] = await Promise.all([
      page.waitForResponse(r => r.url().toLowerCase().includes('/account/login') && r.request().method() === 'POST'),
      loginPage.login(),
    ]);
    expect(loginResponse.ok()).toBeTruthy();
    const loginData = await loginResponse.json();
    expect(loginData.requiresTwoFactor).toBe(true);

    await expect(page.locator('#authenticator_app')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Email' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Text Message' })).toBeVisible();
  });

  test('can complete 2FA login with authenticator app', async ({ page, request }) => {
    const { email, password, sharedKey } = await registerEnableAndLogout(page, request);

    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.fill({ email, password });
    await loginPage.login();

    await expect(page.locator('#authenticator_app')).toBeVisible();
    await page.locator('#authenticator_app').click();

    await expect(page.locator('#verification_code')).toBeVisible();

    const loginCode = computeTotp(sharedKey);
    await page.locator('#verification_code').fill(loginCode);

    const [twoFactorResponse] = await Promise.all([
      page.waitForResponse(r => r.url().toLowerCase().includes('/account/logintwofactor') && r.request().method() === 'POST'),
      page.getByRole('button', { name: 'Verify' }).click(),
    ]);
    expect(twoFactorResponse.ok()).toBeTruthy();
    await expect(page).toHaveURL(/\/account\/account\//);
  });

  test('can complete 2FA login with email code', async ({ page, request }) => {
    const { email, password } = await registerEnableAndLogout(page, request);

    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.fill({ email, password });
    await loginPage.login();

    await expect(page.getByRole('button', { name: 'Email' })).toBeVisible();

    const [sendResponse] = await Promise.all([
      page.waitForResponse(r => r.url().toLowerCase().includes('/account/sendtwofactortoken') && r.request().method() === 'POST'),
      page.getByRole('button', { name: 'Email' }).click(),
    ]);
    expect(sendResponse.ok()).toBeTruthy();

    await expect(page.locator('#verification_code')).toBeVisible();

    const code = await waitForEmailCode(request, email);

    await page.locator('#verification_code').fill(code);

    const [twoFactorResponse] = await Promise.all([
      page.waitForResponse(r => r.url().toLowerCase().includes('/account/logintwofactor') && r.request().method() === 'POST'),
      page.getByRole('button', { name: 'Verify' }).click(),
    ]);
    expect(twoFactorResponse.ok()).toBeTruthy();
    await expect(page).toHaveURL(/\/account\/account\//);
  });

  test('can complete 2FA login with phone (text message) code', async ({ page, request }) => {
    const { email, password, phone } = await registerEnableAndLogout(page, request);

    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.fill({ email, password });
    await loginPage.login();

    await expect(loginPage.phoneButton).toBeVisible();

    // SMS lands in the shared phone@test.com inbox; match on this user's phone number and send time
    const sentAt = new Date();
    const [sendResponse] = await Promise.all([
      page.waitForResponse(r => r.url().toLowerCase().includes('/account/sendtwofactortoken') && r.request().method() === 'POST'),
      loginPage.phoneButton.click(),
    ]);
    expect(sendResponse.ok()).toBeTruthy();

    await expect(loginPage.verificationCodeInput).toBeVisible();

    const code = await waitForSmsCode(request, phone, { since: sentAt });

    await loginPage.verificationCodeInput.fill(code);

    const [twoFactorResponse] = await Promise.all([
      page.waitForResponse(r => r.url().toLowerCase().includes('/account/logintwofactor') && r.request().method() === 'POST'),
      loginPage.verifyButton.click(),
    ]);
    expect(twoFactorResponse.ok()).toBeTruthy();
    await expect(page).toHaveURL(/\/account\/account\//);
  });
});
