<script lang="ts">
	import AlertTriangleIcon from '@lucide/svelte/icons/alert-triangle';
	import CreditCardIcon from '@lucide/svelte/icons/credit-card';
	import DownloadIcon from '@lucide/svelte/icons/download';
	import MegaphoneIcon from '@lucide/svelte/icons/megaphone';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import { createQuery } from '@tanstack/svelte-query';
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
		fetchGymReportExportCatalog,
		fetchGymReportExportRows,
		type GymReportExportKey
	} from '$lib/data/reports-api';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';
	import { downloadPdfReport } from '$lib/utils/pdf-report.js';

	type ExportDefinition = {
		key: GymReportExportKey;
		title: string;
		description: string;
		columns: Array<{ header: string; source: string; width?: number }>;
	};

	const center = useCenterSelectionStore();
	const money = new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' });
	const date = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		year: 'numeric'
	});
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});

	const exportDefinitions: ExportDefinition[] = [
		{
			key: 'revenue-by-location',
			title: 'Ricavi per sede',
			description:
				"Esporta incassi del mese e partite aperte per confrontare l'andamento economico delle location.",
			columns: [
				{ header: 'Sede', source: 'location_name', width: 2 },
				{ header: 'Vendite mese', source: 'sales_count_month' },
				{ header: 'Ricavi mese', source: 'revenue_month_amount' },
				{ header: 'Da incassare', source: 'pending_collections_amount' }
			]
		},
		{
			key: 'at-risk-members',
			title: 'Membri a rischio churn',
			description:
				'Lista operativa dei membri in scadenza senza rinnovo registrato, pronta per recall e follow-up.',
			columns: [
				{ header: 'Membro', source: 'member_name', width: 1.5 },
				{ header: 'Sede', source: 'location_name' },
				{ header: 'Email', source: 'member_email', width: 1.6 },
				{ header: 'Scadenza', source: 'membership_ends_at_utc' },
				{ header: 'Ultimo accesso', source: 'last_access_at_utc' },
				{ header: 'Rinnovo', source: 'has_renewal_sale' }
			]
		},
		{
			key: 'lead-pipeline',
			title: 'Pipeline lead',
			description:
				'Elenco dei lead aperti con owner, follow-up e task in sospeso per coordinare il CRM commerciale.',
			columns: [
				{ header: 'Lead', source: 'full_name', width: 1.4 },
				{ header: 'Sede', source: 'location_name' },
				{ header: 'Stage', source: 'stage' },
				{ header: 'Source', source: 'source' },
				{ header: 'Owner', source: 'owner_name' },
				{ header: 'Follow-up', source: 'next_follow_up_at_utc' }
			]
		}
	];

	let activeDownloadKey = $state<GymReportExportKey | null>(null);
	let downloadError = $state<string | null>(null);

	const exportCatalogQuery = createQuery(() => ({
		queryKey: ['reports-export-catalog', center.selectedGymId, center.selectedLocationId],
		enabled: !!center.selectedGymId,
		queryFn: () => fetchGymReportExportCatalog(center.selectedGymId!, center.selectedLocationId)
	}));

	const exportsWithCounts = $derived.by(() => {
		const counts = new Map(
			(exportCatalogQuery.data?.exports ?? []).map((item) => [item.key, item.recordsCount] as const)
		);

		return exportDefinitions.map((definition) => ({
			...definition,
			recordsCount: counts.get(definition.key) ?? 0
		}));
	});

	const scopeLabel = $derived(
		center.selectedLocationId
			? (center.locations.find((location) => location.id === center.selectedLocationId)?.name ??
					'Sede selezionata')
			: 'Tutte le sedi'
	);

	function columnValue(headers: string[], row: string[], source: string) {
		const index = headers.indexOf(source);
		return index >= 0 ? (row[index] ?? '') : '';
	}

	function formatExportValue(source: string, value: string) {
		if (!value) {
			return '-';
		}

		if (source.endsWith('_amount')) {
			return money.format(Number(value));
		}

		if (source.endsWith('_at_utc') || source.endsWith('_ends_at_utc')) {
			const parsed = new Date(value);
			return Number.isNaN(parsed.getTime()) ? value : date.format(parsed);
		}

		if (value === 'true') return 'Si';
		if (value === 'false') return 'No';
		if (value === 'TrialBooked') return 'Trial';
		if (value === 'Negotiation') return 'Trattativa';
		if (value === 'Contacted') return 'Contattato';
		if (value === 'Won') return 'Chiuso';
		if (value === 'Lost') return 'Perso';
		if (value === 'New') return 'Nuovo';

		return value;
	}

	async function handleDownload(exportKey: GymReportExportKey) {
		if (!center.selectedGymId) {
			return;
		}

		activeDownloadKey = exportKey;
		downloadError = null;

		try {
			const definition = exportDefinitions.find((item) => item.key === exportKey);
			if (!definition) {
				throw new Error('Report export non trovato.');
			}

			const exportRows = await fetchGymReportExportRows(
				center.selectedGymId,
				exportKey,
				center.selectedLocationId
			);
			const rows = exportRows.rows.map((row) =>
				definition.columns.map((column) =>
					formatExportValue(column.source, columnValue(exportRows.headers, row, column.source))
				)
			);

			downloadPdfReport(
				{
					title: definition.title,
					subtitle: definition.description,
					reference: exportKey,
					scope: scopeLabel,
					generatedAt: exportCatalogQuery.data?.generatedAtUtc
						? dateTime.format(exportCatalogQuery.data.generatedAtUtc)
						: dateTime.format(new Date()),
					stats: [{ label: 'Righe esportate', value: `${rows.length}`, note: scopeLabel }],
					tables: [
						{
							title: definition.title,
							columns: definition.columns.map((column) => ({
								header: column.header,
								width: column.width
							})),
							rows,
							emptyText: 'Nessuna riga disponibile per il perimetro selezionato.'
						}
					],
					orientation: definition.columns.length > 5 ? 'landscape' : 'portrait'
				},
				exportRows.fileName.replace(/\.csv$/i, '')
			);
		} catch (error) {
			downloadError =
				error instanceof Error ? error.message : 'Non sono riuscito a generare il PDF richiesto.';
		} finally {
			activeDownloadKey = null;
		}
	}
