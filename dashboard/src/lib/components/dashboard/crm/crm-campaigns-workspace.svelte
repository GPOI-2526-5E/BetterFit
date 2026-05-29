<script lang="ts">
	import { createQuery } from '@tanstack/svelte-query';
	import CalendarClockIcon from '@lucide/svelte/icons/calendar-clock';
	import MailIcon from '@lucide/svelte/icons/mail';
	import MegaphoneIcon from '@lucide/svelte/icons/megaphone';
	import PlusCircleIcon from '@lucide/svelte/icons/plus-circle';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import SendIcon from '@lucide/svelte/icons/send';
	import TimerResetIcon from '@lucide/svelte/icons/timer-reset';
	import ArchiveIcon from '@lucide/svelte/icons/archive';
	import type { GymStaffAssignmentResponse } from '$lib/api';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '$lib/components/ui/card/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import * as Select from '$lib/components/ui/select/index.js';
	import * as Sheet from '$lib/components/ui/sheet/index.js';
	import { Textarea } from '$lib/components/ui/textarea/index.js';
	import {
		archiveGymCampaign,
		createGymCampaign,
		fetchGymCampaigns,
		scheduleGymCampaign,
		sendGymCampaign,
		type CampaignAudienceType,
		type CampaignChannel,
		type CampaignStatus,
		type CreateGymCampaignRequest,
		type GymCampaign,
		type LeadStage
	} from '$lib/data/crm-api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
	type CampaignStatusFilter = CampaignStatus | 'all';
	type CampaignChannelFilter = CampaignChannel | 'all';
	const CRM_CAMPAIGNS_NO_SELECTION_SELECT_VALUE = '__none__';

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

	const newCampaignForm = () => ({
		locationId: center.selectedLocationId ?? center.locations[0]?.id ?? '',
		ownerAssignmentId: '',
		name: '',
		channel: 'Email' as CampaignChannel,
		audienceType: 'ExpiringMemberships' as CampaignAudienceType,
		targetLeadStage: 'TrialBooked' as LeadStage,
		subject: '',
		message: '',
		notes: '',
		scheduledAtLocal: ''
	});

	const toInputDate = (date?: Date | null) => {
		if (!date) return '';
		const year = date.getFullYear();
		const month = `${date.getMonth() + 1}`.padStart(2, '0');
		const day = `${date.getDate()}`.padStart(2, '0');
		const hours = `${date.getHours()}`.padStart(2, '0');
		const minutes = `${date.getMinutes()}`.padStart(2, '0');
		return `${year}-${month}-${day}T${hours}:${minutes}`;
	};

	const ownerLabel = (assignment: GymStaffAssignmentResponse) =>
		assignment.staffProfile?.displayName || assignment.userEmail || 'Staff';

	const statusMeta = (status: CampaignStatus): { label: string; variant: BadgeVariant } => {
		if (status === 'Sent') return { label: 'Inviata', variant: 'success' };
		if (status === 'Scheduled') return { label: 'Schedulata', variant: 'warning' };
		if (status === 'Archived') return { label: 'Archiviata', variant: 'outline' };
		return { label: 'Bozza', variant: 'secondary' };
	};

	const audienceLabel = (audienceType: CampaignAudienceType) =>
		audienceOptions.find((item) => item.value === audienceType)?.label ?? audienceType;

	const leadStageLabel = (stage: LeadStage | null) =>
		stage ? leadStageOptions.find((item) => item.value === stage)?.label ?? stage : 'Non applicabile';

	const statusFilterLabel = (status: CampaignStatusFilter) => {
		if (status === 'all') return 'Tutti gli stati';
		return statusMeta(status).label;
	};

	const channelFilterLabel = (channel: CampaignChannelFilter) => {
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

	let statusFilter = $state<CampaignStatusFilter>('all');
	let channelFilter = $state<CampaignChannelFilter>('all');
	let selectedCampaignId = $state<string | null>(null);
	let createOpen = $state(false);
	let createSubmitting = $state(false);
	let campaignActionPending = $state<string | null>(null);
	let feedbackMessage = $state('');
	let feedbackError = $state('');
	let scheduleAtLocal = $state('');
	let formState = $state(newCampaignForm());

	const staffQuery = createQuery(() => ({
		queryKey: ['crm-campaigns-staff', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdStaffAssignmentsGet({ gymId: center.selectedGymId! });
			if (!response.success) {
				throw new Error(response.error?.message ?? response.message ?? 'Impossibile caricare il team commerciale.');
			}
			return (response.data ?? []) as GymStaffAssignmentResponse[];
		}
	}));

	const campaignsQuery = createQuery(() => ({
		queryKey: ['crm-campaigns', center.selectedGymId, center.selectedLocationId, statusFilter, channelFilter],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymCampaigns(center.selectedGymId!, {
				locationId: center.selectedLocationId,
				status: statusFilter,
				channel: channelFilter
			})
	}));

	const staffAssignments = $derived(staffQuery.data ?? []);
	const campaigns = $derived(campaignsQuery.data ?? []);
	const selectedCampaign = $derived(
		campaigns.find((campaign) => campaign.campaignId === selectedCampaignId) ?? campaigns[0] ?? null
	);
	const ownerOptions = $derived(
		staffAssignments
			.filter((assignment) => assignment.status === 'Active' && assignment.assignmentId)
			.map((assignment) => ({
				id: assignment.assignmentId ?? '',
				label: ownerLabel(assignment)
			}))
	);
	const draftCount = $derived(campaigns.filter((campaign) => campaign.status === 'Draft').length);
	const scheduledCount = $derived(campaigns.filter((campaign) => campaign.status === 'Scheduled').length);
	const sentCount = $derived(campaigns.filter((campaign) => campaign.status === 'Sent').length);
	const totalEstimatedAudience = $derived(
		campaigns.reduce((sum, campaign) => sum + campaign.estimatedAudienceCount, 0)
	);
	const staffQueryError = $derived(
		staffQuery.error instanceof Error
			? staffQuery.error.message
			: staffQuery.error
				? 'Impossibile caricare il team commerciale.'
				: null
	);
	const campaignsQueryError = $derived(
		campaignsQuery.error instanceof Error
			? campaignsQuery.error.message
			: campaignsQuery.error
				? 'Impossibile caricare le campagne CRM.'
				: null
	);
	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms
			|| (!!center.selectedGymId
				&& ((!staffQuery.data && staffQuery.isPending)
					|| (!campaignsQuery.data && campaignsQuery.isPending)))
	);
	const workspaceError = $derived(center.gymsError ?? staffQueryError ?? campaignsQueryError ?? null);

	$effect(() => {
		if (campaigns.length > 0 && (!selectedCampaignId || !campaigns.some((item) => item.campaignId === selectedCampaignId))) {
			selectedCampaignId = campaigns[0].campaignId;
		}
	});

	$effect(() => {
		scheduleAtLocal = toInputDate(selectedCampaign?.scheduledAtUtc);
	});

	function clearFeedback() {
		feedbackMessage = '';
		feedbackError = '';
	}

	function refreshAll() {
		if (!center.selectedGymId) {
			return Promise.resolve([]);
		}

		return Promise.all([campaignsQuery.refetch(), staffQuery.refetch()]);
	}

	function openCreateSheet() {
		clearFeedback();
		formState = newCampaignForm();
		createOpen = true;
	}

	async function handleCreateCampaign(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !formState.locationId || !formState.name.trim() || !formState.subject.trim() || !formState.message.trim()) {
			feedbackError = 'Compila sede, nome campagna, oggetto e messaggio.';
			return;
		}

		if (formState.audienceType === 'LeadsInStage' && !formState.targetLeadStage) {
			feedbackError = 'Se scegli un pubblico per stage devi indicare anche lo stage target.';
			return;
		}

		createSubmitting = true;
		try {
			const created = await createGymCampaign(center.selectedGymId, {
				locationId: formState.locationId,
				ownerAssignmentId: formState.ownerAssignmentId || null,
				name: formState.name.trim(),
				channel: formState.channel,
				audienceType: formState.audienceType,
				targetLeadStage: formState.audienceType === 'LeadsInStage' ? formState.targetLeadStage : null,
				subject: formState.subject.trim(),
				message: formState.message.trim(),
				notes: formState.notes.trim() || null,
				scheduledAtUtc: formState.scheduledAtLocal ? new Date(formState.scheduledAtLocal).toISOString() : null
			} satisfies CreateGymCampaignRequest);

			await campaignsQuery.refetch();
			selectedCampaignId = created.campaignId;
			createOpen = false;
			feedbackMessage = `Campagna "${created.name}" salvata correttamente.`;
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile creare la campagna.';
		} finally {
			createSubmitting = false;
		}
	}

	async function handleScheduleCampaign() {
		if (!center.selectedGymId || !selectedCampaign || !scheduleAtLocal) {
			feedbackError = 'Seleziona una campagna e una data di schedulazione.';
			return;
		}

		clearFeedback();
		campaignActionPending = `${selectedCampaign.campaignId}-schedule`;
		try {
			await scheduleGymCampaign(center.selectedGymId, selectedCampaign.campaignId, {
				scheduledAtUtc: new Date(scheduleAtLocal).toISOString()
			});
			await campaignsQuery.refetch();
			feedbackMessage = 'Campagna schedulata correttamente.';
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile schedulare la campagna.';
		} finally {
			campaignActionPending = null;
		}
	}

	async function handleSendCampaign() {
		if (!center.selectedGymId || !selectedCampaign) return;
		clearFeedback();
		campaignActionPending = `${selectedCampaign.campaignId}-send`;
		try {
			await sendGymCampaign(center.selectedGymId, selectedCampaign.campaignId);
			await campaignsQuery.refetch();
			feedbackMessage = 'Campagna marcata come inviata.';
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile inviare la campagna.';
		} finally {
			campaignActionPending = null;
		}
	}

	async function handleArchiveCampaign() {
		if (!center.selectedGymId || !selectedCampaign) return;
		clearFeedback();
		campaignActionPending = `${selectedCampaign.campaignId}-archive`;
		try {
			await archiveGymCampaign(center.selectedGymId, selectedCampaign.campaignId);
			await campaignsQuery.refetch();
			feedbackMessage = 'Campagna archiviata.';
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile archiviare la campagna.';
		} finally {
			campaignActionPending = null;
		}
	}
