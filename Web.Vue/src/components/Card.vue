<script setup>
/************************************************** Props **************************************************/
const props = defineProps({
  title: { type: String, default: "" },
  saveText: { type: String, default: 'Save' },
  cancelText: { type: String, default: 'Cancel' }
});

/************************************************** Emits **************************************************/
defineEmits(["onSave", "onCancel"]);

/************************************************** Computed **************************************************/

/************************************************** functions **************************************************/
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <div class="card">
    <header>{{ title }}</header>
    <main class="card-content">
      <slot />
    </main>
    <footer class="card-actions">
      <slot name="footer">
        <Button :label="saveText" v-if="saveText.length > 0" @click="$emit('onSave')" />
        <Button :label="cancelText" v-if="cancelText.length > 0" @click="$emit('onCancel')" />
      </slot>
    </footer>
  </div>
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
.card {
  background-color: var(--background-color);
  border: var(--surface-border);
  box-shadow: var(--shadow-card);
  margin: var(--margin);
  border-radius: var(--radius);
  display: flex;
  flex-direction: column;

  header, main, footer {
    padding: var(--padding-section);
  }

  header {
    border-radius: var(--radius-top);
    background-color: var(--primary-color);
    color: var(--primary-color-text);
    font-weight: var(--font-weight-bold);
    font-size: larger;
  }

  main {
    flex: 1;
    overflow-y: auto;
    background-color: var(--background-color);
  }

  footer {
    border-radius: var(--radius-bottom);
    display: flex;
    justify-content: flex-end;
    align-items: center;
    gap: var(--gap-md);
  }
}
</style>
