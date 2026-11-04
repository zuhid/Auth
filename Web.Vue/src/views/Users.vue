<!-------------------------------------------------- script -------------------------------------------------->
<script setup>
import { onMounted, ref } from 'vue';
import { userStore } from '../store';

const tableRef = ref(null);
const showForm = ref(false);
const editingId = ref(null);

const form = ref({ email: '', firstName: '', lastName: '', phoneNumber: '' });

const columns = [
  { key: 'firstName', label: 'First Name' },
  { key: 'lastName', label: 'Last Name' },
  { key: 'email', label: 'Email' },
  { key: 'phoneNumber', label: 'Phone', sortValue: (row) => row.phoneNumber ?? '' },
  { key: 'status', label: 'Status', sortValue: (row) => row.isActive ? 'Active' : 'Inactive' },
  { key: 'actions', label: 'Actions', sortable: false },
];

onMounted(async () => {
  await tableRef.value?.reload();
});

function openAdd() {
  editingId.value = null;
  form.value = { email: '', firstName: '', lastName: '', phoneNumber: '' };
  showForm.value = true;
}

function openEdit(user) {
  editingId.value = user.id;
  form.value = { email: user.email, firstName: user.firstName ?? '', lastName: user.lastName ?? '', phoneNumber: user.phoneNumber ?? '' };
  showForm.value = true;
}

function cancelForm() {
  showForm.value = false;
}

async function saveUser() {
  if (editingId.value) {
    const result = await userStore.update(editingId.value, { email: form.value.email, firstName: form.value.firstName, lastName: form.value.lastName, phoneNumber: form.value.phoneNumber });
    if (result !== undefined) {
      await userStore.forceRefresh();
      await tableRef.value?.reload();
      showForm.value = false;
    }
  } else {
    const result = await userStore.create(form.value);
    if (result) {
      await userStore.forceRefresh();
      await tableRef.value?.reload();
      showForm.value = false;
    }
  }
}

async function toggleActive(user) {
  if (user.isActive) {
    const result = await userStore.deactivate(user.id);
    if (result !== undefined) {
      await userStore.forceRefresh();
      await tableRef.value?.reload();
    }
  } else {
    const result = await userStore.activate(user.id);
    if (result !== undefined) {
      await userStore.forceRefresh();
      await tableRef.value?.reload();
    }
  }
}
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <Card id="users" title="User Management" saveText="" cancelText="">
    <Table ref="tableRef" :store="userStore" :columns="columns" filterable>
      <template #toolbar>
        <Button id="add_user" label="Add" @click="openAdd" />
      </template>
      <template #firstName="{ row }">{{ row.firstName || '—' }}</template>
      <template #lastName="{ row }">{{ row.lastName || '—' }}</template>
      <template #phoneNumber="{ row }">{{ row.phoneNumber || '—' }}</template>
      <template #status="{ row }">
        <span :class="['status-badge', row.isActive ? 'active' : 'inactive']">
          {{ row.isActive ? 'Active' : 'Inactive' }}
        </span>
      </template>
      <template #actions="{ row }">
        <div class="actions">
          <Button :id="'edit_' + row.id" label="Edit" @click="openEdit(row)" />
          <Button :id="'toggle_' + row.id" :label="row.isActive ? 'Deactivate' : 'Activate'" @click="toggleActive(row)" />
        </div>
      </template>
    </Table>
  </Card>

  <Card v-if="showForm" :id="editingId ? 'edit_user_form' : 'add_user_form'" :title="editingId ? 'Edit User' : 'Add User'" saveText="Save" cancelText="Cancel" @onSave="saveUser" @onCancel="cancelForm">
    <Text v-if="!editingId" label="Email" v-model="form.email" />
    <Text label="First Name" v-model="form.firstName" />
    <Text label="Last Name" v-model="form.lastName" />
    <Text id="phone" label="Phone" v-model="form.phoneNumber" />
  </Card>
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
#users {
  min-width: 700px;
}

/* Mirrors Demo/Web's Badge.vue, kept as a local class because the e2e specs target `.status-badge`. */
.status-badge {
  display: inline-block;
  padding: var(--gap-xs) var(--gap-md);
  border-radius: 999px;
  font-size: var(--font-size-sm);
  font-weight: 600;
}

.status-badge.active {
  background-color: var(--success-bg);
  color: var(--success-text);
}

.status-badge.inactive {
  background-color: var(--danger-bg);
  color: var(--danger-text);
}

.actions {
  display: flex;
  gap: var(--gap-md);
}

#edit_user_form,
#add_user_form {
  width: 400px;
  margin-top: 0;
}
</style>
