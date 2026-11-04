<script setup>
import { computed } from "vue";

/************************************************** Props **************************************************/
const props = defineProps({
  id: { type: String, default: "" },
  label: { type: String, default: "" },
  disabled: { type: Boolean, default: false },
});

/************************************************** Emits **************************************************/
defineEmits(["click"]);

/************************************************** Computed **************************************************/
const computedId = computed(() => props.id || props.label.toLowerCase().replace(/\s+/g, "_"));

/************************************************** functions **************************************************/
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <button :id="computedId" :disabled="disabled" @click="$emit('click', $event)">
    <slot>{{ label }}</slot>
  </button>
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
button {
  background: var(--primary-color);
  color: var(--primary-color-text);
  border: none;
  padding: var(--gap-md) calc(var(--gap-md) * 2);
  border-radius: var(--radius);
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: var(--gap-sm);
}

button:hover:not(:disabled) {
  opacity: 0.8;
}

button:focus-visible {
  outline: none;
  box-shadow: var(--focus-ring);
}

button:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}
</style>
