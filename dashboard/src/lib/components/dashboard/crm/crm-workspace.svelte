<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { createQuery } from '@tanstack/svelte-query';
	import CalendarClockIcon from '@lucide/svelte/icons/calendar-clock';
	import CheckCircle2Icon from '@lucide/svelte/icons/check-circle-2';
	import CircleDashedIcon from '@lucide/svelte/icons/circle-dashed';
	import MegaphoneIcon from '@lucide/svelte/icons/megaphone';
	import PlusCircleIcon from '@lucide/svelte/icons/plus-circle';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import type { GymMembershipResponse, GymStaffAssignmentResponse } from '$lib/api';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import { Checkbox } from '$lib/components/ui/checkbox/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import * as Select from '$lib/components/ui/select/index.js';
	import * as Sheet from '$lib/components/ui/sheet/index.js';
	import { Textarea } from '$lib/components/ui/textarea/index.js';
	import {
		type CreateGymLeadRequest,
		type CreateGymLeadTaskRequest,
		type GymLead,
		type LeadSource,
		type LeadStage,
		type LeadTaskStatus,
		createGymLead,
		createGymLeadTask,
		fetchGymCrmOverview,
		fetchGymLeads,
		updateGymLeadStage,
		updateGymLeadTaskStatus
	} from '$lib/data/crm-api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
	type StageFilter = LeadStage | 'all';
	const CRM_NO_SELECTION_SELECT_VALUE = '__none__';

	const api = useApiClient();
	const center = useCenterSelectionStore();
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});

	const stageOptions: Array<{ value: LeadStage; label: string; tone: string }> = [
		{ value: 'New', label: 'Nuovi', tone: 'border-slate-200 bg-slate-50' },
		{ value: 'Contacted', label: 'Contattati', tone: 'border-sky-200 bg-sky-50' },
		{ value: 'TrialBooked', label: 'Trial', tone: 'border-amber-200 bg-amber-50' },
		{ value: 'Negotiation', label: 'Trattativa', tone: 'border-violet-200 bg-violet-50' },
		{ value: 'Won', label: 'Chiusi', tone: 'border-emerald-200 bg-emerald-50' },
		{ value: 'Lost', label: 'Persi', tone: 'border-rose-200 bg-rose-50' }
	];

	const sourceOptions: Array<{ value: LeadSource; label: string }> = [
		{ value: 'Website', label: 'Sito' },
		{ value: 'WalkIn', label: 'Walk-in' },
		{ value: 'MetaAds', label: 'Ads Meta' },
		{ value: 'Referral', label: 'Referral' },
		{ value: 'WhatsApp', label: 'WhatsApp' },
		{ value: 'Corporate', label: 'Corporate' },
		{ value: 'Other', label: 'Altro' }
	];

	const currentHash = $derived(page.url.hash || '#pipeline-overview');
	const sectionNavClass = (hash: string) =>
		[
			'inline-flex items-center rounded-full border px-3 py-1.5 text-sm transition',
			currentHash === hash
				? 'border-primary/30 bg-primary/10 text-primary shadow-sm'
				: 'border-border/70 bg-background text-muted-foreground hover:border-border hover:bg-secondary/20 hover:text-foreground'
		].join(' ');

	const newLeadForm = () => ({
		locationId: center.selectedLocationId ?? center.locations[0]?.id ?? '',
		ownerAssignmentId: '',
		fullName: '',
		email: '',
		phoneNumber: '',
		source: 'Website' as LeadSource,
		interest: '',
		notes: '',
		nextFollowUpLocal: ''
	});

	const newTaskForm = () => ({
		assignedAssignmentId: '',
		title: '',
		notes: '',
		dueAtLocal: ''
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

	const membershipLabel = (membership: GymMembershipResponse) =>
		[membership.memberProfile?.firstName, membership.memberProfile?.lastName]
			.filter(Boolean)
			.join(' ')
			.trim() ||
		membership.userEmail ||
		membership.invitationEmail ||
		'Cliente senza nome';

	const sourceLabel = (value: LeadSource) =>
		sourceOptions.find((option) => option.value === value)?.label ?? value;

	const stageMeta = (stage: LeadStage): { label: string; variant: BadgeVariant } => {
		if (stage === 'Won') return { label: 'Chiuso', variant: 'success' };
		if (stage === 'Lost') return { label: 'Perso', variant: 'destructive' };
		if (stage === 'TrialBooked') return { label: 'Trial', variant: 'warning' };
		if (stage === 'Negotiation') return { label: 'Trattativa', variant: 'secondary' };
		if (stage === 'Contacted') return { label: 'Contattato', variant: 'default' };
		return { label: 'Nuovo', variant: 'outline' };
	};

	const taskMeta = (status: LeadTaskStatus): { label: string; variant: BadgeVariant } => {
		if (status === 'Completed') return { label: 'Completata', variant: 'success' };
		if (status === 'Cancelled') return { label: 'Annullata', variant: 'outline' };
		return { label: 'Aperta', variant: 'warning' };
	};

	let searchTerm = $state('');
	let selectedLeadId = $state<string | null>(null);
	let selectedOwnerId = $state<string>('all');
	let selectedStageFilter = $state<StageFilter>('all');
	let dueOnly = $state(false);
	let leadSheetOpen = $state(false);
	let taskSheetOpen = $state(false);
	let createLeadSubmitting = $state(false);
	let updateLeadSubmitting = $state(false);
	let createTaskSubmitting = $state(false);
	let taskStatusSubmittingId = $state<string | null>(null);
	let feedbackMessage = $state('');
	let feedbackError = $state('');
	let leadForm = $state(newLeadForm());
	let taskForm = $state(newTaskForm());
	let stageForm = $state({
		stage: 'New' as LeadStage,
		lastContactedLocal: '',
		nextFollowUpLocal: '',
		convertedMembershipId: '',
		notes: ''
	});

	const staffQuery = createQuery(() => ({
		queryKey: ['crm-staff', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdStaffAssignmentsGet({
				gymId: center.selectedGymId!
			});
			if (!response.success) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile caricare il team commerciale.'
				);
			}
			return (response.data ?? []) as GymStaffAssignmentResponse[];
		}
	}));

	const membershipsQuery = createQuery(() => ({
		queryKey: ['crm-memberships', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdMembershipsGet({
				gymId: center.selectedGymId!
			});
			if (!response.success) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile caricare i clienti.'
				);
			}
			return (response.data ?? []) as GymMembershipResponse[];
		}
	}));

	const overviewQuery = createQuery(() => ({
		queryKey: ['crm-overview', center.selectedGymId, center.selectedLocationId],
		enabled: !!center.selectedGymId,
		queryFn: () => fetchGymCrmOverview(center.selectedGymId!, center.selectedLocationId)
	}));

	const leadsQuery = createQuery(() => ({
		queryKey: [
			'crm-leads',
			center.selectedGymId,
			center.selectedLocationId,
			selectedOwnerId,
			selectedStageFilter,
			dueOnly,
			searchTerm
		],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymLeads(center.selectedGymId!, {
				locationId: center.selectedLocationId,
				ownerAssignmentId: selectedOwnerId === 'all' ? null : selectedOwnerId,
				stage: selectedStageFilter,
				dueOnly,
				search: searchTerm
			})
	}));

	const staffAssignments = $derived(staffQuery.data ?? []);
	const memberships = $derived(membershipsQuery.data ?? []);
	const leads = $derived(leadsQuery.data ?? []);
	const overview = $derived(overviewQuery.data);
	const selectedLead = $derived(
		leads.find((lead) => lead.leadId === selectedLeadId) ?? leads[0] ?? null
	);
	const scopeLabel = $derived(
		center.selectedLocationId
			? (center.locations.find((location) => location.id === center.selectedLocationId)?.name ??
					'Sede selezionata')
			: 'Tutte le sedi'
	);
	const groupedLeads = $derived.by(() =>
		stageOptions.map((stage) => ({
			...stage,
			leads: leads.filter((lead) => lead.stage === stage.value)
		}))
	);
	const selectedLeadOpenTasksCount = $derived(
		selectedLead?.tasks.filter((task) => task.status === 'Open').length ?? 0
	);
	const selectedLeadLastContactLabel = $derived(
		selectedLead?.lastContactedAtUtc
			? dateTime.format(selectedLead.lastContactedAtUtc)
			: 'Non registrato'
	);
	const selectedLeadNextFollowUpLabel = $derived(
		selectedLead?.nextFollowUpAtUtc
			? dateTime.format(selectedLead.nextFollowUpAtUtc)
			: 'Non pianificato'
	);
	const membershipOptions = $derived(
		memberships
			.filter((membership) => membership.membershipId)
			.map((membership) => ({
				id: membership.membershipId ?? '',
				label: membershipLabel(membership)
			}))
	);
	const ownerOptions = $derived(
		staffAssignments
			.filter((assignment) => assignment.status === 'Active' && assignment.assignmentId)
			.map((assignment) => ({
				id: assignment.assignmentId ?? '',
				label: assignment.staffProfile?.displayName || assignment.userEmail || 'Staff'
			}))
	);
	const selectedOwnerFilterLabel = $derived(
		selectedOwnerId === 'all'
			? 'Tutti i responsabili'
			: (ownerOptions.find((owner) => owner.id === selectedOwnerId)?.label ??
					'Responsabile selezionato')
	);
	const selectedStageFilterLabel = $derived(
		selectedStageFilter === 'all'
			? 'Tutti gli stage'
			: (stageOptions.find((stage) => stage.value === selectedStageFilter)?.label ??
					'Stage selezionato')
	);
	const selectedLeadStageLabel = $derived(
		stageOptions.find((stage) => stage.value === stageForm.stage)?.label ?? 'Seleziona stage'
	);
	const selectedConvertedMembershipLabel = $derived(
		stageForm.convertedMembershipId
			? (membershipOptions.find((membership) => membership.id === stageForm.convertedMembershipId)
					?.label ?? 'Cliente selezionato')
			: 'Nessuna conversione associata'
	);
	const selectedLeadLocationLabel = $derived(
		leadForm.locationId
			? (center.locations.find((location) => location.id === leadForm.locationId)?.name ??
					'Sede selezionata')
			: 'Seleziona sede'
	);
	const selectedLeadOwnerLabel = $derived(
		leadForm.ownerAssignmentId
			? (ownerOptions.find((owner) => owner.id === leadForm.ownerAssignmentId)?.label ??
					'Responsabile selezionato')
			: 'Assegna dopo'
	);
	const selectedLeadSourceLabel = $derived(
		sourceOptions.find((source) => source.value === leadForm.source)?.label ?? 'Seleziona origine'
	);
	const selectedTaskAssigneeLabel = $derived(
		taskForm.assignedAssignmentId
			? (ownerOptions.find((owner) => owner.id === taskForm.assignedAssignmentId)?.label ??
					'Assegnatario selezionato')
			: 'Nessun assegnatario specifico'
	);
	const staffQueryError = $derived(
		staffQuery.error instanceof Error
			? staffQuery.error.message
			: staffQuery.error
				? 'Impossibile caricare il team commerciale.'
				: null
	);
	const membershipsQueryError = $derived(
		membershipsQuery.error instanceof Error
			? membershipsQuery.error.message
			: membershipsQuery.error
				? 'Impossibile caricare i clienti.'
				: null
	);
	const overviewQueryError = $derived(
		overviewQuery.error instanceof Error
			? overviewQuery.error.message
			: overviewQuery.error
				? 'Impossibile caricare l overview CRM.'
				: null
	);
	const leadsQueryError = $derived(
		leadsQuery.error instanceof Error
			? leadsQuery.error.message
			: leadsQuery.error
				? 'Impossibile caricare la pipeline lead.'
				: null
	);
	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms ||
			(!!center.selectedGymId &&
				((!staffQuery.data && staffQuery.isPending) ||
					(!membershipsQuery.data && membershipsQuery.isPending) ||
					(!overviewQuery.data && overviewQuery.isPending) ||
					(!leadsQuery.data && leadsQuery.isPending)))
	);
	const workspaceError = $derived(
		center.gymsError ??
			staffQueryError ??
			membershipsQueryError ??
			overviewQueryError ??
			leadsQueryError ??
			null
	);

	$effect(() => {
		if (center.selectedLocationId && !leadForm.locationId) {
			leadForm = { ...leadForm, locationId: center.selectedLocationId };
		}
	});

	$effect(() => {
		if (
			leads.length > 0 &&
			(!selectedLeadId || !leads.some((lead) => lead.leadId === selectedLeadId))
		) {
			selectedLeadId = leads[0].leadId;
		}
	});

	$effect(() => {
		if (!selectedLead) {
			return;
		}

		stageForm = {
			stage: selectedLead.stage,
			lastContactedLocal: toInputDate(selectedLead.lastContactedAtUtc),
			nextFollowUpLocal: toInputDate(selectedLead.nextFollowUpAtUtc),
			convertedMembershipId: selectedLead.convertedMembershipId ?? '',
			notes: selectedLead.notes ?? ''
		};
	});

	function clearFeedback() {
		feedbackError = '';
		feedbackMessage = '';
	}

	function openLeadSheet() {
		clearFeedback();
		leadForm = newLeadForm();
		leadSheetOpen = true;
	}

	function openTaskSheet() {
		if (!selectedLead) return;
		clearFeedback();
		taskForm = newTaskForm();
		taskSheetOpen = true;
	}

	async function refreshAll() {
		if (!center.selectedGymId) {
			return;
		}

		await Promise.all([
			overviewQuery.refetch(),
			leadsQuery.refetch(),
			staffQuery.refetch(),
			membershipsQuery.refetch()
		]);
	}

	async function handleCreateLead(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !leadForm.locationId || !leadForm.fullName.trim()) {
			feedbackError = 'Compila almeno nome lead e sede di interesse.';
			return;
		}

		if (!leadForm.email.trim() && !leadForm.phoneNumber.trim()) {
			feedbackError = 'Inserisci almeno email o telefono per poter lavorare il lead.';
			return;
		}

		createLeadSubmitting = true;
		try {
			const created = await createGymLead(center.selectedGymId, {
				locationId: leadForm.locationId,
				ownerAssignmentId: leadForm.ownerAssignmentId || null,
				fullName: leadForm.fullName.trim(),
				email: leadForm.email.trim() || null,
				phoneNumber: leadForm.phoneNumber.trim() || null,
				source: leadForm.source,
				interest: leadForm.interest.trim() || null,
				notes: leadForm.notes.trim() || null,
				nextFollowUpAtUtc: leadForm.nextFollowUpLocal
					? new Date(leadForm.nextFollowUpLocal).toISOString()
					: null
			} satisfies CreateGymLeadRequest);

			await Promise.all([overviewQuery.refetch(), leadsQuery.refetch()]);
			selectedLeadId = created.leadId;
			leadSheetOpen = false;
			feedbackMessage = `Lead ${created.fullName} inserito correttamente.`;
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile creare il lead.';
		} finally {
			createLeadSubmitting = false;
		}
	}

	async function handleUpdateLeadStage(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !selectedLead) {
			feedbackError = 'Seleziona prima un lead.';
			return;
		}

		updateLeadSubmitting = true;
		try {
			await updateGymLeadStage(center.selectedGymId, selectedLead.leadId, {
				stage: stageForm.stage,
				convertedMembershipId: stageForm.convertedMembershipId || null,
				lastContactedAtUtc: stageForm.lastContactedLocal
					? new Date(stageForm.lastContactedLocal).toISOString()
					: null,
				nextFollowUpAtUtc: stageForm.nextFollowUpLocal
					? new Date(stageForm.nextFollowUpLocal).toISOString()
					: null,
				notes: stageForm.notes.trim() || null
			});

			await Promise.all([overviewQuery.refetch(), leadsQuery.refetch()]);
			feedbackMessage = 'Pipeline aggiornata.';
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile aggiornare lo stage.';
		} finally {
			updateLeadSubmitting = false;
		}
	}

	async function handleCreateTask(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !selectedLead || !taskForm.title.trim()) {
			feedbackError = 'Inserisci il titolo della task.';
			return;
		}

		createTaskSubmitting = true;
		try {
			await createGymLeadTask(center.selectedGymId, selectedLead.leadId, {
				assignedAssignmentId: taskForm.assignedAssignmentId || null,
				title: taskForm.title.trim(),
				notes: taskForm.notes.trim() || null,
				dueAtUtc: taskForm.dueAtLocal ? new Date(taskForm.dueAtLocal).toISOString() : null
			} satisfies CreateGymLeadTaskRequest);

			await Promise.all([overviewQuery.refetch(), leadsQuery.refetch()]);
			taskSheetOpen = false;
			feedbackMessage = 'Task commerciale creata.';
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile creare la task.';
		} finally {
			createTaskSubmitting = false;
		}
	}

	async function handleTaskStatus(taskId: string, status: LeadTaskStatus) {
		if (!center.selectedGymId || !selectedLead) return;
		clearFeedback();
		taskStatusSubmittingId = taskId;

		try {
			await updateGymLeadTaskStatus(center.selectedGymId, selectedLead.leadId, taskId, {
				status,
				completedAtUtc: status === 'Completed' ? new Date().toISOString() : null
			});
			await Promise.all([overviewQuery.refetch(), leadsQuery.refetch()]);
			feedbackMessage = 'Task aggiornata.';
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile aggiornare la task.';
		} finally {
			taskStatusSubmittingId = null;
		}
	}
