export class LoginPage {
  constructor(page) {
    this.page = page;
    this.emailInput = page.locator('#email');
    this.passwordInput = page.locator('#password');
    this.loginButton = page.getByRole('button', { name: 'Login' });
    this.registerLink = page.getByRole('link', { name: 'Register' });
    this.authenticatorAppButton = page.locator('#authenticator_app');
    this.twoFactorEmailButton = page.getByRole('button', { name: 'Email' });
    this.phoneButton = page.getByRole('button', { name: 'Text Message' });
    this.verificationCodeInput = page.locator('#verification_code');
    this.verifyButton = page.getByRole('button', { name: 'Verify' });
  }

  async goto() {
    await this.page.goto('/account/login');
  }

  async fill({ email, password }) {
    if (email !== undefined) await this.emailInput.fill(email);
    if (password !== undefined) await this.passwordInput.fill(password);
  }

  async login() {
    await this.loginButton.click();
  }
}