</script>

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	<section class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
		<div class="space-y-2">
			<h2 class="text-2xl font-semibold tracking-tight">Campagne CRM</h2>
			<p class="text-sm text-muted-foreground">
				Comunicazioni commerciali reali su lead e clienti, con target calcolato dai dati della sede.
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
				Nuova campagna
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
				<CardTitle>Caricamento campagne CRM</CardTitle>
				<CardDescription>
					Sto recuperando team commerciale e campagne della sede corrente prima di mostrarti il workspace operativo.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-dashed border-[#fecaca] bg-[#fff7f7]">
			<CardHeader>
				<CardTitle>Impossibile caricare le campagne CRM</CardTitle>
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
					Scegli prima la palestra dal selettore in alto a sinistra per vedere campagne, owner e audience della sede corretta.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
	<section class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Bozze attive</CardDescription>
				<CardTitle class="text-3xl">{draftCount}</CardTitle>
			</CardHeader>
			<CardContent class="text-sm text-muted-foreground">Campagne ancora da rifinire o approvare.</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Schedulate</CardDescription>
				<CardTitle class="text-3xl">{scheduledCount}</CardTitle>
			</CardHeader>
			<CardContent class="text-sm text-muted-foreground">Pronte per uscire nel prossimo slot previsto.</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Inviate</CardDescription>
				<CardTitle class="text-3xl">{sentCount}</CardTitle>
			</CardHeader>
			<CardContent class="text-sm text-muted-foreground">Storico di comunicazioni gia eseguite.</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Audience stimata</CardDescription>
				<CardTitle class="text-3xl">{totalEstimatedAudience}</CardTitle>
			</CardHeader>
			<CardContent class="text-sm text-muted-foreground">Somma dei destinatari stimati nel filtro corrente.</CardContent>
		</Card>
	</section>

	<section class="grid gap-4 xl:grid-cols-[1.1fr_1.2fr]">
		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
				<div>
					<CardTitle>Archivio campagne</CardTitle>
					<CardDescription>Filtra per stato e canale per lavorare il piano commerciale.</CardDescription>
				</div>
				<div class="flex flex-wrap gap-2">
					<Select.Root type="single" bind:value={statusFilter}>
						<Select.Trigger class="min-w-[180px]">
							<span data-slot="select-value">{statusFilterLabel(statusFilter)}</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item value="all" label="Tutti gli stati">Tutti gli stati</Select.Item>
							<Select.Item value="Draft" label="Bozze">Bozze</Select.Item>
							<Select.Item value="Scheduled" label="Schedulate">Schedulate</Select.Item>
							<Select.Item value="Sent" label="Inviate">Inviate</Select.Item>
							<Select.Item value="Archived" label="Archiviate">Archiviate</Select.Item>
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
				{#if campaignsQuery.isPending}
					<div class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center">
						<p class="font-semibold">Carico le campagne...</p>
					</div>
				{:else if campaigns.length === 0}
					<div class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center">
						<p class="font-semibold">Nessuna campagna nel perimetro corrente</p>
						<p class="mt-2 text-sm text-muted-foreground">
							Crea la prima campagna commerciale dalla sede selezionata.
						</p>
					</div>
				{:else}
					{#each campaigns as campaign (campaign.campaignId)}
						<button
							type="button"
							class={`w-full rounded-[18px] border p-4 text-left transition ${
								selectedCampaign?.campaignId === campaign.campaignId
									? 'border-primary bg-primary/5'
									: 'border-border/70 bg-background hover:border-primary/40'
							}`}
							onclick={() => {
								selectedCampaignId = campaign.campaignId;
							}}
						>
							<div class="flex flex-wrap items-start justify-between gap-3">
								<div class="space-y-1">
									<p class="font-semibold">{campaign.name}</p>
									<p class="text-sm text-muted-foreground">
										{campaign.locationName} · {campaign.channel} · {audienceLabel(campaign.audienceType)}
									</p>
								</div>
								<Badge variant={statusMeta(campaign.status).variant}>{statusMeta(campaign.status).label}</Badge>
							</div>
							<div class="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
								<p>Destinatari stimati: {campaign.estimatedAudienceCount}</p>
								<p>
									{campaign.status === 'Sent'
										? `Inviata il ${dateTime.format(campaign.sentAtUtc ?? campaign.updatedAtUtc)}`
										: campaign.scheduledAtUtc
											? `Schedulata il ${dateTime.format(campaign.scheduledAtUtc)}`
											: 'Non ancora schedulata'}
								</p>
							</div>
						</button>
					{/each}
				{/if}
			</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader>
				<CardTitle>Dettaglio campagna</CardTitle>
				<CardDescription>Messaggio, target e azioni operative della campagna selezionata.</CardDescription>
			</CardHeader>
			<CardContent class="space-y-4">
				{#if !selectedCampaign}
					<div class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center">
						<p class="font-semibold">Seleziona una campagna</p>
					</div>
				{:else}
					<div class="flex flex-wrap items-start justify-between gap-3">
						<div class="space-y-1">
							<h3 class="text-xl font-semibold">{selectedCampaign.name}</h3>
							<p class="text-sm text-muted-foreground">
								Creata da {selectedCampaign.createdByUserName} · owner {selectedCampaign.ownerName ?? 'Desk'} · {selectedCampaign.locationName}
							</p>
						</div>
						<Badge variant={statusMeta(selectedCampaign.status).variant}>{statusMeta(selectedCampaign.status).label}</Badge>
					</div>

					<div class="grid gap-3 sm:grid-cols-2">
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Canale</p>
							<p class="mt-2 text-sm font-semibold">{selectedCampaign.channel}</p>
						</div>
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Audience</p>
							<p class="mt-2 text-sm font-semibold">{audienceLabel(selectedCampaign.audienceType)}</p>
							<p class="mt-1 text-sm text-muted-foreground">
								{selectedCampaign.audienceType === 'LeadsInStage'
									? `Stage target: ${leadStageLabel(selectedCampaign.targetLeadStage)}`
									: `Destinatari stimati: ${selectedCampaign.estimatedAudienceCount}`}
							</p>
						</div>
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Schedulazione</p>
							<p class="mt-2 text-sm font-semibold">
								{selectedCampaign.scheduledAtUtc ? dateTime.format(selectedCampaign.scheduledAtUtc) : 'Non pianificata'}
							</p>
						</div>
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Ultimo invio</p>
							<p class="mt-2 text-sm font-semibold">
								{selectedCampaign.sentAtUtc ? dateTime.format(selectedCampaign.sentAtUtc) : 'Non ancora inviata'}
							</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Reach registrata: {selectedCampaign.lastAudienceCount ?? 0}
							</p>
						</div>
					</div>

					<div class="rounded-[18px] border border-border/70 p-4">
						<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Oggetto</p>
						<p class="mt-2 text-sm font-semibold">{selectedCampaign.subject}</p>
					</div>

					<div class="rounded-[18px] border border-border/70 p-4">
						<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Messaggio</p>
						<p class="mt-2 whitespace-pre-wrap text-sm leading-relaxed text-muted-foreground">
							{selectedCampaign.message}
						</p>
					</div>

					{#if selectedCampaign.notes}
						<div class="rounded-[18px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">Note interne</p>
							<p class="mt-2 text-sm text-muted-foreground">{selectedCampaign.notes}</p>
						</div>
					{/if}

					<div class="rounded-[18px] border border-border/70 p-4">
						<div class="flex flex-wrap items-center gap-2">
							<CalendarClockIcon class="size-4 text-muted-foreground" />
							<p class="text-sm font-semibold">Schedula o aggiorna invio</p>
						</div>
						<div class="mt-3 flex flex-col gap-2 sm:flex-row">
							<Input bind:value={scheduleAtLocal} type="datetime-local" />
							<Button
								variant="outline"
								onclick={handleScheduleCampaign}
								disabled={campaignActionPending === `${selectedCampaign.campaignId}-schedule` || selectedCampaign.status === 'Archived'}
							>
								<TimerResetIcon class="size-4" />
								Programma
							</Button>
						</div>
					</div>

					<div class="flex flex-wrap gap-2">
						<Button
							onclick={handleSendCampaign}
							disabled={campaignActionPending === `${selectedCampaign.campaignId}-send` || selectedCampaign.status === 'Archived'}
						>
							<SendIcon class="size-4" />
							Invia adesso
						</Button>
						<Button
							variant="outline"
							onclick={handleArchiveCampaign}
							disabled={campaignActionPending === `${selectedCampaign.campaignId}-archive` || selectedCampaign.status === 'Archived'}
						>
							<ArchiveIcon class="size-4" />
							Archivia
						</Button>
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
			<Sheet.Title>Nuova campagna CRM</Sheet.Title>
			<Sheet.Description>
				Crea una comunicazione commerciale sulla sede selezionata con pubblico calcolato dal gestionale.
			</Sheet.Description>
		</Sheet.Header>

		<form class="mt-6 space-y-4" onsubmit={handleCreateCampaign}>
			<div class="grid gap-4 md:grid-cols-2">
				<label class="space-y-2 text-sm">
					<span class="font-medium">Sede</span>
					<Select.Root
						type="single"
						value={formState.locationId || CRM_CAMPAIGNS_NO_SELECTION_SELECT_VALUE}
						onValueChange={(value) =>
							(formState.locationId =
								value === CRM_CAMPAIGNS_NO_SELECTION_SELECT_VALUE ? '' : value)}
					>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{formLocationLabel(formState.locationId)}</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item
								value={CRM_CAMPAIGNS_NO_SELECTION_SELECT_VALUE}
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
						value={formState.ownerAssignmentId || CRM_CAMPAIGNS_NO_SELECTION_SELECT_VALUE}
						onValueChange={(value) =>
							(formState.ownerAssignmentId =
								value === CRM_CAMPAIGNS_NO_SELECTION_SELECT_VALUE ? '' : value)}
					>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{formOwnerLabel(formState.ownerAssignmentId)}</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item
								value={CRM_CAMPAIGNS_NO_SELECTION_SELECT_VALUE}
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
					<span class="font-medium">Nome campagna</span>
					<Input bind:value={formState.name} placeholder="Promo rinnovi di maggio" />
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

			<label class="space-y-2 text-sm">
				<span class="font-medium">Oggetto</span>
				<Input bind:value={formState.subject} placeholder="Rinnova ora il tuo percorso FitUp" />
			</label>

			<label class="space-y-2 text-sm">
				<span class="font-medium">Messaggio</span>
				<Textarea bind:value={formState.message} rows={7} placeholder="Scrivi il testo della comunicazione..." />
			</label>

			<div class="grid gap-4 md:grid-cols-2">
				<label class="space-y-2 text-sm">
					<span class="font-medium">Schedula subito</span>
					<Input bind:value={formState.scheduledAtLocal} type="datetime-local" />
				</label>

				<label class="space-y-2 text-sm">
					<span class="font-medium">Note interne</span>
					<Textarea bind:value={formState.notes} rows={3} placeholder="Contesto interno per il team..." />
				</label>
			</div>

			<div class="flex flex-wrap gap-2 pt-2">
				<Button type="submit" disabled={createSubmitting}>
					{#if createSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvo...
					{:else}
						<MegaphoneIcon class="size-4" />
						Crea campagna
					{/if}
				</Button>
				<Button type="button" variant="outline" onclick={() => (createOpen = false)}>
					Chiudi
				</Button>
			</div>
		</form>
	</Sheet.Content>
</Sheet.Root>
