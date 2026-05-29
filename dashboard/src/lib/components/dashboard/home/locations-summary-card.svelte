<script lang="ts">
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import type { HomeLocationSnapshot } from './types';

	let { locations }: { locations: HomeLocationSnapshot[] } = $props();
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>Sedi sotto controllo</CardTitle>
		<CardDescription>Volumi giornalieri e priorita operative per ogni sede visibile.</CardDescription>
	</CardHeader>
	<CardContent class="space-y-3 pt-4">
		{#if locations.length === 0}
			<div class="rounded-[14px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center">
				<p class="text-sm font-semibold">Nessuna sede nel perimetro corrente</p>
				<p class="mt-2 text-sm text-muted-foreground">
					Seleziona un tenant o verifica i permessi per caricare il riepilogo per sede.
				</p>
			</div>
		{:else}
			{#each locations as location}
				<div class="rounded-[12px] border border-border bg-background p-4">
					<div class="flex flex-wrap items-center justify-between gap-2">
						<p class="text-sm font-semibold">{location.locationName}</p>
						<p class="text-sm font-medium text-primary">{location.revenueTodayLabel}</p>
					</div>

					<div class="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-3">
						<div class="rounded-[10px] bg-secondary/30 px-3 py-2">
							<p class="font-medium text-foreground">{location.accessesTodayCount}</p>
							<p class="mt-1 text-xs">Accessi consentiti oggi</p>
						</div>
						<div class="rounded-[10px] bg-secondary/30 px-3 py-2">
							<p class="font-medium text-foreground">{location.expiringMembershipsCount}</p>
							<p class="mt-1 text-xs">Rinnovi in scadenza</p>
						</div>
						<div class="rounded-[10px] bg-secondary/30 px-3 py-2">
							<p class="font-medium text-foreground">{location.pendingActivationsCount}</p>
							<p class="mt-1 text-xs">Attivazioni aperte</p>
						</div>
					</div>
				</div>
			{/each}
		{/if}
	</CardContent>
</Card>
