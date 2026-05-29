<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { createQuery } from '@tanstack/svelte-query';
	import BarChart3Icon from '@lucide/svelte/icons/bar-chart-3';
	import CalendarDaysIcon from '@lucide/svelte/icons/calendar-days';
	import CreditCardIcon from '@lucide/svelte/icons/credit-card';
	import DownloadIcon from '@lucide/svelte/icons/download';
	import DoorOpenIcon from '@lucide/svelte/icons/door-open';
	import DumbbellIcon from '@lucide/svelte/icons/dumbbell';
	import MegaphoneIcon from '@lucide/svelte/icons/megaphone';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import {
		Table,
		TableBody,
		TableCell,
		TableHead,
		TableHeader,
		TableRow
	} from '$lib/components/ui/table/index.js';
	import { fetchGymKpiDashboard, type ReportLeadStage } from '$lib/data/reports-api';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';
	import { downloadPdfReport } from '$lib/utils/pdf-report.js';

	const center = useCenterSelectionStore();
	const money = new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' });
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});

	const stageLabel = (stage: ReportLeadStage) => {
		if (stage === 'TrialBooked') return 'Trial';
		if (stage === 'Negotiation') return 'Trattativa';
		if (stage === 'Won') return 'Chiusi';
		if (stage === 'Lost') return 'Persi';
		if (stage === 'Contacted') return 'Contattati';
		return 'Nuovi';
	};

	const stageVariant = (stage: ReportLeadStage) => {
		if (stage === 'Won') return 'success';
		if (stage === 'Lost') return 'destructive';
		if (stage === 'TrialBooked') return 'warning';
		if (stage === 'Negotiation') return 'secondary';
		return 'outline';
	};

	const kpiQuery = createQuery(() => ({
		queryKey: ['reports-kpi', center.selectedGymId, center.selectedLocationId],
		enabled: !!center.selectedGymId,
		queryFn: () => fetchGymKpiDashboard(center.selectedGymId!, center.selectedLocationId)
	}));

	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms || (!!center.selectedGymId && !kpiQuery.data && kpiQuery.isPending)
	);
	const workspaceError = $derived(
		center.gymsError ??
			(kpiQuery.error instanceof Error
				? kpiQuery.error.message
				: kpiQuery.error
					? 'Impossibile caricare il dashboard KPI.'
					: null)
	);
	const dashboard = $derived(kpiQuery.data);
	const scopeLabel = $derived(
		center.selectedLocationId
			? (center.locations.find((location) => location.id === center.selectedLocationId)?.name ??
					'Sede selezionata')
			: 'Tutte le sedi'
	);
	const occupancyLeader = $derived.by(() => {
		const sessions = dashboard?.upcomingActivities ?? [];
		if (sessions.length === 0) return null;
		return [...sessions].sort(
			(left, right) => right.occupancyRatePercentage - left.occupancyRatePercentage
		)[0];
	});
	const pendingCollectionsLeader = $derived.by(() => {
		const locations = dashboard?.revenueByLocation ?? [];
		if (locations.length === 0) {
			return null;
		}

		return [...locations].sort(
			(left, right) => right.pendingCollectionsAmount - left.pendingCollectionsAmount
		)[0];
	});

	function refreshKpi() {
		void kpiQuery.refetch();
	}

	function downloadKpiPdf() {
		if (!dashboard) {
			return;
		}

		downloadPdfReport(
			{
				title: 'KPI e reporting operativo',
				subtitle: 'Vista manageriale su vendite, accessi, attivita, training e CRM.',
				reference: 'KPI',
				scope: scopeLabel,
				generatedAt: dashboard.generatedAtUtc
					? dateTime.format(dashboard.generatedAtUtc)
					: dateTime.format(new Date()),
				stats: [
					{
						label: 'Ricavi mese',
						value: money.format(dashboard.revenueMonthAmount),
						note: `${money.format(dashboard.pendingCollectionsAmount)} da incassare`
					},
					{
						label: 'Accessi oggi',
						value: `${dashboard.accessesTodayCount}`,
						note: `${dashboard.activeMembersCount} membri attivi`
					},
					{
						label: 'Lead pipeline',
						value: `${dashboard.leadsInPipelineCount}`,
						note: `${dashboard.leadConversionRatePercentage.toFixed(1)}% conversione`
					},
					{
						label: 'Training attivo',
						value: `${dashboard.activeWorkoutAssignmentsCount}`,
						note: `${dashboard.upcomingBookingsCount} prenotazioni prossimi 7 giorni`
					}
				],
				tables: [
					{
						title: 'Rendimento per sede',
						columns: [{ header: 'Sede', width: 2 }, 'Vendite', 'Ricavi mese', 'Da incassare'],
						rows: dashboard.revenueByLocation.map((row) => [
							row.locationName,
							`${row.salesCount}`,
							money.format(row.revenueMonthAmount),
							money.format(row.pendingCollectionsAmount)
						])
					},
					{
						title: 'Pipeline lead',
						columns: ['Stage', 'Lead'],
						rows: dashboard.leadPipeline.map((row) => [stageLabel(row.stage), `${row.leadsCount}`])
					},
					{
						title: 'Attivita in arrivo',
						columns: [
							{ header: 'Sessione', width: 2 },
							'Sede',
							'Orario',
							'Istruttore',
							'Prenotati'
						],
						rows: dashboard.upcomingActivities.map((session) => [
							session.title,
							session.locationName,
							dateTime.format(session.startsAtUtc),
							session.instructorName,
							`${session.bookedCount}/${session.capacity} (${session.occupancyRatePercentage.toFixed(1)}%)`
						])
					},
					{
						title: 'Operativita sedi',
						columns: [
							{ header: 'Sede', width: 2 },
							'Accessi concessi',
							'Negati',
							'Piani attivi',
							'Revisioni'
						],
						rows: dashboard.accessByLocation.map((access) => {
							const training = dashboard.trainingByLocation.find(
								(item) => item.locationId === access.locationId
							);
							return [
								access.locationName,
								`${access.grantedTodayCount}`,
								`${access.deniedTodayCount}`,
								`${training?.activeAssignmentsCount ?? 0}`,
								`${training?.revisionDueCount ?? 0}`
							];
						})
					}
				],
				orientation: 'landscape'
			},
			`betterfit-kpi-${scopeLabel}`
		);
	}

	const currentHash = $derived(page.url.hash || '#kpi-summary');
	const sectionNavClass = (hash: string) =>
		`inline-flex items-center rounded-full border px-3 py-1.5 text-sm transition ${
			currentHash === hash
				? 'border-primary/40 bg-primary/10 text-foreground shadow-sm'
				: 'border-border/70 bg-background text-muted-foreground hover:border-border hover:bg-secondary/20 hover:text-foreground'
		}`;
	const sectionCardClass = (hash: string) =>
		`scroll-mt-24 ${
			currentHash === hash ? 'border-primary/40 bg-primary/[0.03] shadow-sm' : 'border-border/70'
		}`;
	const sectionPanelClass = (hash: string) =>
		`scroll-mt-24 rounded-[24px] border p-1 ${
			currentHash === hash
				? 'border-primary/40 bg-primary/[0.03] shadow-sm'
				: 'border-border/70 bg-background'
		}`;

	const visibleLocationsCount = $derived(dashboard?.revenueByLocation.length ?? 0);
