export class AccountPage {
  constructor(page) {
    this.page = page;
    this.enableAuthenticatorButton = page.locator('#enable_ms_authenticator');
    this.qrCode = page.locator('img[alt="QR Code"]');
    this.verificationCodeInput = page.locator('#verification_code');
    this.verifyAndEnableButton = page.getByRole('button', { name: 'Verify & Enable' });
    this.twoFactorEnabledMessage = page.getByText('Two-factor authentication is enabled.');
  }

  async getSharedKey() {
    const text = await this.page.locator('p:has(b)').filter({ hasText: 'Key:' }).textContent();
    return text.replace('Key:', '').trim();
  }
}
