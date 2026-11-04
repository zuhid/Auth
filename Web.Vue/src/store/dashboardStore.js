export class DashboardStore {
  #apiClient;
  #url = "dashboard"

  constructor(apiClient) {
    this.#apiClient = apiClient
  }

  async getStats() {
    return await this.#apiClient.get(this.#url + '/stats');
  }
}
