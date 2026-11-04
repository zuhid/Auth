export class Storage {
  #tokenKey = "token";
  get token() { return localStorage.getItem(this.#tokenKey); }
  set token(value) { this.#set("token", value); }

  get userId() {
    const token = this.token;
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.nameid || payload.sub;
    } catch (e) {
      return null;
    }
  }

  get isAdmin() {
    const token = this.token;
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const role = payload.roles || payload.role;
      if (!role) return false;
      return Array.isArray(role) ? role.includes('Admin') : role === 'Admin';
    } catch (e) {
      return false;
    }
  }

  #set(key, value) {
    value ? localStorage.setItem(key, value) : localStorage.removeItem(key);
  }

  clear() {
    localStorage.clear();
    sessionStorage.clear();
  }
}

export const storage = new Storage();
