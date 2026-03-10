<script lang="ts">
	import { AreaChart } from 'layerchart';
	import * as Chart from '$lib/components/ui/chart/index.js';
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

	const chartConfig = $derived({
		value: {
			label: metric.title,
			color: metric.color
		}
	} as Chart.ChartConfig);

	const axisProps = $derived.by(() => {
		if (metric.format === 'currency') {
			return {
				xAxis: {
					tickSpacing: 58
				},
				yAxis: {
					format: (value: number) => `€${Math.round(value)}`
				}
			};
		}
		return {
			xAxis: {
				tickSpacing: 58
			}
		};
	});
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<div class="flex flex-wrap items-start justify-between gap-3">
			<div>
				<CardTitle class="text-[22px]">{metric.title}</CardTitle>
				<CardDescription class="pt-1">
					{m.home_chart_peak_value({ value: metric.maxValue, date: metric.maxDate })}
				</CardDescription>
			</div>
			<p class="text-xs text-muted-foreground">
				{m.home_chart_updated_label()}
				{metric.updatedAt}
			<!---->
			</p>
		</div>
	</CardHeader>
	<CardContent class="pt-4">
		<Chart.Container config={chartConfig} class="!aspect-auto h-[220px] w-full">
			<AreaChart
				data={metric.points}
				x="label"
				axis
				rule={false}
				points={false}
				legend={false}
				series={[
					{
						key: 'value',
						label: metric.title,
						color: metric.color
					}
				]}
				props={axisProps}
			>
				{#snippet tooltip()}
					<Chart.Tooltip indicator="line" />
				{/snippet}
			</AreaChart>
		</Chart.Container>
	</CardContent>
</Card>
