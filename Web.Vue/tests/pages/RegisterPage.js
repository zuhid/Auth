export class RegisterPage {
  constructor(page) {
    this.page = page;
    this.heading = page.locator('.card header');
    this.emailInput = page.locator('#email');
    this.passwordInput = page.locator('#password');
    this.firstNameInput = page.locator('#first_name');
    this.lastNameInput = page.locator('#last_name');
    this.phoneInput = page.locator('#phone');
    this.registerButton = page.getByRole('button', { name: 'Register' });
    this.loginLink = page.getByRole('link', { name: 'Login' });
  }

  async goto() {
    await this.page.goto('/account/register');
  }

  async fill({ email, password, firstName, lastName, phone }) {
    if (email !== undefined) await this.emailInput.fill(email);
    if (password !== undefined) await this.passwordInput.fill(password);
    if (firstName !== undefined) await this.firstNameInput.fill(firstName);
    if (lastName !== undefined) await this.lastNameInput.fill(lastName);
    if (phone !== undefined) await this.phoneInput.fill(phone);
  }

  async register() {
    await this.registerButton.click();
  }

  async interceptRegisterRequest(route) {
    const body = route.request().postDataJSON();
    await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
    return body;
  }
}
