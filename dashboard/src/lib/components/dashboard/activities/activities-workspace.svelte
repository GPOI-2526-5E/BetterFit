<script lang="ts">
	import { page } from '$app/state';
	import { createQuery } from '@tanstack/svelte-query';
	import CalendarDaysIcon from '@lucide/svelte/icons/calendar-days';
	import Clock3Icon from '@lucide/svelte/icons/clock-3';
	import PlusCircleIcon from '@lucide/svelte/icons/plus-circle';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import SearchIcon from '@lucide/svelte/icons/search';
	import UserPlusIcon from '@lucide/svelte/icons/user-plus';
	import UsersIcon from '@lucide/svelte/icons/users';
	import type {
		GymLocationResponse,
		GymMembershipResponse,
		GymStaffAssignmentResponse
	} from '$lib/api';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Checkbox } from '$lib/components/ui/checkbox/index.js';
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
	import {
		Table,
		TableBody,
		TableCell,
		TableHead,
		TableHeader,
		TableRow
	} from '$lib/components/ui/table/index.js';
	import { Textarea } from '$lib/components/ui/textarea/index.js';
	import {
		type ActivityBookingStatus,
		type ActivitySessionStatus,
		type CreateGymActivityBookingRequest,
		type CreateGymActivitySessionRequest,
		type CreateGymActivityTemplateRequest,
		type GymActivitySession,
		createGymActivityBooking,
		createGymActivitySession,
		createGymActivityTemplate,
		fetchGymActivitiesOverview,
		fetchGymActivitySessions,
		fetchGymActivityTemplates,
		updateGymActivityTemplateActivation,
		updateGymActivitySessionStatus,
		updateGymActivityBookingStatus
	} from '$lib/data/activities-api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
	type LocationOption = { id: string; name: string };
	type TemplateOption = { id: string; label: string; locationId: string };
	type MembershipOption = {
		id: string;
		label: string;
		email: string;
		status: string;
		locationIds: string[];
	};
	type InstructorOption = {
		id: string;
		label: string;
		scopeLocationId: string | null;
	};
	const ACTIVITIES_ALL_LOCATIONS_SELECT_VALUE = '__all_locations__';
	const ACTIVITIES_NO_INSTRUCTOR_SELECT_VALUE = '__no_instructor__';

	const api = useApiClient();
	const center = useCenterSelectionStore();
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});
	const shortDate = new Intl.DateTimeFormat('it-IT', { day: '2-digit', month: 'short' });
	const timeOnly = new Intl.DateTimeFormat('it-IT', { hour: '2-digit', minute: '2-digit' });

	const toInputDate = (date = new Date()) => {
		const next = new Date(date);
		next.setSeconds(0, 0);
		next.setMinutes(0);
		next.setHours(next.getHours() + 1);

		const year = next.getFullYear();
		const month = `${next.getMonth() + 1}`.padStart(2, '0');
		const day = `${next.getDate()}`.padStart(2, '0');
		const hours = `${next.getHours()}`.padStart(2, '0');
		const minutes = `${next.getMinutes()}`.padStart(2, '0');
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

	const instructorLabel = (assignment: GymStaffAssignmentResponse) =>
		assignment.staffProfile?.displayName?.trim() ||
		assignment.userEmail?.trim() ||
		assignment.roleName?.trim() ||
		'Staff';

	const sessionStatusMeta = (
		status: ActivitySessionStatus
	): { label: string; variant: BadgeVariant } => {
		if (status === 'Completed') return { label: 'Completata', variant: 'success' };
		if (status === 'Cancelled') return { label: 'Annullata', variant: 'outline' };
		return { label: 'Programmata', variant: 'secondary' };
	};

	const bookingStatusMeta = (
		status: ActivityBookingStatus
	): { label: string; variant: BadgeVariant } => {
		if (status === 'CheckedIn') return { label: 'Check-in', variant: 'success' };
		if (status === 'Cancelled') return { label: 'Annullata', variant: 'outline' };
		if (status === 'NoShow') return { label: 'No-show', variant: 'destructive' };
		return { label: 'Prenotata', variant: 'secondary' };
	};

	const templateStatusMeta = (isActive: boolean): { label: string; variant: BadgeVariant } =>
		isActive
			? { label: 'Attivo', variant: 'success' }
			: { label: 'Disattivato', variant: 'outline' };

	const currentHash = $derived(page.url.hash || '#desk');
	const sectionNavClass = (hash: string) =>
		[
			'inline-flex items-center rounded-full border px-3 py-1.5 text-sm transition',
			currentHash === hash
				? 'border-primary/30 bg-primary/10 text-primary shadow-sm'
				: 'border-border/70 bg-background text-muted-foreground hover:border-border hover:bg-secondary/20 hover:text-foreground'
		].join(' ');

	const sessionTiming = (startsAtUtc: Date, endsAtUtc: Date) =>
		`${dateTime.format(startsAtUtc)} - ${timeOnly.format(endsAtUtc)}`;

	let locationFilter = $state(center.selectedLocationId ?? '');
	let searchTerm = $state('');
	let selectedSessionId = $state<string | null>(null);
	let feedbackMessage = $state('');
	let feedbackError = $state('');
	let templateOpen = $state(false);
	let sessionOpen = $state(false);
	let bookingOpen = $state(false);
	let templateSubmitting = $state(false);
	let templateActionPendingId = $state<string | null>(null);
	let sessionSubmitting = $state(false);
	let bookingSubmitting = $state(false);
	let sessionActionPending = $state<string | null>(null);
	let bookingActionPendingId = $state<string | null>(null);

	let templateForm = $state({
		locationId: center.selectedLocationId ?? '',
		instructorAssignmentId: '',
		name: '',
		category: 'Corso',
		description: '',
		colorHex: '#1d4ed8',
		capacity: '20',
		durationMinutes: '60',
		requiresBooking: true
	});
	let sessionForm = $state({
		templateId: '',
		startsAtLocal: toInputDate(),
		endsAtLocal: '',
		notes: ''
	});
	let bookingForm = $state({
		membershipId: '',
		notes: ''
	});

	const membershipsQuery = createQuery(() => ({
		queryKey: ['activities-memberships', center.selectedGymId],
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

	const staffQuery = createQuery(() => ({
		queryKey: ['activities-staff', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdStaffAssignmentsGet({
				gymId: center.selectedGymId!
			});
			if (!response.success) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile caricare il team.'
				);
			}

			return response.data ?? [];
		}
	}));

	const overviewQuery = createQuery(() => ({
		queryKey: ['activities-overview', center.selectedGymId, locationFilter],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymActivitiesOverview(center.selectedGymId!, {
				locationId: locationFilter || null
			})
	}));

	const templatesQuery = createQuery(() => ({
		queryKey: ['activities-templates', center.selectedGymId, locationFilter],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymActivityTemplates(center.selectedGymId!, {
				locationId: locationFilter || null
			})
	}));

	const sessionsQuery = createQuery(() => ({
		queryKey: ['activities-sessions', center.selectedGymId, locationFilter],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymActivitySessions(center.selectedGymId!, {
				locationId: locationFilter || null,
				fromUtc: new Date(new Date().setHours(0, 0, 0, 0)).toISOString()
			})
	}));

	const locationOptions = $derived.by(() => {
		const locations = (center.locations ?? []) as GymLocationResponse[];
		return locations
			.filter((location) => location.id && location.name && location.isActive !== false)
			.map((location) => ({
				id: location.id!,
				name: location.name!
			})) satisfies LocationOption[];
	});

	const templateOptions = $derived.by(() => {
		return (templatesQuery.data ?? [])
			.filter((template) => template.isActive)
			.map((template) => ({
				id: template.templateId,
				label: `${template.name} - ${template.locationName}`,
				locationId: template.locationId
			})) satisfies TemplateOption[];
	});

	const membershipOptions = $derived.by(() => {
		return (membershipsQuery.data ?? [])
			.filter((membership) => membership.membershipId)
			.map((membership) => ({
				id: membership.membershipId ?? '',
				label: membershipLabel(membership),
				email: membership.userEmail ?? membership.invitationEmail ?? '',
				status: membership.status ?? 'PendingClaim',
				locationIds: (membership.locations ?? [])
					.map((location) => location.id ?? '')
					.filter(Boolean)
			})) satisfies MembershipOption[];
	});

	const instructorOptions = $derived.by(() => {
		return (staffQuery.data ?? [])
			.filter((assignment) => assignment.assignmentId && assignment.status === 'Active')
			.map((assignment) => ({
				id: assignment.assignmentId ?? '',
				label: instructorLabel(assignment),
				scopeLocationId: assignment.scopeLocationId ?? null
			})) satisfies InstructorOption[];
	});

	const filteredInstructorOptions = $derived.by(() => {
		if (!templateForm.locationId) {
			return instructorOptions;
		}

		return instructorOptions.filter(
			(assignment) =>
				!assignment.scopeLocationId || assignment.scopeLocationId === templateForm.locationId
		);
	});

	const sessions = $derived.by(() => {
		const term = searchTerm.trim().toLowerCase();
		if (!term) {
			return sessionsQuery.data ?? [];
		}

		return (sessionsQuery.data ?? []).filter((session) => {
			const searchIndex = [
				session.title,
				session.category,
				session.locationName,
				session.instructorName,
				...session.bookings.map((booking) => booking.memberName),
				...session.bookings.map((booking) => booking.memberEmail)
			]
				.join(' ')
				.toLowerCase();

			return searchIndex.includes(term);
		});
	});

	const selectedSession = $derived.by(() => {
		if (!selectedSessionId) {
			return sessions[0] ?? null;
		}

		return (
			sessions.find((session) => session.sessionId === selectedSessionId) ?? sessions[0] ?? null
		);
	});

	const selectedTemplate = $derived.by(() => {
		if (!selectedSession) {
			return null;
		}

		return (
			(templatesQuery.data ?? []).find(
				(template) => template.templateId === selectedSession.templateId
			) ?? null
		);
	});
	const deskFocusSession = $derived.by(() => {
		const upcoming = overviewQuery.data?.upcomingSessions ?? [];
		return upcoming[0] ?? selectedSession ?? null;
	});

	const availableBookingMembers = $derived.by(() => {
		if (!selectedSession) {
			return [] satisfies MembershipOption[];
		}

		const bookedMembershipIds = new Set(
			selectedSession.bookings.map((booking) => booking.membershipId)
		);
		return membershipOptions.filter(
			(membership) =>
				!bookedMembershipIds.has(membership.id) &&
				(membership.status === 'Active' || membership.status === 'PendingClaim') &&
				membership.locationIds.includes(selectedSession.locationId)
		);
	});

	const locationFilterLabel = $derived(
		locationOptions.find((location) => location.id === locationFilter)?.name ?? 'Tutte le sedi'
	);
	const scopeLabel = $derived(locationFilterLabel);
	const templateLocationLabel = $derived(
		locationOptions.find((location) => location.id === templateForm.locationId)?.name ??
			'Seleziona una sede'
	);
	const templateInstructorLabel = $derived(
		filteredInstructorOptions.find(
			(instructor) => instructor.id === templateForm.instructorAssignmentId
		)?.label ?? 'Nessun istruttore fisso'
	);
	const sessionTemplateLabel = $derived(
		templateOptions.find((template) => template.id === sessionForm.templateId)?.label ??
			'Seleziona un modello'
	);
	const bookingMemberLabel = $derived(
		availableBookingMembers.find((membership) => membership.id === bookingForm.membershipId)
			?.label ?? 'Seleziona un membro'
	);
	const membershipsQueryError = $derived(
		membershipsQuery.error instanceof Error
			? membershipsQuery.error.message
			: membershipsQuery.error
				? 'Impossibile caricare le membership.'
				: null
	);
	const staffQueryError = $derived(
		staffQuery.error instanceof Error
			? staffQuery.error.message
			: staffQuery.error
				? 'Impossibile caricare il team.'
				: null
	);
	const overviewQueryError = $derived(
		overviewQuery.error instanceof Error
			? overviewQuery.error.message
			: overviewQuery.error
				? 'Impossibile caricare l overview attivita.'
				: null
	);
	const templatesQueryError = $derived(
		templatesQuery.error instanceof Error
			? templatesQuery.error.message
			: templatesQuery.error
				? 'Impossibile caricare i modelli attivita.'
				: null
	);
	const sessionsQueryError = $derived(
		sessionsQuery.error instanceof Error
			? sessionsQuery.error.message
			: sessionsQuery.error
				? 'Impossibile caricare il registro sessioni.'
				: null
	);
	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms ||
			(!!center.selectedGymId &&
				((!membershipsQuery.data && membershipsQuery.isPending) ||
					(!staffQuery.data && staffQuery.isPending) ||
					(!overviewQuery.data && overviewQuery.isPending) ||
					(!templatesQuery.data && templatesQuery.isPending) ||
					(!sessionsQuery.data && sessionsQuery.isPending)))
	);
	const workspaceError = $derived(
		center.gymsError ??
			membershipsQueryError ??
			staffQueryError ??
			overviewQueryError ??
			templatesQueryError ??
			sessionsQueryError ??
			null
	);

	$effect(() => {
		if (!locationFilter && center.selectedLocationId) {
			locationFilter = center.selectedLocationId;
		}
	});

	$effect(() => {
		if (!templateForm.locationId && locationOptions.length > 0) {
			templateForm = {
				...templateForm,
				locationId: locationFilter || locationOptions[0].id
			};
		}
	});

	$effect(() => {
		if (!sessionForm.templateId && templateOptions.length > 0) {
			sessionForm = {
				...sessionForm,
				templateId: templateOptions[0].id
			};
		}
	});

	$effect(() => {
		if (!bookingForm.membershipId && availableBookingMembers.length > 0) {
			bookingForm = {
				...bookingForm,
				membershipId: availableBookingMembers[0].id
			};
		}
	});

	$effect(() => {
		if (sessions.length === 0) {
			selectedSessionId = null;
			return;
		}

		if (
			!selectedSessionId ||
			!sessions.some((session) => session.sessionId === selectedSessionId)
		) {
			selectedSessionId = sessions[0].sessionId;
		}
	});

	function clearFeedback() {
		feedbackMessage = '';
		feedbackError = '';
	}

	function resetTemplateForm() {
		templateForm = {
			locationId: locationFilter || locationOptions[0]?.id || '',
			instructorAssignmentId: '',
			name: '',
			category: 'Corso',
			description: '',
			colorHex: '#1d4ed8',
			capacity: '20',
			durationMinutes: '60',
			requiresBooking: true
		};
	}

	function resetSessionForm(templateId = templateOptions[0]?.id ?? '') {
		sessionForm = {
			templateId,
			startsAtLocal: toInputDate(),
			endsAtLocal: '',
			notes: ''
		};
	}

	function resetBookingForm() {
		bookingForm = {
			membershipId: availableBookingMembers[0]?.id ?? '',
			notes: ''
		};
	}

	function bookingCandidatesForSession(session: GymActivitySession) {
		const bookedMembershipIds = new Set(session.bookings.map((booking) => booking.membershipId));
		return membershipOptions.filter(
			(membership) =>
				!bookedMembershipIds.has(membership.id) &&
				(membership.status === 'Active' || membership.status === 'PendingClaim') &&
				membership.locationIds.includes(session.locationId)
		);
	}

	async function refreshAll() {
		if (!center.selectedGymId) {
			return;
		}

		await Promise.all([
			overviewQuery.refetch(),
			templatesQuery.refetch(),
			sessionsQuery.refetch(),
			membershipsQuery.refetch(),
			staffQuery.refetch()
		]);
	}

	function openTemplateSheet() {
		clearFeedback();
		resetTemplateForm();
		templateOpen = true;
	}

	function openSessionSheet(templateId = templateOptions[0]?.id ?? '') {
		clearFeedback();
		if (templateOptions.length === 0) {
			feedbackError = 'Serve almeno un modello attivo per pianificare una sessione.';
			return;
		}
		resetSessionForm(templateId);
		sessionOpen = true;
	}

	function openBookingSheet() {
		clearFeedback();
		if (!selectedSession) {
			feedbackError = 'Seleziona una sessione prima di aggiungere una prenotazione.';
			return;
		}

		resetBookingForm();
		bookingOpen = true;
	}

	function focusSession(sessionId: string) {
		selectedSessionId = sessionId;
	}

	function openBookingForSession(sessionId: string) {
		clearFeedback();

		const session =
			sessions.find((item) => item.sessionId === sessionId) ??
			overviewQuery.data?.upcomingSessions.find((item) => item.sessionId === sessionId) ??
			null;
		if (!session) {
			feedbackError = 'Sessione non trovata nel registro corrente.';
			return;
		}

		if (session.status !== 'Scheduled') {
			feedbackError = 'Puoi aggiungere prenotazioni solo a sessioni ancora programmate.';
			return;
		}

		if (session.remainingSpots <= 0) {
			feedbackError = 'La sessione selezionata non ha piu posti disponibili.';
			return;
		}

		const candidates = bookingCandidatesForSession(session);
		if (candidates.length === 0) {
			feedbackError = 'Non ci sono membri prenotabili per questa sessione.';
			return;
		}

		selectedSessionId = session.sessionId;
		bookingForm = {
			membershipId: candidates[0].id,
			notes: ''
		};
		bookingOpen = true;
	}

	async function handleCreateTemplate(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !templateForm.locationId || !templateForm.name.trim()) {
			feedbackError = 'Compila sede e nome attivita prima di salvare il modello.';
			return;
		}

		templateSubmitting = true;
		try {
			const created = await createGymActivityTemplate(center.selectedGymId, {
				locationId: templateForm.locationId,
				instructorAssignmentId: templateForm.instructorAssignmentId || null,
				name: templateForm.name.trim(),
				category: templateForm.category.trim() || 'Corso',
				description: templateForm.description.trim() || null,
				colorHex: templateForm.colorHex.trim() || null,
				capacity: Number.parseInt(templateForm.capacity, 10) || 20,
				durationMinutes: Number.parseInt(templateForm.durationMinutes, 10) || 60,
				requiresBooking: templateForm.requiresBooking
			} satisfies CreateGymActivityTemplateRequest);

			await Promise.all([templatesQuery.refetch(), overviewQuery.refetch()]);
			sessionForm = {
				...sessionForm,
				templateId: created.templateId
			};
			templateOpen = false;
			feedbackMessage = `Modello ${created.name} creato con successo.`;
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile creare il modello attivita.';
		} finally {
			templateSubmitting = false;
		}
	}

	async function handleCreateSession(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !sessionForm.templateId || !sessionForm.startsAtLocal) {
			feedbackError = 'Seleziona modello e data della sessione.';
			return;
		}

		sessionSubmitting = true;
		try {
			const created = await createGymActivitySession(center.selectedGymId, {
				templateId: sessionForm.templateId,
				startsAtUtc: new Date(sessionForm.startsAtLocal).toISOString(),
				endsAtUtc: sessionForm.endsAtLocal ? new Date(sessionForm.endsAtLocal).toISOString() : null,
				notes: sessionForm.notes.trim() || null
			} satisfies CreateGymActivitySessionRequest);

			await Promise.all([sessionsQuery.refetch(), overviewQuery.refetch()]);
			selectedSessionId = created.sessionId;
			sessionOpen = false;
			feedbackMessage = `Sessione ${created.title} pianificata correttamente.`;
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile pianificare la sessione.';
		} finally {
			sessionSubmitting = false;
		}
	}

	async function handleCreateBooking(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !selectedSession || !bookingForm.membershipId) {
			feedbackError = 'Seleziona una sessione e un membro prima di aggiungere la prenotazione.';
			return;
		}

		bookingSubmitting = true;
		try {
			const updated = await createGymActivityBooking(
				center.selectedGymId,
				selectedSession.sessionId,
				{
					membershipId: bookingForm.membershipId,
					notes: bookingForm.notes.trim() || null
				} satisfies CreateGymActivityBookingRequest
			);

			await Promise.all([sessionsQuery.refetch(), overviewQuery.refetch()]);
			selectedSessionId = updated.sessionId;
			bookingOpen = false;
			feedbackMessage = 'Prenotazione registrata con successo.';
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile registrare la prenotazione.';
		} finally {
			bookingSubmitting = false;
		}
	}

	async function handleTemplateActivation(templateId: string, isActive: boolean, label: string) {
		if (!center.selectedGymId) {
			return;
		}

		clearFeedback();
		templateActionPendingId = templateId;

		try {
			await updateGymActivityTemplateActivation(center.selectedGymId, templateId, {
				isActive
			});
			await Promise.all([templatesQuery.refetch(), overviewQuery.refetch()]);
			if (!isActive && sessionForm.templateId === templateId) {
				sessionForm = {
					...sessionForm,
					templateId: ''
				};
			}
			feedbackMessage = label;
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile aggiornare il modello.';
		} finally {
			templateActionPendingId = null;
		}
	}

	async function handleSessionStatus(status: ActivitySessionStatus, label: string) {
		if (!center.selectedGymId || !selectedSession) {
			return;
		}

		clearFeedback();
		sessionActionPending = `${selectedSession.sessionId}-${status}`;

		try {
			const updated = await updateGymActivitySessionStatus(
				center.selectedGymId,
				selectedSession.sessionId,
				{
					status
				}
			);

			await Promise.all([sessionsQuery.refetch(), overviewQuery.refetch()]);
			selectedSessionId = updated.sessionId;
			feedbackMessage = label;
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile aggiornare lo stato della sessione.';
		} finally {
			sessionActionPending = null;
		}
	}

	async function handleBookingStatus(
		bookingId: string,
		status: ActivityBookingStatus,
		label: string
	) {
		if (!center.selectedGymId || !selectedSession) {
			return;
		}

		clearFeedback();
		bookingActionPendingId = bookingId;
		try {
			const updated = await updateGymActivityBookingStatus(center.selectedGymId, bookingId, {
				status,
				notes: null
			});

			await Promise.all([sessionsQuery.refetch(), overviewQuery.refetch()]);
			selectedSessionId = updated.sessionId;
			feedbackMessage = label;
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error
					? error.message
					: 'Impossibile aggiornare lo stato della prenotazione.';
		} finally {
			bookingActionPendingId = null;
		}
	}
