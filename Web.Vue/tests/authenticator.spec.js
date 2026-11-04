import { expect, test } from './fixtures.js';
import { AccountPage } from './pages/AccountPage.js';
import { LoginPage } from './pages/LoginPage.js';
import { RegisterPage } from './pages/RegisterPage.js';
import { random } from './random.js';
import { computeTotp } from './totp.js';
import { waitForVerifyPath } from './helpers/mail.js';


async function registerVerifyAndLogin(page, request) {
  // Use a plain ASCII email so the API accepts it and MailHog recipient search is unambiguous
  const email = `test_${Date.now()}_${Math.random().toString(36).slice(2, 6)}@example.com`;
  const user = { email, password: 'Secure123!', firstName: 'Test', lastName: 'User', phone: random.phone() };


  const registerPage = new RegisterPage(page);
  await registerPage.goto();
  await registerPage.fill(user);

  // Wait for the API response so failures surface immediately rather than timing out
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
  await loginPage.fill({ email: user.email, password: user.password });
  await loginPage.login();
  await expect(page).toHaveURL(/\/account\/account\//);
}

test.describe('Microsoft Authenticator', () => {
  test('enable 2FA button is visible on account page', async ({ page, request }) => {
    await registerVerifyAndLogin(page, request);
    const accountPage = new AccountPage(page);
    await expect(accountPage.enableAuthenticatorButton).toBeVisible();
    await expect(accountPage.enableAuthenticatorButton).toContainText('Enable Microsoft Authenticator');
  });

  test('clicking the button shows QR code and shared key', async ({ page, request }) => {
    await registerVerifyAndLogin(page, request);
    const accountPage = new AccountPage(page);

    await accountPage.enableAuthenticatorButton.click();

    await expect(accountPage.qrCode).toBeVisible();
    const sharedKey = await accountPage.getSharedKey();
    expect(sharedKey.length).toBeGreaterThan(0);
  });

  test('submitting an invalid code shows an error', async ({ page, request }) => {
    await registerVerifyAndLogin(page, request);
    const accountPage = new AccountPage(page);

    await accountPage.enableAuthenticatorButton.click();
    await expect(accountPage.verificationCodeInput).toBeVisible();

    await accountPage.verificationCodeInput.fill('000000');
    await accountPage.verifyAndEnableButton.click();

    await expect(accountPage.twoFactorEnabledMessage).not.toBeVisible();
  });

  test('submitting a valid TOTP code enables 2FA', async ({ page, request }) => {
    await registerVerifyAndLogin(page, request);
    const accountPage = new AccountPage(page);

    await accountPage.enableAuthenticatorButton.click();
    await expect(accountPage.qrCode).toBeVisible();

    const sharedKey = await accountPage.getSharedKey();
    const code = computeTotp(sharedKey);

    await accountPage.verificationCodeInput.fill(code);
    await accountPage.verifyAndEnableButton.click();

    await expect(accountPage.twoFactorEnabledMessage).toBeVisible();
    await expect(accountPage.enableAuthenticatorButton).not.toBeVisible();
  });
});
