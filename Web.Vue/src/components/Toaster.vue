<script setup>
import { toasterStore } from '../lib/toasterStore';
</script>

<template>
  <div class="toaster">
    <TransitionGroup name="toast">
      <div v-for="toast in toasterStore.toasts" :key="toast.id" :class="['toast', toast.type]">
        <span class="message">{{ toast.message }}</span>
        <button class="close-btn" @click="toasterStore.dismiss(toast.id)">&times;</button>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toaster {
  position: fixed;
  top: 20px;
  right: 20px;
  z-index: 1000;
  display: flex;
  flex-direction: column;
  gap: var(--gap-lg);
}

.toast {
  padding: 12px 20px;
  border-radius: var(--radius);
  color: var(--primary-color-text);
  box-shadow: var(--shadow-popup);
  min-width: 200px;
  max-width: 400px;
  word-break: break-word;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--gap-lg);
}

.message {
  flex: 1;
  white-space: pre-line;
}

.close-btn {
  background: none;
  border: none;
  color: var(--primary-color-text);
  font-size: 20px;
  line-height: 1;
  cursor: pointer;
  padding: 0;
  opacity: 0.7;
}

.close-btn:hover {
  opacity: 1;
}

.error {
  background-color: var(--toast-error);
}

.success {
  background-color: var(--toast-success);
}

.info {
  background-color: var(--toast-info);
}

.warning {
  background-color: var(--toast-warning);
}

.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s ease;
}

.toast-enter-from {
  opacity: 0;
  transform: translateX(30px);
}

.toast-leave-to {
  opacity: 0;
  transform: translateX(30px);
}
</style>
