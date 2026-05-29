<script lang="ts">
	import { goto } from '$app/navigation';
	import { createQuery } from '@tanstack/svelte-query';
	import BadgeCheckIcon from '@lucide/svelte/icons/badge-check';
	import Building2Icon from '@lucide/svelte/icons/building-2';
	import DoorOpenIcon from '@lucide/svelte/icons/door-open';
	import MailIcon from '@lucide/svelte/icons/mail';
	import MessageCircleIcon from '@lucide/svelte/icons/message-circle';
	import ReceiptTextIcon from '@lucide/svelte/icons/receipt-text';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import RouterIcon from '@lucide/svelte/icons/router';
	import TestTubeDiagonalIcon from '@lucide/svelte/icons/test-tube-diagonal';
	import type { GymLocationResponse } from '$lib/api';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import * as Select from '$lib/components/ui/select/index.js';
	import * as Sheet from '$lib/components/ui/sheet/index.js';
	import { Textarea } from '$lib/components/ui/textarea/index.js';
	import {
		fetchGymIntegrations,
		testGymIntegration,
		upsertGymIntegration,
		type GymIntegration,
		type GymIntegrationStatus,
		type GymIntegrationType
	} from '$lib/data/integrations-api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	type IntegrationDefinition = {
		type: GymIntegrationType;
		title: string;
		description: string;
		scope: 'tenant' | 'location';
		requiredFields: string[];
	};
	const SETTINGS_INTEGRATIONS_SELECT_LOCATION_VALUE = '__select_location__';

	const api = useApiClient();
	const center = useCenterSelectionStore();
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});

	const definitions: IntegrationDefinition[] = [
		{
			type: 'EmailDelivery',
			title: 'Email delivery',
			description: 'Invio email per campagne CRM, automazioni e reminder commerciali.',
			scope: 'tenant',
			requiredFields: ['Provider', 'Endpoint', 'Username', 'Credenziale', 'Sender identity']
		},
		{
			type: 'WhatsAppMessaging',
			title: 'WhatsApp messaging',
			description: 'Canale rapido per lead follow-up, trial reminder e recall rinnovi.',
			scope: 'tenant',
			requiredFields: ['Provider', 'Endpoint', 'Credenziale', 'Sender identity']
		},
		{
			type: 'AccessControl',
			title: 'Access control',
			description: 'Bridge operativo con tornelli, varchi o desk check-in per una sede reale.',
			scope: 'location',
			requiredFields: ['Sede', 'Endpoint', 'Credenziale']
		},
		{
			type: 'AccountingExport',
			title: 'Accounting export',
			description: 'Export vendite e incassi verso il sistema amministrativo o fiscale esterno.',
			scope: 'tenant',
			requiredFields: ['Provider', 'Endpoint', 'Credenziale', 'External account']
		}
	];

	const statusOptions: Array<{ value: GymIntegrationStatus; label: string }> = [
		{ value: 'Draft', label: 'Bozza' },
		{ value: 'Active', label: 'Attiva' },
		{ value: 'Disabled', label: 'Disabilitata' }
	];

	let feedbackMessage = $state('');
	let feedbackError = $state('');
	let sheetOpen = $state(false);
	let saving = $state(false);
	let testing = $state(false);
	let selectedType = $state<GymIntegrationType>('EmailDelivery');
	let form = $state({
		locationId: '',
		displayName: '',
		providerName: '',
		status: 'Draft' as GymIntegrationStatus,
		endpointUrl: '',
		username: '',
		apiKey: '',
		externalAccountId: '',
		senderIdentity: '',
		notes: ''
	});

	const integrationsQuery = createQuery(() => ({
		queryKey: ['settings-integrations', center.selectedGymId, center.selectedLocationId],
		enabled: !!center.selectedGymId,
		queryFn: () => fetchGymIntegrations(center.selectedGymId!, center.selectedLocationId)
	}));

	const locationsQuery = createQuery(() => ({
		queryKey: ['settings-integrations-locations', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdLocationsGet({ gymId: center.selectedGymId! });
			if (!response.success) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile caricare le sedi.'
				);
			}
			return (response.data ?? []) as GymLocationResponse[];
		}
	}));

	const integrations = $derived(integrationsQuery.data ?? []);
	const locations = $derived(locationsQuery.data ?? []);
	const integrationsError = $derived(
		integrationsQuery.error instanceof Error
			? integrationsQuery.error.message
			: integrationsQuery.error
				? 'Impossibile caricare le integrazioni del tenant.'
				: null
	);
	const locationsError = $derived(
		locationsQuery.error instanceof Error
			? locationsQuery.error.message
			: locationsQuery.error
				? 'Impossibile caricare le sedi collegate.'
				: null
	);
	const activeIntegrationsCount = $derived(
		integrations.filter((item) => item.status === 'Active').length
	);
	const healthyIntegrationsCount = $derived(
		integrations.filter((item) => item.lastSyncSucceeded === true).length
	);
	const selectedDefinition = $derived(
		definitions.find((definition) => definition.type === selectedType) ?? definitions[0]
	);
	const selectedIntegration = $derived(
		integrations.find((integration) => integration.type === selectedType) ?? null
	);
	const selectedStatusLabel = $derived(
		statusOptions.find((option) => option.value === form.status)?.label ?? 'Seleziona stato'
	);
	const selectedIntegrationLocationLabel = $derived(
		form.locationId
			? (locations.find((location) => location.id === form.locationId)?.name ?? 'Sede selezionata')
			: 'Seleziona una sede'
	);
	const selectedSheetScopeLabel = $derived(
		selectedDefinition.scope === 'tenant'
			? 'Tutto il tenant'
			: form.locationId
				? (locations.find((location) => location.id === form.locationId)?.name ??
					'Sede selezionata')
				: 'Nessuna sede assegnata'
	);
	const integrationCards = $derived.by(() =>
		definitions.map((definition) => ({
			...definition,
			integration: integrations.find((item) => item.type === definition.type) ?? null
		}))
	);
	const workspaceLoading = $derived(
		center.isLoadingGyms ||
			(!!center.selectedGymId && (integrationsQuery.isPending || locationsQuery.isPending))
	);
	const workspaceError = $derived(center.gymsError ?? integrationsError ?? locationsError ?? null);
	const hasSelectedGym = $derived(!!center.selectedGymId);

	function clearFeedback() {
		feedbackMessage = '';
		feedbackError = '';
	}

	function iconFor(type: GymIntegrationType) {
		if (type === 'EmailDelivery') return MailIcon;
		if (type === 'WhatsAppMessaging') return MessageCircleIcon;
		if (type === 'AccessControl') return DoorOpenIcon;
		return ReceiptTextIcon;
	}

	const EmailDeliveryIcon = iconFor('EmailDelivery');
	const WhatsAppMessagingIcon = iconFor('WhatsAppMessaging');
	const AccessControlIcon = iconFor('AccessControl');
	const AccountingExportIcon = iconFor('AccountingExport');

	function iconComponent(type: GymIntegrationType) {
		if (type === 'EmailDelivery') return EmailDeliveryIcon;
		if (type === 'WhatsAppMessaging') return WhatsAppMessagingIcon;
		if (type === 'AccessControl') return AccessControlIcon;
		return AccountingExportIcon;
	}

	function statusMeta(integration: GymIntegration | null) {
		if (!integration) return { label: 'Non configurata', variant: 'outline' as const };
		if (integration.status === 'Active') return { label: 'Attiva', variant: 'success' as const };
		if (integration.status === 'Disabled')
			return { label: 'Disabilitata', variant: 'outline' as const };
		return { label: 'Bozza', variant: 'secondary' as const };
	}

	function healthMeta(integration: GymIntegration | null) {
		if (!integration) return { label: 'Mai testata', variant: 'outline' as const };
		if (integration.lastSyncSucceeded === true)
			return { label: 'Test OK', variant: 'success' as const };
		if (integration.lastSyncSucceeded === false)
			return { label: 'Test KO', variant: 'warning' as const };
		return { label: 'Da verificare', variant: 'secondary' as const };
	}

	const selectedSheetStatus = $derived(statusMeta(selectedIntegration));
	const selectedSheetHealth = $derived(healthMeta(selectedIntegration));
	const selectedSheetLastAttemptLabel = $derived(
		selectedIntegration?.lastSyncAttemptAtUtc
			? dateTime.format(selectedIntegration.lastSyncAttemptAtUtc)
			: 'Mai eseguito'
	);

	function openSheet(definition: IntegrationDefinition, integration: GymIntegration | null) {
		clearFeedback();
		selectedType = definition.type;
		form = {
			locationId:
				integration?.locationId ??
				(definition.scope === 'location' ? (center.selectedLocationId ?? '') : ''),
			displayName: integration?.displayName ?? definition.title,
			providerName: integration?.providerName ?? '',
			status: integration?.status ?? 'Draft',
			endpointUrl: integration?.endpointUrl ?? '',
			username: integration?.username ?? '',
			apiKey: '',
			externalAccountId: integration?.externalAccountId ?? '',
			senderIdentity: integration?.senderIdentity ?? '',
			notes: integration?.notes ?? ''
		};
		sheetOpen = true;
	}

	async function refreshAll() {
		if (!center.selectedGymId) {
			return;
		}

		await Promise.all([integrationsQuery.refetch(), locationsQuery.refetch()]);
	}

	async function handleSave(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !selectedDefinition) {
			feedbackError = 'Seleziona una palestra prima di configurare le integrazioni.';
			return;
		}

		if (!form.displayName.trim() || !form.providerName.trim()) {
			feedbackError = 'Nome integrazione e provider sono obbligatori.';
			return;
		}

		if (selectedDefinition.scope === 'location' && !form.locationId) {
			feedbackError = 'Questa integrazione richiede una sede specifica.';
			return;
		}

		saving = true;
		try {
			const saved = await upsertGymIntegration(center.selectedGymId, selectedDefinition.type, {
				locationId: selectedDefinition.scope === 'location' ? form.locationId || null : null,
				displayName: form.displayName.trim(),
				providerName: form.providerName.trim(),
				status: form.status,
				endpointUrl: form.endpointUrl.trim() || null,
				username: form.username.trim() || null,
				apiKey: form.apiKey.trim() || null,
				externalAccountId: form.externalAccountId.trim() || null,
				senderIdentity: form.senderIdentity.trim() || null,
				notes: form.notes.trim() || null
			});

			await integrationsQuery.refetch();
			sheetOpen = false;
			feedbackMessage = `Integrazione ${saved.displayName} salvata correttamente.`;
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile salvare l integrazione.';
		} finally {
			saving = false;
		}
	}

	async function handleTest(integrationType: GymIntegrationType) {
		clearFeedback();

		if (!center.selectedGymId) {
			feedbackError = 'Seleziona una palestra prima di lanciare il test.';
			return;
		}

		testing = true;
		try {
			const tested = await testGymIntegration(center.selectedGymId, integrationType);
			await integrationsQuery.refetch();
			feedbackMessage = tested.lastSyncSucceeded
				? `${tested.displayName}: test completato con esito positivo.`
				: `${tested.displayName}: test fallito, controlla i campi richiesti.`;
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile eseguire il test integrazione.';
		} finally {
			testing = false;
		}
	}
