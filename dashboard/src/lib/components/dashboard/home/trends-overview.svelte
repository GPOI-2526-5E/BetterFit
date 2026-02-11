<script lang="ts">
	import TrendChartCard from './trend-chart-card.svelte';
	import type { HomeTrendCard } from './types';

	let {
		periodLabel,
		trendCards
	}: {
		periodLabel: string;
		trendCards: HomeTrendCard[];
	} = $props();
</script>

<section class="grid gap-4">
	<h2 class="text-lg font-semibold tracking-tight">{periodLabel}</h2>
	<div class="grid gap-4 xl:grid-cols-2">
		{#each trendCards as trendCard (trendCard.id)}
			<TrendChartCard metric={trendCard} />
		{/each}
	</div>
</section>
