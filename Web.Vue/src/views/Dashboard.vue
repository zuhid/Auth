<!-------------------------------------------------- script -------------------------------------------------->
<script setup>
import { computed, onMounted, ref } from 'vue';
import { dashboardStore } from '../store';
import PieChart from '../components/PieChart.vue';

const stats = ref({ active: 0, inactive: 0 });

const pieData = computed(() => [
  { label: 'Active', value: stats.value.active, color: '#10b981' },
  { label: 'Inactive', value: stats.value.inactive, color: '#ef4444' },
]);

onMounted(async () => {
  const result = await dashboardStore.getStats();
  if (result) stats.value = result;
});
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <Card id="user_status" title="User Status" saveText="" cancelText="">
    <PieChart :data="pieData" :size="180" />
  </Card>
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
#user_status {
  width: 280px;
}
</style>