</script>

<main class="grid gap-6 p-4 md:gap-8 md:p-6">
	<section class="space-y-6">
		<div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
			<div>
				<h2 class="text-2xl font-semibold tracking-tight">CRM e lead pipeline</h2>
				<p class="text-sm text-muted-foreground">
					Gestisci lead, follow-up commerciali e conversioni per sede.
				</p>
			</div>
			<div class="flex flex-wrap gap-2">
				<Button variant="outline" onclick={() => goto('/crm/campaigns')}>
					<MegaphoneIcon class="mr-2 size-4" />
					Campagne CRM
				</Button>
				<Button variant="outline" onclick={() => goto('/crm/automations')}>Automazioni CRM</Button>
				<Button
					variant="outline"
					onclick={refreshAll}
					disabled={!hasSelectedGym || workspaceLoading}
				>
					<RefreshCwIcon class="mr-2 size-4" />
					Aggiorna
				</Button>
				<Button onclick={openLeadSheet} disabled={!hasSelectedGym || workspaceLoading}>
					<PlusCircleIcon class="mr-2 size-4" />
					Nuovo lead
				</Button>
			</div>
		</div>

		<nav
			class="flex flex-wrap gap-2 rounded-[20px] border border-border/70 bg-muted/20 p-2"
			aria-label="Sezioni CRM"
		>
			<a href="#pipeline-overview" class={sectionNavClass('#pipeline-overview')}>
				Overview
			</a>
			<a href="#pipeline-filters" class={sectionNavClass('#pipeline-filters')}>
				Filtri
			</a>
			<a href="#pipeline-board" class={sectionNavClass('#pipeline-board')}>
				Pipeline
			</a>
			<a href="#pipeline-detail" class={sectionNavClass('#pipeline-detail')}>
				Dettaglio lead
			</a>
		</nav>

		{#if feedbackError}
			<div
				class="rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive"
			>
				{feedbackError}
			</div>
		{/if}
		{#if feedbackMessage}
			<div
				class="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700"
			>
				{feedbackMessage}
			</div>
		{/if}

		{#if workspaceLoading}
			<Card class="border-dashed border-border/70 bg-muted/25">
				<CardHeader>
					<CardTitle>Caricamento area CRM</CardTitle>
					<CardDescription>
						Sto recuperando team, clienti, overview e pipeline lead prima di mostrarti il quadro
						commerciale reale del tenant.
					</CardDescription>
				</CardHeader>
			</Card>
		{:else if workspaceError}
			<Card class="border-dashed border-[#fecaca] bg-[#fff7f7]">
				<CardHeader>
					<CardTitle>Impossibile caricare l area CRM</CardTitle>
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
						Scegli prima la palestra dal selettore in alto a sinistra per vedere lead, follow-up,
						task e conversioni del tenant corretto.
					</CardDescription>
				</CardHeader>
			</Card>
		{:else}
			<Card class="border-border/70">
				<CardHeader class="pb-3">
					<CardTitle>Perimetro commerciale</CardTitle>
					<CardDescription>
						Stai lavorando su <span class="font-medium text-foreground">{scopeLabel}</span> con
						filtro owner <span class="font-medium text-foreground">{selectedOwnerFilterLabel}</span>
						e stage <span class="font-medium text-foreground">{selectedStageFilterLabel}</span>.
					</CardDescription>
				</CardHeader>
				<CardContent class="grid gap-3 text-sm text-muted-foreground md:grid-cols-3">
					<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
						<div class="text-xs font-medium tracking-[0.18em] uppercase">Lead visibili</div>
						<div class="mt-2 text-lg font-semibold text-foreground">{leads.length}</div>
					</div>
					<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
						<div class="text-xs font-medium tracking-[0.18em] uppercase">Da ricontattare</div>
						<div class="mt-2 text-lg font-semibold text-foreground">
							{overview?.leadsNeedingFollowUpCount ?? 0}
						</div>
					</div>
					<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
						<div class="text-xs font-medium tracking-[0.18em] uppercase">Task aperte</div>
						<div class="mt-2 text-lg font-semibold text-foreground">
							{overview?.openTasksCount ?? 0}
						</div>
					</div>
				</CardContent>
			</Card>

			<div
				id="pipeline-overview"
				class="grid scroll-mt-24 gap-4 md:grid-cols-2 lg:gap-5 xl:grid-cols-4"
			>
				<Card>
					<CardHeader class="pb-3">
						<CardDescription>Lead attivi</CardDescription>
						<CardTitle class="text-3xl">{overview?.totalLeads ?? 0}</CardTitle>
					</CardHeader>
				</Card>
				<Card>
					<CardHeader class="pb-3">
						<CardDescription>Follow-up in scadenza</CardDescription>
						<CardTitle class="text-3xl">{overview?.leadsNeedingFollowUpCount ?? 0}</CardTitle>
					</CardHeader>
				</Card>
				<Card>
					<CardHeader class="pb-3">
						<CardDescription>Chiusi questo mese</CardDescription>
						<CardTitle class="text-3xl">{overview?.leadsWonThisMonthCount ?? 0}</CardTitle>
					</CardHeader>
				</Card>
				<Card>
					<CardHeader class="pb-3">
						<CardDescription>Task aperte</CardDescription>
						<CardTitle class="text-3xl">{overview?.openTasksCount ?? 0}</CardTitle>
					</CardHeader>
				</Card>
			</div>

			<Card id="pipeline-filters" class="scroll-mt-24">
				<CardHeader class="pb-4">
					<CardTitle>Filtri operativi</CardTitle>
					<CardDescription
						>Lavora la pipeline per responsabile, stage o lead da ricontattare.</CardDescription
					>
				</CardHeader>
				<CardContent class="grid gap-4 md:grid-cols-2 lg:gap-5 xl:grid-cols-4">
					<Input bind:value={searchTerm} placeholder="Cerca per nome, contatto o interesse" />
					<Select.Root type="single" bind:value={selectedOwnerId}>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{selectedOwnerFilterLabel}</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item value="all" label="Tutti i responsabili"
								>Tutti i responsabili</Select.Item
							>
							{#each ownerOptions as owner}
								<Select.Item value={owner.id} label={owner.label}>
									{owner.label}
								</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
					<Select.Root type="single" bind:value={selectedStageFilter}>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{selectedStageFilterLabel}</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item value="all" label="Tutti gli stage">Tutti gli stage</Select.Item>
							{#each stageOptions as stage}
								<Select.Item value={stage.value} label={stage.label}>
									{stage.label}
								</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
					<label class="flex items-center gap-3 rounded-md border border-input px-3 py-2 text-sm">
						<Checkbox bind:checked={dueOnly} />
						<span>Solo follow-up urgenti</span>
					</label>
				</CardContent>
			</Card>

			<div
				id="pipeline-board"
				class="grid scroll-mt-24 gap-6 xl:grid-cols-[minmax(0,1.7fr)_minmax(360px,0.95fr)] 2xl:gap-8"
			>
				<div class="space-y-4">
					<div class="grid gap-4 md:grid-cols-2 lg:gap-5 2xl:grid-cols-3">
						{#each groupedLeads as group}
							<Card class={`flex h-full flex-col rounded-3xl border ${group.tone}`}>
								<CardHeader class="pb-3">
									<div class="flex flex-wrap items-start justify-between gap-3">
										<div>
											<CardTitle class="text-base">{group.label}</CardTitle>
											<CardDescription>{group.leads.length} lead</CardDescription>
										</div>
										<Badge variant={stageMeta(group.value).variant}
											>{stageMeta(group.value).label}</Badge
										>
									</div>
								</CardHeader>
								<CardContent class="space-y-3">
									{#if group.leads.length === 0}
										<p class="text-sm text-muted-foreground">Nessun lead in questa fase.</p>
									{:else}
										{#each group.leads as lead}
											<button
												type="button"
												class={`w-full rounded-2xl border px-4 py-3 text-left transition hover:border-primary/40 hover:bg-background ${
													selectedLead?.leadId === lead.leadId
														? 'border-primary bg-background shadow-sm'
														: 'border-border/70 bg-white/70'
												}`}
												onclick={() => (selectedLeadId = lead.leadId)}
											>
												<div class="flex flex-wrap items-start justify-between gap-3">
													<div>
														<div class="font-medium">{lead.fullName}</div>
														<div class="text-xs text-muted-foreground">{lead.locationName}</div>
													</div>
													<Badge variant="outline">{sourceLabel(lead.source)}</Badge>
												</div>
												{#if lead.interest}
													<p class="mt-2 text-sm text-muted-foreground">{lead.interest}</p>
												{/if}
												<div
													class="mt-3 flex flex-wrap gap-x-3 gap-y-2 text-xs text-muted-foreground"
												>
													<span>{lead.ownerName ?? 'Senza owner'}</span>
													<span
														>{lead.tasks.filter((task) => task.status === 'Open').length} task aperte</span
													>
													{#if lead.nextFollowUpAtUtc}
														<span>Follow-up {dateTime.format(lead.nextFollowUpAtUtc)}</span>
													{/if}
												</div>
											</button>
										{/each}
									{/if}
								</CardContent>
							</Card>
						{/each}
					</div>
				</div>

				<Card id="pipeline-detail" class="h-fit scroll-mt-24 rounded-3xl xl:sticky xl:top-6">
					<CardHeader class="pb-3">
						<div class="flex flex-wrap items-start justify-between gap-3">
							<div>
								<CardTitle>{selectedLead?.fullName ?? 'Dettaglio lead'}</CardTitle>
								<CardDescription>
									{selectedLead
										? `${selectedLead.locationName} - ${sourceLabel(selectedLead.source)}`
										: 'Seleziona un lead dalla pipeline.'}
								</CardDescription>
							</div>
							{#if selectedLead}
								<Badge variant={stageMeta(selectedLead.stage).variant}
									>{stageMeta(selectedLead.stage).label}</Badge
								>
							{/if}
						</div>
					</CardHeader>
					<CardContent class="space-y-5">
						{#if !selectedLead}
							<p class="text-sm text-muted-foreground">
								La pipeline e vuota per i filtri correnti.
							</p>
						{:else}
							<div class="grid gap-3 sm:grid-cols-3">
								<div class="rounded-[18px] border border-border/70 bg-muted/15 px-4 py-3">
									<div
										class="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase"
									>
										Follow-up
									</div>
									<div class="mt-2 text-sm font-medium text-foreground">
										{selectedLeadNextFollowUpLabel}
									</div>
								</div>
								<div class="rounded-[18px] border border-border/70 bg-muted/15 px-4 py-3">
									<div
										class="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase"
									>
										Ultimo contatto
									</div>
									<div class="mt-2 text-sm font-medium text-foreground">
										{selectedLeadLastContactLabel}
									</div>
								</div>
								<div class="rounded-[18px] border border-border/70 bg-muted/15 px-4 py-3">
									<div
										class="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase"
									>
										Task aperte
									</div>
									<div class="mt-2 text-sm font-medium text-foreground">
										{selectedLeadOpenTasksCount}
									</div>
								</div>
							</div>

							<div class="grid gap-3 text-sm sm:grid-cols-2">
								<div>
									<span class="font-medium">Owner:</span>
									{selectedLead.ownerName ?? 'Non assegnato'}
								</div>
								<div>
									<span class="font-medium">Email:</span>
									{selectedLead.email ?? 'Non disponibile'}
								</div>
								<div>
									<span class="font-medium">Telefono:</span>
									{selectedLead.phoneNumber ?? 'Non disponibile'}
								</div>
								<div>
									<span class="font-medium">Interesse:</span>
									{selectedLead.interest ?? 'Non specificato'}
								</div>
							</div>

							<form
								class="space-y-3 rounded-2xl border border-border/70 p-4"
								onsubmit={handleUpdateLeadStage}
							>
								<div class="flex items-center gap-2 text-sm font-medium">
									<MegaphoneIcon class="size-4" />
									Aggiorna stage e follow-up
								</div>
								<Select.Root type="single" bind:value={stageForm.stage}>
									<Select.Trigger class="w-full">
										<span data-slot="select-value">{selectedLeadStageLabel}</span>
									</Select.Trigger>
									<Select.Content>
										{#each stageOptions as stage}
											<Select.Item value={stage.value} label={stage.label}>
												{stage.label}
											</Select.Item>
										{/each}
									</Select.Content>
								</Select.Root>
								<Input bind:value={stageForm.lastContactedLocal} type="datetime-local" />
								<Input bind:value={stageForm.nextFollowUpLocal} type="datetime-local" />
								<Select.Root
									type="single"
									value={stageForm.convertedMembershipId || CRM_NO_SELECTION_SELECT_VALUE}
									onValueChange={(value) =>
										(stageForm.convertedMembershipId =
											value === CRM_NO_SELECTION_SELECT_VALUE ? '' : value)}
								>
									<Select.Trigger class="w-full">
										<span data-slot="select-value">{selectedConvertedMembershipLabel}</span>
									</Select.Trigger>
									<Select.Content>
										<Select.Item
											value={CRM_NO_SELECTION_SELECT_VALUE}
											label="Nessuna conversione associata"
										>
											Nessuna conversione associata
										</Select.Item>
										{#each membershipOptions as membership}
											<Select.Item value={membership.id} label={membership.label}>
												{membership.label}
											</Select.Item>
										{/each}
									</Select.Content>
								</Select.Root>
								<Textarea
									bind:value={stageForm.notes}
									rows={4}
									placeholder="Note commerciali, obiezioni, prossima mossa..."
								/>
								<Button type="submit" class="w-full" disabled={updateLeadSubmitting}>
									{updateLeadSubmitting ? 'Salvataggio...' : 'Aggiorna pipeline'}
								</Button>
							</form>

							<div id="pipeline-tasks" class="space-y-3 rounded-2xl border border-border/70 p-4">
								<div
									class="flex flex-col items-start gap-3 sm:flex-row sm:items-center sm:justify-between"
								>
									<div class="text-sm font-medium">Task commerciali</div>
									<Button variant="outline" size="sm" onclick={openTaskSheet}>
										<PlusCircleIcon class="mr-2 size-4" />
										Nuova task
									</Button>
								</div>
								{#if selectedLead.tasks.length === 0}
									<p class="text-sm text-muted-foreground">
										Nessuna task ancora aperta per questo lead.
									</p>
								{:else}
									<div class="space-y-3">
										{#each selectedLead.tasks as task}
											<div class="rounded-2xl border border-border/70 p-3">
												<div class="flex items-start justify-between gap-3">
													<div>
														<div class="font-medium">{task.title}</div>
														<div class="text-xs text-muted-foreground">
															{task.assignedStaffName ?? 'Non assegnata'}
															{#if task.dueAtUtc}
																- Scade {dateTime.format(task.dueAtUtc)}
															{/if}
														</div>
													</div>
													<Badge variant={taskMeta(task.status).variant}
														>{taskMeta(task.status).label}</Badge
													>
												</div>
												{#if task.notes}
													<p class="mt-2 text-sm text-muted-foreground">{task.notes}</p>
												{/if}
												<div class="mt-3 flex flex-wrap gap-2">
													<Button
														variant="outline"
														size="sm"
														disabled={taskStatusSubmittingId === task.taskId}
														onclick={() => handleTaskStatus(task.taskId, 'Completed')}
													>
														<CheckCircle2Icon class="mr-2 size-4" />
														Completa
													</Button>
													<Button
														variant="outline"
														size="sm"
														disabled={taskStatusSubmittingId === task.taskId}
														onclick={() => handleTaskStatus(task.taskId, 'Open')}
													>
														<CircleDashedIcon class="mr-2 size-4" />
														Riapri
													</Button>
													<Button
														variant="outline"
														size="sm"
														disabled={taskStatusSubmittingId === task.taskId}
														onclick={() => handleTaskStatus(task.taskId, 'Cancelled')}
													>
														<CalendarClockIcon class="mr-2 size-4" />
														Annulla
													</Button>
												</div>
											</div>
										{/each}
									</div>
								{/if}
							</div>
						{/if}
					</CardContent>
				</Card>
			</div>
		{/if}
	</section>
</main>

<Sheet.Root bind:open={leadSheetOpen}>
	<Sheet.Content class="w-full sm:max-w-xl">
		<Sheet.Header>
			<Sheet.Title>Nuovo lead</Sheet.Title>
			<Sheet.Description>Registra un contatto reale da lavorare commercialmente.</Sheet.Description>
		</Sheet.Header>
		<form class="mt-6 space-y-4" onsubmit={handleCreateLead}>
			<Select.Root
				type="single"
				value={leadForm.locationId || CRM_NO_SELECTION_SELECT_VALUE}
				onValueChange={(value) =>
					(leadForm.locationId = value === CRM_NO_SELECTION_SELECT_VALUE ? '' : value)}
			>
				<Select.Trigger class="w-full">
					<span data-slot="select-value">{selectedLeadLocationLabel}</span>
				</Select.Trigger>
				<Select.Content>
					<Select.Item value={CRM_NO_SELECTION_SELECT_VALUE} label="Seleziona sede">
						Seleziona sede
					</Select.Item>
					{#each center.locations as location}
						<Select.Item value={location.id} label={location.name}>
							{location.name}
						</Select.Item>
					{/each}
				</Select.Content>
			</Select.Root>
			<Select.Root
				type="single"
				value={leadForm.ownerAssignmentId || CRM_NO_SELECTION_SELECT_VALUE}
				onValueChange={(value) =>
					(leadForm.ownerAssignmentId = value === CRM_NO_SELECTION_SELECT_VALUE ? '' : value)}
			>
				<Select.Trigger class="w-full">
					<span data-slot="select-value">{selectedLeadOwnerLabel}</span>
				</Select.Trigger>
				<Select.Content>
					<Select.Item value={CRM_NO_SELECTION_SELECT_VALUE} label="Assegna dopo">
						Assegna dopo
					</Select.Item>
					{#each ownerOptions as owner}
						<Select.Item value={owner.id} label={owner.label}>
							{owner.label}
						</Select.Item>
					{/each}
				</Select.Content>
			</Select.Root>
			<Input bind:value={leadForm.fullName} placeholder="Nome e cognome" />
			<div class="grid gap-3 md:grid-cols-2">
				<Input bind:value={leadForm.email} type="email" placeholder="Email" />
				<Input bind:value={leadForm.phoneNumber} placeholder="Telefono" />
			</div>
			<Select.Root type="single" bind:value={leadForm.source}>
				<Select.Trigger class="w-full">
					<span data-slot="select-value">{selectedLeadSourceLabel}</span>
				</Select.Trigger>
				<Select.Content>
					{#each sourceOptions as source}
						<Select.Item value={source.value} label={source.label}>
							{source.label}
						</Select.Item>
					{/each}
				</Select.Content>
			</Select.Root>
			<Input bind:value={leadForm.interest} placeholder="Interesse principale" />
			<Input bind:value={leadForm.nextFollowUpLocal} type="datetime-local" />
			<Textarea
				bind:value={leadForm.notes}
				rows={4}
				placeholder="Appunto iniziale, esigenza, budget, contesto..."
			/>
			<Button type="submit" class="w-full" disabled={createLeadSubmitting}>
				{createLeadSubmitting ? 'Salvataggio...' : 'Crea lead'}
			</Button>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={taskSheetOpen}>
	<Sheet.Content class="w-full sm:max-w-lg">
		<Sheet.Header>
			<Sheet.Title>Nuova task commerciale</Sheet.Title>
			<Sheet.Description>Programma il prossimo passo per il lead selezionato.</Sheet.Description>
		</Sheet.Header>
		<form class="mt-6 space-y-4" onsubmit={handleCreateTask}>
			<Select.Root
				type="single"
				value={taskForm.assignedAssignmentId || CRM_NO_SELECTION_SELECT_VALUE}
				onValueChange={(value) =>
					(taskForm.assignedAssignmentId = value === CRM_NO_SELECTION_SELECT_VALUE ? '' : value)}
			>
				<Select.Trigger class="w-full">
					<span data-slot="select-value">{selectedTaskAssigneeLabel}</span>
				</Select.Trigger>
				<Select.Content>
					<Select.Item value={CRM_NO_SELECTION_SELECT_VALUE} label="Nessun assegnatario specifico">
						Nessun assegnatario specifico
					</Select.Item>
					{#each ownerOptions as owner}
						<Select.Item value={owner.id} label={owner.label}>
							{owner.label}
						</Select.Item>
					{/each}
				</Select.Content>
			</Select.Root>
			<Input bind:value={taskForm.title} placeholder="Titolo task" />
			<Input bind:value={taskForm.dueAtLocal} type="datetime-local" />
			<Textarea
				bind:value={taskForm.notes}
				rows={4}
				placeholder="Dettagli operativi per il follow-up..."
			/>
			<Button type="submit" class="w-full" disabled={createTaskSubmitting}>
				{createTaskSubmitting ? 'Salvataggio...' : 'Crea task'}
			</Button>
		</form>
	</Sheet.Content>
</Sheet.Root>
