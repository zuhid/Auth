<script setup>
import { computed, ref, watch, watchEffect } from 'vue';

const props = defineProps({
  columns: { type: Array, required: true }, // [{ key, label, sortable?, sortValue? }]
  rows: { type: Array, default: () => [] },
  filterable: { type: Boolean, default: false },
  store: { type: Object, default: null },
});

const filterText = ref('');
const sortKey = ref(null);
const sortDir = ref('asc');
const currentPage = ref(1);
const perPage = ref(10);

// ── store path: store.getAll() does filter / sort / page ───────────────────
const queryResult = ref({ rows: [], total: 0 });
let _seq = 0;

async function runQuery() {
  if (!props.store) return;
  const seq = ++_seq;
  const result = await props.store.getAll({
    filterText: filterText.value,
    columns: props.columns,
    sortKey: sortKey.value,
    sortDir: sortDir.value,
    page: currentPage.value,
    perPage: perPage.value,
  });
  if (seq === _seq) queryResult.value = result ?? { rows: [], total: 0 };
}

watch([filterText, sortKey, perPage], () => { currentPage.value = 1; });
watch([filterText, sortKey, sortDir, perPage, currentPage, () => props.store], runQuery, { immediate: true });

async function reload() {
  currentPage.value = 1;
  await runQuery();
}
defineExpose({ reload });

// ── rows-prop path: local computation (backward compat when no store) ───────
const allRows = ref([]);
watchEffect(() => { if (!props.store) allRows.value = props.rows; });
watch(allRows, () => { if (!props.store) currentPage.value = 1; });

function getColValue(col, row) {
  return col.sortValue ? col.sortValue(row) : (row[col.key] ?? '');
}

const filteredSortedRows = computed(() => {
  if (props.store) return [];
  let result = allRows.value;
  if (filterText.value.trim()) {
    const q = filterText.value.trim().toLowerCase();
    result = result.filter(row =>
      props.columns.some(col => String(getColValue(col, row)).toLowerCase().includes(q))
    );
  }
  if (sortKey.value) {
    const col = props.columns.find(c => c.key === sortKey.value);
    const dir = sortDir.value === 'asc' ? 1 : -1;
    result = [...result].sort((a, b) => {
      const av = String(getColValue(col, a)).toLowerCase();
      const bv = String(getColValue(col, b)).toLowerCase();
      return av < bv ? -dir : av > bv ? dir : 0;
    });
  }
  return result;
});

// ── unified output ──────────────────────────────────────────────────────────
const totalFiltered = computed(() =>
  props.store ? queryResult.value.total : filteredSortedRows.value.length
);

const displayedRows = computed(() => {
  if (props.store) return queryResult.value.rows;
  const start = (currentPage.value - 1) * perPage.value;
  return filteredSortedRows.value.slice(start, start + perPage.value);
});

const totalPages = computed(() => Math.max(1, Math.ceil(totalFiltered.value / perPage.value)));

const pageInfo = computed(() => {
  const total = totalFiltered.value;
  if (total === 0) return '0 records';
  const start = (currentPage.value - 1) * perPage.value + 1;
  const end = Math.min(currentPage.value * perPage.value, total);
  return `${start}–${end} of ${total}`;
});

const pageNumbers = computed(() => {
  const total = totalPages.value;
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  const cur = currentPage.value;
  const pages = [1];
  if (cur > 3) pages.push('...');
  for (let p = Math.max(2, cur - 1); p <= Math.min(total - 1, cur + 1); p++) pages.push(p);
  if (cur < total - 2) pages.push('...');
  pages.push(total);
  return pages;
});

function setPage(p) { currentPage.value = Math.max(1, Math.min(p, totalPages.value)); }
function prevPage() { if (currentPage.value > 1) currentPage.value--; }
function nextPage() { if (currentPage.value < totalPages.value) currentPage.value++; }

