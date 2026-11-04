import { expect, test } from './fixtures.js';
import { LoginPage } from './pages/LoginPage.js';
import { RegisterPage } from './pages/RegisterPage.js';
import { random } from './random.js';
import { waitForVerifyPath } from './helpers/mail.js';

const randomUser = () => ({
  email: random.email(),
  password: 'Secure123!',
  firstName: random.name(),
  lastName: random.name(),
  phone: random.phone(),
});

test.describe('Register screen', () => {
  let registerPage;

  test.beforeEach(async ({ page }) => {
    registerPage = new RegisterPage(page);
    await registerPage.goto();
  });

  test('shows a login link that navigates to the login page', async ({ page }) => {
    await expect(registerPage.loginLink).toBeVisible();
    await registerPage.loginLink.click();
    await expect(page).toHaveURL('/account/login');
  });

  test('submits the form with correct payload on register', async ({ page, request }) => {
    const user = randomUser();

    await registerPage.fill(user);
    await registerPage.register();

    const verifyPath = await waitForVerifyPath(request, user.email);

    // Navigate to the verify email link and assert success
    await page.goto(verifyPath);
    await expect(page.locator('.card')).toContainText('successfully verified');

    // Login
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.fill({ email: user.email, password: user.password });
    await loginPage.login();

    await expect(page).toHaveURL(/\/account\/account\//);
  });
});
