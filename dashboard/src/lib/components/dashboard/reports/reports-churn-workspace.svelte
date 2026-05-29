<script lang="ts">
	import { goto } from '$app/navigation';
	import { createQuery } from '@tanstack/svelte-query';
	import AlertTriangleIcon from '@lucide/svelte/icons/alert-triangle';
	import BarChart3Icon from '@lucide/svelte/icons/bar-chart-3';
	import DownloadIcon from '@lucide/svelte/icons/download';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import ShieldCheckIcon from '@lucide/svelte/icons/shield-check';
	import UsersIcon from '@lucide/svelte/icons/users';
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
	import { fetchGymRetentionChurn } from '$lib/data/reports-api';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';
	import { downloadPdfReport } from '$lib/utils/pdf-report.js';

	const center = useCenterSelectionStore();
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});

	const churnQuery = createQuery(() => ({
		queryKey: ['reports-churn', center.selectedGymId, center.selectedLocationId],
		enabled: !!center.selectedGymId,
		queryFn: () => fetchGymRetentionChurn(center.selectedGymId!, center.selectedLocationId)
	}));

	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms || (!!center.selectedGymId && !churnQuery.data && churnQuery.isPending)
	);
	const workspaceError = $derived(
		center.gymsError ??
			(churnQuery.error instanceof Error
				? churnQuery.error.message
				: churnQuery.error
					? 'Impossibile caricare il report retention e churn.'
					: null)
	);
	const report = $derived(churnQuery.data);
	const topRiskMember = $derived(report?.atRiskMembers[0] ?? null);
	const scopeLabel = $derived(
		center.selectedLocationId
			? (center.locations.find((location) => location.id === center.selectedLocationId)?.name ??
					'Sede selezionata')
			: 'Tutte le sedi'
	);

	const riskUrgencyLabel = (daysUntilExpiry: number) => {
		if (daysUntilExpiry <= 3) return 'Urgente';
		if (daysUntilExpiry <= 10) return 'Priorita alta';
		return 'Da pianificare';
	};

	function openRenewal(membershipId: string) {
		void goto(`/sales/renewals?membershipId=${encodeURIComponent(membershipId)}`);
	}

	function refreshReport() {
		void churnQuery.refetch();
	}

	function downloadChurnPdf() {
		if (!report) {
			return;
		}

		downloadPdfReport(
			{
				title: 'Retention e churn',
				subtitle: 'Rinnovi, membri a rischio e churn reale per sede.',
				reference: 'CHURN',
				scope: scopeLabel,
				generatedAt: report.generatedAtUtc
					? dateTime.format(report.generatedAtUtc)
					: dateTime.format(new Date()),
				stats: [
					{
						label: 'Membri attivi',
						value: `${report.activeMembersCount}`,
						note: `${report.expiringNext30DaysCount} in scadenza 30g`
					},
					{
						label: 'A rischio',
						value: `${report.atRiskMembersCount}`,
						note: 'Senza rinnovo registrato'
					},
					{
						label: 'Retention 90g',
						value: `${report.retentionRatePercentage.toFixed(1)}%`,
						note: `${report.renewedLast90DaysCount} rinnovi`
					},
					{
						label: 'Churn 90g',
						value: `${report.churnRatePercentage.toFixed(1)}%`,
						note: `${report.churnedLast90DaysCount} membri persi`
					}
				],
				tables: [
					{
						title: 'Rendimento per sede',
						columns: [
							{ header: 'Sede', width: 2 },
							'Attivi',
							'In scadenza',
							'A rischio',
							'Retention',
							'Churn'
						],
						rows: report.locations.map((row) => [
							row.locationName,
							`${row.activeMembersCount}`,
							`${row.expiringNext30DaysCount}`,
							`${row.atRiskMembersCount}`,
							`${row.retentionRatePercentage.toFixed(1)}%`,
							`${row.churnRatePercentage.toFixed(1)}%`
						])
					},
					{
						title: 'Membri da lavorare subito',
						columns: [
							{ header: 'Membro', width: 1.5 },
							'Sede',
							'Scadenza',
							'Ultimo accesso',
							'Rinnovo'
						],
						rows: report.atRiskMembers.map((member) => [
							member.memberName,
							member.locationName,
							member.membershipEndsAtUtc
								? dateTime.format(member.membershipEndsAtUtc)
								: 'Non definita',
							member.lastAccessAtUtc ? dateTime.format(member.lastAccessAtUtc) : 'Nessun accesso',
							member.hasRenewalSale ? 'Si' : 'No'
						])
					}
				],
				orientation: 'landscape'
			},
			`betterfit-retention-churn-${scopeLabel}`
		);
	}
</script>

