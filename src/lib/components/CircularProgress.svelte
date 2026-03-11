<script lang="ts">
  let {
    percentage = 0,
    color = '#4CAF50',
    size = 80,
    strokeWidth = 8,
    label = '',
  }: {
    percentage?: number;
    color?: string;
    size?: number;
    strokeWidth?: number;
    label?: string;
  } = $props();

  const radius = $derived((size - strokeWidth) / 2);
  const circumference = $derived(2 * Math.PI * radius);
  const offset = $derived(circumference - (percentage / 100) * circumference);
  const displayPct = $derived(Math.round(percentage));
</script>

<div class="circular-progress" style="width: {size}px; height: {size}px;">
  <svg viewBox="0 0 {size} {size}">
    <!-- 배경 트랙 -->
    <circle
      cx={size / 2}
      cy={size / 2}
      r={radius}
      fill="none"
      stroke="rgba(255,255,255,0.1)"
      stroke-width={strokeWidth}
    />
    <!-- 프로그레스 -->
    <circle
      cx={size / 2}
      cy={size / 2}
      r={radius}
      fill="none"
      stroke={color}
      stroke-width={strokeWidth}
      stroke-dasharray={circumference}
      stroke-dashoffset={offset}
      stroke-linecap="round"
      transform="rotate(-90 {size / 2} {size / 2})"
      class="progress-ring"
    />
    <!-- 퍼센트 텍스트 -->
    <text
      x={size / 2}
      y={size / 2 + 1}
      text-anchor="middle"
      dominant-baseline="middle"
      fill="white"
      font-size={size * 0.22}
      font-weight="bold"
    >
      {displayPct}%
    </text>
  </svg>
  {#if label}
    <div class="label">{label}</div>
  {/if}
</div>

<style>
  .circular-progress {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
  }
  .progress-ring {
    transition: stroke-dashoffset 0.5s ease;
  }
  .label {
    font-size: 11px;
    color: #999;
    text-align: center;
  }
</style>
