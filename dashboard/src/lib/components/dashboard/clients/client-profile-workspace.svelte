<script lang="ts">
	import { createQuery } from '@tanstack/svelte-query';
	import ArrowLeftIcon from '@lucide/svelte/icons/arrow-left';
	import CreditCardIcon from '@lucide/svelte/icons/credit-card';
	import DoorOpenIcon from '@lucide/svelte/icons/door-open';
	import DumbbellIcon from '@lucide/svelte/icons/dumbbell';
	import MailPlusIcon from '@lucide/svelte/icons/mail-plus';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import type { GymMembershipResponse, GymMembershipSource, GymMembershipStatus } from '$lib/api';
	import * as Avatar from '$lib/components/ui/avatar/index.js';
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
	import * as Tabs from '$lib/components/ui/tabs/index.js';
	import { fetchGymAccessEvents, type GymAccessEvent } from '$lib/data/access-api';
	import {
		fetchGymActivitySessions,
		type ActivityBookingStatus,
		type GymActivityBooking,
		type GymActivitySession
	} from '$lib/data/activities-api';
	import { fetchGymLeads, type GymLead, type LeadStage } from '$lib/data/crm-api';
	import { fetchGymSales, type GymSale } from '$lib/data/sales-api';
	import {
		fetchGymWorkoutAssignments,
		fetchGymWorkoutAssessments,
		type GymWorkoutAssessment,
		type GymWorkoutAssignment
	} from '$lib/data/training-api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';
	import {
		formatMembershipCustomFieldValue,
		formatMembershipDate,
		formatMembershipDateTime,
		membershipDisplayEmail,
		membershipDisplayName,
		membershipInitials,
		membershipSourceLabel,
		membershipStatusMeta
	} from '$lib/utils/membership-presenters.js';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
	type TimelineItem = {
		id: string;
		happenedAt: Date;
		title: string;
		description: string;
		variant: BadgeVariant;
	};
	type MemberBookedSession = {
		session: GymActivitySession;
		booking: GymActivityBooking | null;
	};
	type LatestActivity = {
		at: Date;
		label: string;
	};

	let { membershipId }: { membershipId: string } = $props();

	const api = useApiClient();
	const center = useCenterSelectionStore();
	const money = new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' });
	const decimal = new Intl.NumberFormat('it-IT', {
		minimumFractionDigits: 0,
		maximumFractionDigits: 1
	});
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

	let activeTab = $state('overview');
	let invitationPending = $state(false);
	let invitationMessage = $state('');
	let invitationError = $state('');

	const formatDate = (value: Date | null | undefined, fallback = 'Non disponibile') =>
		formatMembershipDate(value, date, fallback);

	const formatDateTime = (value: Date | null | undefined, fallback = 'Non disponibile') =>
		formatMembershipDateTime(value, dateTime, fallback);

	const formatCustomFieldValue = (
		value: string | null | undefined,
		valueType: string | null | undefined
	) => formatMembershipCustomFieldValue(value, valueType, formatDate);

	const sourceLabel = (source: GymMembershipSource | undefined) => membershipSourceLabel(source);

	const statusMeta = (status: GymMembershipStatus | undefined) => membershipStatusMeta(status);

	function accessMeta(event: GymAccessEvent) {
		if (event.result === 'Granted') {
			return { label: 'Consentito', variant: 'success' as BadgeVariant };
		}
		if (event.result === 'ManualOverride') {
			return { label: 'Override', variant: 'warning' as BadgeVariant };
		}
		return { label: 'Negato', variant: 'destructive' as BadgeVariant };
	}

	function salePaymentMeta(sale: GymSale) {
		if (sale.paymentStatus === 'Failed') {
			return { label: 'Failed', variant: 'destructive' as BadgeVariant };
		}
		if (sale.remainingAmount > 0) {
			return { label: sale.paymentStatus, variant: 'warning' as BadgeVariant };
		}
		return { label: sale.paymentStatus, variant: 'success' as BadgeVariant };
	}

	function assignmentMeta(assignment: GymWorkoutAssignment) {
		if (assignment.status === 'Completed') {
			return { label: 'Completato', variant: 'success' as BadgeVariant };
		}
		if (assignment.status === 'Archived') {
			return { label: 'Archiviato', variant: 'outline' as BadgeVariant };
		}
		return { label: 'Attivo', variant: 'secondary' as BadgeVariant };
	}

	function bookingStatusMeta(status: ActivityBookingStatus | null | undefined) {
		if (status === 'CheckedIn') {
			return { label: 'Check-in', variant: 'success' as BadgeVariant };
		}
		if (status === 'Cancelled') {
			return { label: 'Annullata', variant: 'outline' as BadgeVariant };
		}
		if (status === 'NoShow') {
			return { label: 'No show', variant: 'destructive' as BadgeVariant };
		}
		return { label: 'Prenotata', variant: 'secondary' as BadgeVariant };
	}

	function leadStageMeta(stage: LeadStage) {
		if (stage === 'Won') return { label: 'Vinto', variant: 'success' as BadgeVariant };
		if (stage === 'Lost') return { label: 'Perso', variant: 'destructive' as BadgeVariant };
		if (stage === 'Negotiation')
			return { label: 'Negoziazione', variant: 'warning' as BadgeVariant };
		if (stage === 'TrialBooked') return { label: 'Trial', variant: 'secondary' as BadgeVariant };
		if (stage === 'Contacted') return { label: 'Contattato', variant: 'outline' as BadgeVariant };
		return { label: 'Nuovo', variant: 'secondary' as BadgeVariant };
	}

	function activityWindowStartUtc() {
		const now = new Date();
		now.setDate(now.getDate() - 30);
		return now.toISOString();
	}

	function isOpenBooking(status: ActivityBookingStatus | null | undefined) {
		return status === 'Booked' || status === 'CheckedIn';
	}

	const membershipsQuery = createQuery(() => ({
		queryKey: ['client-profile-memberships', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdMembershipsGet({
				gymId: center.selectedGymId!
			});
			if (!response.success) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile caricare le membership.'
				);
			}
			return response.data ?? [];
		}
	}));

	const membership = $derived(
		(membershipsQuery.data ?? []).find((item) => item.membershipId === membershipId) ?? null
	);

	const salesQuery = createQuery(() => ({
		queryKey: ['client-profile-sales', center.selectedGymId, membershipId],
		enabled: !!center.selectedGymId && !!membershipId,
		queryFn: () => fetchGymSales(center.selectedGymId!, { membershipId })
	}));

	const accessQuery = createQuery(() => ({
		queryKey: ['client-profile-access', center.selectedGymId, membershipId],
		enabled: !!center.selectedGymId && !!membershipId,
		queryFn: () => fetchGymAccessEvents(center.selectedGymId!, { membershipId })
	}));

	const trainingAssignmentsQuery = createQuery(() => ({
		queryKey: ['client-profile-training-assignments', center.selectedGymId, membershipId],
		enabled: !!center.selectedGymId && !!membershipId,
		queryFn: () => fetchGymWorkoutAssignments(center.selectedGymId!, { membershipId })
	}));

	const trainingAssessmentsQuery = createQuery(() => ({
		queryKey: ['client-profile-training-assessments', center.selectedGymId, membershipId],
		enabled: !!center.selectedGymId && !!membershipId,
		queryFn: () => fetchGymWorkoutAssessments(center.selectedGymId!, { membershipId })
	}));

	const activitiesQuery = createQuery(() => ({
		queryKey: ['client-profile-activities', center.selectedGymId, membershipId],
		enabled: !!center.selectedGymId && !!membershipId,
		queryFn: () =>
			fetchGymActivitySessions(center.selectedGymId!, {
				membershipId,
				fromUtc: activityWindowStartUtc()
			})
	}));

	const crmQuery = createQuery(() => ({
		queryKey: ['client-profile-crm', center.selectedGymId, membershipId],
		enabled: !!center.selectedGymId && !!membershipId,
		queryFn: () => fetchGymLeads(center.selectedGymId!, { membershipId })
	}));

	const sales = $derived(salesQuery.data ?? []);
	const accessEvents = $derived(accessQuery.data ?? []);
	const workoutAssignments = $derived(trainingAssignmentsQuery.data ?? []);
	const workoutAssessments = $derived(trainingAssessmentsQuery.data ?? []);
	const activitySessions = $derived(activitiesQuery.data ?? []);
	const relatedLeads = $derived(crmQuery.data ?? []);
	const memberName = $derived(membership ? membershipDisplayName(membership) : 'Cliente');
	const memberEmail = $derived(membership ? membershipDisplayEmail(membership) : 'Email non disponibile');
	const currentStatus = $derived(statusMeta(membership?.status));
	const customFields = $derived(
		(membership?.customFields ?? []).slice().sort((left, right) => {
			const leftOrder = left.sortOrder ?? 0;
			const rightOrder = right.sortOrder ?? 0;
			return leftOrder - rightOrder;
		})
	);
	const currentLocations = $derived(
		(membership?.locations ?? []).map((location) => location.name).filter(Boolean)
	);
	const memberSessions = $derived(
		activitySessions.map((session) => ({
			session,
			booking: session.bookings.find((booking) => booking.membershipId === membershipId) ?? null
		}))
	);
	const totalSpent = $derived(
		sales
			.filter((sale) => sale.status !== 'Cancelled')
			.reduce((sum, sale) => sum + sale.paidAmount, 0)
	);
	const pendingAmount = $derived(
		sales
			.filter((sale) => sale.status !== 'Cancelled' && sale.status !== 'Refunded')
			.reduce((sum, sale) => sum + sale.remainingAmount, 0)
	);
	const grantedAccessCount = $derived(
		accessEvents.filter((event) => event.result === 'Granted').length
	);
	const deniedAccessCount = $derived(
		accessEvents.filter((event) => event.result === 'Denied').length
	);
	const activeAssignmentsCount = $derived(
		workoutAssignments.filter((assignment) => assignment.status === 'Active').length
	);
	const dueAssignmentsCount = $derived.by(() => {
		const now = Date.now();
		return workoutAssignments.filter(
			(assignment) =>
				assignment.status === 'Active' &&
				assignment.revisionDueAtUtc !== null &&
				assignment.revisionDueAtUtc.getTime() <= now
		).length;
	});
	const upcomingBookingsCount = $derived.by(() => {
		const now = Date.now();
		return memberSessions.filter(
			(row) => row.session.endsAtUtc.getTime() >= now && isOpenBooking(row.booking?.status)
		).length;
	});
	const openCrmTasksCount = $derived(
		relatedLeads.reduce(
			(sum, lead) => sum + lead.tasks.filter((task) => task.status === 'Open').length,
			0
		)
	);
	const lastSale = $derived(sales[0] ?? null);
	const lastAccess = $derived(accessEvents[0] ?? null);
	const lastAssessment = $derived(workoutAssessments[0] ?? null);
	const latestActivity = $derived.by(() => {
		const items: LatestActivity[] = [];

		if (lastAccess) {
			items.push({
				at: lastAccess.occurredAtUtc,
				label: `${lastAccess.result === 'Granted' ? 'Accesso' : 'Tentativo'} su ${lastAccess.locationName}`
			});
		}

		if (lastSale) {
			items.push({
				at: lastSale.soldAtUtc,
				label: `Vendita ${lastSale.referenceCode}`
			});
		}

		if (lastAssessment) {
			items.push({
				at: lastAssessment.recordedAtUtc,
				label: `Valutazione fisica registrata da ${lastAssessment.recordedByUserName}`
			});
		}

		if (workoutAssignments[0]) {
			items.push({
				at: workoutAssignments[0].assignedAtUtc,
				label: `Scheda ${workoutAssignments[0].title}`
			});
		}

		if (memberSessions[0]) {
			items.push({
				at: memberSessions[0].session.startsAtUtc,
				label: `Attivita ${memberSessions[0].session.title}`
			});
		}

		if (relatedLeads[0]) {
			items.push({
				at: relatedLeads[0].updatedAtUtc,
				label: `CRM ${leadStageMeta(relatedLeads[0].stage).label}`
			});
		}

		if (membership) {
			items.push({
				at: membership.updatedAtUtc ?? membership.createdAtUtc ?? new Date(),
				label: 'Profilo membership aggiornato'
			});
		}

		return items.sort((left, right) => right.at.getTime() - left.at.getTime())[0] ?? null;
	});

	const timeline = $derived.by(() => {
		const items: TimelineItem[] = [];

		if (membership) {
			items.push({
				id: `${membershipId}-created`,
				happenedAt: membership.createdAtUtc ?? new Date(),
				title: 'Membership creata',
				description: `Creata nel tenant ${membership.gymName ?? center.selectedGym?.name ?? 'corrente'}.`,
				variant: 'secondary'
			});

			if (membership.claimedAtUtc) {
				items.push({
					id: `${membershipId}-claimed`,
					happenedAt: membership.claimedAtUtc,
					title: 'Account claimato',
					description: 'Il cliente ha completato il collegamento con il proprio account Betterfit.',
					variant: 'success'
				});
			}
		}

		for (const sale of sales.slice(0, 6)) {
			items.push({
				id: sale.saleId,
				happenedAt: sale.soldAtUtc,
				title: `Vendita ${sale.referenceCode}`,
				description: `${money.format(sale.totalAmount)} - ${sale.locationName}`,
				variant: salePaymentMeta(sale).variant
			});
		}

		for (const event of accessEvents.slice(0, 6)) {
			items.push({
				id: event.eventId,
				happenedAt: event.occurredAtUtc,
				title: event.result === 'Granted' ? 'Accesso consentito' : 'Accesso negato',
				description: `${event.locationName} - ${event.gateName}${event.reason ? ` - ${event.reason}` : ''}`,
				variant: accessMeta(event).variant
			});
		}

		for (const assignment of workoutAssignments.slice(0, 4)) {
			items.push({
				id: assignment.assignmentId,
				happenedAt: assignment.assignedAtUtc,
				title: `Scheda ${assignment.title}`,
				description: `${assignment.locationName}${assignment.goal ? ` - ${assignment.goal}` : ''}`,
				variant: assignmentMeta(assignment).variant
			});
		}

		for (const assessment of workoutAssessments.slice(0, 4)) {
			const values = [
				assessment.weightKg !== null ? `${decimal.format(assessment.weightKg)} kg` : null,
				assessment.bodyFatPercentage !== null
					? `${decimal.format(assessment.bodyFatPercentage)}% body fat`
					: null
			]
				.filter(Boolean)
				.join(' - ');

			items.push({
				id: assessment.assessmentId,
				happenedAt: assessment.recordedAtUtc,
				title: 'Misure e valutazione',
				description: values || assessment.notes || assessment.locationName,
				variant: 'secondary'
			});
		}

		for (const row of memberSessions.slice(0, 4)) {
			items.push({
				id: row.session.sessionId,
				happenedAt: row.session.startsAtUtc,
				title: `Attivita ${row.session.title}`,
				description: `${row.session.locationName} - ${bookingStatusMeta(row.booking?.status).label}`,
				variant: bookingStatusMeta(row.booking?.status).variant
			});
		}

		for (const lead of relatedLeads.slice(0, 4)) {
			items.push({
				id: lead.leadId,
				happenedAt: lead.updatedAtUtc,
				title: `Lead CRM ${lead.fullName}`,
				description: `${leadStageMeta(lead.stage).label}${lead.nextFollowUpAtUtc ? ` - Follow-up ${formatDateTime(lead.nextFollowUpAtUtc)}` : ''}`,
				variant: leadStageMeta(lead.stage).variant
			});
		}

		return items
			.sort((left, right) => right.happenedAt.getTime() - left.happenedAt.getTime())
			.slice(0, 14);
	});

	async function refreshAll() {
		await Promise.all([
			membershipsQuery.refetch(),
			salesQuery.refetch(),
			accessQuery.refetch(),
			trainingAssignmentsQuery.refetch(),
			trainingAssessmentsQuery.refetch(),
			activitiesQuery.refetch(),
			crmQuery.refetch()
		]);
	}

	async function handleCreateInvitation() {
		if (!center.selectedGymId || !membership?.membershipId) {
			return;
		}

		invitationPending = true;
		invitationError = '';
		invitationMessage = '';

		try {
			const response = await api.client.apiGymsGymIdMembershipsMembershipIdInvitationsPost({
				gymId: center.selectedGymId,
				membershipId: membership.membershipId,
				createGymInvitationRequest: {
					expiresInHours: 72
				}
			});

			if (!response.success || !response.data) {
				throw new Error(
					response.error?.message ?? response.message ?? "Impossibile generare l'invito."
				);
			}

			invitationMessage = `Invito generato per ${response.data.email ?? memberEmail} con scadenza ${formatDateTime(response.data.expiresAtUtc ?? null)}.`;
		} catch (error: unknown) {
			invitationError = error instanceof Error ? error.message : "Impossibile generare l'invito.";
		} finally {
			invitationPending = false;
		}
	}
