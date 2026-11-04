import { setTheme } from './themeStore.js';

export class AccountStore {
  #apiClient;
  #storage;
  #url = "account"

  constructor(apiClient, storage) {
    this.#apiClient = apiClient
    this.#storage = storage
  }

  async register(model) {
    return await this.#apiClient.post(this.#url + "/register", model);
  }

  async verifyEmail(email, token) {
    return await this.#apiClient.post(this.#url + "/verifyemail", { email, token });
  }

  async login(model) {
    const response = await this.#apiClient.post(this.#url + "/login", { email: model.email, password: model.password });
    if (response?.token) {
      this.#storage.token = response.token;
      if (response.theme) setTheme(response.theme);
      return { success: true };
    }
    if (response?.requiresTwoFactor) {
      return { requiresTwoFactor: true, userId: response.userId, providers: response.providers };
    }
    return { success: false };
  }

  async get() {
    return await this.#apiClient.get(this.#url);
  }

  async verifyPhone() {
    return await this.#apiClient.post(this.#url + "/verifyphone", {});
  }

  async confirmPhone(token) {
    return await this.#apiClient.post(this.#url + "/confirmphone", { token });
  }

  async sendTwoFactorToken(userId, provider) {
    return await this.#apiClient.post(this.#url + "/sendtwofactortoken", { userId, provider });
  }

  async loginTwoFactor(userId, code, provider) {
    const response = await this.#apiClient.post(this.#url + "/logintwofactor", { userId, code, provider });
    if (response?.token) {
      this.#storage.token = response.token;
      if (response.theme) setTheme(response.theme);
      return true;
    }
    return false;
  }

  async updateTheme(theme) {
    return await this.#apiClient.post(this.#url + "/updatetheme", { theme });
  }

  async getEnableTwoFactor() {
    return await this.#apiClient.get(this.#url + "/enabletwofactor");
  }

  async enableTwoFactor(code) {
    return await this.#apiClient.post(this.#url + "/enabletwofactor", { code });
  }

  logout() {
    this.#storage.clear();
  }
}
