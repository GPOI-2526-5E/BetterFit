<script lang="ts">
	import { createQuery } from '@tanstack/svelte-query';
	import AlertsCard from '$lib/components/dashboard/home/alerts-card.svelte';
	import DevicesCard from '$lib/components/dashboard/home/devices-card.svelte';
	import HomeError from '$lib/components/dashboard/home/home-error.svelte';
	import HomeHeader from '$lib/components/dashboard/home/home-header.svelte';
	import HomeLoading from '$lib/components/dashboard/home/home-loading.svelte';
	import KpiGrid from '$lib/components/dashboard/home/kpi-grid.svelte';
	import LocationsSummaryCard from '$lib/components/dashboard/home/locations-summary-card.svelte';
	import OperationalAlertBanner from '$lib/components/dashboard/home/operational-alert-banner.svelte';
	import RecentCollectionsCard from '$lib/components/dashboard/home/recent-collections-card.svelte';
	import StatusOverviewGrid from '$lib/components/dashboard/home/status-overview-grid.svelte';
	import TasksTableCard from '$lib/components/dashboard/home/tasks-table-card.svelte';
	import TimelinePreviewCard from '$lib/components/dashboard/home/timeline-preview-card.svelte';
	import { fetchDashboardHomeData } from '$lib/data/home-api';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	const center = useCenterSelectionStore();

	const homeQuery = createQuery(() => ({
		queryKey: ['dashboard', 'home-overview', center.selectedGymId, center.selectedLocationId],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchDashboardHomeData({
				gymId: center.selectedGymId!,
				locationId: center.selectedLocationId
			})
	}));

	const homeErrorMessage = $derived(
		homeQuery.error instanceof Error
			? homeQuery.error.message
			: homeQuery.error
				? 'Errore imprevisto durante il caricamento della dashboard.'
				: null
	);
</script>

<HomeHeader />

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	{#if center.isLoadingGyms || (!center.selectedGymId && !center.gymsError) || homeQuery.isPending}
		<HomeLoading />
	{:else if homeQuery.isError}
		<HomeError onRetry={() => homeQuery.refetch()} message={homeErrorMessage} />
	{:else if homeQuery.data}
		{#if homeQuery.data.coverageNotice}
			<section
				class="rounded-[20px] border border-[#dbeafe] bg-[#eff6ff] px-4 py-3 text-sm text-[#1d4ed8]"
			>
				{homeQuery.data.coverageNotice}
			</section>
		{/if}

		{#if homeQuery.data.operationalMessage}
			<section>
				<OperationalAlertBanner message={homeQuery.data.operationalMessage} />
			</section>
		{/if}

		<section id="overview">
			<KpiGrid kpis={homeQuery.data.kpis} />
		</section>

		{#if homeQuery.data.statusCards.length > 0}
			<section id="status-overview">
				<StatusOverviewGrid cards={homeQuery.data.statusCards} />
			</section>
		{/if}

		<section id="tasks" class="grid gap-4 xl:grid-cols-[1.45fr_1fr]">
			<TasksTableCard tasks={homeQuery.data.tasks} />
			<div id="alerts">
				<AlertsCard alerts={homeQuery.data.alerts} />
			</div>
		</section>

		<section class="grid gap-4 xl:grid-cols-[1.05fr_0.95fr_0.95fr]">
			<div class="space-y-4">
				<div id="locations">
					<LocationsSummaryCard locations={homeQuery.data.locations} />
				</div>
				{#if homeQuery.data.devices.length > 0}
					<div id="systems">
						<DevicesCard devices={homeQuery.data.devices} />
					</div>
				{/if}
			</div>
			<RecentCollectionsCard recentCollections={homeQuery.data.recentCollections} />
			<TimelinePreviewCard timeline={homeQuery.data.timeline} />
		</section>
	{/if}
</main>
