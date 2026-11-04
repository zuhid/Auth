import { apiClient, storage, toasterStore } from "../lib/index.js";
import { AccountStore } from "./accountStore.js";
import { DashboardStore } from "./dashboardStore.js";
import { UserStore } from "./userStore.js";

export const accountStore = new AccountStore(apiClient, storage);
export const userStore = new UserStore(apiClient);
export const dashboardStore = new DashboardStore(apiClient);
export { storage, toasterStore };
export { currentTheme, setTheme, THEMES } from "./themeStore.js";

