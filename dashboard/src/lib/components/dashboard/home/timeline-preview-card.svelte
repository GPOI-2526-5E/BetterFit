<script lang="ts">
	import CircleAlertIcon from '@lucide/svelte/icons/circle-alert';
	import CircleCheckIcon from '@lucide/svelte/icons/circle-check';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import type { HomeTimelineEvent } from './types';

	let { timeline }: { timeline: HomeTimelineEvent[] } = $props();
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>Timeline operativa</CardTitle>
		<CardDescription>Ultimi eventi reali registrati tra vendite, accessi e preregistrazioni.</CardDescription>
	</CardHeader>
	<CardContent class="space-y-2 pt-4">
		{#if timeline.length === 0}
			<div class="rounded-[14px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center">
				<CircleAlertIcon class="mx-auto size-5 text-muted-foreground" />
				<p class="mt-3 text-sm font-semibold">Ancora nessun evento disponibile</p>
				<p class="mt-2 text-sm text-muted-foreground">
					La timeline si popola automaticamente quando il tenant registra operazioni vere.
				</p>
			</div>
		{:else}
			{#each timeline as event}
				<div class="flex items-start gap-2 rounded-[10px] border border-border bg-background p-3">
					<CircleCheckIcon class="mt-0.5 size-4 shrink-0 text-chart-2" />
					<div class="min-w-0">
						<p class="text-sm font-medium">{event.text}</p>
						<p class="mt-1 text-xs text-muted-foreground">{event.time}</p>
					</div>
				</div>
			{/each}
		{/if}
	</CardContent>
</Card>
