<script lang="ts">
	import { createQuery } from '@tanstack/svelte-query';
	import CalendarClockIcon from '@lucide/svelte/icons/calendar-clock';
	import MegaphoneIcon from '@lucide/svelte/icons/megaphone';
	import PlusCircleIcon from '@lucide/svelte/icons/plus-circle';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import SendIcon from '@lucide/svelte/icons/send';
	import TimerResetIcon from '@lucide/svelte/icons/timer-reset';
	import type { GymStaffAssignmentResponse } from '$lib/api';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '$lib/components/ui/card/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import * as Select from '$lib/components/ui/select/index.js';
	import * as Sheet from '$lib/components/ui/sheet/index.js';
	import { Textarea } from '$lib/components/ui/textarea/index.js';
	import {
		createGymAutomationRule,
		fetchGymAutomationRules,
		pauseGymAutomationRule,
		resumeGymAutomationRule,
		runGymAutomationRule,
		type AutomationScheduleType,
		type AutomationStatus,
		type CampaignAudienceType,
		type CampaignChannel,
		type CreateGymAutomationRuleRequest,
		type GymAutomationRule,
		type LeadStage
	} from '$lib/data/crm-api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
	type AutomationStatusFilter = AutomationStatus | 'all';
	type AutomationChannelFilter = CampaignChannel | 'all';
	const CRM_AUTOMATIONS_NO_SELECTION_SELECT_VALUE = '__none__';

	const api = useApiClient();
	const center = useCenterSelectionStore();
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});

	const channelOptions: Array<{ value: CampaignChannel; label: string }> = [
		{ value: 'Email', label: 'Email' },
		{ value: 'Sms', label: 'SMS' },
		{ value: 'WhatsApp', label: 'WhatsApp' }
	];

	const audienceOptions: Array<{ value: CampaignAudienceType; label: string; description: string }> = [
		{ value: 'ExpiringMemberships', label: 'Rinnovi in scadenza', description: 'Clienti attivi con scadenza entro 30 giorni.' },
		{ value: 'LeadsDueFollowUp', label: 'Lead da richiamare', description: 'Lead con follow-up dovuto entro domani.' },
		{ value: 'LeadsInStage', label: 'Lead per stage', description: 'Lead filtrati su uno stage preciso della pipeline.' },
		{ value: 'ActiveMembers', label: 'Clienti attivi', description: 'Tutti i membri attivi della sede selezionata.' }
	];

	const leadStageOptions: Array<{ value: LeadStage; label: string }> = [
		{ value: 'New', label: 'Nuovi' },
		{ value: 'Contacted', label: 'Contattati' },
		{ value: 'TrialBooked', label: 'Trial booked' },
		{ value: 'Negotiation', label: 'Trattativa' },
		{ value: 'Won', label: 'Chiusi' },
		{ value: 'Lost', label: 'Persi' }
	];

	const scheduleOptions: Array<{ value: AutomationScheduleType; label: string; description: string }> = [
		{ value: 'Daily', label: 'Ogni giorno', description: 'La regola riparte automaticamente ogni 24 ore.' },
		{ value: 'Weekly', label: 'Ogni settimana', description: 'La regola riparte ogni 7 giorni dal prossimo run.' }
	];

	const nextRunDefault = () => {
		const next = new Date();
		next.setDate(next.getDate() + 1);
		next.setHours(8, 30, 0, 0);
		return next;
	};

	const toInputDate = (date?: Date | null) => {
		if (!date) return '';
		const year = date.getFullYear();
		const month = `${date.getMonth() + 1}`.padStart(2, '0');
		const day = `${date.getDate()}`.padStart(2, '0');
		const hours = `${date.getHours()}`.padStart(2, '0');
		const minutes = `${date.getMinutes()}`.padStart(2, '0');
		return `${year}-${month}-${day}T${hours}:${minutes}`;
	};

	const newAutomationForm = () => ({
		locationId: center.selectedLocationId ?? center.locations[0]?.id ?? '',
		ownerAssignmentId: '',
		name: '',
		channel: 'Email' as CampaignChannel,
		audienceType: 'ExpiringMemberships' as CampaignAudienceType,
		targetLeadStage: 'TrialBooked' as LeadStage,
		scheduleType: 'Daily' as AutomationScheduleType,
		nextRunLocal: toInputDate(nextRunDefault()),
		subjectTemplate: '',
		messageTemplate: '',
		notes: ''
	});

	const ownerLabel = (assignment: GymStaffAssignmentResponse) =>
		assignment.staffProfile?.displayName || assignment.userEmail || 'Staff';

	const statusMeta = (status: AutomationStatus): { label: string; variant: BadgeVariant } => {
		if (status === 'Active') return { label: 'Attiva', variant: 'success' };
		return { label: 'In pausa', variant: 'outline' };
	};

	const audienceLabel = (audienceType: CampaignAudienceType) =>
		audienceOptions.find((item) => item.value === audienceType)?.label ?? audienceType;

	const leadStageLabel = (stage: LeadStage | null) =>
		stage ? leadStageOptions.find((item) => item.value === stage)?.label ?? stage : 'Non applicabile';

	const scheduleLabel = (scheduleType: AutomationScheduleType) =>
		scheduleOptions.find((item) => item.value === scheduleType)?.label ?? scheduleType;

	const statusFilterLabel = (status: AutomationStatusFilter) => {
		if (status === 'all') return 'Tutti gli stati';
		return statusMeta(status).label;
	};

	const channelFilterLabel = (channel: AutomationChannelFilter) => {
		if (channel === 'all') return 'Tutti i canali';
		return channelOptions.find((item) => item.value === channel)?.label ?? channel;
	};

	const formLocationLabel = (locationId: string) =>
		center.locations.find((location) => location.id === locationId)?.name ?? 'Seleziona sede';

	const formOwnerLabel = (assignmentId: string) =>
		ownerOptions.find((option) => option.id === assignmentId)?.label ?? 'Desk / non assegnato';

	const formChannelLabel = (channel: CampaignChannel) =>
		channelOptions.find((option) => option.value === channel)?.label ?? channel;

	const formAudienceLabel = (audienceType: CampaignAudienceType) =>
		audienceLabel(audienceType);

	const formLeadStageLabel = (stage: LeadStage) =>
		leadStageOptions.find((option) => option.value === stage)?.label ?? stage;

	const formScheduleLabel = (scheduleType: AutomationScheduleType) =>
		scheduleOptions.find((option) => option.value === scheduleType)?.label ?? scheduleType;

	let statusFilter = $state<AutomationStatusFilter>('all');
	let channelFilter = $state<AutomationChannelFilter>('all');
	let selectedAutomationRuleId = $state<string | null>(null);
	let createOpen = $state(false);
	let createSubmitting = $state(false);
	let actionPending = $state<string | null>(null);
	let feedbackMessage = $state('');
	let feedbackError = $state('');
	let formState = $state(newAutomationForm());

	const staffQuery = createQuery(() => ({
		queryKey: ['crm-automations-staff', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdStaffAssignmentsGet({ gymId: center.selectedGymId! });
			if (!response.success) {
				throw new Error(response.error?.message ?? response.message ?? 'Impossibile caricare il team commerciale.');
			}
			return (response.data ?? []) as GymStaffAssignmentResponse[];
		}
	}));

	const automationRulesQuery = createQuery(() => ({
		queryKey: ['crm-automations', center.selectedGymId, center.selectedLocationId, statusFilter, channelFilter],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymAutomationRules(center.selectedGymId!, {
				locationId: center.selectedLocationId,
				status: statusFilter,
				channel: channelFilter
			})
	}));

	const staffAssignments = $derived(staffQuery.data ?? []);
	const automationRules = $derived(automationRulesQuery.data ?? []);
	const selectedAutomationRule = $derived(
		automationRules.find((rule) => rule.automationRuleId === selectedAutomationRuleId) ?? automationRules[0] ?? null
	);
	const ownerOptions = $derived(
		staffAssignments
			.filter((assignment) => assignment.status === 'Active' && assignment.assignmentId)
			.map((assignment) => ({
				id: assignment.assignmentId ?? '',
				label: ownerLabel(assignment)
			}))
	);
	const activeCount = $derived(automationRules.filter((rule) => rule.status === 'Active').length);
	const pausedCount = $derived(automationRules.filter((rule) => rule.status === 'Paused').length);
	const dueSoonCount = $derived(
		automationRules.filter(
			(rule) => rule.status === 'Active' && rule.nextRunAtUtc.getTime() <= Date.now() + 24 * 60 * 60 * 1000
		).length
	);
	const totalEstimatedAudience = $derived(
		automationRules.reduce((sum, rule) => sum + rule.estimatedAudienceCount, 0)
	);
	const staffQueryError = $derived(
		staffQuery.error instanceof Error
			? staffQuery.error.message
			: staffQuery.error
				? 'Impossibile caricare il team commerciale.'
				: null
	);
	const automationRulesQueryError = $derived(
		automationRulesQuery.error instanceof Error
			? automationRulesQuery.error.message
			: automationRulesQuery.error
				? 'Impossibile caricare le automazioni CRM.'
				: null
	);
	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms
			|| (!!center.selectedGymId
				&& ((!staffQuery.data && staffQuery.isPending)
					|| (!automationRulesQuery.data && automationRulesQuery.isPending)))
	);
	const workspaceError = $derived(
		center.gymsError ?? staffQueryError ?? automationRulesQueryError ?? null
	);

	$effect(() => {
		if (
			automationRules.length > 0 &&
			(!selectedAutomationRuleId ||
				!automationRules.some((item) => item.automationRuleId === selectedAutomationRuleId))
		) {
			selectedAutomationRuleId = automationRules[0].automationRuleId;
		}
	});

	function clearFeedback() {
		feedbackMessage = '';
		feedbackError = '';
	}

	function openCreateSheet() {
		clearFeedback();
		formState = newAutomationForm();
		createOpen = true;
	}

	function refreshAll() {
		if (!center.selectedGymId) {
			return Promise.resolve([]);
		}

		return Promise.all([automationRulesQuery.refetch(), staffQuery.refetch()]);
	}

	async function handleCreateAutomationRule(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (
			!center.selectedGymId ||
			!formState.locationId ||
			!formState.name.trim() ||
			!formState.subjectTemplate.trim() ||
			!formState.messageTemplate.trim() ||
			!formState.nextRunLocal
		) {
			feedbackError = 'Compila sede, nome, prossimo run, oggetto e messaggio.';
			return;
		}

		if (formState.audienceType === 'LeadsInStage' && !formState.targetLeadStage) {
			feedbackError = 'Se scegli un pubblico per stage devi indicare anche lo stage target.';
			return;
		}

		createSubmitting = true;
		try {
			const created = await createGymAutomationRule(center.selectedGymId, {
				locationId: formState.locationId,
				ownerAssignmentId: formState.ownerAssignmentId || null,
				name: formState.name.trim(),
				channel: formState.channel,
				audienceType: formState.audienceType,
				targetLeadStage: formState.audienceType === 'LeadsInStage' ? formState.targetLeadStage : null,
				scheduleType: formState.scheduleType,
				nextRunAtUtc: new Date(formState.nextRunLocal).toISOString(),
				subjectTemplate: formState.subjectTemplate.trim(),
				messageTemplate: formState.messageTemplate.trim(),
				notes: formState.notes.trim() || null
			} satisfies CreateGymAutomationRuleRequest);

			await automationRulesQuery.refetch();
			selectedAutomationRuleId = created.automationRuleId;
			createOpen = false;
			feedbackMessage = `Automazione "${created.name}" salvata correttamente.`;
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile creare l automazione.';
		} finally {
			createSubmitting = false;
		}
	}

	async function handlePauseAutomationRule() {
		if (!center.selectedGymId || !selectedAutomationRule) return;
		clearFeedback();
		actionPending = `${selectedAutomationRule.automationRuleId}-pause`;
		try {
			await pauseGymAutomationRule(center.selectedGymId, selectedAutomationRule.automationRuleId);
			await automationRulesQuery.refetch();
			feedbackMessage = 'Automazione messa in pausa.';
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile mettere in pausa la regola.';
		} finally {
			actionPending = null;
		}
	}

	async function handleResumeAutomationRule() {
		if (!center.selectedGymId || !selectedAutomationRule) return;
		clearFeedback();
		actionPending = `${selectedAutomationRule.automationRuleId}-resume`;
		try {
			await resumeGymAutomationRule(center.selectedGymId, selectedAutomationRule.automationRuleId);
			await automationRulesQuery.refetch();
			feedbackMessage = 'Automazione riattivata.';
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile riattivare la regola.';
		} finally {
			actionPending = null;
		}
	}

	async function handleRunAutomationRule() {
		if (!center.selectedGymId || !selectedAutomationRule) return;
		clearFeedback();
		actionPending = `${selectedAutomationRule.automationRuleId}-run`;
		try {
			await runGymAutomationRule(center.selectedGymId, selectedAutomationRule.automationRuleId);
			await automationRulesQuery.refetch();
			feedbackMessage = 'Run manuale registrato correttamente.';
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile eseguire la regola.';
		} finally {
			actionPending = null;
		}
	}