</script>

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	<section class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
		<div class="space-y-2">
			<div class="flex flex-wrap items-center gap-2">
				<Badge variant="secondary" class="rounded-full px-3 py-1">Palinsesto attivita</Badge>
				{#if center.selectedGym}
					<Badge variant="outline" class="rounded-full px-3 py-1">{center.selectedGym.name}</Badge>
				{/if}
				{#if locationFilter}
					<Badge variant="outline" class="rounded-full px-3 py-1">
						{locationOptions.find((location) => location.id === locationFilter)?.name ??
							'Sede selezionata'}
					</Badge>
				{/if}
			</div>
			<div>
				<h2 class="text-2xl font-semibold tracking-tight">Attivita, corsi e presenze</h2>
				<p class="text-sm text-muted-foreground">
					Gestisci modelli attivita, sessioni pianificate, prenotazioni e stato presenza dei membri.
				</p>
			</div>
		</div>
		<div class="flex flex-wrap items-center gap-2">
			{#if overviewQuery.data}
				<p class="text-xs text-muted-foreground">
					Ultimo sync {dateTime.format(overviewQuery.data.generatedAtUtc)}
				</p>
			{/if}
			<Select.Root
				type="single"
				value={locationFilter || ACTIVITIES_ALL_LOCATIONS_SELECT_VALUE}
				onValueChange={(value) =>
					(locationFilter = value === ACTIVITIES_ALL_LOCATIONS_SELECT_VALUE ? '' : value)}
			>
				<Select.Trigger class="min-w-[180px]" disabled={!hasSelectedGym || workspaceLoading}>
					<span data-slot="select-value">{locationFilterLabel}</span>
				</Select.Trigger>
				<Select.Content>
					<Select.Item value={ACTIVITIES_ALL_LOCATIONS_SELECT_VALUE} label="Tutte le sedi">
						Tutte le sedi
					</Select.Item>
					{#each locationOptions as location (location.id)}
						<Select.Item value={location.id} label={location.name}>{location.name}</Select.Item>
					{/each}
				</Select.Content>
			</Select.Root>
			<Button
				variant="outline"
				size="sm"
				onclick={refreshAll}
				disabled={!hasSelectedGym || workspaceLoading}
			>
				<RefreshCwIcon class="size-4" />
				Aggiorna
			</Button>
			<Button
				variant="outline"
				size="sm"
				onclick={openTemplateSheet}
				disabled={!hasSelectedGym || workspaceLoading}
			>
				<PlusCircleIcon class="size-4" />
				Nuovo modello
			</Button>
			<Button
				size="sm"
				onclick={() => openSessionSheet()}
				disabled={!hasSelectedGym || workspaceLoading || templateOptions.length === 0}
			>
				<CalendarDaysIcon class="size-4" />
				Nuova sessione
			</Button>
		</div>
	</section>

	<nav
		class="flex flex-wrap gap-2 rounded-[20px] border border-border/70 bg-muted/20 p-2"
		aria-label="Sezioni attivita"
	>
		<a href="#desk" class={sectionNavClass('#desk')}>
			Desk operativo
		</a>
		<a href="#templates" class={sectionNavClass('#templates')}>
			Catalogo
		</a>
		<a href="#sessions" class={sectionNavClass('#sessions')}>
			Agenda
		</a>
		<a href="#registry" class={sectionNavClass('#registry')}>
			Registro sessioni
		</a>
		<a href="#bookings" class={sectionNavClass('#bookings')}>
			Prenotazioni e presenze
		</a>
	</nav>

	{#if feedbackMessage}
		<section
			class="rounded-[20px] border border-[#bbf7d0] bg-[#f0fdf4] px-4 py-3 text-sm text-[#166534]"
		>
			{feedbackMessage}
		</section>
	{/if}
	{#if feedbackError}
		<section
			class="rounded-[20px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]"
		>
			{feedbackError}
		</section>
	{/if}

	{#if workspaceLoading}
		<Card class="border-dashed border-border/70 bg-muted/25">
			<CardHeader>
				<CardTitle>Caricamento area attivita</CardTitle>
				<CardDescription>
					Sto recuperando membership, team, overview, modelli e sessioni prima di mostrarti il
					palinsesto reale del tenant.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-dashed border-[#fecaca] bg-[#fff7f7]">
			<CardHeader>
				<CardTitle>Impossibile caricare l area attivita</CardTitle>
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
					Scegli prima la palestra dal selettore in alto a sinistra per vedere palinsesto, sessioni,
					prenotazioni e presenze del tenant corretto.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
		<Card class="border-border/70">
			<CardHeader class="pb-3">
				<CardTitle>Perimetro attivita</CardTitle>
				<CardDescription>
					Stai leggendo il palinsesto di <span class="font-medium text-foreground"
						>{scopeLabel}</span
					>.
				</CardDescription>
			</CardHeader>
			<CardContent class="grid gap-3 text-sm text-muted-foreground md:grid-cols-3">
				<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
					<div class="text-xs font-medium tracking-[0.18em] uppercase">Modelli attivi</div>
					<div class="mt-2 text-lg font-semibold text-foreground">
						{templatesQuery.data?.filter((template) => template.isActive).length ?? 0}
					</div>
				</div>
				<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
					<div class="text-xs font-medium tracking-[0.18em] uppercase">Sessioni visibili</div>
					<div class="mt-2 text-lg font-semibold text-foreground">{sessions.length}</div>
				</div>
				<div class="rounded-[18px] border border-border/70 bg-muted/20 px-4 py-3">
					<div class="text-xs font-medium tracking-[0.18em] uppercase">Membri prenotabili</div>
					<div class="mt-2 text-lg font-semibold text-foreground">
						{availableBookingMembers.length}
					</div>
				</div>
			</CardContent>
		</Card>

		{#if overviewQuery.data}
			<section class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
				<Card>
					<CardHeader class="pb-2">
						<CardDescription>Sessioni prossimi 7 giorni</CardDescription>
						<CardTitle class="text-3xl">{overviewQuery.data.sessionsNext7DaysCount}</CardTitle>
					</CardHeader>
					<CardContent class="text-sm text-muted-foreground">
						Sessioni pianificate per il calendario operativo.
					</CardContent>
				</Card>
				<Card>
					<CardHeader class="pb-2">
						<CardDescription>Prenotazioni prossimi 7 giorni</CardDescription>
						<CardTitle class="text-3xl">{overviewQuery.data.bookingsNext7DaysCount}</CardTitle>
					</CardHeader>
					<CardContent class="text-sm text-muted-foreground">
						Prenotazioni confermate sulle sessioni imminenti.
					</CardContent>
				</Card>
				<Card>
					<CardHeader class="pb-2">
						<CardDescription>Check-in di oggi</CardDescription>
						<CardTitle class="text-3xl">{overviewQuery.data.checkedInTodayCount}</CardTitle>
					</CardHeader>
					<CardContent class="text-sm text-muted-foreground">
						Presenze registrate sulle attivita del giorno.
					</CardContent>
				</Card>
				<Card>
					<CardHeader class="pb-2">
						<CardDescription>No-show ultimi 30 giorni</CardDescription>
						<CardTitle class="text-3xl">{overviewQuery.data.noShowLast30DaysCount}</CardTitle>
					</CardHeader>
					<CardContent class="text-sm text-muted-foreground">
						Membri che non si sono presentati alle prenotazioni.
					</CardContent>
				</Card>
			</section>
		{/if}

		{#if deskFocusSession}
			<section id="desk" class="scroll-mt-24">
				<Card class="border-border/70">
					<CardHeader>
						<div class="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
							<div>
								<CardTitle>Desk operativo</CardTitle>
								<CardDescription>
									La prossima sessione da presidiare con prenotazioni, presenza e stato aula.
								</CardDescription>
							</div>
							<div class="flex flex-wrap gap-2">
								<Button
									variant="outline"
									size="sm"
									onclick={() => focusSession(deskFocusSession.sessionId)}
								>
									Apri dettaglio
								</Button>
								<Button
									size="sm"
									onclick={() => openBookingForSession(deskFocusSession.sessionId)}
									disabled={deskFocusSession.status !== 'Scheduled' ||
										deskFocusSession.remainingSpots <= 0}
								>
									<UserPlusIcon class="size-4" />
									Aggiungi prenotazione
								</Button>
							</div>
						</div>
					</CardHeader>
					<CardContent class="grid gap-4 lg:grid-cols-[1.1fr_0.9fr]">
						<div class="rounded-[18px] border border-border/70 p-4">
							<div class="flex flex-wrap items-center gap-2">
								<p class="font-semibold">{deskFocusSession.title}</p>
								<Badge variant={sessionStatusMeta(deskFocusSession.status).variant}>
									{sessionStatusMeta(deskFocusSession.status).label}
								</Badge>
							</div>
							<p class="mt-2 text-sm text-muted-foreground">
								{deskFocusSession.locationName} - {deskFocusSession.instructorName}
							</p>
							<p class="mt-1 text-sm text-muted-foreground">
								{sessionTiming(deskFocusSession.startsAtUtc, deskFocusSession.endsAtUtc)}
							</p>
							{#if deskFocusSession.notes}
								<p class="mt-3 text-sm text-muted-foreground">{deskFocusSession.notes}</p>
							{/if}
						</div>
						<div class="grid gap-3 sm:grid-cols-3 lg:grid-cols-1 xl:grid-cols-3">
							<div class="rounded-[18px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Prenotati</p>
								<p class="mt-2 text-lg font-semibold">
									{deskFocusSession.activeBookingsCount}/{deskFocusSession.capacity}
								</p>
							</div>
							<div class="rounded-[18px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Check-in</p>
								<p class="mt-2 text-lg font-semibold">{deskFocusSession.checkedInCount}</p>
							</div>
							<div class="rounded-[18px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
									Posti liberi
								</p>
								<p class="mt-2 text-lg font-semibold">{deskFocusSession.remainingSpots}</p>
							</div>
						</div>
					</CardContent>
				</Card>
			</section>
		{/if}

		<section class="grid gap-4 xl:grid-cols-[0.95fr_1.05fr]">
			<Card id="templates" class="scroll-mt-24">
				<CardHeader class="gap-4">
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>Catalogo attivita</CardTitle>
							<CardDescription>Modelli pronti per la pianificazione delle sessioni.</CardDescription
							>
						</div>
						<Badge variant="outline">{templatesQuery.data?.length ?? 0} modelli</Badge>
					</div>
				</CardHeader>
				<CardContent class="space-y-3">
					{#if templatesQuery.isPending}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Carico i modelli attivita...</p>
						</div>
					{:else if (templatesQuery.data ?? []).length === 0}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessun modello disponibile</p>
							<p class="mt-2 text-sm text-muted-foreground">
								Crea il primo modello per iniziare a pianificare sessioni reali.
							</p>
						</div>
					{:else}
						{#each templatesQuery.data ?? [] as template (template.templateId)}
							<div class="rounded-[18px] border border-border/70 p-4">
								<div class="flex items-start justify-between gap-3">
									<div class="space-y-1">
										<div class="flex flex-wrap items-center gap-2">
											<span
												class="inline-flex size-3 rounded-full border border-black/10"
												style={`background:${template.colorHex ?? '#1d4ed8'}`}
											></span>
											<p class="font-semibold">{template.name}</p>
											<Badge variant="outline">{template.category}</Badge>
											<Badge variant={templateStatusMeta(template.isActive).variant}>
												{templateStatusMeta(template.isActive).label}
											</Badge>
										</div>
										<p class="text-sm text-muted-foreground">
											{template.locationName} - {template.instructorName}
										</p>
									</div>
									<div class="flex flex-wrap gap-2">
										<Button
											variant="outline"
											size="sm"
											onclick={() => openSessionSheet(template.templateId)}
											disabled={!template.isActive}
										>
											<CalendarDaysIcon class="size-4" />
											Pianifica
										</Button>
										<Button
											variant="outline"
											size="sm"
											onclick={() =>
												handleTemplateActivation(
													template.templateId,
													!template.isActive,
													template.isActive
														? `Modello ${template.name} disattivato.`
														: `Modello ${template.name} riattivato.`
												)}
											disabled={templateActionPendingId === template.templateId}
										>
											{template.isActive ? 'Disattiva' : 'Riattiva'}
										</Button>
									</div>
								</div>
								<div class="mt-3 grid gap-3 text-sm text-muted-foreground sm:grid-cols-3">
									<p>Durata {template.durationMinutes} min</p>
									<p>Capacita {template.capacity} posti</p>
									<p>{template.requiresBooking ? 'Prenotazione richiesta' : 'Accesso libero'}</p>
								</div>
								{#if template.description}
									<p class="mt-3 text-sm text-muted-foreground">{template.description}</p>
								{/if}
							</div>
						{/each}
					{/if}
				</CardContent>
			</Card>

			<Card id="sessions" class="scroll-mt-24">
				<CardHeader class="gap-4">
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>Agenda imminente</CardTitle>
							<CardDescription>
								Le prossime sessioni in arrivo da tenere sotto controllo.
							</CardDescription>
						</div>
						<Badge variant="outline"
							>{overviewQuery.data?.upcomingSessions.length ?? 0} sessioni</Badge
						>
					</div>
				</CardHeader>
				<CardContent class="space-y-3">
					{#if overviewQuery.isPending}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Carico le prossime sessioni...</p>
						</div>
					{:else if (overviewQuery.data?.upcomingSessions ?? []).length === 0}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessuna sessione imminente</p>
							<p class="mt-2 text-sm text-muted-foreground">
								Crea una sessione dal catalogo o dal calendario operativo.
							</p>
						</div>
					{:else}
						{#each overviewQuery.data?.upcomingSessions ?? [] as session (session.sessionId)}
							<button
								type="button"
								class={`w-full rounded-[18px] border border-border/70 p-4 text-left transition hover:border-border hover:bg-secondary/20 ${selectedSession?.sessionId === session.sessionId ? 'bg-[#eef4ff]' : ''}`}
								onclick={() => (selectedSessionId = session.sessionId)}
							>
								<div class="flex items-start justify-between gap-3">
									<div>
										<p class="font-semibold">{session.title}</p>
										<p class="mt-1 text-sm text-muted-foreground">
											{session.locationName} - {session.instructorName}
										</p>
									</div>
									<Badge variant={sessionStatusMeta(session.status).variant}>
										{sessionStatusMeta(session.status).label}
									</Badge>
								</div>
								<div class="mt-3 flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
									<span class="inline-flex items-center gap-1">
										<Clock3Icon class="size-4" />
										{sessionTiming(session.startsAtUtc, session.endsAtUtc)}
									</span>
									<span class="inline-flex items-center gap-1">
										<UsersIcon class="size-4" />
										{session.activeBookingsCount}/{session.capacity} prenotati
									</span>
								</div>
							</button>
						{/each}
					{/if}
				</CardContent>
			</Card>
		</section>

		<section class="grid gap-4 xl:grid-cols-[1.05fr_0.95fr]">
			<Card id="registry" class="scroll-mt-24">
				<CardHeader class="gap-4">
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>Registro sessioni</CardTitle>
							<CardDescription>
								Tabella operativa delle sessioni con ricerca su corso, istruttore e membri.
							</CardDescription>
						</div>
						<Badge variant="outline">{sessions.length} sessioni</Badge>
					</div>
					<div class="relative">
						<SearchIcon
							class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
						/>
						<Input
							class="pl-9"
							placeholder="Cerca per titolo, istruttore, sede o membro"
							bind:value={searchTerm}
						/>
					</div>
				</CardHeader>
				<CardContent>
					{#if sessionsQuery.isPending}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Carico il registro sessioni...</p>
						</div>
					{:else if sessions.length === 0}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessuna sessione trovata</p>
							<p class="mt-2 text-sm text-muted-foreground">
								Modifica i filtri o crea una nuova sessione dal calendario.
							</p>
						</div>
					{:else}
						<div class="overflow-hidden rounded-[20px] border border-border/70">
							<Table>
								<TableHeader>
									<TableRow class="bg-secondary/30 hover:bg-secondary/30">
										<TableHead>Sessione</TableHead>
										<TableHead>Istruttore</TableHead>
										<TableHead class="hidden md:table-cell">Prenotazioni</TableHead>
										<TableHead>Stato</TableHead>
									</TableRow>
								</TableHeader>
								<TableBody>
									{#each sessions as session (session.sessionId)}
										<TableRow
											class={`cursor-pointer hover:bg-secondary/35 ${selectedSession?.sessionId === session.sessionId ? 'bg-[#eef4ff]' : ''}`}
											onclick={() => (selectedSessionId = session.sessionId)}
										>
											<TableCell>
												<div>
													<p class="font-semibold">{session.title}</p>
													<p class="text-sm text-muted-foreground">
														{shortDate.format(session.startsAtUtc)} - {timeOnly.format(
															session.startsAtUtc
														)} - {session.locationName}
													</p>
												</div>
											</TableCell>
											<TableCell>
												<div>
													<p class="font-medium">{session.instructorName}</p>
													<p class="text-sm text-muted-foreground">{session.category}</p>
												</div>
											</TableCell>
											<TableCell class="hidden md:table-cell">
												<p class="font-medium">{session.activeBookingsCount}/{session.capacity}</p>
												<p class="text-sm text-muted-foreground">
													Check-in {session.checkedInCount}
												</p>
											</TableCell>
											<TableCell>
												<Badge variant={sessionStatusMeta(session.status).variant}>
													{sessionStatusMeta(session.status).label}
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

			<Card id="bookings" class="scroll-mt-24">
				<CardHeader>
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>{selectedSession ? selectedSession.title : 'Dettaglio sessione'}</CardTitle
							>
							<CardDescription>
								{#if selectedSession}
									{selectedSession.locationName} - {sessionTiming(
										selectedSession.startsAtUtc,
										selectedSession.endsAtUtc
									)}
								{:else}
									Seleziona una sessione dal registro per gestire prenotazioni e presenze.
								{/if}
							</CardDescription>
						</div>
						{#if selectedSession}
							<Button
								size="sm"
								onclick={openBookingSheet}
								disabled={selectedSession.status !== 'Scheduled' ||
									availableBookingMembers.length === 0 ||
									selectedSession.remainingSpots <= 0}
							>
								<UserPlusIcon class="size-4" />
								Nuova prenotazione
							</Button>
						{/if}
					</div>
				</CardHeader>
				<CardContent>
					{#if selectedSession}
						<div class="space-y-4">
							<div class="grid gap-3 sm:grid-cols-3">
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Posti</p>
									<p class="mt-2 text-lg font-semibold">
										{selectedSession.activeBookingsCount}/{selectedSession.capacity}
									</p>
									<p class="mt-1 text-sm text-muted-foreground">
										{selectedSession.remainingSpots} disponibili
									</p>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Presenze</p>
									<p class="mt-2 text-lg font-semibold">{selectedSession.checkedInCount}</p>
									<p class="mt-1 text-sm text-muted-foreground">Check-in registrati</p>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Formato</p>
									<p class="mt-2 text-lg font-semibold">
										{selectedTemplate?.durationMinutes ?? 0} min
									</p>
									<p class="mt-1 text-sm text-muted-foreground">
										{selectedTemplate?.requiresBooking
											? 'Prenotazione obbligatoria'
											: 'Accesso libero'}
									</p>
								</div>
							</div>

							<div class="rounded-[18px] border border-border/70 p-4">
								<div class="flex flex-wrap items-center gap-2">
									<Badge variant="outline">{selectedSession.category}</Badge>
									<Badge variant={sessionStatusMeta(selectedSession.status).variant}>
										{sessionStatusMeta(selectedSession.status).label}
									</Badge>
								</div>
								<p class="mt-3 text-sm text-muted-foreground">
									Istruttore: {selectedSession.instructorName}
								</p>
								{#if selectedSession.notes}
									<p class="mt-2 text-sm text-muted-foreground">{selectedSession.notes}</p>
								{/if}
							</div>

							<div class="rounded-[18px] border border-border/70 p-4">
								<div class="flex flex-wrap items-center justify-between gap-3">
									<div>
										<p class="font-semibold">Azioni sessione</p>
										<p class="mt-1 text-sm text-muted-foreground">
											Completa, annulla o riapri la sessione mantenendo coerente il registro
											operativo.
										</p>
									</div>
									<div class="flex flex-wrap gap-2">
										{#if selectedSession.status === 'Scheduled'}
											<Button
												size="sm"
												onclick={() =>
													handleSessionStatus(
														'Completed',
														`Sessione ${selectedSession.title} completata.`
													)}
												disabled={sessionActionPending === `${selectedSession.sessionId}-Completed`}
											>
												Completa sessione
											</Button>
											<Button
												size="sm"
												variant="outline"
												onclick={() =>
													handleSessionStatus(
														'Cancelled',
														`Sessione ${selectedSession.title} annullata.`
													)}
												disabled={sessionActionPending === `${selectedSession.sessionId}-Cancelled`}
											>
												Annulla sessione
											</Button>
										{:else}
											<Button
												size="sm"
												variant="outline"
												onclick={() =>
													handleSessionStatus(
														'Scheduled',
														`Sessione ${selectedSession.title} ripristinata.`
													)}
												disabled={sessionActionPending === `${selectedSession.sessionId}-Scheduled`}
											>
												Riprogramma
											</Button>
										{/if}
									</div>
								</div>
							</div>

							<div class="rounded-[18px] border border-border/70 p-4">
								<div class="mb-3 flex items-center justify-between gap-3">
									<p class="font-semibold">Prenotazioni</p>
									<Badge variant="outline">{selectedSession.bookings.length} movimenti</Badge>
								</div>
								{#if selectedSession.bookings.length === 0}
									<div
										class="rounded-[14px] border border-dashed border-border bg-secondary/15 px-4 py-6 text-center text-sm text-muted-foreground"
									>
										Nessuna prenotazione registrata su questa sessione.
									</div>
								{:else}
									<div class="space-y-3">
										{#each selectedSession.bookings as booking (booking.bookingId)}
											<div class="rounded-[14px] border border-border/60 bg-secondary/15 p-3">
												<div class="flex items-start justify-between gap-3">
													<div>
														<p class="font-medium">{booking.memberName}</p>
														<p class="text-sm text-muted-foreground">{booking.memberEmail}</p>
													</div>
													<Badge variant={bookingStatusMeta(booking.status).variant}>
														{bookingStatusMeta(booking.status).label}
													</Badge>
												</div>
												<p class="mt-2 text-xs text-muted-foreground">
													Prenotato {dateTime.format(booking.bookedAtUtc)}
												</p>
												{#if booking.notes}
													<p class="mt-2 text-xs text-muted-foreground">{booking.notes}</p>
												{/if}
												<div class="mt-3 flex flex-wrap gap-2">
													{#if booking.status === 'Booked'}
														<Button
															size="sm"
															variant="outline"
															disabled={bookingActionPendingId === booking.bookingId ||
																selectedSession.status !== 'Scheduled'}
															onclick={() =>
																handleBookingStatus(
																	booking.bookingId,
																	'CheckedIn',
																	`Check-in registrato per ${booking.memberName}.`
																)}
														>
															Check-in
														</Button>
														<Button
															size="sm"
															variant="outline"
															disabled={bookingActionPendingId === booking.bookingId}
															onclick={() =>
																handleBookingStatus(
																	booking.bookingId,
																	'NoShow',
																	`No-show registrato per ${booking.memberName}.`
																)}
														>
															No-show
														</Button>
														<Button
															size="sm"
															variant="outline"
															disabled={bookingActionPendingId === booking.bookingId}
															onclick={() =>
																handleBookingStatus(
																	booking.bookingId,
																	'Cancelled',
																	`Prenotazione annullata per ${booking.memberName}.`
																)}
														>
															Annulla
														</Button>
													{:else if booking.status === 'Cancelled' || booking.status === 'NoShow'}
														<Button
															size="sm"
															variant="outline"
															disabled={bookingActionPendingId === booking.bookingId ||
																selectedSession.status !== 'Scheduled'}
															onclick={() =>
																handleBookingStatus(
																	booking.bookingId,
																	'Booked',
																	`Prenotazione ripristinata per ${booking.memberName}.`
																)}
														>
															Ripristina
														</Button>
													{:else}
														<span class="text-xs text-muted-foreground">
															Presenza gia consolidata.
														</span>
													{/if}
												</div>
											</div>
										{/each}
									</div>
								{/if}
							</div>
						</div>
					{:else}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessuna sessione selezionata</p>
						</div>
					{/if}
				</CardContent>
			</Card>
		</section>
	{/if}
</main>

<Sheet.Root bind:open={templateOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-2xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Nuovo modello attivita</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				Crea un modello reale riutilizzabile per la pianificazione delle sessioni.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateTemplate}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Sede</span>
						<Select.Root type="single" bind:value={templateForm.locationId}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{templateLocationLabel}</span>
							</Select.Trigger>
							<Select.Content>
								{#each locationOptions as location (location.id)}
									<Select.Item value={location.id} label={location.name}
										>{location.name}</Select.Item
									>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Istruttore di riferimento</span>
						<Select.Root
							type="single"
							value={templateForm.instructorAssignmentId || ACTIVITIES_NO_INSTRUCTOR_SELECT_VALUE}
							onValueChange={(value) =>
								(templateForm.instructorAssignmentId =
									value === ACTIVITIES_NO_INSTRUCTOR_SELECT_VALUE ? '' : value)}
						>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{templateInstructorLabel}</span>
							</Select.Trigger>
							<Select.Content>
								<Select.Item
									value={ACTIVITIES_NO_INSTRUCTOR_SELECT_VALUE}
									label="Nessun istruttore fisso"
								>
									Nessun istruttore fisso
								</Select.Item>
								{#each filteredInstructorOptions as instructor (instructor.id)}
									<Select.Item value={instructor.id} label={instructor.label}>
										{instructor.label}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Nome attivita</span>
						<Input
							bind:value={templateForm.name}
							placeholder="Es. HIIT serale, Yoga base, Mobility"
						/>
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Categoria</span>
						<Input bind:value={templateForm.category} placeholder="Corso, Small group, PT..." />
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Capacita</span>
						<Input type="number" min="1" max="500" bind:value={templateForm.capacity} />
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Durata (minuti)</span>
						<Input type="number" min="15" max="480" bind:value={templateForm.durationMinutes} />
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Colore calendario</span>
						<Input bind:value={templateForm.colorHex} placeholder="#1d4ed8" />
					</label>

					<label class="flex items-center gap-3 rounded-[16px] border border-border/70 px-4 py-3">
						<Checkbox bind:checked={templateForm.requiresBooking} />
						<div>
							<p class="text-sm font-medium">Richiede prenotazione</p>
							<p class="text-xs text-muted-foreground">
								Se attivo, la sessione viene gestita con posti e prenotazioni.
							</p>
						</div>
					</label>
				</div>

				<label class="space-y-2">
					<span class="text-sm font-medium">Descrizione operativa</span>
					<Textarea
						rows={4}
						bind:value={templateForm.description}
						placeholder="Obiettivo del corso, attrezzatura richiesta, note per il desk..."
					/>
				</label>
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (templateOpen = false)}>
					Annulla
				</Button>
				<Button type="submit" disabled={templateSubmitting}>
					{#if templateSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<PlusCircleIcon class="size-4" />
						Salva modello
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={sessionOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Nuova sessione</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				Pianifica una sessione reale partendo da un modello attivita.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateSession}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<label class="space-y-2">
					<span class="text-sm font-medium">Modello</span>
					<Select.Root type="single" bind:value={sessionForm.templateId}>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{sessionTemplateLabel}</span>
						</Select.Trigger>
						<Select.Content>
							{#each templateOptions as template (template.id)}
								<Select.Item value={template.id} label={template.label}>
									{template.label}
								</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
				</label>

				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Inizio sessione</span>
						<input
							class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
							type="datetime-local"
							bind:value={sessionForm.startsAtLocal}
						/>
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Fine sessione (opzionale)</span>
						<input
							class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
							type="datetime-local"
							bind:value={sessionForm.endsAtLocal}
						/>
					</label>
				</div>

				<label class="space-y-2">
					<span class="text-sm font-medium">Note operative</span>
					<Textarea
						rows={4}
						bind:value={sessionForm.notes}
						placeholder="Es. spostata in sala 2, porta elastici, test nuova coach..."
					/>
				</label>
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (sessionOpen = false)}>
					Annulla
				</Button>
				<Button type="submit" disabled={sessionSubmitting}>
					{#if sessionSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<CalendarDaysIcon class="size-4" />
						Crea sessione
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={bookingOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Nuova prenotazione</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				{#if selectedSession}
					{selectedSession.title} - {shortDate.format(selectedSession.startsAtUtc)} - {timeOnly.format(
						selectedSession.startsAtUtc
					)}
				{:else}
					Seleziona una sessione dal registro.
				{/if}
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateBooking}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<label class="space-y-2">
					<span class="text-sm font-medium">Membro</span>
					<Select.Root type="single" bind:value={bookingForm.membershipId}>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{bookingMemberLabel}</span>
						</Select.Trigger>
						<Select.Content>
							{#each availableBookingMembers as membership (membership.id)}
								<Select.Item
									value={membership.id}
									label={`${membership.label} - ${membership.email}`}
								>
									{membership.label} - {membership.email}
								</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
				</label>

				{#if availableBookingMembers.length === 0}
					<div
						class="rounded-[18px] border border-dashed border-border bg-secondary/15 px-4 py-6 text-sm text-muted-foreground"
					>
						Non ci sono membri prenotabili per questa sessione: controlla sede, stato membership o
						disponibilita posti.
					</div>
				{/if}

				<label class="space-y-2">
					<span class="text-sm font-medium">Note desk</span>
					<Textarea
						rows={4}
						bind:value={bookingForm.notes}
						placeholder="Es. prenotazione telefonica, inserita dal desk, lista attesa..."
					/>
				</label>
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (bookingOpen = false)}>
					Annulla
				</Button>
				<Button type="submit" disabled={bookingSubmitting || availableBookingMembers.length === 0}>
					{#if bookingSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<UserPlusIcon class="size-4" />
						Salva prenotazione
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>