<main class="grid gap-6 p-4 md:gap-8 md:p-6">
	<div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
		<div>
			<h2 class="text-2xl font-semibold tracking-tight">Retention e churn</h2>
			<p class="text-sm text-muted-foreground">
				Controlla rinnovi, membri a rischio e churn reale per sede.
			</p>
		</div>
		<div class="flex flex-wrap gap-2">
			<Button variant="outline" onclick={downloadChurnPdf} disabled={!report}>
				<DownloadIcon class="mr-2 size-4" />
				Scarica PDF
			</Button>
			<Button
				variant="outline"
				onclick={() => goto('/analytics/export')}
				disabled={!hasSelectedGym || workspaceLoading}
			>
				Export churn
			</Button>
			<Button variant="outline" onclick={refreshReport} disabled={!hasSelectedGym}>
				<RefreshCwIcon class="mr-2 size-4" />
				Aggiorna report
			</Button>
		</div>
	</div>

	{#if workspaceLoading}
		<Card class="border-border/70">
			<CardHeader>
				<CardTitle>Caricamento retention e churn</CardTitle>
				<CardDescription>
					Sto preparando lo storico rinnovi, le scadenze e i membri a rischio del tenant
					selezionato.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-destructive/40">
			<CardHeader>
				<CardTitle>Report non disponibile</CardTitle>
				<CardDescription>{workspaceError}</CardDescription>
			</CardHeader>
			<CardContent>
				<Button variant="outline" onclick={refreshReport} disabled={!hasSelectedGym}>
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
					Per leggere retention e churn dobbiamo prima sapere su quale palestra lavorare.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
		<Card class="border-border/70">
			<CardHeader>
				<CardTitle>Perimetro report</CardTitle>
				<CardDescription>
					Stai leggendo i dati di <span class="font-medium text-foreground">{scopeLabel}</span>.
				</CardDescription>
			</CardHeader>
			<CardContent
				class="flex flex-col gap-3 text-sm text-muted-foreground lg:flex-row lg:items-center lg:justify-between"
			>
				<div>
					Il report considera rinnovi e churn sugli ultimi 90 giorni e i membri in scadenza nei
					prossimi 30 giorni.
				</div>
				<div>
					Generato:
					{#if report?.generatedAtUtc}
						{dateTime.format(report.generatedAtUtc)}
					{:else}
						in attesa dati
					{/if}
				</div>
			</CardContent>
		</Card>

		<div class="grid gap-4 md:grid-cols-2 lg:gap-5 xl:grid-cols-4">
			<Card>
				<CardHeader class="pb-3">
					<div class="flex items-center gap-2 text-muted-foreground">
						<UsersIcon class="size-4" />
						<span class="text-sm">Membri attivi</span>
					</div>
					<CardTitle class="text-3xl">{report?.activeMembersCount ?? 0}</CardTitle>
					<CardDescription>
						{report?.expiringNext30DaysCount ?? 0} in scadenza nei prossimi 30 giorni
					</CardDescription>
				</CardHeader>
			</Card>
			<Card>
				<CardHeader class="pb-3">
					<div class="flex items-center gap-2 text-muted-foreground">
						<AlertTriangleIcon class="size-4" />
						<span class="text-sm">A rischio</span>
					</div>
					<CardTitle class="text-3xl">{report?.atRiskMembersCount ?? 0}</CardTitle>
					<CardDescription>Membri in scadenza senza rinnovo gia registrato</CardDescription>
				</CardHeader>
			</Card>
			<Card>
				<CardHeader class="pb-3">
					<div class="flex items-center gap-2 text-muted-foreground">
						<ShieldCheckIcon class="size-4" />
						<span class="text-sm">Retention 90g</span>
					</div>
					<CardTitle class="text-3xl">
						{(report?.retentionRatePercentage ?? 0).toFixed(1)}%
					</CardTitle>
					<CardDescription>
						{report?.renewedLast90DaysCount ?? 0} rinnovi su storico scaduto
					</CardDescription>
				</CardHeader>
			</Card>
			<Card>
				<CardHeader class="pb-3">
					<div class="flex items-center gap-2 text-muted-foreground">
						<BarChart3Icon class="size-4" />
						<span class="text-sm">Churn 90g</span>
					</div>
					<CardTitle class="text-3xl">{(report?.churnRatePercentage ?? 0).toFixed(1)}%</CardTitle>
					<CardDescription>
						{report?.churnedLast90DaysCount ?? 0} membri persi negli ultimi 90 giorni
					</CardDescription>
				</CardHeader>
			</Card>
		</div>

		<div class="grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_minmax(320px,0.9fr)] xl:gap-7">
			<Card>
				<CardHeader>
					<CardTitle>Rendimento per sede</CardTitle>
					<CardDescription>
						Confronto rapido su retention, churn e membri a rischio.
					</CardDescription>
				</CardHeader>
				<CardContent>
					{#if (report?.locations.length ?? 0) === 0}
						<p class="text-sm text-muted-foreground">
							Non ci sono sedi visibili nel perimetro corrente del report.
						</p>
					{:else}
						<Table>
							<TableHeader>
								<TableRow>
									<TableHead>Sede</TableHead>
									<TableHead class="text-right">Attivi</TableHead>
									<TableHead class="text-right">In scadenza</TableHead>
									<TableHead class="text-right">A rischio</TableHead>
									<TableHead class="text-right">Retention</TableHead>
									<TableHead class="text-right">Churn</TableHead>
								</TableRow>
							</TableHeader>
							<TableBody>
								{#each report?.locations ?? [] as row}
									<TableRow>
										<TableCell class="font-medium">{row.locationName}</TableCell>
										<TableCell class="text-right">{row.activeMembersCount}</TableCell>
										<TableCell class="text-right">{row.expiringNext30DaysCount}</TableCell>
										<TableCell class="text-right">{row.atRiskMembersCount}</TableCell>
										<TableCell class="text-right">
											{row.retentionRatePercentage.toFixed(1)}%
										</TableCell>
										<TableCell class="text-right">{row.churnRatePercentage.toFixed(1)}%</TableCell>
									</TableRow>
								{/each}
							</TableBody>
						</Table>
					{/if}
				</CardContent>
			</Card>

			<Card>
				<CardHeader>
					<CardTitle>Focus operativo</CardTitle>
					<CardDescription>Membro da contattare prima che diventi churn effettivo.</CardDescription>
				</CardHeader>
				<CardContent>
					{#if topRiskMember}
						<div class="space-y-4">
							<div class="rounded-2xl border border-border/70 p-4">
								<div class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
									<div class="min-w-0">
										<div class="font-medium">{topRiskMember.memberName}</div>
										<div class="text-sm text-muted-foreground">
											{topRiskMember.locationName} - {topRiskMember.memberEmail}
										</div>
									</div>
									<Badge class="w-fit" variant="warning">
										{riskUrgencyLabel(topRiskMember.daysUntilExpiry)}
									</Badge>
								</div>
								<div class="mt-4 grid gap-3 text-sm text-muted-foreground">
									<div>Scadenza tra {topRiskMember.daysUntilExpiry} giorni</div>
									<div>
										Ultimo accesso:
										{topRiskMember.lastAccessAtUtc
											? dateTime.format(topRiskMember.lastAccessAtUtc)
											: 'nessun accesso recente'}
									</div>
									<div>Rinnovo registrato: {topRiskMember.hasRenewalSale ? 'si' : 'no'}</div>
								</div>
							</div>
							<div class="flex flex-wrap gap-2">
								<Button size="sm" onclick={() => openRenewal(topRiskMember.membershipId)}>
									Avvia rinnovo
								</Button>
								<Button variant="outline" size="sm" onclick={() => goto('/analytics/export')}>
									Esporta lista rischio
								</Button>
							</div>
							<p class="text-sm text-muted-foreground">
								Usa CRM campagne o automazioni per anticipare la perdita dei membri in questa
								fascia.
							</p>
						</div>
					{:else}
						<p class="text-sm text-muted-foreground">
							Nessun membro a rischio nel perimetro selezionato.
						</p>
					{/if}
				</CardContent>
			</Card>
		</div>

		<Card>
			<CardHeader>
				<CardTitle>Membri da lavorare subito</CardTitle>
				<CardDescription>
					Lista breve dei casi in scadenza senza rinnovo gia registrato.
				</CardDescription>
			</CardHeader>
			<CardContent>
				{#if (report?.atRiskMembers.length ?? 0) === 0}
					<p class="text-sm text-muted-foreground">Nessun membro a rischio nel periodo corrente.</p>
				{:else}
					<Table>
						<TableHeader>
							<TableRow>
								<TableHead>Membro</TableHead>
								<TableHead>Sede</TableHead>
								<TableHead>Scadenza</TableHead>
								<TableHead>Ultimo accesso</TableHead>
								<TableHead>Azioni</TableHead>
								<TableHead class="text-right">Stato</TableHead>
							</TableRow>
						</TableHeader>
						<TableBody>
							{#each report?.atRiskMembers ?? [] as member}
								<TableRow>
									<TableCell>
										<div>
											<div class="font-medium">{member.memberName}</div>
											<div class="text-sm text-muted-foreground">{member.memberEmail}</div>
										</div>
									</TableCell>
									<TableCell>{member.locationName}</TableCell>
									<TableCell>
										{member.membershipEndsAtUtc
											? dateTime.format(member.membershipEndsAtUtc)
											: 'Non definita'}
									</TableCell>
									<TableCell>
										{member.lastAccessAtUtc
											? dateTime.format(member.lastAccessAtUtc)
											: 'Nessun accesso recente'}
									</TableCell>
									<TableCell>
										<Button
											variant="outline"
											size="sm"
											onclick={() => openRenewal(member.membershipId)}
										>
											Avvia rinnovo
										</Button>
									</TableCell>
									<TableCell class="text-right">
										<Badge variant={member.hasRenewalSale ? 'success' : 'warning'}>
											{member.hasRenewalSale ? 'Rinnovato' : 'Da contattare'}
										</Badge>
									</TableCell>
								</TableRow>
							{/each}
						</TableBody>
					</Table>
				{/if}
			</CardContent>
		</Card>
	{/if}
</main>