</script>

<main class="grid gap-6 p-4 md:gap-8 md:p-6">
	<div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
		<div>
			<h2 class="text-2xl font-semibold tracking-tight">Export report</h2>
			<p class="text-sm text-muted-foreground">
				Scarica PDF reali, filtrati sul centro selezionato, per analisi operative e controllo
				manageriale.
			</p>
		</div>
		<Button variant="outline" onclick={() => exportCatalogQuery.refetch()}>
			<RefreshCwIcon class="mr-2 size-4" />
			Aggiorna export
		</Button>
	</div>

	<Card class="border-border/70">
		<CardHeader>
			<CardTitle>Pronti per uso reale</CardTitle>
			<CardDescription>
				I file vengono generati sul momento dai dati correnti di Betterfit e sono pronti per
				archivio o condivisione.
			</CardDescription>
		</CardHeader>
		<CardContent
			class="flex flex-col gap-4 text-sm text-muted-foreground lg:flex-row lg:items-center lg:justify-between"
		>
			<div>
				I download seguono il filtro sede attivo nella sidebar, senza esportare dati fuori
				perimetro.
			</div>
			<div>
				Generato:
				{#if exportCatalogQuery.data?.generatedAtUtc}
					{dateTime.format(exportCatalogQuery.data.generatedAtUtc)}
				{:else}
					in attesa dati
				{/if}
			</div>
		</CardContent>
	</Card>

	{#if downloadError}
		<p class="text-sm text-destructive">{downloadError}</p>
	{/if}

	<div class="grid gap-6 md:grid-cols-2 xl:grid-cols-3 xl:gap-7">
		{#each exportsWithCounts as report}
			<Card class="h-full min-w-0 border-border/70">
				<CardHeader class="space-y-4">
					<div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
						<div class="flex min-w-0 items-start gap-3">
							<div class="rounded-2xl border border-border/70 p-3 text-muted-foreground">
								{#if report.key === 'revenue-by-location'}
									<CreditCardIcon class="size-5" />
								{:else if report.key === 'at-risk-members'}
									<AlertTriangleIcon class="size-5" />
								{:else}
									<MegaphoneIcon class="size-5" />
								{/if}
							</div>
							<div class="min-w-0">
								<CardTitle class="text-lg">{report.title}</CardTitle>
								<CardDescription>{report.description}</CardDescription>
							</div>
						</div>
						<Badge class="w-fit" variant="secondary">{report.recordsCount} righe</Badge>
					</div>
				</CardHeader>
				<CardContent class="space-y-4">
					<div class="flex flex-wrap gap-2.5">
						{#each report.columns as column}
							<Badge variant="outline">{column.header}</Badge>
						{/each}
					</div>
					<Button
						class="w-full"
						onclick={() => handleDownload(report.key)}
						disabled={activeDownloadKey !== null}
					>
						<DownloadIcon class="mr-2 size-4" />
						{activeDownloadKey === report.key ? 'Generazione in corso...' : 'Scarica PDF'}
					</Button>
				</CardContent>
			</Card>
		{/each}
	</div>
</main>