</script>

<main class="grid gap-6 p-4 md:gap-8 md:p-6">
	<div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
		<div>
			<h2 class="text-2xl font-semibold tracking-tight">KPI e reporting operativo</h2>
			<p class="text-sm text-muted-foreground">
				Vista manageriale su vendite, accessi, attivita, training e CRM.
			</p>
		</div>
		<div class="flex flex-wrap gap-2">
			<Button variant="outline" onclick={downloadKpiPdf} disabled={!dashboard}>
				<DownloadIcon class="mr-2 size-4" />
				Scarica PDF
			</Button>
			<Button variant="outline" onclick={() => goto('/analytics/churn')}>Retention e churn</Button>
			<Button variant="outline" onclick={() => goto('/analytics/export')}>Export report</Button>
			<Button variant="outline" onclick={refreshKpi} disabled={!hasSelectedGym}>
				<RefreshCwIcon class="mr-2 size-4" />
				Aggiorna KPI
			</Button>
		</div>
	</div>

	<nav
		class="flex flex-wrap gap-2 rounded-[20px] border border-border/70 bg-muted/20 p-2"
		aria-label="Sezioni report"
	>
		<a href="#kpi-summary" class={sectionNavClass('#kpi-summary')}> KPI </a>
		<a href="#lead-pipeline" class={sectionNavClass('#lead-pipeline')}> Pipeline </a>
		<a href="#upcoming-activities" class={sectionNavClass('#upcoming-activities')}> Attivita </a>
		<a href="#location-operations" class={sectionNavClass('#location-operations')}>
			Operativita sedi
		</a>
	</nav>

	{#if workspaceLoading}
		<Card class="border-border/70">
			<CardHeader>
				<CardTitle>Caricamento KPI</CardTitle>
				<CardDescription>
					Sto preparando ricavi, accessi, agenda attivita, training e CRM del tenant selezionato.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-destructive/40">
			<CardHeader>
				<CardTitle>Dashboard KPI non disponibile</CardTitle>
				<CardDescription>{workspaceError}</CardDescription>
			</CardHeader>
			<CardContent>
				<Button variant="outline" onclick={refreshKpi} disabled={!hasSelectedGym}>
					<RefreshCwIcon class="mr-2 size-4" />
					Riprova
				</Button>
			</CardContent>
		</Card>
	{:else if !hasSelectedGym}
		<Card class="border-border/70">
			<CardHeader>
				<CardTitle>Seleziona un tenant</CardTitle>
				<CardDescription>
					Per leggere KPI e reporting dobbiamo prima sapere su quale palestra lavorare.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
		<Card class="border-border/70">
			<CardHeader>
				<CardTitle>Perimetro KPI</CardTitle>
				<CardDescription>
					Stai leggendo i dati di <span class="font-medium text-foreground">{scopeLabel}</span>.
				</CardDescription>
			</CardHeader>
			<CardContent class="grid gap-3 text-sm text-muted-foreground lg:grid-cols-3">
				<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
					<div class="text-xs font-medium tracking-[0.18em] uppercase">Perimetro</div>
					<div class="mt-2 text-sm font-medium text-foreground">{scopeLabel}</div>
				</div>
				<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
					<div class="text-xs font-medium tracking-[0.18em] uppercase">Sedi visibili</div>
					<div class="mt-2 text-sm font-medium text-foreground">{visibleLocationsCount}</div>
				</div>
				<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
					<div class="text-xs font-medium tracking-[0.18em] uppercase">Generato</div>
					<div class="mt-2 text-sm font-medium text-foreground">
						{#if dashboard?.generatedAtUtc}
							{dateTime.format(dashboard.generatedAtUtc)}
						{:else}
							In attesa dati
						{/if}
					</div>
				</div>
			</CardContent>
		</Card>

		<section id="kpi-summary" class={sectionPanelClass('#kpi-summary')}>
			<div class="grid gap-4 md:grid-cols-2 lg:gap-5 xl:grid-cols-4">
				<Card class={sectionCardClass('#kpi-summary')}>
					<CardHeader class="pb-3">
						<div class="flex items-center gap-2 text-muted-foreground">
							<BarChart3Icon class="size-4" />
							<span class="text-sm">Ricavi mese</span>
						</div>
						<CardTitle class="text-3xl"
							>{money.format(dashboard?.revenueMonthAmount ?? 0)}</CardTitle
						>
						<CardDescription>
							{money.format(dashboard?.pendingCollectionsAmount ?? 0)} ancora da incassare
						</CardDescription>
					</CardHeader>
					<CardContent>
						<Button variant="outline" size="sm" onclick={() => goto('/sales/payments')}>
							Apri incassi
						</Button>
					</CardContent>
				</Card>
				<Card class={sectionCardClass('#kpi-summary')}>
					<CardHeader class="pb-3">
						<div class="flex items-center gap-2 text-muted-foreground">
							<DoorOpenIcon class="size-4" />
							<span class="text-sm">Traffico di oggi</span>
						</div>
						<CardTitle class="text-3xl">{dashboard?.accessesTodayCount ?? 0}</CardTitle>
						<CardDescription>
							{dashboard?.activeMembersCount ?? 0} membri attivi nel perimetro visibile
						</CardDescription>
					</CardHeader>
					<CardContent>
						<Button variant="outline" size="sm" onclick={() => goto('/access/checkin')}>
							Apri check-in
						</Button>
					</CardContent>
				</Card>
				<Card class={sectionCardClass('#kpi-summary')}>
					<CardHeader class="pb-3">
						<div class="flex items-center gap-2 text-muted-foreground">
							<MegaphoneIcon class="size-4" />
							<span class="text-sm">Funnel CRM</span>
						</div>
						<CardTitle class="text-3xl">{dashboard?.leadsInPipelineCount ?? 0}</CardTitle>
						<CardDescription>
							Conversione ultimi 90 giorni:
							{(dashboard?.leadConversionRatePercentage ?? 0).toFixed(1)}%
						</CardDescription>
					</CardHeader>
					<CardContent>
						<Button variant="outline" size="sm" onclick={() => goto('/crm')}>Apri CRM</Button>
					</CardContent>
				</Card>
				<Card class={sectionCardClass('#kpi-summary')}>
					<CardHeader class="pb-3">
						<div class="flex items-center gap-2 text-muted-foreground">
							<DumbbellIcon class="size-4" />
							<span class="text-sm">Training attivo</span>
						</div>
						<CardTitle class="text-3xl">{dashboard?.activeWorkoutAssignmentsCount ?? 0}</CardTitle>
						<CardDescription>
							{dashboard?.upcomingBookingsCount ?? 0} prenotazioni attivita nei prossimi 7 giorni
						</CardDescription>
					</CardHeader>
					<CardContent>
						<Button variant="outline" size="sm" onclick={() => goto('/training')}>
							Apri training
						</Button>
					</CardContent>
				</Card>
			</div>
		</section>

		<div class="grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_minmax(320px,0.85fr)] xl:gap-7">
			<Card class={sectionCardClass('#kpi-summary')}>
				<CardHeader>
					<CardTitle>Rendimento per sede</CardTitle>
					<CardDescription>Incassi del mese e partite aperte per location.</CardDescription>
				</CardHeader>
				<CardContent>
					{#if (dashboard?.revenueByLocation.length ?? 0) === 0}
						<p class="text-sm text-muted-foreground">
							Non ci sono sedi visibili nel perimetro corrente.
						</p>
					{:else}
						<Table>
							<TableHeader>
								<TableRow>
									<TableHead>Sede</TableHead>
									<TableHead class="text-right">Vendite</TableHead>
									<TableHead class="text-right">Ricavi mese</TableHead>
									<TableHead class="text-right">Da incassare</TableHead>
								</TableRow>
							</TableHeader>
							<TableBody>
								{#each dashboard?.revenueByLocation ?? [] as row}
									<TableRow>
										<TableCell class="font-medium">{row.locationName}</TableCell>
										<TableCell class="text-right">{row.salesCount}</TableCell>
										<TableCell class="text-right">{money.format(row.revenueMonthAmount)}</TableCell>
										<TableCell class="text-right">
											{money.format(row.pendingCollectionsAmount)}
										</TableCell>
									</TableRow>
								{/each}
							</TableBody>
						</Table>
					{/if}
				</CardContent>
			</Card>

			<Card id="lead-pipeline" class={sectionCardClass('#lead-pipeline')}>
				<CardHeader>
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>Pipeline lead</CardTitle>
							<CardDescription>Distribuzione dello stato commerciale corrente.</CardDescription>
						</div>
						<Button variant="outline" size="sm" onclick={() => goto('/crm#pipeline-board')}>
							Vai al CRM
						</Button>
					</div>
				</CardHeader>
				<CardContent class="space-y-3">
					{#if (dashboard?.leadPipeline.length ?? 0) === 0}
						<p class="text-sm text-muted-foreground">
							Nessun lead aperto nel perimetro selezionato.
						</p>
					{:else}
						{#each dashboard?.leadPipeline ?? [] as stage}
							<div
								class="flex items-center justify-between rounded-2xl border border-border/70 px-4 py-3"
							>
								<div class="flex items-center gap-3">
									<Badge variant={stageVariant(stage.stage)}>{stageLabel(stage.stage)}</Badge>
								</div>
								<div class="text-lg font-semibold">{stage.leadsCount}</div>
							</div>
						{/each}
					{/if}
				</CardContent>
			</Card>
		</div>

		<div class="grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_minmax(320px,0.9fr)] xl:gap-7">
			<Card id="upcoming-activities" class={sectionCardClass('#upcoming-activities')}>
				<CardHeader>
					<div class="flex items-start justify-between gap-3">
						<div class="flex items-center gap-2">
							<CalendarDaysIcon class="size-4 text-muted-foreground" />
							<CardTitle>Attivita in arrivo</CardTitle>
						</div>
						<Button variant="outline" size="sm" onclick={() => goto('/activities#sessions')}>
							Apri attivita
						</Button>
					</div>
					<CardDescription>Prenotazioni e saturazione delle prossime sessioni.</CardDescription>
				</CardHeader>
				<CardContent class="space-y-3">
					{#if (dashboard?.upcomingActivities?.length ?? 0) === 0}
						<p class="text-sm text-muted-foreground">
							Nessuna sessione in programma nel periodo selezionato.
						</p>
					{:else}
						{#each dashboard?.upcomingActivities ?? [] as session}
							<div class="rounded-2xl border border-border/70 p-4">
								<div class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
									<div class="min-w-0">
										<div class="font-medium">{session.title}</div>
										<div class="text-xs text-muted-foreground">
											{session.locationName} - {dateTime.format(session.startsAtUtc)} -
											{session.instructorName}
										</div>
									</div>
									<Badge
										class="w-fit"
										variant={session.occupancyRatePercentage >= 80 ? 'warning' : 'outline'}
									>
										{session.bookedCount}/{session.capacity}
									</Badge>
								</div>
								<div class="mt-3 h-2 rounded-full bg-muted">
									<div
										class="h-2 rounded-full bg-primary"
										style={`width: ${Math.min(session.occupancyRatePercentage, 100)}%`}
									></div>
								</div>
								<div class="mt-2 text-sm text-muted-foreground">
									Occupazione {session.occupancyRatePercentage.toFixed(1)}%
								</div>
							</div>
						{/each}
					{/if}
				</CardContent>
			</Card>

			<Card id="location-operations" class={sectionCardClass('#location-operations')}>
				<CardHeader>
					<div class="flex items-start justify-between gap-3">
						<div class="flex items-center gap-2">
							<CreditCardIcon class="size-4 text-muted-foreground" />
							<CardTitle>Accessi e training per sede</CardTitle>
						</div>
						<Button variant="outline" size="sm" onclick={() => goto('/training')}>
							Apri training
						</Button>
					</div>
					<CardDescription>Confronto rapido su presidio operativo e coaching.</CardDescription>
				</CardHeader>
				<CardContent class="space-y-4">
					{#if (dashboard?.accessByLocation.length ?? 0) === 0}
						<p class="text-sm text-muted-foreground">
							Nessun dato accessi disponibile per la selezione corrente.
						</p>
					{:else}
						{#each dashboard?.accessByLocation ?? [] as access}
							<div class="rounded-2xl border border-border/70 p-4">
								<div class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
									<div class="min-w-0">
										<div class="font-medium">{access.locationName}</div>
										<div class="text-xs text-muted-foreground">
											{access.grantedTodayCount} accessi concessi - {access.deniedTodayCount}
											negati oggi
										</div>
									</div>
									{#if dashboard}
										{@const training = dashboard.trainingByLocation.find(
											(item) => item.locationId === access.locationId
										)}
										<div class="text-right text-xs text-muted-foreground">
											<div>{training?.activeAssignmentsCount ?? 0} piani attivi</div>
											<div>{training?.revisionDueCount ?? 0} revisioni in scadenza</div>
											<div>{training?.assessmentsLast30DaysCount ?? 0} valutazioni 30g</div>
										</div>
									{/if}
								</div>
								<div class="mt-3 flex flex-wrap gap-2">
									<Button variant="outline" size="sm" onclick={() => goto('/access/checkin')}>
										Check-in
									</Button>
									<Button variant="outline" size="sm" onclick={() => goto('/training')}>
										Training
									</Button>
								</div>
							</div>
						{/each}
					{/if}
				</CardContent>
			</Card>
		</div>

		{#if occupancyLeader || pendingCollectionsLeader}
			<Card id="focus-operativo" class="scroll-mt-24">
				<CardHeader>
					<CardTitle>Focus operativo</CardTitle>
					<CardDescription>
						Il punto che richiede attenzione adesso tra agenda e incassi aperti.
					</CardDescription>
				</CardHeader>
				<CardContent class="grid gap-4 lg:grid-cols-2">
					{#if occupancyLeader}
						<div class="rounded-[18px] border border-border/70 p-4">
							<div class="flex items-start justify-between gap-3">
								<div>
									<p class="font-medium">{occupancyLeader.title}</p>
									<p class="mt-1 text-sm text-muted-foreground">
										{occupancyLeader.locationName} - {dateTime.format(occupancyLeader.startsAtUtc)} -
										{occupancyLeader.instructorName}
									</p>
								</div>
								<Badge
									variant={occupancyLeader.occupancyRatePercentage >= 80 ? 'warning' : 'secondary'}
								>
									{occupancyLeader.occupancyRatePercentage.toFixed(1)}% occupazione
								</Badge>
							</div>
							<div class="mt-3">
								<Button variant="outline" size="sm" onclick={() => goto('/activities#bookings')}>
									Apri sessioni
								</Button>
							</div>
						</div>
					{/if}
					{#if pendingCollectionsLeader}
						<div class="rounded-[18px] border border-border/70 p-4">
							<div class="flex items-start justify-between gap-3">
								<div>
									<p class="font-medium">{pendingCollectionsLeader.locationName}</p>
									<p class="mt-1 text-sm text-muted-foreground">
										{pendingCollectionsLeader.salesCount} vendite nel mese
									</p>
								</div>
								<Badge variant="warning">
									{money.format(pendingCollectionsLeader.pendingCollectionsAmount)}
								</Badge>
							</div>
							<p class="mt-3 text-sm text-muted-foreground">
								Questa sede ha il residuo aperto piu alto nel perimetro corrente.
							</p>
							<div class="mt-3">
								<Button variant="outline" size="sm" onclick={() => goto('/sales/payments')}>
									Apri incassi
								</Button>
							</div>
						</div>
					{/if}
				</CardContent>
			</Card>
		{/if}
	{/if}
</main>
