<script lang="ts">
	import { createQuery } from '@tanstack/svelte-query';
	import AccessControllersCard from '$lib/components/dashboard/access-live/access-controllers-card.svelte';
	import AccessDeniedsCard from '$lib/components/dashboard/access-live/access-denials-card.svelte';
	import AccessDeskActionsCard from '$lib/components/dashboard/access-live/access-desk-actions-card.svelte';
	import AccessEventsTableCard from '$lib/components/dashboard/access-live/access-events-table-card.svelte';
	import AccessLiveError from '$lib/components/dashboard/access-live/access-live-error.svelte';
	import AccessLiveKpiGrid from '$lib/components/dashboard/access-live/access-live-kpi-grid.svelte';
	import AccessLiveLoading from '$lib/components/dashboard/access-live/access-live-loading.svelte';
	import HomeHeader from '$lib/components/dashboard/home/home-header.svelte';
	import { Button } from '$lib/components/ui/button/index.js';
	import { fetchAccessLivePlaceholder } from '$lib/data/access-live-placeholder';
	import * as m from '$lib/paraglide/messages.js';
	import { center } from '$lib/stores/center-selection.svelte';

	const accessLiveQuery = createQuery(() => ({
		queryKey: ['dashboard', 'access-live', center.selectedId],
		queryFn: () => fetchAccessLivePlaceholder(center.selectedId)
	}));
</script>

<HomeHeader />

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	<section class="flex flex-wrap items-center justify-between gap-3">
		<div>
			<h2 class="text-xl font-semibold tracking-tight">{m.access_live_page_title()}</h2>
			<p class="text-sm text-muted-foreground">{m.access_live_page_desc()}</p>
		</div>
		<div class="flex items-center gap-2">
			{#if accessLiveQuery.data}
				<p class="text-xs text-muted-foreground">
					{m.access_live_last_sync({ time: accessLiveQuery.data.lastSync })}
				</p>
			{/if}
			<Button variant="outline" size="sm" onclick={() => accessLiveQuery.refetch()}>
				{m.access_live_refresh()}
			</Button>
		</div>
	</section>

	{#if accessLiveQuery.isPending}
		<AccessLiveLoading />
	{:else if accessLiveQuery.isError}
		<AccessLiveError onRetry={() => accessLiveQuery.refetch()} />
	{:else if accessLiveQuery.data}
		<section>
			<AccessLiveKpiGrid kpis={accessLiveQuery.data.kpis} />
		</section>

		<section class="grid gap-4 xl:grid-cols-[1.6fr_1fr]">
			<AccessEventsTableCard events={accessLiveQuery.data.events} />
			<AccessControllersCard controllers={accessLiveQuery.data.controllers} />
		</section>

		<section class="grid gap-4 xl:grid-cols-2">
			<AccessDeniedsCard deniedAttempts={accessLiveQuery.data.deniedAttempts} />
			<AccessDeskActionsCard />
		</section>
	{/if}
</main>
