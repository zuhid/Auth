export class UsersPage {
  constructor(page) {
    this.page = page;
    this.addUserButton = page.locator('#add_user');
    this.searchInput = page.getByRole('searchbox');
    this.emailInput = page.locator('#email');
    this.firstNameInput = page.locator('#first_name');
    this.lastNameInput = page.locator('#last_name');
    this.phoneInput = page.locator('#phone');
    this.saveButton = page.getByRole('button', { name: 'Save' });
    this.cancelButton = page.getByRole('button', { name: 'Cancel' });
  }

  async goto() {
    await this.page.goto('/users');
  }

  async fillAddForm({ email, firstName, lastName, phone }) {
    if (email !== undefined) await this.emailInput.fill(email);
    if (firstName !== undefined) await this.firstNameInput.fill(firstName);
    if (lastName !== undefined) await this.lastNameInput.fill(lastName);
    if (phone !== undefined) await this.phoneInput.fill(phone);
  }

  // The table is paginated, so narrow it to the row(s) of interest before asserting.
  async search(text) {
    await this.searchInput.fill(text);
  }

  userRow(email) {
    return this.page.locator('tr').filter({ hasText: email });
  }

  // POST /User returns no body, so read the new user's id from its row's Edit button (id="edit_<guid>").
  async userIdFor(email) {
    const editId = await this.userRow(email).locator('[id^="edit_"]').getAttribute('id');
    return editId.slice('edit_'.length);
  }

  editButton(userId) {
    return this.page.locator(`#edit_${userId}`);
  }

  toggleButton(userId) {
    return this.page.locator(`#toggle_${userId}`);
  }
}
