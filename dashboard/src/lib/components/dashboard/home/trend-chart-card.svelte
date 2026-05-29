<script lang="ts">
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import * as m from '$lib/paraglide/messages.js';
	import type { HomeTrendCard } from './types';

	let { metric }: { metric: HomeTrendCard } = $props();

	const width = 320;
	const height = 160;
	const paddingX = 16;
	const paddingY = 14;

	const points = $derived(metric.points.length > 0 ? metric.points : [{ label: '', value: 0 }]);
	const values = $derived(points.map((point) => point.value));
	const maxValue = $derived(Math.max(...values));
	const minValue = $derived(Math.min(...values));
	const range = $derived(Math.max(maxValue - minValue, 1));
	const stepX = $derived(points.length > 1 ? (width - paddingX * 2) / (points.length - 1) : 0);
	const coords = $derived(
		points.map((point, index) => {
			const x = points.length === 1 ? width / 2 : paddingX + index * stepX;
			const y = height - paddingY - ((point.value - minValue) / range) * (height - paddingY * 2);
			return { ...point, x, y };
		})
	);
	const linePath = $derived(
		coords.map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`).join(' ')
	);
	const areaPath = $derived(
		coords.length > 0
			? `${linePath} L ${coords.at(-1)?.x ?? width / 2} ${height - paddingY} L ${coords[0]?.x ?? width / 2} ${height - paddingY} Z`
			: ''
	);
	const chartLabel = $derived(
		m.home_chart_peak_value({ value: metric.maxValue, date: metric.maxDate })
	);
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<div class="flex flex-wrap items-start justify-between gap-3">
			<div>
				<CardTitle class="text-[22px]">{metric.title}</CardTitle>
				<CardDescription class="pt-1">{chartLabel}</CardDescription>
			</div>
			<p class="text-xs text-muted-foreground">
				{m.home_chart_updated_label()}
				{metric.updatedAt}
			</p>
		</div>
	</CardHeader>
	<CardContent class="pt-4">
		<div class="rounded-[18px] border border-border/70 bg-muted/20 p-3">
			<svg viewBox={`0 0 ${width} ${height}`} class="h-[220px] w-full" role="img" aria-label={metric.title}>
				{#if coords.length > 1}
					<path d={areaPath} fill={metric.color} opacity="0.14"></path>
					<path d={linePath} fill="none" stroke={metric.color} stroke-width="3" stroke-linecap="round" stroke-linejoin="round"></path>
				{/if}

				{#each coords as point, index (point.label + index)}
					<circle cx={point.x} cy={point.y} r="4.5" fill={metric.color}></circle>
				{/each}
			</svg>
		</div>
	</CardContent>
</Card>
