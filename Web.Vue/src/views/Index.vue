<!-------------------------------------------------- script -------------------------------------------------->
<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { accountStore, currentTheme, setTheme, storage, THEMES } from '../store';

const isAdmin = computed(() => storage.isAdmin);

const router = useRouter();
const dropdownOpen = ref(false);
const dropdownRef = ref(null);

function logout() {
  accountStore.logout();
  dropdownOpen.value = false;
  router.push('/account/login');
}

function toggleDropdown() {
  dropdownOpen.value = !dropdownOpen.value;
}

function handleOutsideClick(e) {
  if (dropdownRef.value && !dropdownRef.value.contains(e.target)) {
    dropdownOpen.value = false;
  }
}

async function changeTheme(theme) {
  setTheme(theme);
  if (storage.token) {
    await accountStore.updateTheme(theme);
  }
}

onMounted(() => document.addEventListener('click', handleOutsideClick));
onBeforeUnmount(() => document.removeEventListener('click', handleOutsideClick));
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <nav class="navbar">
    <span class="navbar-brand">Auth</span>
    <div class="navbar-links">
      <router-link to="/dashboard">Dashboard</router-link>
      <router-link :to="`/account/account/${storage.userId}`">Account</router-link>
      <router-link v-if="isAdmin" to="/users">Users</router-link>
    </div>
    <div class="user-dropdown" ref="dropdownRef">
      <button id="user-menu-btn" class="user-menu-btn" @click="toggleDropdown" :aria-expanded="dropdownOpen">
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="8" r="4" />
          <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" />
        </svg>
      </button>
      <div v-if="dropdownOpen" class="dropdown-panel">
        <div class="dropdown-item theme-item">
          <span class="dropdown-label">Theme</span>
          <select id="theme-switcher" class="theme-select" @change="changeTheme($event.target.value)">
            <option v-for="t in THEMES" :key="t" :value="t" :selected="t === currentTheme">{{ t.charAt(0).toUpperCase() + t.slice(1) }}</option>
          </select>
        </div>
        <div class="dropdown-divider"></div>
        <button id="logout" class="dropdown-logout" @click="logout">Logout</button>
      </div>
    </div>
  </nav>
  <router-view />
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
.navbar {
  display: flex;
  align-items: center;
  background-color: var(--primary-color);
  color: var(--primary-color-text);
  padding: 0 1.5rem;
  height: 54px;
  box-shadow: var(--shadow-popup);
  position: sticky;
  top: 0;
  z-index: 100;
  gap: 0.75rem;
}

.navbar-brand {
  font-size: 1.15rem;
  font-weight: var(--font-weight-bold);
  letter-spacing: 0.04em;
  margin-right: 1rem;
  white-space: nowrap;
}

.navbar-links {
  display: flex;
  gap: 0.25rem;
  flex: 1;
}

.navbar-links a {
  color: var(--on-primary-muted);
  text-decoration: none;
  padding: 0.35rem 0.8rem;
  border-radius: var(--radius);
  font-size: 0.9rem;
  transition: background 0.15s, color 0.15s;
}

.navbar-links a:hover,
.navbar-links a.router-link-exact-active {
  background-color: var(--on-primary-hover);
  color: var(--primary-color-text);
}

.user-dropdown {
  position: relative;
}

.user-menu-btn {
  background: var(--on-primary-soft);
  color: var(--primary-color-text);
  border: 1px solid var(--on-primary-border);
  padding: 5px 10px;
  border-radius: var(--radius);
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: background 0.15s;
}

.user-menu-btn:hover,
.user-menu-btn[aria-expanded="true"] {
  background: var(--on-primary-strong);
}

.dropdown-panel {
  position: absolute;
  right: 0;
  top: calc(100% + 6px);
  background: var(--background-color);
  border: var(--border);
  border-radius: var(--radius);
  box-shadow: var(--shadow-popup);
  min-width: 180px;
  padding: 0.5rem 0;
  z-index: 200;
}

.dropdown-item {
  padding: 0.5rem 1rem;
}

.theme-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.dropdown-label {
  font-size: 0.88rem;
  color: var(--text-primary);
  white-space: nowrap;
}

.theme-select {
  background: var(--surface-color);
  color: var(--text-primary);
  border: var(--border);
  padding: 3px 6px;
  border-radius: var(--radius);
  cursor: pointer;
  font-size: 0.85rem;
  outline: none;
  flex: 1;
}

.dropdown-divider {
  height: 1px;
  background: var(--background-shadow);
  margin: 0.35rem 0;
}

.dropdown-logout {
  display: block;
  width: 100%;
  text-align: left;
  background: none;
  color: var(--text-primary);
  border: none;
  padding: 0.5rem 1rem;
  cursor: pointer;
  font-size: 0.88rem;
  transition: background 0.15s;
}

.dropdown-logout:hover {
  background: var(--surface-color);
}
</style>
