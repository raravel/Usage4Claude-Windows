<script lang="ts">
  import { t } from 'svelte-i18n';
  import CircularProgress from './CircularProgress.svelte';
  import CountdownTimer from './CountdownTimer.svelte';
  import type { UsageLimitInfo } from '$lib/types';
  import { settingsStore } from '$lib/state/settings.svelte';

  let { limit }: { limit: UsageLimitInfo } = $props();

  // LimitType별 색상 매핑 (Rust의 LimitType::color_for_percentage와 동일)
  const colorMap: Record<string, [string, string, string]> = {
    fiveHour: ['#4CAF50', '#FF9800', '#F44336'],
    sevenDay: ['#CE93D8', '#9C27B0', '#E91E63'],
    extra: ['#F48FB1', '#E91E63', '#AD1457'],
    opus: ['#FFB74D', '#FFA000', '#E65100'],
    sonnet: ['#64B5F6', '#1976D2', '#283593'],
  };

  const labelKeyMap: Record<string, string> = {
    fiveHour: 'usage.fiveHour',
    sevenDay: 'usage.sevenDay',
    opus: 'usage.opus',
    sonnet: 'usage.sonnet',
    extra: 'usage.extra',
  };

  const pct = $derived(limit.percentage * 100);
  const color = $derived.by(() => {
    const colors = colorMap[limit.limitType] || ['#4CAF50', '#FF9800', '#F44336'];
    if (pct < 50) return colors[0];
    if (pct < 75) return colors[1];
    return colors[2];
  });

  const label = $derived(
    labelKeyMap[limit.limitType] ? $t(labelKeyMap[limit.limitType]) : limit.limitType
  );
</script>

<div class="usage-row">
  <CircularProgress
    percentage={pct}
    {color}
    {label}
  />
  <CountdownTimer
    resetsAt={limit.resetsAt}
    timeFormat={settingsStore.data?.timeFormat ?? 'system'}
  />
</div>

<style>
  .usage-row {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
  }
</style>
