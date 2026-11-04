import { ref } from 'vue';

const STORAGE_KEY = 'app-theme';
export const THEMES = ['light', 'dark'];

const stored = localStorage.getItem(STORAGE_KEY);
export const currentTheme = ref(THEMES.includes(stored) ? stored : THEMES[0]);

// Falls back to the default for any unknown id — a user row saved before a theme was
// retired still carries the old value, and it arrives here straight off the login response.
export function setTheme(name) {
  const theme = THEMES.includes(name) ? name : THEMES[0];
  currentTheme.value = theme;
  localStorage.setItem(STORAGE_KEY, theme);
  document.documentElement.setAttribute('data-theme', theme);
}

setTheme(currentTheme.value);