</script>

<main class="grid gap-6 p-4 md:gap-8 md:p-6">
	<div class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
		<div class="space-y-2">
			<div class="flex flex-wrap items-center gap-2">
				<Badge variant="secondary" class="rounded-full px-3 py-1">Integrazioni</Badge>
				{#if center.selectedGym}
					<Badge variant="outline" class="rounded-full px-3 py-1">{center.selectedGym.name}</Badge>
				{/if}
			</div>
			<div>
				<h2 class="text-2xl font-semibold tracking-tight">Provider, bridge e canali esterni</h2>
				<p class="text-sm text-muted-foreground">
					Configura i collegamenti che servono davvero a CRM, accessi e amministrazione del club.
				</p>
			</div>
		</div>
		<div class="flex flex-wrap items-center gap-2">
			<Button variant="outline" size="sm" onclick={() => goto('/settings')}>
				<Building2Icon class="size-4" />
				Torna a settings
			</Button>
			<Button
				variant="outline"
				size="sm"
				onclick={refreshAll}
				disabled={!hasSelectedGym || workspaceLoading}
			>
				<RefreshCwIcon class="size-4" />
				Aggiorna
			</Button>
		</div>
	</div>

	{#if feedbackMessage}
		<section
			class="rounded-[20px] border border-[#bbf7d0] bg-[#f0fdf4] px-4 py-3 text-sm text-[#166534]"
		>
			{feedbackMessage}
		</section>
	{/if}

	{#if feedbackError}
		<section
			class="rounded-[20px] border border-[#fecaca] bg-[#fff1f2] px-4 py-3 text-sm text-[#b91c1c]"
		>
			{feedbackError}
		</section>
	{/if}

	{#if workspaceLoading}
		<Card class="border-dashed border-border/70 bg-muted/25">
			<CardHeader>
				<CardTitle>Caricamento integrazioni</CardTitle>
				<CardDescription>
					Stiamo recuperando tenant, sedi e configurazioni collegate al gestionale.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-dashed border-[#fecaca] bg-[#fff1f2]">
			<CardHeader>
				<CardTitle>Impossibile caricare le integrazioni</CardTitle>
				<CardDescription>{workspaceError}</CardDescription>
			</CardHeader>
			<CardContent>
				<Button variant="outline" onclick={refreshAll} disabled={!hasSelectedGym}>
					<RefreshCwIcon class="mr-2 size-4" />
					Riprova
				</Button>
			</CardContent>
		</Card>
	{:else if !hasSelectedGym}
		<Card class="border-dashed border-border/70 bg-muted/25">
			<CardHeader>
				<CardTitle>Seleziona un tenant</CardTitle>
				<CardDescription>
					Scegli prima la palestra dal selettore in alto a sinistra per vedere lo stato reale delle
					integrazioni e configurarle.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
		<div class="grid gap-4 md:grid-cols-2 lg:gap-5 xl:grid-cols-4">
			<Card>
				<CardHeader class="pb-3">
					<CardDescription>Integrazioni attive</CardDescription>
					<CardTitle class="text-3xl">{activeIntegrationsCount}</CardTitle>
				</CardHeader>
			</Card>
			<Card>
				<CardHeader class="pb-3">
					<CardDescription>Test positivi</CardDescription>
					<CardTitle class="text-3xl">{healthyIntegrationsCount}</CardTitle>
				</CardHeader>
			</Card>
			<Card>
				<CardHeader class="pb-3">
					<CardDescription>Scope per sede</CardDescription>
					<CardTitle class="text-3xl"
						>{integrationCards.filter((item) => item.scope === 'location').length}</CardTitle
					>
				</CardHeader>
			</Card>
			<Card>
				<CardHeader class="pb-3">
					<CardDescription>Provider gestiti</CardDescription>
					<CardTitle class="text-3xl">{definitions.length}</CardTitle>
				</CardHeader>
			</Card>
		</div>

		<div class="grid gap-6 lg:grid-cols-2 xl:gap-7">
			{#each integrationCards as item}
				{@const Icon = iconComponent(item.type)}
				<Card class="h-full min-w-0 border-border/70">
					<CardHeader class="space-y-4">
						<div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
							<div class="flex min-w-0 items-start gap-3">
								<div class="rounded-2xl border border-border/70 p-3 text-muted-foreground">
									<Icon class="size-5" />
								</div>
								<div class="min-w-0">
									<CardTitle class="text-lg">{item.title}</CardTitle>
									<CardDescription>{item.description}</CardDescription>
								</div>
							</div>
							<div class="flex flex-wrap items-start gap-2 sm:flex-col sm:items-end">
								<Badge variant={statusMeta(item.integration).variant}
									>{statusMeta(item.integration).label}</Badge
								>
								<Badge variant={healthMeta(item.integration).variant}
									>{healthMeta(item.integration).label}</Badge
								>
							</div>
						</div>
					</CardHeader>
					<CardContent class="space-y-4">
						<div class="grid gap-3 text-sm text-muted-foreground sm:grid-cols-2">
							<div
								class="flex flex-col gap-1 rounded-[16px] border border-border/50 bg-secondary/10 px-3 py-3 sm:min-h-[76px]"
							>
								<span>Provider</span>
								<span class="font-medium text-foreground"
									>{item.integration?.providerName ?? 'Non impostato'}</span
								>
							</div>
							<div
								class="flex flex-col gap-1 rounded-[16px] border border-border/50 bg-secondary/10 px-3 py-3 sm:min-h-[76px]"
							>
								<span>Scope</span>
								<span class="font-medium text-foreground">
									{item.integration?.locationName ??
										(item.scope === 'tenant' ? 'Tutto il tenant' : 'Da scegliere')}
								</span>
							</div>
							<div
								class="flex flex-col gap-1 rounded-[16px] border border-border/50 bg-secondary/10 px-3 py-3 sm:min-h-[76px]"
							>
								<span>Credenziale</span>
								<span class="font-medium text-foreground">
									{item.integration?.hasCredentialConfigured ? 'Configurata' : 'Mancante'}
								</span>
							</div>
							<div
								class="flex flex-col gap-1 rounded-[16px] border border-border/50 bg-secondary/10 px-3 py-3 sm:min-h-[76px]"
							>
								<span>Ultimo test</span>
								<span class="font-medium text-foreground">
									{item.integration?.lastSyncAttemptAtUtc
										? dateTime.format(item.integration.lastSyncAttemptAtUtc)
										: 'Mai eseguito'}
								</span>
							</div>
						</div>

						<div
							class="rounded-2xl border border-border/70 bg-muted/30 px-4 py-3 text-sm text-muted-foreground"
						>
							<div class="mb-1 flex items-center gap-2 font-medium text-foreground">
								<RouterIcon class="size-4" />
								Requisiti operativi
							</div>
							{item.requiredFields.join(', ')}
						</div>

						<div class="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
							<Button variant="outline" onclick={() => openSheet(item, item.integration)}>
								Configura
							</Button>
							<Button
								variant="outline"
								onclick={() => handleTest(item.type)}
								disabled={testing || !item.integration}
							>
								<TestTubeDiagonalIcon class="mr-2 size-4" />
								Test connessione
							</Button>
						</div>

						{#if item.integration?.lastSyncMessage}
							<p class="text-sm text-muted-foreground">{item.integration.lastSyncMessage}</p>
						{/if}
					</CardContent>
				</Card>
			{/each}
		</div>
	{/if}

	<Sheet.Root bind:open={sheetOpen}>
		<Sheet.Content side="right" class="flex w-full max-w-3xl flex-col p-0">
			<Sheet.Header class="border-b border-border/70 px-5 py-4 sm:px-6">
				<div class="space-y-3">
					<div class="flex flex-wrap items-center gap-2">
						<Badge variant={selectedSheetStatus.variant}>{selectedSheetStatus.label}</Badge>
						<Badge variant={selectedSheetHealth.variant}>{selectedSheetHealth.label}</Badge>
						<Badge variant="outline">
							{selectedDefinition.scope === 'tenant' ? 'Scope tenant' : 'Scope sede'}
						</Badge>
					</div>
					<div class="space-y-2">
						<Sheet.Title>{selectedDefinition.title}</Sheet.Title>
						<Sheet.Description>{selectedDefinition.description}</Sheet.Description>
					</div>
				</div>
			</Sheet.Header>

			<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleSave}>
				<div class="flex-1 space-y-5 overflow-y-auto px-5 py-5 sm:px-6">
					<section class="grid gap-3 sm:grid-cols-3 xl:grid-cols-4">
						<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
							<p class="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
								Scope
							</p>
							<p class="mt-2 text-sm font-medium text-foreground">{selectedSheetScopeLabel}</p>
						</div>
						<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
							<p class="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
								Ultimo test
							</p>
							<p class="mt-2 text-sm font-medium text-foreground">
								{selectedSheetLastAttemptLabel}
							</p>
						</div>
						<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
							<p class="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
								Credenziale
							</p>
							<p class="mt-2 text-sm font-medium text-foreground">
								{selectedIntegration?.hasCredentialConfigured ? 'Gia configurata' : 'Da inserire'}
							</p>
						</div>
						<div
							class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3 sm:col-span-3 xl:col-span-1"
						>
							<p class="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
								Campi minimi
							</p>
							<p class="mt-2 text-sm font-medium text-foreground">
								{selectedDefinition.requiredFields.length} richiesti
							</p>
						</div>
					</section>

					<section
						class="rounded-[24px] border border-border/70 bg-card px-4 py-4 shadow-sm sm:px-5"
					>
						<div>
							<h3 class="text-sm font-semibold text-foreground">Identita integrazione</h3>
							<p class="mt-1 text-sm text-muted-foreground">
								Nome operativo, provider e stato usati dal team per riconoscere questa connessione.
							</p>
						</div>
						<div class="mt-4 grid gap-4 lg:grid-cols-2">
							<label class="grid gap-2 text-sm font-medium">
								Nome integrazione
								<Input bind:value={form.displayName} placeholder="Nome operativo integrazione" />
								<p class="text-xs font-normal text-muted-foreground">
									Il nome che vedono staff, reception e amministrazione.
								</p>
							</label>

							<label class="grid gap-2 text-sm font-medium">
								Provider
								<Input
									bind:value={form.providerName}
									placeholder="Es. Brevo, Twilio, GymGate, Fatture in Cloud"
								/>
								<p class="text-xs font-normal text-muted-foreground">
									Indica il servizio reale o il bridge tecnico collegato.
								</p>
							</label>

							<label class="grid gap-2 text-sm font-medium">
								Stato
								<Select.Root type="single" bind:value={form.status}>
									<Select.Trigger class="w-full">
										<span data-slot="select-value">{selectedStatusLabel}</span>
									</Select.Trigger>
									<Select.Content>
										{#each statusOptions as option}
											<Select.Item value={option.value} label={option.label}>
												{option.label}
											</Select.Item>
										{/each}
									</Select.Content>
								</Select.Root>
								<p class="text-xs font-normal text-muted-foreground">
									Usa "Bozza" finche il test non e stato confermato dal team.
								</p>
							</label>

							{#if selectedDefinition.scope === 'location'}
								<label class="grid gap-2 text-sm font-medium">
									Sede collegata
									<Select.Root
										type="single"
										value={form.locationId || SETTINGS_INTEGRATIONS_SELECT_LOCATION_VALUE}
										onValueChange={(value) =>
											(form.locationId =
												value === SETTINGS_INTEGRATIONS_SELECT_LOCATION_VALUE ? '' : value)}
									>
										<Select.Trigger class="w-full">
											<span data-slot="select-value">{selectedIntegrationLocationLabel}</span>
										</Select.Trigger>
										<Select.Content>
											<Select.Item
												value={SETTINGS_INTEGRATIONS_SELECT_LOCATION_VALUE}
												label="Seleziona una sede"
											>
												Seleziona una sede
											</Select.Item>
											{#each locations as location}
												{#if location.id}
													<Select.Item value={location.id} label={location.name ?? 'Sede'}>
														{location.name ?? 'Sede'}
													</Select.Item>
												{/if}
											{/each}
										</Select.Content>
									</Select.Root>
									<p class="text-xs font-normal text-muted-foreground">
										Serve per bridge accessi o sistemi attivi solo su una singola sede.
									</p>
								</label>
							{/if}
						</div>
					</section>

					<section
						class="rounded-[24px] border border-border/70 bg-card px-4 py-4 shadow-sm sm:px-5"
					>
						<div>
							<h3 class="text-sm font-semibold text-foreground">Connessione</h3>
							<p class="mt-1 text-sm text-muted-foreground">
								Endpoint, utente tecnico e credenziali richieste dal provider o dal bridge esterno.
							</p>
						</div>
						<div class="mt-4 grid gap-4 lg:grid-cols-2">
							<label class="grid gap-2 text-sm font-medium lg:col-span-2">
								Endpoint
								<Input bind:value={form.endpointUrl} placeholder="https://provider.example/api" />
								<p class="text-xs font-normal text-muted-foreground">
									Inserisci URL base, endpoint SOAP o indirizzo del bridge locale.
								</p>
							</label>

							<label class="grid gap-2 text-sm font-medium">
								Utente tecnico
								<Input
									bind:value={form.username}
									placeholder="Utente tecnico, mailbox o service account"
								/>
								<p class="text-xs font-normal text-muted-foreground">
									Facoltativo, ma utile quando il provider usa login dedicati.
								</p>
							</label>

							<label class="grid gap-2 text-sm font-medium">
								Credenziale / API key
								<Input
									bind:value={form.apiKey}
									placeholder={selectedIntegration?.hasCredentialConfigured
										? 'Lascia vuoto per mantenere la chiave attuale'
										: 'Inserisci la credenziale'}
								/>
								<p class="text-xs font-normal text-muted-foreground">
									La chiave attuale non viene sovrascritta se lasci il campo vuoto.
								</p>
							</label>

							<label class="grid gap-2 text-sm font-medium">
								Account esterno
								<Input
									bind:value={form.externalAccountId}
									placeholder="Id account, business unit o bridge esterno"
								/>
								<p class="text-xs font-normal text-muted-foreground">
									Utile per export contabilita, tenant remoti o ambienti multi-club.
								</p>
							</label>

							<label class="grid gap-2 text-sm font-medium">
								Identita mittente
								<Input
									bind:value={form.senderIdentity}
									placeholder="Email mittente, numero WhatsApp o alias tecnico"
								/>
								<p class="text-xs font-normal text-muted-foreground">
									Compare nei messaggi inviati a lead, clienti o staff.
								</p>
							</label>
						</div>
					</section>

					<section
						class="rounded-[24px] border border-border/70 bg-card px-4 py-4 shadow-sm sm:px-5"
					>
						<div>
							<h3 class="text-sm font-semibold text-foreground">Contesto operativo</h3>
							<p class="mt-1 text-sm text-muted-foreground">
								Note interne e checklist minima per capire se il test connessione e pronto.
							</p>
						</div>

						<label class="mt-4 grid gap-2 text-sm font-medium">
							Note operative
							<Textarea
								bind:value={form.notes}
								rows={6}
								placeholder="Annotazioni per il team operativo o IT"
							/>
							<p class="text-xs font-normal text-muted-foreground">
								Registra porte, referenti, account tecnici o dipendenze note prima del go-live.
							</p>
						</label>

						<div
							class="mt-4 rounded-[20px] border border-border/70 bg-muted/30 px-4 py-4 text-sm text-muted-foreground"
						>
							<div class="mb-2 flex items-center gap-2 font-medium text-foreground">
								<BadgeCheckIcon class="size-4" />
								Campi richiesti per il test
							</div>
							<p>{selectedDefinition.requiredFields.join(', ')}</p>
							{#if selectedIntegration?.lastSyncMessage}
								<div class="mt-3 rounded-2xl border border-border/70 bg-background px-3 py-3">
									<p class="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
										Ultimo esito registrato
									</p>
									<p class="mt-2 text-sm text-foreground">
										{selectedIntegration.lastSyncMessage}
									</p>
								</div>
							{/if}
						</div>
					</section>
				</div>

				<Sheet.Footer
					class="border-t border-border/70 bg-background/95 px-5 py-4 sm:flex-row sm:justify-between sm:px-6"
				>
					<div class="hidden text-sm text-muted-foreground sm:block">
						Salva la configurazione e poi lancia un test connessione dalla scheda integrazione.
					</div>
					<Button type="button" variant="outline" onclick={() => (sheetOpen = false)}>
						Annulla
					</Button>
					<Button type="submit" disabled={saving}>
						{saving ? 'Salvataggio...' : 'Salva integrazione'}
					</Button>
				</Sheet.Footer>
			</form>
		</Sheet.Content>
	</Sheet.Root>
</main>
