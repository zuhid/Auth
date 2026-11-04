import { createRouter, createWebHistory } from 'vue-router';
import { storage } from '../lib/index.js';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/account', meta: { requiresAuth: false },
      children: [
        { path: '/account/register', component: () => import('../views/account/Register.vue') },
        { path: '/account/verify-email/:email/:token(.*)', props: true, component: () => import('../views/account/VerifyEmail.vue') },
        { path: '/account/login', component: () => import('../views/account/Login.vue') },
        { path: '', redirect: '/account/login' }
      ]
    },

    {
      path: '/', component: () => import('../views/Index.vue'), meta: { requiresAuth: true },
      children: [
        { path: 'dashboard', component: () => import('../views/Dashboard.vue') },
        { path: 'account/account/:id', props: true, component: () => import('../views/account/Account.vue') },
        { path: 'users', component: () => import('../views/Users.vue') },
        { path: '', redirect: 'dashboard' }
      ]
    },
    { path: '/:pathMatch(.*)*', redirect: '/' } // Catch-all route (must be LAST)
  ],
})

router.beforeEach((to) => {
  const isAuthenticated = !!storage.token
  const requiresAuth = to.meta.requiresAuth
  if (requiresAuth && !isAuthenticated) {
    return '/account/login'
  }
})

export default router
