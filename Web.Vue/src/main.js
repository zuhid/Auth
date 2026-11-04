import { apiClient } from '@/lib';
import router from '@/router';
import { registerSW } from 'virtual:pwa-register';
import { createApp } from 'vue';
import App from './App.vue';
import './style.css';

function registerGlobalComponents(app) {
  const components = import.meta.glob('./components/*.vue', { eager: true })
  Object.entries(components).forEach(([path, module]) => {
    const name = path.split('/').pop().replace('.vue', '')
    app.component(name, module.default)
  })
}

const app = createApp(App);
registerSW();
registerGlobalComponents(app)
app.use(router);
app.provide('apiClient', apiClient)
app.mount('#app');

