<script lang="ts">
	import { createQuery } from '@tanstack/svelte-query';
	import AlertsCard from '$lib/components/dashboard/home/alerts-card.svelte';
	import DevicesCard from '$lib/components/dashboard/home/devices-card.svelte';
	import HomeError from '$lib/components/dashboard/home/home-error.svelte';
	import HomeHeader from '$lib/components/dashboard/home/home-header.svelte';
	import HomeLoading from '$lib/components/dashboard/home/home-loading.svelte';
	import KpiGrid from '$lib/components/dashboard/home/kpi-grid.svelte';
	import TasksTableCard from '$lib/components/dashboard/home/tasks-table-card.svelte';
	import TimelinePreviewCard from '$lib/components/dashboard/home/timeline-preview-card.svelte';
	import { fetchDashboardHomePlaceholder } from '$lib/data/dashboard-home-placeholder';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	const center = useCenterSelectionStore();

	const homeQuery = createQuery(() => ({
		queryKey: ['dashboard', 'home-overview', center.selectedId],
		queryFn: () => fetchDashboardHomePlaceholder(center.selectedId)
	}));
</script>

<HomeHeader />

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	{#if homeQuery.isPending}
		<HomeLoading />
	{:else if homeQuery.isError}
		<HomeError onRetry={() => homeQuery.refetch()} />
	{:else if homeQuery.data}
		<section id="overview">
			<KpiGrid kpis={homeQuery.data.kpis} />
		</section>

		<section id="tasks" class="grid gap-4 xl:grid-cols-[1.45fr_1fr]">
			<TasksTableCard tasks={homeQuery.data.tasks} />
			<div id="alerts">
				<AlertsCard alerts={homeQuery.data.alerts} />
			</div>
		</section>

		<section class="grid gap-4 xl:grid-cols-[1.1fr_1fr]">
			<div id="devices">
				<DevicesCard devices={homeQuery.data.devices} />
			</div>
			<TimelinePreviewCard timeline={homeQuery.data.timeline} />
		</section>
	{/if}
</main>
