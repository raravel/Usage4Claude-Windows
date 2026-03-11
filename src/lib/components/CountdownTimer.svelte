<!-- REVIEW: PASS -->
<script lang="ts">
  import { onMount } from 'svelte';

  let {
    resetsAt,
    timeFormat = 'system',
  }: {
    resetsAt: string; // ISO 8601
    timeFormat?: string;
  } = $props();

  let showAbsolute = $state(false);
  let now = $state(Date.now());
  let intervalId: ReturnType<typeof setInterval>;

  const remaining = $derived.by(() => {
    const resetTime = new Date(resetsAt).getTime();
    const diff = resetTime - now;
    if (diff <= 0) return 'Reset!';

    const hours = Math.floor(diff / 3600000);
    const minutes = Math.floor((diff % 3600000) / 60000);
    const seconds = Math.floor((diff % 60000) / 1000);

    if (hours > 0) return `${hours}h ${minutes}m ${seconds}s`;
    if (minutes > 0) return `${minutes}m ${seconds}s`;
    return `${seconds}s`;
  });

  const absoluteTime = $derived.by(() => {
    const date = new Date(resetsAt);
    if (timeFormat === 'twelveHour') {
      return date.toLocaleString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    } else if (timeFormat === 'twentyFourHour') {
      return date.toLocaleString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });
    }
    // system: 시스템 로케일 사용
    return date.toLocaleTimeString();
  });

  function toggle() {
    showAbsolute = !showAbsolute;
  }

  onMount(() => {
    intervalId = setInterval(() => {
      now = Date.now();
    }, 1000);
    return () => clearInterval(intervalId);
  });
</script>

<button class="countdown" onclick={toggle} title="클릭하여 전환">
  {showAbsolute ? absoluteTime : remaining}
</button>

<style>
  .countdown {
    background: none;
    border: none;
    color: #999;
    cursor: pointer;
    font-size: 11px;
    padding: 2px 4px;
    border-radius: 4px;
  }
  .countdown:hover {
    background: rgba(255, 255, 255, 0.1);
    color: #ccc;
  }
</style>