</script>

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	<section class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
		<div class="space-y-2">
			<h2 class="text-2xl font-semibold tracking-tight">Automazioni CRM</h2>
			<p class="text-sm text-muted-foreground">
				Regole ricorrenti per lavorare lead e rinnovi con prossima esecuzione reale per sede.
			</p>
		</div>

		<div class="flex flex-wrap items-center gap-2">
			<Button
				variant="outline"
				size="sm"
				onclick={() => refreshAll()}
				disabled={!hasSelectedGym || workspaceLoading}
			>
				<RefreshCwIcon class="size-4" />
				Aggiorna
			</Button>
			<Button size="sm" onclick={openCreateSheet} disabled={!hasSelectedGym || workspaceLoading}>
				<PlusCircleIcon class="size-4" />
				Nuova automazione
			</Button>
		</div>
	</section>

	{#if feedbackMessage}
		<section class="rounded-[20px] border border-[#bbf7d0] bg-[#f0fdf4] px-4 py-3 text-sm text-[#166534]">
			{feedbackMessage}
		</section>
	{/if}

	{#if feedbackError}
		<section class="rounded-[20px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]">
			{feedbackError}
		</section>
	{/if}

	{#if workspaceLoading}
		<Card class="border-dashed border-border/70 bg-muted/25">
			<CardHeader>
				<CardTitle>Caricamento automazioni CRM</CardTitle>
				<CardDescription>
					Sto recuperando team commerciale e regole automatiche della sede corrente prima di mostrarti il workspace operativo.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-dashed border-[#fecaca] bg-[#fff7f7]">
			<CardHeader>
				<CardTitle>Impossibile caricare le automazioni CRM</CardTitle>
				<CardDescription>{workspaceError}</CardDescription>
			</CardHeader>
			<CardContent>
				<Button variant="outline" onclick={() => refreshAll()} disabled={!hasSelectedGym}>
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
					Scegli prima la palestra dal selettore in alto a sinistra per vedere regole, owner e prossimi run della sede corretta.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
	<section class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Regole attive</CardDescription>
				<CardTitle class="text-3xl">{activeCount}</CardTitle>
			</CardHeader>
			<CardContent class="text-sm text-muted-foreground">Automazioni che continueranno a ripartire da sole.</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>In pausa</CardDescription>
				<CardTitle class="text-3xl">{pausedCount}</CardTitle>
			</CardHeader>
			<CardContent class="text-sm text-muted-foreground">Regole conservate ma non pianificate per il prossimo slot.</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Run entro 24h</CardDescription>
				<CardTitle class="text-3xl">{dueSoonCount}</CardTitle>
			</CardHeader>
			<CardContent class="text-sm text-muted-foreground">Esecuzioni imminenti da monitorare nel team commerciale.</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Audience stimata</CardDescription>
				<CardTitle class="text-3xl">{totalEstimatedAudience}</CardTitle>
			</CardHeader>
			<CardContent class="text-sm text-muted-foreground">Totale destinatari potenziali delle regole filtrate.</CardContent>
		</Card>
	</section>

	<section class="grid gap-4 xl:grid-cols-[1.1fr_1.2fr]">
		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
				<div>
					<CardTitle>Archivio automazioni</CardTitle>
					<CardDescription>Filtra per stato e canale per controllare le regole in uso.</CardDescription>
				</div>
				<div class="flex flex-wrap gap-2">
					<Select.Root type="single" bind:value={statusFilter}>
						<Select.Trigger class="min-w-[180px]">
							<span data-slot="select-value">{statusFilterLabel(statusFilter)}</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item value="all" label="Tutti gli stati">Tutti gli stati</Select.Item>
							<Select.Item value="Active" label="Attive">Attive</Select.Item>
							<Select.Item value="Paused" label="In pausa">In pausa</Select.Item>
						</Select.Content>
					</Select.Root>
					<Select.Root type="single" bind:value={channelFilter}>
						<Select.Trigger class="min-w-[180px]">
							<span data-slot="select-value">{channelFilterLabel(channelFilter)}</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item value="all" label="Tutti i canali">Tutti i canali</Select.Item>
							<Select.Item value="Email" label="Email">Email</Select.Item>
							<Select.Item value="Sms" label="SMS">SMS</Select.Item>
							<Select.Item value="WhatsApp" label="WhatsApp">WhatsApp</Select.Item>
						</Select.Content>
					</Select.Root>
				</div>
			</CardHeader>
			<CardContent class="space-y-3">
				{#if automationRulesQuery.isPending}
					<div class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center">
						<p class="font-semibold">Carico le automazioni...</p>
					</div>
				{:else if automationRules.length === 0}
					<div class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center">
						<p class="font-semibold">Nessuna automazione nel perimetro corrente</p>
						<p class="mt-2 text-sm text-muted-foreground">
							Crea la prima regola ricorrente per lead e rinnovi.
						</p>
					</div>
				{:else}
					{#each automationRules as rule (rule.automationRuleId)}
						<button
							type="button"
							class={`w-full rounded-[18px] border p-4 text-left transition ${
								selectedAutomationRule?.automationRuleId === rule.automationRuleId
									? 'border-primary bg-primary/5'
									: 'border-border/70 bg-background hover:border-primary/40'
							}`}
							onclick={() => {
								selectedAutomationRuleId = rule.automationRuleId;
							}}
						>
							<div class="flex flex-wrap items-start justify-between gap-3">
								<div class="space-y-1">
									<p class="font-semibold">{rule.name}</p>
									<p class="text-sm text-muted-foreground">
										{rule.locationName} · {rule.channel} · {scheduleLabel(rule.scheduleType)}
									</p>
								</div>
								<Badge variant={statusMeta(rule.status).variant}>{statusMeta(rule.status).label}</Badge>
							</div>
							<div class="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
								<p>Audience stimata: {rule.estimatedAudienceCount}</p>
								<p>Prossimo run {dateTime.format(rule.nextRunAtUtc)}</p>
							</div>
						</button>
					{/each}
				{/if}
			</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader>
				<CardTitle>Dettaglio automazione</CardTitle>
				<CardDescription>Regola, scheduling, target e azioni manuali della regola selezionata.</CardDescription>
			</CardHeader>
			<CardContent class="space-y-4">
				{#if !selectedAutomationRule}
					<div class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center">
						<p class="font-semibold">Seleziona una automazione</p>
					</div>
				{:else}
					<div class="flex flex-wrap items-start justify-between gap-3">
						<div class="space-y-1">
							<h3 class="text-xl font-semibold">{selectedAutomationRule.name}</h3>
							<p class="text-sm text-muted-foreground">
								Creata da {selectedAutomationRule.createdByUserName} · owner {selectedAutomationRule.ownerName ?? 'Desk'} · {selectedAutomationRule.locationName}
							</p>
						</div>
						<Badge variant={statusMeta(selectedAutomationRule.status).variant}>
							{statusMeta(selectedAutomationRule.status).label}
						</Badge>
					</div>

					<div class="grid gap-3 sm:grid-cols-2">
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Canale</p>
							<p class="mt-2 text-sm font-semibold">{selectedAutomationRule.channel}</p>
						</div>
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Audience</p>
							<p class="mt-2 text-sm font-semibold">{audienceLabel(selectedAutomationRule.audienceType)}</p>
							<p class="mt-1 text-sm text-muted-foreground">
								{selectedAutomationRule.audienceType === 'LeadsInStage'
									? `Stage target: ${leadStageLabel(selectedAutomationRule.targetLeadStage)}`
									: `Destinatari stimati: ${selectedAutomationRule.estimatedAudienceCount}`}
							</p>
						</div>
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Scheduling</p>
							<p class="mt-2 text-sm font-semibold">{scheduleLabel(selectedAutomationRule.scheduleType)}</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Prossimo run {dateTime.format(selectedAutomationRule.nextRunAtUtc)}
							</p>
						</div>
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Ultimo run</p>
							<p class="mt-2 text-sm font-semibold">
								{selectedAutomationRule.lastRunAtUtc
									? dateTime.format(selectedAutomationRule.lastRunAtUtc)
									: 'Mai eseguita'}
							</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Reach registrata: {selectedAutomationRule.lastAudienceCount ?? 0}
							</p>
						</div>
					</div>

					<div class="rounded-[18px] border border-border/70 p-4">
						<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Oggetto</p>
						<p class="mt-2 text-sm font-semibold">{selectedAutomationRule.subjectTemplate}</p>
					</div>

					<div class="rounded-[18px] border border-border/70 p-4">
						<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Messaggio</p>
						<p class="mt-2 whitespace-pre-wrap text-sm leading-relaxed text-muted-foreground">
							{selectedAutomationRule.messageTemplate}
						</p>
					</div>

					{#if selectedAutomationRule.notes}
						<div class="rounded-[18px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Note interne</p>
							<p class="mt-2 text-sm text-muted-foreground">{selectedAutomationRule.notes}</p>
						</div>
					{/if}

					<div class="rounded-[18px] border border-border/70 p-4">
						<div class="flex flex-wrap items-center gap-2">
							<CalendarClockIcon class="size-4 text-muted-foreground" />
							<p class="text-sm font-semibold">Controllo regola</p>
						</div>
						<div class="mt-3 flex flex-wrap gap-2">
							<Button
								onclick={handleRunAutomationRule}
								disabled={actionPending === `${selectedAutomationRule.automationRuleId}-run`}
							>
								<SendIcon class="size-4" />
								Run adesso
							</Button>

							{#if selectedAutomationRule.status === 'Active'}
								<Button
									variant="outline"
									onclick={handlePauseAutomationRule}
									disabled={actionPending === `${selectedAutomationRule.automationRuleId}-pause`}
								>
									<TimerResetIcon class="size-4" />
									Metti in pausa
								</Button>
							{:else}
								<Button
									variant="outline"
									onclick={handleResumeAutomationRule}
									disabled={actionPending === `${selectedAutomationRule.automationRuleId}-resume`}
								>
									<TimerResetIcon class="size-4" />
									Riattiva
								</Button>
							{/if}
						</div>
					</div>
				{/if}
			</CardContent>
		</Card>
	</section>
	{/if}
</main>

<Sheet.Root bind:open={createOpen}>
	<Sheet.Content class="w-full overflow-y-auto sm:max-w-2xl">
		<Sheet.Header class="space-y-2">
			<Sheet.Title>Nuova automazione CRM</Sheet.Title>
			<Sheet.Description>
				Crea una regola ricorrente che continua a ripartire dalla sede selezionata.
			</Sheet.Description>
		</Sheet.Header>

		<form class="mt-6 space-y-4" onsubmit={handleCreateAutomationRule}>
			<div class="grid gap-4 md:grid-cols-2">
				<label class="space-y-2 text-sm">
					<span class="font-medium">Sede</span>
					<Select.Root
						type="single"
						value={formState.locationId || CRM_AUTOMATIONS_NO_SELECTION_SELECT_VALUE}
						onValueChange={(value) =>
							(formState.locationId =
								value === CRM_AUTOMATIONS_NO_SELECTION_SELECT_VALUE ? '' : value)}
					>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{formLocationLabel(formState.locationId)}</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item
								value={CRM_AUTOMATIONS_NO_SELECTION_SELECT_VALUE}
								label="Seleziona sede"
							>
								Seleziona sede
							</Select.Item>
							{#each center.locations as location}
								<Select.Item value={location.id} label={location.name}>{location.name}</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
				</label>

				<label class="space-y-2 text-sm">
					<span class="font-medium">Owner commerciale</span>
					<Select.Root
						type="single"
						value={formState.ownerAssignmentId || CRM_AUTOMATIONS_NO_SELECTION_SELECT_VALUE}
						onValueChange={(value) =>
							(formState.ownerAssignmentId =
								value === CRM_AUTOMATIONS_NO_SELECTION_SELECT_VALUE ? '' : value)}
					>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{formOwnerLabel(formState.ownerAssignmentId)}</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item
								value={CRM_AUTOMATIONS_NO_SELECTION_SELECT_VALUE}
								label="Desk / non assegnato"
							>
								Desk / non assegnato
							</Select.Item>
							{#each ownerOptions as option}
								<Select.Item value={option.id} label={option.label}>{option.label}</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
				</label>
			</div>

			<div class="grid gap-4 md:grid-cols-2">
				<label class="space-y-2 text-sm">
					<span class="font-medium">Nome automazione</span>
					<Input bind:value={formState.name} placeholder="Reminder rinnovi desk" />
				</label>

				<label class="space-y-2 text-sm">
					<span class="font-medium">Canale</span>
					<Select.Root type="single" bind:value={formState.channel}>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{formChannelLabel(formState.channel)}</span>
						</Select.Trigger>
						<Select.Content>
							{#each channelOptions as option}
								<Select.Item value={option.value} label={option.label}>{option.label}</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
				</label>
			</div>

			<div class="grid gap-4 md:grid-cols-[1.1fr_0.9fr]">
				<label class="space-y-2 text-sm">
					<span class="font-medium">Audience</span>
					<Select.Root type="single" bind:value={formState.audienceType}>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{formAudienceLabel(formState.audienceType)}</span>
						</Select.Trigger>
						<Select.Content>
							{#each audienceOptions as option}
								<Select.Item value={option.value} label={option.label}>{option.label}</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
					<p class="text-xs text-muted-foreground">
						{audienceOptions.find((option) => option.value === formState.audienceType)?.description}
					</p>
				</label>

				{#if formState.audienceType === 'LeadsInStage'}
					<label class="space-y-2 text-sm">
						<span class="font-medium">Stage target</span>
						<Select.Root type="single" bind:value={formState.targetLeadStage}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{formLeadStageLabel(formState.targetLeadStage)}</span>
							</Select.Trigger>
							<Select.Content>
								{#each leadStageOptions as option}
									<Select.Item value={option.value} label={option.label}>{option.label}</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
				{/if}
			</div>

			<div class="grid gap-4 md:grid-cols-2">
				<label class="space-y-2 text-sm">
					<span class="font-medium">Frequenza</span>
					<Select.Root type="single" bind:value={formState.scheduleType}>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{formScheduleLabel(formState.scheduleType)}</span>
						</Select.Trigger>
						<Select.Content>
							{#each scheduleOptions as option}
								<Select.Item value={option.value} label={option.label}>{option.label}</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
					<p class="text-xs text-muted-foreground">
						{scheduleOptions.find((option) => option.value === formState.scheduleType)?.description}
					</p>
				</label>

				<label class="space-y-2 text-sm">
					<span class="font-medium">Primo prossimo run</span>
					<Input bind:value={formState.nextRunLocal} type="datetime-local" />
				</label>
			</div>

			<label class="space-y-2 text-sm">
				<span class="font-medium">Oggetto</span>
				<Input bind:value={formState.subjectTemplate} placeholder="Promemoria rinnovo FitUp" />
			</label>

			<label class="space-y-2 text-sm">
				<span class="font-medium">Messaggio</span>
				<Textarea bind:value={formState.messageTemplate} rows={7} placeholder="Scrivi il testo della regola ricorrente..." />
			</label>

			<label class="space-y-2 text-sm">
				<span class="font-medium">Note interne</span>
				<Textarea bind:value={formState.notes} rows={3} placeholder="Obiettivo interno, owner, contesto..." />
			</label>

			<div class="flex flex-wrap gap-2 pt-2">
				<Button type="submit" disabled={createSubmitting}>
					{#if createSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvo...
					{:else}
						<MegaphoneIcon class="size-4" />
						Crea automazione
					{/if}
				</Button>
				<Button type="button" variant="outline" onclick={() => (createOpen = false)}>
					Chiudi
				</Button>
			</div>
		</form>
	</Sheet.Content>
</Sheet.Root>