</script>

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	<section class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
		<div class="space-y-2">
			<div class="flex flex-wrap items-center gap-2">
				<Button variant="outline" size="sm" href="/users">
					<ArrowLeftIcon class="size-4" />
					Torna alla lista
				</Button>
				{#if center.selectedGym}
					<Badge variant="outline" class="rounded-full px-3 py-1">{center.selectedGym.name}</Badge>
				{/if}
				{#if center.selectedLocation}
					<Badge variant="outline" class="rounded-full px-3 py-1"
						>{center.selectedLocation.name}</Badge
					>
				{/if}
			</div>
			<div>
				<h2 class="text-2xl font-semibold tracking-tight">Scheda cliente</h2>
				<p class="text-sm text-muted-foreground">
					Vista operativa unica su anagrafica, vendite, accessi, training, attivita e CRM del
					cliente.
				</p>
			</div>
		</div>

		<div class="flex flex-wrap items-center gap-2">
			<Button variant="outline" size="sm" onclick={refreshAll}>
				<RefreshCwIcon class="size-4" />
				Aggiorna
			</Button>
			<Button size="sm" href={`/sales/new?membershipId=${encodeURIComponent(membershipId)}`}>
				<CreditCardIcon class="size-4" />
				Nuova vendita
			</Button>
			<Button
				variant="outline"
				size="sm"
				href={`/access/checkin?membershipId=${encodeURIComponent(membershipId)}`}
			>
				<DoorOpenIcon class="size-4" />
				Check-in desk
			</Button>
			<Button
				variant="outline"
				size="sm"
				href={`/training?membershipId=${encodeURIComponent(membershipId)}`}
			>
				<DumbbellIcon class="size-4" />
				Training
			</Button>
		</div>
	</section>

	{#if invitationMessage}
		<section
			class="rounded-[20px] border border-[#bbf7d0] bg-[#f0fdf4] px-4 py-3 text-sm text-[#166534]"
		>
			{invitationMessage}
		</section>
	{/if}

	{#if invitationError}
		<section
			class="rounded-[20px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]"
		>
			{invitationError}
		</section>
	{/if}

	{#if membershipsQuery.isPending}
		<section
			class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
		>
			<p class="text-base font-semibold">Carico il profilo cliente...</p>
		</section>
	{:else if !membership}
		<section
			class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
		>
			<p class="text-base font-semibold">Cliente non trovato nel tenant selezionato</p>
			<p class="mt-2 text-sm text-muted-foreground">
				Verifica di essere nella palestra corretta oppure torna alla lista utenti.
			</p>
		</section>
	{:else}
		<section class="grid gap-4 xl:grid-cols-[1.4fr_1fr]">
			<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
				<CardContent class="flex flex-col gap-5 p-6 lg:flex-row lg:items-start lg:justify-between">
					<div class="flex items-start gap-4">
						<Avatar.Root class="size-16 border border-border/70 bg-secondary">
							<Avatar.Image
								src={membership.memberProfile?.avatarUrl ??
									`https://api.dicebear.com/9.x/initials/svg?seed=${encodeURIComponent(memberName)}`}
								alt={memberName}
							/>
							<Avatar.Fallback class="bg-primary/10 text-primary">
								{membershipInitials(memberName)}
							</Avatar.Fallback>
						</Avatar.Root>

						<div class="space-y-2">
							<div class="flex flex-wrap items-center gap-2">
								<h3 class="text-2xl font-semibold">{memberName}</h3>
								<Badge variant={currentStatus.variant}>{currentStatus.label}</Badge>
								<Badge variant="secondary">{sourceLabel(membership.source)}</Badge>
							</div>
							<p class="text-sm text-muted-foreground">{memberEmail}</p>
							<div class="grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
								<p>
									Sedi abilitate: {currentLocations.length > 0
										? currentLocations.join(', ')
										: 'Nessuna sede'}
								</p>
								<p>Scadenza membership: {formatDate(membership.endedAtUtc, 'Non impostata')}</p>
								<p>Attivato il: {formatDate(membership.joinedAtUtc, 'Non ancora attivato')}</p>
								<p>Ultimo aggiornamento: {formatDateTime(membership.updatedAtUtc)}</p>
							</div>
						</div>
					</div>

					<div class="grid gap-2 sm:grid-cols-2 lg:grid-cols-1">
						<Button href={`/sales/new?membershipId=${encodeURIComponent(membershipId)}`}>
							<CreditCardIcon class="size-4" />
							Registra vendita
						</Button>
						<Button
							variant="outline"
							href={`/access/checkin?membershipId=${encodeURIComponent(membershipId)}`}
						>
							<DoorOpenIcon class="size-4" />
							Registra ingresso
						</Button>
						<Button
							variant="outline"
							href={`/training?membershipId=${encodeURIComponent(membershipId)}`}
						>
							<DumbbellIcon class="size-4" />
							Apri training
						</Button>
						{#if membership.status === 'PendingClaim'}
							<Button
								variant="outline"
								onclick={handleCreateInvitation}
								disabled={invitationPending}
							>
								{#if invitationPending}
									<RefreshCwIcon class="size-4 animate-spin" />
									Genero invito...
								{:else}
									<MailPlusIcon class="size-4" />
									Invia reminder attivazione
								{/if}
							</Button>
						{/if}
					</div>
				</CardContent>
			</Card>

			<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
				<CardHeader>
					<CardTitle>Note operative</CardTitle>
					<CardDescription>Contesto rapido per reception, amministrazione e coach.</CardDescription>
				</CardHeader>
				<CardContent class="space-y-4">
					<div class="rounded-[18px] border border-border/70 p-4">
						<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">
							Note membership
						</p>
						<p class="mt-2 text-sm leading-relaxed text-foreground">
							{membership.notes?.trim() || 'Nessuna nota operativa registrata.'}
						</p>
					</div>
					<div class="grid gap-3 sm:grid-cols-2">
						<div class="rounded-[18px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">
								Codice fiscale
							</p>
							<p class="mt-2 text-sm font-semibold">{membership.taxCode ?? 'Non registrato'}</p>
						</div>
						<div class="rounded-[18px] border border-border/70 p-4">
							<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">
								Claim account
							</p>
							<p class="mt-2 text-sm font-semibold">
								{formatDateTime(membership.claimedAtUtc, 'Non ancora claimato')}
							</p>
						</div>
					</div>
					<div class="rounded-[18px] border border-border/70 p-4">
						<p class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase">
							Ultima attivita
						</p>
						<p class="mt-2 text-sm font-semibold">
							{latestActivity ? formatDateTime(latestActivity.at) : 'Nessuna attivita recente'}
						</p>
						<p class="mt-1 text-sm text-muted-foreground">
							{latestActivity?.label ??
								'Il profilo non ha ancora eventi utili nello storico caricato.'}
						</p>
					</div>
				</CardContent>
			</Card>
		</section>

		<section class="grid gap-4 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-6">
			<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
				<CardHeader class="space-y-1 pb-2">
					<CardDescription>Incassato totale</CardDescription>
					<CardTitle class="text-3xl">{money.format(totalSpent)}</CardTitle>
				</CardHeader>
				<CardContent class="text-sm text-muted-foreground">
					{sales.length} vendite registrate sul cliente
				</CardContent>
			</Card>

			<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
				<CardHeader class="space-y-1 pb-2">
					<CardDescription>Da incassare</CardDescription>
					<CardTitle class="text-3xl">{money.format(pendingAmount)}</CardTitle>
				</CardHeader>
				<CardContent class="text-sm text-muted-foreground">
					Residuo aperto sulle vendite attive
				</CardContent>
			</Card>

			<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
				<CardHeader class="space-y-1 pb-2">
					<CardDescription>Accessi consentiti</CardDescription>
					<CardTitle class="text-3xl">{grantedAccessCount}</CardTitle>
				</CardHeader>
				<CardContent class="text-sm text-muted-foreground">
					Negati: {deniedAccessCount}
				</CardContent>
			</Card>

			<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
				<CardHeader class="space-y-1 pb-2">
					<CardDescription>Piani attivi</CardDescription>
					<CardTitle class="text-3xl">{activeAssignmentsCount}</CardTitle>
				</CardHeader>
				<CardContent class="text-sm text-muted-foreground">
					Revisioni da verificare: {dueAssignmentsCount}
				</CardContent>
			</Card>

			<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
				<CardHeader class="space-y-1 pb-2">
					<CardDescription>Prenotazioni future</CardDescription>
					<CardTitle class="text-3xl">{upcomingBookingsCount}</CardTitle>
				</CardHeader>
				<CardContent class="text-sm text-muted-foreground">
					Storico attivita caricato: {memberSessions.length}
				</CardContent>
			</Card>

			<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
				<CardHeader class="space-y-1 pb-2">
					<CardDescription>Task CRM aperti</CardDescription>
					<CardTitle class="text-3xl">{openCrmTasksCount}</CardTitle>
				</CardHeader>
				<CardContent class="text-sm text-muted-foreground">
					Lead collegati: {relatedLeads.length}
				</CardContent>
			</Card>
		</section>

		<section>
			<Tabs.Root bind:value={activeTab} class="gap-4">
				<Tabs.List
					class="flex w-full flex-wrap gap-2 rounded-[20px] border border-border/70 bg-secondary/20 p-2"
				>
					<Tabs.Trigger value="overview">Panoramica</Tabs.Trigger>
					<Tabs.Trigger value="sales">Vendite</Tabs.Trigger>
					<Tabs.Trigger value="access">Accessi</Tabs.Trigger>
					<Tabs.Trigger value="training">Training</Tabs.Trigger>
					<Tabs.Trigger value="activities">Attivita</Tabs.Trigger>
					<Tabs.Trigger value="crm">CRM</Tabs.Trigger>
					<Tabs.Trigger value="profile">Profilo</Tabs.Trigger>
				</Tabs.List>

				<Tabs.Content value="overview" class="space-y-4">
					<div class="grid gap-4 xl:grid-cols-[1.35fr_1fr]">
						<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
							<CardHeader>
								<CardTitle>Timeline</CardTitle>
								<CardDescription
									>Eventi recenti davvero utili per capire il contesto del cliente.</CardDescription
								>
							</CardHeader>
							<CardContent class="space-y-3">
								{#if timeline.length === 0}
									<div
										class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
									>
										<p class="font-semibold">Nessuna attivita registrata</p>
									</div>
								{:else}
									{#each timeline as item (item.id)}
										<div class="rounded-[16px] border border-border/70 p-4">
											<div class="flex flex-wrap items-center justify-between gap-2">
												<p class="font-semibold">{item.title}</p>
												<Badge variant={item.variant}>{formatDateTime(item.happenedAt)}</Badge>
											</div>
											<p class="mt-2 text-sm text-muted-foreground">{item.description}</p>
										</div>
									{/each}
								{/if}
							</CardContent>
						</Card>

						<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
							<CardHeader>
								<CardTitle>Azioni consigliate</CardTitle>
								<CardDescription
									>Prossimi passi rapidi suggeriti dallo stato attuale.</CardDescription
								>
							</CardHeader>
							<CardContent class="space-y-3">
								{#if membership.status === 'PendingClaim'}
									<div class="rounded-[16px] border border-border/70 p-4">
										<p class="font-semibold">Completare attivazione</p>
										<p class="mt-2 text-sm text-muted-foreground">
											Il cliente e ancora in PendingClaim: conviene reinviare il reminder o
											completare il claim al desk.
										</p>
									</div>
								{/if}
								{#if membership.endedAtUtc}
									<div class="rounded-[16px] border border-border/70 p-4">
										<p class="font-semibold">Monitorare scadenza</p>
										<p class="mt-2 text-sm text-muted-foreground">
											La membership risulta in scadenza il {formatDate(membership.endedAtUtc)}.
										</p>
									</div>
								{/if}
								{#if pendingAmount > 0}
									<div class="rounded-[16px] border border-border/70 p-4">
										<p class="font-semibold">Chiudere il residuo</p>
										<p class="mt-2 text-sm text-muted-foreground">
											Ci sono ancora {money.format(pendingAmount)} da incassare sulle vendite aperte.
										</p>
									</div>
								{/if}
								{#if deniedAccessCount > 0}
									<div class="rounded-[16px] border border-border/70 p-4">
										<p class="font-semibold">Verificare accessi negati</p>
										<p class="mt-2 text-sm text-muted-foreground">
											Il cliente ha {deniedAccessCount} tentativi negati nello storico disponibile.
										</p>
									</div>
								{/if}
								{#if dueAssignmentsCount > 0}
									<div class="rounded-[16px] border border-border/70 p-4">
										<p class="font-semibold">Rivedere la scheda</p>
										<p class="mt-2 text-sm text-muted-foreground">
											Ci sono {dueAssignmentsCount} piani attivi con revisione scaduta o da aggiornare.
										</p>
									</div>
								{/if}
								{#if upcomingBookingsCount > 0}
									<div class="rounded-[16px] border border-border/70 p-4">
										<p class="font-semibold">Confermare le prossime presenze</p>
										<p class="mt-2 text-sm text-muted-foreground">
											Il cliente ha {upcomingBookingsCount} prenotazioni future da monitorare in attivita.
										</p>
									</div>
								{/if}
								{#if openCrmTasksCount > 0}
									<div class="rounded-[16px] border border-border/70 p-4">
										<p class="font-semibold">Chiudere task commerciali</p>
										<p class="mt-2 text-sm text-muted-foreground">
											Sono presenti {openCrmTasksCount} task CRM aperti collegati al cliente.
										</p>
									</div>
								{/if}
								{#if membership.status === 'Active' && pendingAmount === 0 && deniedAccessCount === 0 && dueAssignmentsCount === 0 && openCrmTasksCount === 0}
									<div class="rounded-[16px] border border-border/70 p-4">
										<p class="font-semibold">Profilo operativo regolare</p>
										<p class="mt-2 text-sm text-muted-foreground">
											Il cliente non mostra blocchi amministrativi o alert operativi nello storico
											corrente.
										</p>
									</div>
								{/if}
							</CardContent>
						</Card>
					</div>
				</Tabs.Content>

				<Tabs.Content value="sales" class="space-y-4">
					<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
						<CardHeader class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
							<div>
								<CardTitle>Vendite e pagamenti</CardTitle>
								<CardDescription>Storico economico del cliente nel tenant corrente.</CardDescription
								>
							</div>
							<Button
								size="sm"
								href={`/sales/new?membershipId=${encodeURIComponent(membershipId)}`}
							>
								<CreditCardIcon class="size-4" />
								Nuova vendita
							</Button>
						</CardHeader>
						<CardContent>
							{#if salesQuery.isPending}
								<div
									class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
								>
									<p class="font-semibold">Carico le vendite...</p>
								</div>
							{:else if sales.length === 0}
								<div
									class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
								>
									<p class="font-semibold">Nessuna vendita registrata</p>
									<p class="mt-2 text-sm text-muted-foreground">
										Puoi creare direttamente la prima vendita dal profilo cliente.
									</p>
								</div>
							{:else}
								<div class="overflow-hidden rounded-[20px] border border-border/70">
									<Table>
										<TableHeader>
											<TableRow class="bg-secondary/30 hover:bg-secondary/30">
												<TableHead>Riferimento</TableHead>
												<TableHead>Data</TableHead>
												<TableHead>Sede</TableHead>
												<TableHead>Totale</TableHead>
												<TableHead>Residuo</TableHead>
												<TableHead>Stato</TableHead>
											</TableRow>
										</TableHeader>
										<TableBody>
											{#each sales as sale (sale.saleId)}
												<TableRow>
													<TableCell>
														<div>
															<p class="font-semibold">{sale.referenceCode}</p>
															{#if sale.notes}
																<p class="text-xs text-muted-foreground">{sale.notes}</p>
															{/if}
														</div>
													</TableCell>
													<TableCell>{formatDateTime(sale.soldAtUtc)}</TableCell>
													<TableCell>{sale.locationName}</TableCell>
													<TableCell>{money.format(sale.totalAmount)}</TableCell>
													<TableCell>{money.format(sale.remainingAmount)}</TableCell>
													<TableCell>
														<Badge variant={salePaymentMeta(sale).variant}>
															{salePaymentMeta(sale).label}
														</Badge>
													</TableCell>
												</TableRow>
											{/each}
										</TableBody>
									</Table>
								</div>
							{/if}
						</CardContent>
					</Card>
				</Tabs.Content>

				<Tabs.Content value="access" class="space-y-4">
					<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
						<CardHeader class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
							<div>
								<CardTitle>Storico accessi</CardTitle>
								<CardDescription>Timeline reale di check-in consentiti e negati.</CardDescription>
							</div>
							<Button
								variant="outline"
								size="sm"
								href={`/access/checkin?membershipId=${encodeURIComponent(membershipId)}`}
							>
								<DoorOpenIcon class="size-4" />
								Check-in desk
							</Button>
						</CardHeader>
						<CardContent>
							{#if accessQuery.isPending}
								<div
									class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
								>
									<p class="font-semibold">Carico gli accessi...</p>
								</div>
							{:else if accessEvents.length === 0}
								<div
									class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
								>
									<p class="font-semibold">Nessun accesso registrato</p>
								</div>
							{:else}
								<div class="overflow-hidden rounded-[20px] border border-border/70">
									<Table>
										<TableHeader>
											<TableRow class="bg-secondary/30 hover:bg-secondary/30">
												<TableHead>Quando</TableHead>
												<TableHead>Sede</TableHead>
												<TableHead>Gate</TableHead>
												<TableHead>Esito</TableHead>
												<TableHead>Origine</TableHead>
											</TableRow>
										</TableHeader>
										<TableBody>
											{#each accessEvents as event (event.eventId)}
												<TableRow>
													<TableCell>
														<div>
															<p class="font-semibold">{formatDateTime(event.occurredAtUtc)}</p>
															{#if event.reason}
																<p class="text-xs text-muted-foreground">{event.reason}</p>
															{/if}
														</div>
													</TableCell>
													<TableCell>{event.locationName}</TableCell>
													<TableCell>{event.gateName}</TableCell>
													<TableCell>
														<Badge variant={accessMeta(event).variant}
															>{accessMeta(event).label}</Badge
														>
													</TableCell>
													<TableCell>{event.origin}</TableCell>
												</TableRow>
											{/each}
										</TableBody>
									</Table>
								</div>
							{/if}
						</CardContent>
					</Card>
				</Tabs.Content>

				<Tabs.Content value="training" class="space-y-4">
					<div class="grid gap-4 xl:grid-cols-[1.25fr_1fr]">
						<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
							<CardHeader
								class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"
							>
								<div>
									<CardTitle>Piani assegnati</CardTitle>
									<CardDescription>Schede attive, revisioni e note del cliente.</CardDescription>
								</div>
								<Button
									variant="outline"
									size="sm"
									href={`/training?membershipId=${encodeURIComponent(membershipId)}`}
								>
									<DumbbellIcon class="size-4" />
									Apri area training
								</Button>
							</CardHeader>
							<CardContent class="space-y-3">
								{#if trainingAssignmentsQuery.isPending}
									<div
										class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
									>
										<p class="font-semibold">Carico i piani di training...</p>
									</div>
								{:else if workoutAssignments.length === 0}
									<div
										class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
									>
										<p class="font-semibold">Nessuna scheda assegnata</p>
									</div>
								{:else}
									{#each workoutAssignments as assignment (assignment.assignmentId)}
										<div class="rounded-[16px] border border-border/70 p-4">
											<div class="flex flex-wrap items-center justify-between gap-2">
												<div>
													<p class="font-semibold">{assignment.title}</p>
													<p class="text-sm text-muted-foreground">
														{assignment.locationName} - Coach: {assignment.coachName}
													</p>
												</div>
												<Badge variant={assignmentMeta(assignment).variant}>
													{assignmentMeta(assignment).label}
												</Badge>
											</div>
											<div class="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
												<p>Assegnata: {formatDateTime(assignment.assignedAtUtc)}</p>
												<p>Start: {formatDate(assignment.startsAtUtc, 'Non impostato')}</p>
												<p>Revisione: {formatDate(assignment.revisionDueAtUtc, 'Non impostata')}</p>
												<p>
													Giorni strutturati: {new Set(
														assignment.items.map((item) => item.dayNumber)
													).size}
												</p>
											</div>
											{#if assignment.goal || assignment.notes}
												<p class="mt-3 text-sm text-muted-foreground">
													{assignment.goal ?? assignment.notes}
												</p>
											{/if}
										</div>
									{/each}
								{/if}
							</CardContent>
						</Card>

						<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
							<CardHeader>
								<CardTitle>Misure e valutazioni</CardTitle>
								<CardDescription>Ultime rilevazioni fisiche registrate sul cliente.</CardDescription
								>
							</CardHeader>
							<CardContent class="space-y-3">
								{#if trainingAssessmentsQuery.isPending}
									<div
										class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
									>
										<p class="font-semibold">Carico le valutazioni...</p>
									</div>
								{:else if workoutAssessments.length === 0}
									<div
										class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
									>
										<p class="font-semibold">Nessuna valutazione registrata</p>
									</div>
								{:else}
									{#each workoutAssessments as assessment (assessment.assessmentId)}
										<div class="rounded-[16px] border border-border/70 p-4">
											<div class="flex flex-wrap items-center justify-between gap-2">
												<p class="font-semibold">{formatDateTime(assessment.recordedAtUtc)}</p>
												<Badge variant="secondary">{assessment.locationName}</Badge>
											</div>
											<div class="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
												<p>
													Peso: {assessment.weightKg !== null
														? `${decimal.format(assessment.weightKg)} kg`
														: 'Non rilevato'}
												</p>
												<p>
													Body fat: {assessment.bodyFatPercentage !== null
														? `${decimal.format(assessment.bodyFatPercentage)}%`
														: 'Non rilevato'}
												</p>
												<p>
													Lean mass: {assessment.leanMassKg !== null
														? `${decimal.format(assessment.leanMassKg)} kg`
														: 'Non rilevata'}
												</p>
												<p>
													FC riposo: {assessment.restingHeartRateBpm !== null
														? `${assessment.restingHeartRateBpm} bpm`
														: 'Non rilevata'}
												</p>
											</div>
											<p class="mt-3 text-sm text-muted-foreground">
												Registrata da {assessment.recordedByUserName}
											</p>
											{#if assessment.notes}
												<p class="mt-2 text-sm text-muted-foreground">{assessment.notes}</p>
											{/if}
										</div>
									{/each}
								{/if}
							</CardContent>
						</Card>
					</div>
				</Tabs.Content>

				<Tabs.Content value="activities" class="space-y-4">
					<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
						<CardHeader>
							<CardTitle>Attivita e prenotazioni</CardTitle>
							<CardDescription
								>Corsi e servizi collegati al cliente nelle ultime settimane.</CardDescription
							>
						</CardHeader>
						<CardContent class="space-y-3">
							{#if activitiesQuery.isPending}
								<div
									class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
								>
									<p class="font-semibold">Carico le attivita...</p>
								</div>
							{:else if memberSessions.length === 0}
								<div
									class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
								>
									<p class="font-semibold">Nessuna attivita collegata al cliente</p>
								</div>
							{:else}
								{#each memberSessions as row (row.session.sessionId)}
									<div class="rounded-[16px] border border-border/70 p-4">
										<div class="flex flex-wrap items-center justify-between gap-2">
											<div>
												<p class="font-semibold">{row.session.title}</p>
												<p class="text-sm text-muted-foreground">
													{row.session.category} - {row.session.locationName}
												</p>
											</div>
											<Badge variant={bookingStatusMeta(row.booking?.status).variant}>
												{bookingStatusMeta(row.booking?.status).label}
											</Badge>
										</div>
										<div class="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
											<p>Inizio: {formatDateTime(row.session.startsAtUtc)}</p>
											<p>Fine: {formatDateTime(row.session.endsAtUtc)}</p>
											<p>Istruttore: {row.session.instructorName}</p>
											<p>Capienza residua: {row.session.remainingSpots}</p>
										</div>
										{#if row.booking}
											<p class="mt-3 text-sm text-muted-foreground">
												Prenotata il {formatDateTime(row.booking.bookedAtUtc)}
												{#if row.booking.checkedInAtUtc}
													, check-in alle {formatDateTime(row.booking.checkedInAtUtc)}
												{/if}
											</p>
										{/if}
										{#if row.booking?.notes || row.session.notes}
											<p class="mt-2 text-sm text-muted-foreground">
												{row.booking?.notes ?? row.session.notes}
											</p>
										{/if}
									</div>
								{/each}
							{/if}
						</CardContent>
					</Card>
				</Tabs.Content>

				<Tabs.Content value="crm" class="space-y-4">
					<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
						<CardHeader>
							<CardTitle>Lead e task commerciali</CardTitle>
							<CardDescription
								>Contesto CRM collegato al cliente o al suo storico di conversione.</CardDescription
							>
						</CardHeader>
						<CardContent class="space-y-3">
							{#if crmQuery.isPending}
								<div
									class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
								>
									<p class="font-semibold">Carico il CRM...</p>
								</div>
							{:else if relatedLeads.length === 0}
								<div
									class="rounded-[18px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
								>
									<p class="font-semibold">Nessun lead CRM collegato</p>
									<p class="mt-2 text-sm text-muted-foreground">
										Il cliente non ha ancora conversioni o contatti CRM agganciati al profilo.
									</p>
								</div>
							{:else}
								{#each relatedLeads as lead (lead.leadId)}
									<div class="rounded-[16px] border border-border/70 p-4">
										<div class="flex flex-wrap items-center justify-between gap-2">
											<div>
												<p class="font-semibold">{lead.fullName}</p>
												<p class="text-sm text-muted-foreground">
													{lead.locationName} - {lead.source}
												</p>
											</div>
											<Badge variant={leadStageMeta(lead.stage).variant}>
												{leadStageMeta(lead.stage).label}
											</Badge>
										</div>
										<div class="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
											<p>Owner: {lead.ownerName ?? 'Non assegnato'}</p>
											<p>Follow-up: {formatDateTime(lead.nextFollowUpAtUtc, 'Non pianificato')}</p>
											<p>
												Ultimo contatto: {formatDateTime(lead.lastContactedAtUtc, 'Non registrato')}
											</p>
											<p>Aggiornato: {formatDateTime(lead.updatedAtUtc)}</p>
										</div>
										{#if lead.notes}
											<p class="mt-3 text-sm text-muted-foreground">{lead.notes}</p>
										{/if}
										{#if lead.tasks.length > 0}
											<div class="mt-4 space-y-2">
												<p
													class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
												>
													Task commerciali
												</p>
												{#each lead.tasks as task (task.taskId)}
													<div class="rounded-[14px] border border-border/60 px-3 py-3">
														<div class="flex flex-wrap items-center justify-between gap-2">
															<p class="text-sm font-semibold">{task.title}</p>
															<Badge
																variant={task.status === 'Completed'
																	? 'success'
																	: task.status === 'Cancelled'
																		? 'outline'
																		: 'warning'}
															>
																{task.status}
															</Badge>
														</div>
														<p class="mt-1 text-sm text-muted-foreground">
															Assegnato a {task.assignedStaffName ?? 'Desk'}
															{task.dueAtUtc ? ` - scadenza ${formatDateTime(task.dueAtUtc)}` : ''}
														</p>
														{#if task.notes}
															<p class="mt-2 text-sm text-muted-foreground">{task.notes}</p>
														{/if}
													</div>
												{/each}
											</div>
										{/if}
									</div>
								{/each}
							{/if}
						</CardContent>
					</Card>
				</Tabs.Content>

				<Tabs.Content value="profile" class="space-y-4">
					<div class="space-y-4">
						<div class="grid gap-4 xl:grid-cols-2">
							<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
								<CardHeader>
									<CardTitle>Anagrafica</CardTitle>
									<CardDescription
										>Dati personali gia disponibili nel modello membership/account.</CardDescription
									>
								</CardHeader>
								<CardContent class="grid gap-3 sm:grid-cols-2">
									<div class="rounded-[16px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Nome
										</p>
										<p class="mt-2 text-sm font-semibold">
											{membership.memberProfile?.firstName ?? 'Non disponibile'}
										</p>
									</div>
									<div class="rounded-[16px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Cognome
										</p>
										<p class="mt-2 text-sm font-semibold">
											{membership.memberProfile?.lastName ?? 'Non disponibile'}
										</p>
									</div>
									<div class="rounded-[16px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Data nascita
										</p>
										<p class="mt-2 text-sm font-semibold">
											{formatDate(membership.memberProfile?.birthDate, 'Non disponibile')}
										</p>
									</div>
									<div class="rounded-[16px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Email operativa
										</p>
										<p class="mt-2 text-sm font-semibold">{memberEmail}</p>
									</div>
								</CardContent>
							</Card>

							<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
								<CardHeader>
									<CardTitle>Contatti e sicurezza</CardTitle>
									<CardDescription>Informazioni utili per reception e supporto.</CardDescription>
								</CardHeader>
								<CardContent class="space-y-3">
									<div class="rounded-[16px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Contatto emergenza
										</p>
										<p class="mt-2 text-sm font-semibold">
											{membership.memberProfile?.emergencyContactName ?? 'Non disponibile'}
										</p>
										<p class="mt-1 text-sm text-muted-foreground">
											{membership.memberProfile?.emergencyContactPhoneNumber ??
												'Telefono non disponibile'}
										</p>
									</div>
									<div class="rounded-[16px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											IDs
										</p>
										<p class="mt-2 text-sm break-all text-muted-foreground">
											Membership ID: {membership.membershipId}
										</p>
										<p class="mt-1 text-sm break-all text-muted-foreground">
											User ID: {membership.userId ?? 'Non collegato'}
										</p>
									</div>
								</CardContent>
							</Card>
						</div>

						<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
							<CardHeader>
								<CardTitle>Campi personalizzati tenant</CardTitle>
								<CardDescription>
									Informazioni aggiuntive configurate dal tenant per questa scheda cliente.
								</CardDescription>
							</CardHeader>
							<CardContent>
								{#if customFields.length === 0}
									<div
										class="rounded-[16px] border border-dashed border-border bg-secondary/20 px-4 py-6 text-center text-sm text-muted-foreground"
									>
										Nessun campo personalizzato compilato per questo cliente.
									</div>
								{:else}
									<div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
										{#each customFields as field (field.fieldDefinitionId)}
											<div class="rounded-[16px] border border-border/70 p-4">
												<p
													class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
												>
													{field.label ?? field.key}
												</p>
												<p class="mt-2 text-sm font-semibold">
													{formatCustomFieldValue(field.value, field.valueType)}
												</p>
												{#if field.description}
													<p class="mt-2 text-xs text-muted-foreground">{field.description}</p>
												{/if}
											</div>
										{/each}
									</div>
								{/if}
							</CardContent>
						</Card>
					</div>
				</Tabs.Content>
			</Tabs.Root>
		</section>
	{/if}
</main>
