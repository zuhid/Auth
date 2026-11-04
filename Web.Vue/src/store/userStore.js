import { getAll, saveAll, clearStore } from '../lib/db.js';

export class UserStore {
  #apiClient;
  #url = "user"

  constructor(apiClient) {
    this.#apiClient = apiClient
  }

  async getAll(options) {
    const cached = await getAll('users');
    if (!cached) {
      const data = await this.#apiClient.get(this.#url);
      if (data) await saveAll('users', data);
    }
    return await getAll('users', options);
  }

  async create(model) {
    return await this.#apiClient.post(this.#url, model);
  }

  async update(id, model) {
    return await this.#apiClient.put(this.#url + `/${id}`, model);
  }

  async deactivate(id) {
    return await this.#apiClient.post(this.#url + `/${id}/deactivate`, {});
  }

  async activate(id) {
    return await this.#apiClient.post(this.#url + `/${id}/activate`, {});
  }

  async forceRefresh() {
    await clearStore('users');
    const data = await this.#apiClient.get(this.#url);
    if (data) await saveAll('users', data);
  }
}
