<script lang="ts">
  import CircularProgress from './CircularProgress.svelte';
  import type { UsageLimitInfo } from '$lib/types';

  let { limit }: { limit: UsageLimitInfo } = $props();

  // LimitType별 색상 매핑 (Rust의 LimitType::color_for_percentage와 동일)
  const colorMap: Record<string, [string, string, string]> = {
    fiveHour: ['#4CAF50', '#FF9800', '#F44336'],
    sevenDay: ['#CE93D8', '#9C27B0', '#E91E63'],
    extra: ['#F48FB1', '#E91E63', '#AD1457'],
    opus: ['#FFB74D', '#FFA000', '#E65100'],
    sonnet: ['#64B5F6', '#1976D2', '#283593'],
  };

  const labelMap: Record<string, string> = {
    fiveHour: '5-Hour',
    sevenDay: '7-Day',
    opus: 'Opus',
    sonnet: 'Sonnet',
    extra: 'Extra',
  };

  const pct = $derived(limit.percentage * 100);
  const color = $derived.by(() => {
    const colors = colorMap[limit.limitType] || ['#4CAF50', '#FF9800', '#F44336'];
    if (pct < 50) return colors[0];
    if (pct < 75) return colors[1];
    return colors[2];
  });
</script>

<CircularProgress
  percentage={pct}
  {color}
  label={labelMap[limit.limitType] || limit.limitType}
/>
