<script setup>
import { computed } from 'vue';

const props = defineProps({
  data: { type: Array, required: true }, // [{ label, value, color }]
  size: { type: Number, default: 160 },
});

const total = computed(() => props.data.reduce((s, d) => s + d.value, 0));

function polarToXY(cx, cy, r, angleRad) {
  return { x: cx + r * Math.cos(angleRad), y: cy + r * Math.sin(angleRad) };
}

const slices = computed(() => {
  if (total.value === 0) return [];
  const cx = props.size / 2;
  const cy = props.size / 2;
  const r = props.size / 2 - 4;
  let startAngle = -Math.PI / 2;
  return props.data.map(d => {
    const angle = (d.value / total.value) * 2 * Math.PI;
    const endAngle = startAngle + angle;
    let path;
    if (angle >= 2 * Math.PI - 0.0001) {
      // Full circle: SVG arc can't go from a point to itself, so draw two semicircles
      const top = polarToXY(cx, cy, r, -Math.PI / 2);
      const bottom = polarToXY(cx, cy, r, Math.PI / 2);
      path = `M ${top.x.toFixed(2)} ${top.y.toFixed(2)} A ${r} ${r} 0 1 1 ${bottom.x.toFixed(2)} ${bottom.y.toFixed(2)} A ${r} ${r} 0 1 1 ${top.x.toFixed(2)} ${top.y.toFixed(2)} Z`;
    } else {
      const start = polarToXY(cx, cy, r, startAngle);
      const end = polarToXY(cx, cy, r, endAngle);
      const largeArc = angle > Math.PI ? 1 : 0;
      path = `M ${cx} ${cy} L ${start.x.toFixed(2)} ${start.y.toFixed(2)} A ${r} ${r} 0 ${largeArc} 1 ${end.x.toFixed(2)} ${end.y.toFixed(2)} Z`;
    }
    startAngle = endAngle;
    return { ...d, path };
  });
});
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <div class="pie-chart">
    <svg :width="size" :height="size" :viewBox="`0 0 ${size} ${size}`">
      <text v-if="total === 0" :x="size / 2" :y="size / 2" text-anchor="middle" dominant-baseline="middle" class="empty-label">No data</text>
      <path v-for="(slice, i) in slices" :key="i" :d="slice.path" :fill="slice.color" />
    </svg>
    <ul class="legend">
      <li v-for="(item, i) in data" :key="i">
        <span class="swatch" :style="{ background: item.color }" />
        {{ item.label }}: <strong>{{ item.value }}</strong>
      </li>
    </ul>
  </div>
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
.pie-chart {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
}

.legend {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  gap: 1.25rem;
  font-size: 0.9rem;
}

.swatch {
  display: inline-block;
  width: 12px;
  height: 12px;
  border-radius: 2px;
  margin-right: 4px;
  vertical-align: middle;
}

.empty-label {
  font-size: 0.85rem;
  fill: var(--text-muted);
}
</style>