function toggleSort(col) {
  if (col.sortable === false) return;
  if (sortKey.value === col.key) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc';
  } else {
    sortKey.value = col.key;
    sortDir.value = 'asc';
  }
}
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <div class="table-wrapper">
    <div v-if="filterable || $slots.toolbar" class="table-toolbar">
      <input v-if="filterable" v-model="filterText" class="filter-input" placeholder="Search..." type="search" />
      <div class="toolbar-actions">
        <slot name="toolbar" />
      </div>
    </div>
    <table class="table">
      <thead>
        <tr>
          <th v-for="col in columns" :key="col.key" :class="{ sortable: col.sortable !== false, sorted: sortKey === col.key }" @click="toggleSort(col)">
            {{ col.label }}
            <span v-if="col.sortable !== false" class="sort-icon">
              <span v-if="sortKey === col.key">{{ sortDir === 'asc' ? '▲' : '▼' }}</span>
              <span v-else class="sort-placeholder">⇅</span>
            </span>
          </th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(row, i) in displayedRows" :key="row.id ?? i">
          <td v-for="col in columns" :key="col.key">
            <slot :name="col.key" :row="row">{{ row[col.key] ?? '—' }}</slot>
          </td>
        </tr>
        <tr v-if="displayedRows.length === 0">
          <td :colspan="columns.length" class="empty">No results.</td>
        </tr>
      </tbody>
    </table>
    <div v-if="totalFiltered > 0" class="pagination">
      <span class="page-info">{{ pageInfo }}</span>
      <div v-if="totalPages > 1" class="page-nav">
        <button class="page-btn" :disabled="currentPage === 1" @click="prevPage">‹</button>
        <template v-for="p in pageNumbers" :key="p">
          <span v-if="p === '...'" class="page-ellipsis">…</span>
          <button v-else class="page-btn" :class="{ active: p === currentPage }" @click="setPage(p)">{{ p }}</button>
        </template>
        <button class="page-btn" :disabled="currentPage === totalPages" @click="nextPage">›</button>
      </div>
      <div v-else class="page-nav-spacer" />
      <div class="per-page">
        <select v-model.number="perPage" class="per-page-select">
          <option :value="10">10</option>
          <option :value="25">25</option>
          <option :value="50">50</option>
          <option :value="100">100</option>
        </select>
        <span>per page</span>
      </div>
    </div>
  </div>
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
.table-wrapper {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.table-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.toolbar-actions {
  display: flex;
  gap: 0.5rem;
}

.filter-input {
  padding: 0.4rem 0.75rem;
  border: var(--border);
  border-radius: var(--radius);
  background: var(--background-color);
  color: var(--text-primary);
  font-size: 0.875rem;
  width: 220px;
}

.filter-input:focus {
  outline: none;
  border-color: var(--primary-color);
}

.table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}

.table th,
.table td {
  padding: 0.5rem 0.75rem;
  text-align: left;
  border-bottom: var(--border);
}

.table th {
  font-weight: 600;
  background-color: var(--surface-color);
}

.table th.sortable {
  cursor: pointer;
  user-select: none;
}

.table th.sortable:hover {
  background-color: var(--background-shadow);
}

.table th.sorted {
  color: var(--primary-color);
}

.sort-icon {
  margin-left: 0.35rem;
  font-size: 0.7rem;
}

.sort-placeholder {
  opacity: 0.4;
}

.empty {
  text-align: center;
  padding: 1.5rem;
  opacity: 0.5;
}

.pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding-top: 0.25rem;
  font-size: 0.875rem;
}

.page-info {
  opacity: 0.65;
  min-width: 100px;
}

.page-nav {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.page-nav-spacer {
  flex: 1;
}

.page-btn {
  min-width: 2rem;
  height: 2rem;
  padding: 0 0.4rem;
  border: var(--border);
  border-radius: var(--radius);
  background: var(--surface-color);
  color: var(--text-primary);
  cursor: pointer;
  font-size: 0.875rem;
  line-height: 1;
}

.page-btn:hover:not(:disabled) {
  background: var(--background-shadow);
}

.page-btn.active {
  background: var(--primary-color);
  color: var(--primary-color-text);
  border-color: var(--primary-color);
}

.page-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.page-ellipsis {
  padding: 0 0.2rem;
  opacity: 0.45;
}

.per-page {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  opacity: 0.65;
}

.per-page-select {
  padding: 0.2rem 0.4rem;
  border: var(--border);
  border-radius: var(--radius);
  background: var(--surface-color);
  color: var(--text-primary);
  font-size: 0.875rem;
  cursor: pointer;
}
</style>
