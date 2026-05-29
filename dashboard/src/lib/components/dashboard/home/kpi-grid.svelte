<script lang="ts">
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import type { HomeKpi } from './types';

	let { kpis }: { kpis: HomeKpi[] } = $props();
</script>

<section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
	{#each kpis as kpi}
		<Card
			class={kpi.disabled
				? 'border-l-4 border-l-border/80 opacity-80'
				: kpi.positive
					? 'border-l-4 border-l-chart-2'
					: 'border-l-4 border-l-warning-600'}
		>
			<CardHeader>
				<CardDescription>{kpi.label}</CardDescription>
				<CardTitle class="text-2xl">{kpi.value}</CardTitle>
			</CardHeader>
			<CardContent>
				<p
					class={kpi.disabled
						? 'text-xs font-medium text-muted-foreground'
						: kpi.positive
						? 'text-xs font-medium text-chart-2'
						: 'text-xs font-medium text-warning-600'}
				>
					{kpi.trend}
				</p>
			</CardContent>
		</Card>
	{/each}
</section>
