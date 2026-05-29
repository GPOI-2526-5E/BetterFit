<script lang="ts">
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import HomeHeader from '$lib/components/dashboard/home/home-header.svelte';
	import type { Snippet } from 'svelte';
	import NoTenantEmptyState from '$lib/components/dashboard/no-tenant-empty-state.svelte';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	let { children }: { children: Snippet } = $props();

	const center = useCenterSelectionStore();
	const showTenantScopeLoading = $derived(
		center.isLoadingGyms || (!center.gymsError && center.gyms.length > 0 && !center.selectedGymId)
	);
	const showNoTenants = $derived(
		!center.isLoadingGyms && !center.gymsError && center.gyms.length === 0
	);
	const showGymsError = $derived(!center.isLoadingGyms && !!center.gymsError);
</script>

{#if showTenantScopeLoading}
	<HomeHeader />
	<section class="min-h-[calc(100svh-9rem)] p-4 md:p-6">
		<div class="grid h-full place-items-center">
			<Card class="w-full max-w-2xl border-dashed border-border/70 bg-muted/20">
				<CardHeader class="text-center">
					<div class="mx-auto mb-2 rounded-full border border-border/70 p-3 text-muted-foreground">
						<RefreshCwIcon class="size-5 animate-spin" />
					</div>
					<CardTitle>Sto preparando il tenant</CardTitle>
					<CardDescription>
						Recupero palestre disponibili, scope e selezione attiva prima di mostrare i dati reali
						del gestionale.
					</CardDescription>
				</CardHeader>
				<CardContent class="flex justify-center">
					<Button variant="outline" onclick={() => center.refetchGyms()} disabled={center.isLoadingGyms}>
						<RefreshCwIcon class="mr-2 size-4" />
						Aggiorna tenant
					</Button>
				</CardContent>
			</Card>
		</div>
	</section>
{:else if showGymsError}
	<HomeHeader />
	<NoTenantEmptyState
		mode="error"
		message={center.gymsError}
		onRetry={() => center.refetchGyms()}
	/>
{:else if showNoTenants}
	<HomeHeader />
	<NoTenantEmptyState onRetry={() => center.refetchGyms()} />
{:else}
	{@render children()}
{/if}
