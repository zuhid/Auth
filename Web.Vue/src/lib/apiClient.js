import { toasterStore } from "./toasterStore.js";
import { storage } from "./storage.js";

class ApiClient {
  constructor(baseURL, defaultHeaders = {}) {
    this.baseURL = baseURL;
    this.defaultHeaders = {
      'Content-Type': 'application/json',
      ...defaultHeaders
    };
  }

  get = async (endpoint, headers = {}) => this.#request(endpoint, 'GET', null, headers);
  post = async (endpoint, body, headers = {}) => this.#request(endpoint, 'POST', body, headers);
  put = async (endpoint, body, headers = {}) => this.#request(endpoint, 'PUT', body, headers);
  delete = async (endpoint, headers = {}) => this.#request(endpoint, 'DELETE', null, headers);

  async #request(endpoint, method = 'GET', body = null, headers = {}) {
    const url = `${this.baseURL}/${endpoint}`;
    const token = storage.token;
    const authHeader = token ? { 'Authorization': `Bearer ${token}` } : {};
    const options = { method, headers: { ...this.defaultHeaders, ...authHeader, ...headers } };
    if (body) { options.body = JSON.stringify(body); }

    try {
      const response = await fetch(url, options);
      if (response.ok) {
        return await response.json().catch(() => ({}));
      }

      const errorData = await response.json().catch(() => ({}));
      let message = errorData.message || response.statusText;

      switch (response.status) {
        case 400:
          const messages = [];
          Object.keys(errorData).forEach(key => messages.push(...errorData[key]));
          if (messages.length > 0) {
            message = messages.map(msg => `${msg}`).join('\n');
          }
          toasterStore.warning(message);
          break;
        case 401:
          toasterStore.error(`Unauthorized: ${message}`);
          break;
        case 500:
          toasterStore.error(`Internal Server Error: ${message}`);
          break;
        default:
          toasterStore.error(`Error ${response.status}: ${message}`);
      }
      return;
    } catch (error) {
      toasterStore.error(`Failed to ${method.toLowerCase()} ${endpoint}.`);
    }
  }
}

export const apiClient = new ApiClient(import.meta.env.VITE_API_BASE_URL)
