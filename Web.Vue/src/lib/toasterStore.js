import { reactive } from 'vue';

class Toaster {
  #state = reactive({
    toasts: []
  });

  get toasts() {
    return this.#state.toasts;
  }

  success = (message, duration = 2) => this.#add(message, 'success', duration);
  info = (message, duration = 2) => this.#add(message, 'info', duration);
  warning = (message, duration = 5) => this.#add(message, 'warning', duration);
  error = (message, duration = 3) => this.#add(message, 'error', duration);
  dismiss = (id) => this.#remove(id);

  #add(message, type = 'error', duration = 3) {
    const id = Date.now();
    this.#state.toasts.push({ id, message, type, duration });
    if (duration > 0) {
      setTimeout(() => this.#remove(id), duration * 1000);
    }
  }

  #remove(id) {
    this.#state.toasts = this.#state.toasts.filter(t => t.id !== id);
  }
}

export const toasterStore = new Toaster();
