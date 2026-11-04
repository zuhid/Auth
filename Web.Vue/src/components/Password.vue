<script setup>
import { computed } from "vue";

/************************************************** Props **************************************************/
const model = defineModel({ type: [String, Number], default: "" });
const props = defineProps({
  id: { type: String, default: "" },
  label: { type: String, default: "" },
  disabled: { type: Boolean, default: false },
});

/************************************************** Emits **************************************************/
defineEmits(["blur"]);

/************************************************** Computed **************************************************/
const computedId = computed(() => props.id || props.label.toLowerCase().replace(/\s+/g, "_"));

/************************************************** functions **************************************************/
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <div class="field password">
    <label v-if="label" :for="computedId">{{ label }}</label>
    <input :id="computedId" type="password" :placeholder="label" :disabled="disabled" v-model="model" @blur="$emit('blur', $event)" />
  </div>
</template>
