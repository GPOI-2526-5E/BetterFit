<script lang="ts">
	import { createQuery } from '@tanstack/svelte-query';
	import BellRingIcon from '@lucide/svelte/icons/bell-ring';
	import CalendarClockIcon from '@lucide/svelte/icons/calendar-clock';
	import CircleAlertIcon from '@lucide/svelte/icons/circle-alert';
	import Clock3Icon from '@lucide/svelte/icons/clock-3';
	import MailPlusIcon from '@lucide/svelte/icons/mail-plus';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import SearchIcon from '@lucide/svelte/icons/search';
	import UserRoundPlusIcon from '@lucide/svelte/icons/user-round-plus';
	import UsersIcon from '@lucide/svelte/icons/users';
	import type {
		AssignUserToGymRequest,
		GymMembershipResponse,
		GymMembershipSource,
		GymMembershipStatus
	} from '$lib/api';
	import { GymMembershipSource as GymMembershipSourceValue } from '$lib/api';
	import { GymMembershipStatus as GymMembershipStatusValue } from '$lib/api';
	import * as Avatar from '$lib/components/ui/avatar/index.js';
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
	import * as Tabs from '$lib/components/ui/tabs/index.js';
	import {
		fetchGymCustomFields,
		type GymCustomFieldDefinition
	} from '$lib/data/custom-fields-api.js';
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

	type ClientsWorkspaceMode = 'all' | 'new' | 'expiring';
	type StatusFilter = 'all' | GymMembershipStatus;
	type SourceFilter = 'all' | GymMembershipSource;
	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';

	type ClientViewModel = {
		id: string;
		record: GymMembershipResponse;
		name: string;
		email: string;
		avatar: string;
		initials: string;
		statusLabel: string;
		statusVariant: BadgeVariant;
		sourceLabel: string;
		locationLabel: string;
		locationIds: string[];
		joinedLabel: string;
		updatedLabel: string;
		expiryLabel: string;
		expiringSoon: boolean;
		daysToExpiry: number | null;
		searchBlob: string;
		notesPreview: string;
		fullNamePieces: {
			firstName: string | null;
			lastName: string | null;
		};
	};

	type MembershipFormState = {
		email: string;
		firstName: string;
		lastName: string;
		taxCode: string;
		notes: string;
		primaryLocationId: string;
		customFieldValues: Record<string, string>;
	};

	type MembershipEditFormState = {
		email: string;
		firstName: string;
		lastName: string;
		taxCode: string;
		notes: string;
		status: GymMembershipStatus;
		primaryLocationId: string;
		enabledLocationIds: string[];
		customFieldValues: Record<string, string>;
	};
	const CLIENTS_ALL_TENANT_LOCATIONS_SELECT_VALUE = '__all_tenant_locations__';
	const CLIENTS_EMPTY_CUSTOM_FIELD_SELECT_VALUE = '__empty_custom_field__';

	let {
		mode = 'all',
		autoOpenCreateSheet = false
	}: {
		mode?: ClientsWorkspaceMode;
		autoOpenCreateSheet?: boolean;
	} = $props();

	const api = useApiClient();
	const center = useCenterSelectionStore();

	const dateFormatter = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		year: 'numeric'
	});
	const dateTimeFormatter = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});

	let searchTerm = $state('');
	let statusFilter = $state<StatusFilter>('all');
	let sourceFilter = $state<SourceFilter>('all');
	let locationFilter = $state('all');
	let selectedClientId = $state<string | null>(null);
	let detailTab = $state('overview');
	let createOpen = $state(false);
	let editOpen = $state(false);
	let hasAutoOpenedCreateSheet = $state(false);
	let createSubmitting = $state(false);
	let editSubmitting = $state(false);
	let createError = $state('');
	let editError = $state('');
	let createSuccess = $state('');
	let formState = $state<MembershipFormState>({
		email: '',
		firstName: '',
		lastName: '',
		taxCode: '',
		notes: '',
		primaryLocationId: center.selectedLocationId ?? '',
		customFieldValues: {}
	});
	let editFormState = $state<MembershipEditFormState>({
		email: '',
		firstName: '',
		lastName: '',
		taxCode: '',
		notes: '',
		status: GymMembershipStatusValue.Active,
		primaryLocationId: '',
		enabledLocationIds: [],
		customFieldValues: {}
	});

	const statusOptions: Array<{ value: StatusFilter; label: string }> = [
		{ value: 'all', label: 'Tutti gli stati' },
		{ value: GymMembershipStatusValue.Active, label: 'Attivi' },
		{ value: GymMembershipStatusValue.PendingClaim, label: 'Da attivare' },
		{ value: GymMembershipStatusValue.Suspended, label: 'Sospesi' },
		{ value: GymMembershipStatusValue.Archived, label: 'Archiviati' }
	];

	const sourceOptions: Array<{ value: SourceFilter; label: string }> = [
		{ value: 'all', label: 'Tutte le origini' },
		{ value: GymMembershipSourceValue.StaffInvite, label: 'Invito desk' },
		{ value: GymMembershipSourceValue.SelfSignup, label: 'Registrazione autonoma' },
		{ value: GymMembershipSourceValue.Import, label: 'Importato' },
		{ value: GymMembershipSourceValue.Migration, label: 'Migrazione' }
	];

	const formatDate = (value: Date | null | undefined, fallback = 'Non disponibile') =>
		formatMembershipDate(value, dateFormatter, fallback);

	const formatDateTime = (value: Date | null | undefined, fallback = 'Non disponibile') =>
		formatMembershipDateTime(value, dateTimeFormatter, fallback);

	function getDaysToExpiry(value: Date | null | undefined) {
		if (!value) {
			return null;
		}

		const now = new Date();
		const midnightNow = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
		const midnightExpiry = new Date(
			value.getFullYear(),
			value.getMonth(),
			value.getDate()
		).getTime();
		return Math.ceil((midnightExpiry - midnightNow) / 86_400_000);
	}

	function normalizeText(value: string | null | undefined) {
		return value?.trim().toLowerCase() ?? '';
	}

	const statusMeta = (status: GymMembershipStatus | undefined) => membershipStatusMeta(status);

	function customFieldValuesFromMembership(record: GymMembershipResponse) {
		return Object.fromEntries(
			(record.customFields ?? [])
				.filter((field) => field.fieldDefinitionId)
				.map((field) => [field.fieldDefinitionId!, field.value ?? ''])
		);
	}

	function buildCustomFieldInputs(
		values: Record<string, string>,
		definitions: GymCustomFieldDefinition[]
	) {
		return definitions.map((field) => ({
			fieldDefinitionId: field.id,
			value: values[field.id] ?? ''
		}));
	}

	const formatCustomFieldValue = (value: string | null | undefined, valueType: string | null | undefined) =>
		formatMembershipCustomFieldValue(value, valueType, formatDate);

	function updateCreateCustomFieldValue(fieldId: string, value: string) {
		formState = {
			...formState,
			customFieldValues: {
				...formState.customFieldValues,
				[fieldId]: value
			}
		};
	}

	function updateEditCustomFieldValue(fieldId: string, value: string) {
		editFormState = {
			...editFormState,
			customFieldValues: {
				...editFormState.customFieldValues,
				[fieldId]: value
			}
		};
	}

	function mapMembership(record: GymMembershipResponse): ClientViewModel {
		const name = membershipDisplayName(record);
		const email = membershipDisplayEmail(record);
		const status = statusMeta(record.status);
		const expiryDays = getDaysToExpiry(record.endedAtUtc);
		const locationNames = (record.locations ?? []).map((location) => location.name).filter(Boolean);

		return {
			id:
				record.membershipId ??
				`${email}-${record.createdAtUtc?.toISOString() ?? record.updatedAtUtc?.toISOString() ?? name}`,
			record,
			name,
			email,
			avatar:
				record.memberProfile?.avatarUrl?.trim() ||
				`https://api.dicebear.com/9.x/initials/svg?seed=${encodeURIComponent(name)}`,
			initials: membershipInitials(name),
			statusLabel: status.label,
			statusVariant: status.variant,
			sourceLabel: membershipSourceLabel(record.source),
			locationLabel:
				locationNames.length > 0 ? locationNames.join(', ') : 'Tutte le sedi del tenant',
			locationIds: (record.locations ?? [])
				.map((location) => location.id)
				.filter(Boolean) as string[],
			joinedLabel: formatDate(
				record.joinedAtUtc ?? record.claimedAtUtc ?? record.createdAtUtc,
				'Non ancora attivato'
			),
			updatedLabel: formatDateTime(record.updatedAtUtc ?? record.createdAtUtc),
			expiryLabel:
				record.endedAtUtc && expiryDays !== null
					? expiryDays < 0
						? `Scaduto da ${Math.abs(expiryDays)} giorni`
						: expiryDays === 0
							? 'Scade oggi'
							: `Scade tra ${expiryDays} giorni`
					: 'Nessuna scadenza registrata',
			expiringSoon: expiryDays !== null && expiryDays >= 0 && expiryDays <= 30,
			daysToExpiry: expiryDays,
			searchBlob: normalizeText(
				[
					name,
					email,
					record.taxCode,
					record.notes,
					record.memberProfile?.firstName,
					record.memberProfile?.lastName,
					...(record.customFields ?? []).map((field) => field.value ?? '')
				].join(' ')
			),
			notesPreview: record.notes?.trim() || 'Nessuna nota operativa registrata.',
			fullNamePieces: {
				firstName: record.memberProfile?.firstName?.trim() || null,
				lastName: record.memberProfile?.lastName?.trim() || null
			}
		};
	}

	const membershipsQuery = createQuery(() => ({
		queryKey: ['dashboard', 'clients', center.selectedGymId],
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

			return response.data ?? [];
		}
	}));
	const customFieldsQuery = createQuery(() => ({
		queryKey: ['clients-custom-fields', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => fetchGymCustomFields(center.selectedGymId!)
	}));

	const clients = $derived((membershipsQuery.data ?? []).map(mapMembership));
	const activeCustomFields = $derived(
		(customFieldsQuery.data ?? [])
			.filter((field) => field.isActive)
			.slice()
			.sort((left, right) => left.sortOrder - right.sortOrder)
	);
	const totalClients = $derived(clients.length);
	const activeClients = $derived(
		clients.filter((client) => client.record.status === GymMembershipStatusValue.Active).length
	);
	const pendingClients = $derived(
		clients.filter((client) => client.record.status === GymMembershipStatusValue.PendingClaim)
			.length
	);
	const expiringClients = $derived(clients.filter((client) => client.expiringSoon).length);
	const availableLocations = $derived(center.locations);
	const statusFilterLabel = $derived(
		statusOptions.find((option) => option.value === statusFilter)?.label ?? 'Tutti gli stati'
	);
	const sourceFilterLabel = $derived(
		sourceOptions.find((option) => option.value === sourceFilter)?.label ?? 'Tutte le origini'
	);
	const locationFilterLabel = $derived(
		locationFilter === 'all'
			? 'Tutte le sedi'
			: (availableLocations.find((location) => location.id === locationFilter)?.name ??
					'Tutte le sedi')
	);
	const primaryLocationLabel = $derived(
		formState.primaryLocationId
			? (availableLocations.find((location) => location.id === formState.primaryLocationId)?.name ??
					'Sede selezionata')
			: 'Tutte le sedi del tenant'
	);
	const editPrimaryLocationLabel = $derived(
		editFormState.primaryLocationId
			? (availableLocations.find((location) => location.id === editFormState.primaryLocationId)
					?.name ?? 'Sede primaria')
			: 'Seleziona una sede primaria'
	);
	const filteredClients = $derived.by(() => {
		const needle = normalizeText(searchTerm);

		return clients.filter((client) => {
			if (mode === 'expiring' && !client.expiringSoon) {
				return false;
			}

			if (statusFilter !== 'all' && client.record.status !== statusFilter) {
				return false;
			}

			if (sourceFilter !== 'all' && client.record.source !== sourceFilter) {
				return false;
			}

			if (locationFilter !== 'all' && !client.locationIds.includes(locationFilter)) {
				return false;
			}

			if (needle && !client.searchBlob.includes(needle)) {
				return false;
			}

			return true;
		});
	});
	const selectedClient = $derived(
		filteredClients.find((client) => client.id === selectedClientId) ?? filteredClients[0] ?? null
	);
	const editableStatusOptions = $derived.by(() => {
		if (!selectedClient) {
			return [] as Array<{ value: GymMembershipStatus; label: string }>;
		}

		if (!selectedClient.record.userId) {
			return [{ value: GymMembershipStatusValue.PendingClaim, label: 'Da attivare' }];
		}

		return [
			{ value: GymMembershipStatusValue.Active, label: 'Attivo' },
			{ value: GymMembershipStatusValue.Suspended, label: 'Sospeso' },
			{ value: GymMembershipStatusValue.Archived, label: 'Archiviato' }
		];
	});
	const editEnabledLocations = $derived(
		availableLocations.filter((location) => editFormState.enabledLocationIds.includes(location.id))
	);
	const selectedClientCustomFields = $derived(
		(selectedClient?.record.customFields ?? []).slice().sort((left, right) => {
			const leftOrder = left.sortOrder ?? 0;
			const rightOrder = right.sortOrder ?? 0;
			return leftOrder - rightOrder;
		})
	);
	const selectedClientMembershipId = $derived(
		selectedClient?.record.membershipId ?? selectedClient?.id ?? null
	);
	const selectedClientSalesHref = $derived.by(() =>
		selectedClientMembershipId
			? `/sales/new?membershipId=${encodeURIComponent(selectedClientMembershipId)}`
			: '/sales/new'
	);
	const selectedClientRenewalHref = $derived.by(() => {
		if (!selectedClientMembershipId) {
			return '/sales/renewals';
		}

		return `/sales/renewals?membershipId=${encodeURIComponent(selectedClientMembershipId)}`;
	});
	const selectedClientCanStartRenewal = $derived.by(() => {
		if (!selectedClient || selectedClient.record.status !== GymMembershipStatusValue.Active) {
			return false;
		}

		const daysToExpiry = selectedClient.daysToExpiry;
		return daysToExpiry !== null && daysToExpiry >= -30 && daysToExpiry <= 30;
	});

	const pageMeta = $derived.by(() => {
		if (mode === 'new') {
			return {
				title: 'Nuovo cliente',
				description: 'Preregistra un cliente nel tenant corrente e prepara la sua membership.'
			};
		}

		if (mode === 'expiring') {
			return {
				title: 'Clienti in scadenza',
				description: 'Vista prioritaria per gestire rinnovi e contatti urgenti dal desk.'
			};
		}

		return {
			title: 'Clienti',
			description: 'Anagrafica clienti, stato membership, sedi abilitate e note operative.'
		};
	});
	const membershipsQueryError = $derived(
		membershipsQuery.error instanceof Error
			? membershipsQuery.error.message
			: membershipsQuery.error
				? 'Impossibile caricare i clienti.'
				: null
	);
	const customFieldsQueryError = $derived(
		customFieldsQuery.error instanceof Error
			? customFieldsQuery.error.message
			: customFieldsQuery.error
				? 'Impossibile caricare i campi personalizzati.'
				: null
	);
	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms
			|| (!!center.selectedGymId
				&& ((!membershipsQuery.data && membershipsQuery.isPending)
					|| (!customFieldsQuery.data && customFieldsQuery.isPending)))
	);
	const workspaceError = $derived(
		center.gymsError ?? membershipsQueryError ?? customFieldsQueryError ?? null
	);

	$effect(() => {
		if (!filteredClients.length) {
			if (selectedClientId !== null) {
				selectedClientId = null;
			}
			return;
		}

		if (!selectedClientId || !filteredClients.some((client) => client.id === selectedClientId)) {
			selectedClientId = filteredClients[0]?.id ?? null;
		}
	});

	$effect(() => {
		if (center.selectedLocationId && formState.primaryLocationId === '') {
			formState = {
				...formState,
				primaryLocationId: center.selectedLocationId
			};
		}
	});

	$effect(() => {
		if (mode === 'new' && autoOpenCreateSheet && !hasAutoOpenedCreateSheet && !createSubmitting) {
			createOpen = true;
			hasAutoOpenedCreateSheet = true;
		}
	});

	$effect(() => {
		if (!editOpen) {
			return;
		}

		if (editFormState.enabledLocationIds.length === 0 && availableLocations.length > 0) {
			const fallbackLocationId =
				center.selectedLocationId ??
				selectedClient?.record.primaryLocationId ??
				availableLocations[0]?.id ??
				'';
			if (!fallbackLocationId) {
				return;
			}

			editFormState = {
				...editFormState,
				enabledLocationIds: [fallbackLocationId],
				primaryLocationId: fallbackLocationId
			};
			return;
		}

		if (
			editFormState.primaryLocationId &&
			!editFormState.enabledLocationIds.includes(editFormState.primaryLocationId)
		) {
			editFormState = {
				...editFormState,
				primaryLocationId: editFormState.enabledLocationIds[0] ?? ''
			};
			return;
		}

		if (!editFormState.primaryLocationId && editFormState.enabledLocationIds.length > 0) {
			editFormState = {
				...editFormState,
				primaryLocationId: editFormState.enabledLocationIds[0] ?? ''
			};
		}
	});

	function resetCreateForm() {
		formState = {
			email: '',
			firstName: '',
			lastName: '',
			taxCode: '',
			notes: '',
			primaryLocationId: center.selectedLocationId ?? '',
			customFieldValues: {}
		};
		createError = '';
	}

	function resetEditForm() {
		editFormState = {
			email: '',
			firstName: '',
			lastName: '',
			taxCode: '',
			notes: '',
			status: GymMembershipStatusValue.Active,
			primaryLocationId: '',
			enabledLocationIds: [],
			customFieldValues: {}
		};
		editError = '';
	}

	function openEditClient() {
		if (!selectedClient) {
			return;
		}

		const fallbackLocationId =
			selectedClient.record.primaryLocationId ??
			selectedClient.locationIds[0] ??
			center.selectedLocationId ??
			availableLocations[0]?.id ??
			'';
		const enabledLocationIds =
			selectedClient.locationIds.length > 0
				? [...selectedClient.locationIds]
				: fallbackLocationId
					? [fallbackLocationId]
					: [];

		editFormState = {
			email: selectedClient.email,
			firstName: selectedClient.fullNamePieces.firstName ?? '',
			lastName: selectedClient.fullNamePieces.lastName ?? '',
			taxCode: selectedClient.record.taxCode ?? '',
			notes: selectedClient.record.notes ?? '',
			status: selectedClient.record.status ?? GymMembershipStatusValue.Active,
			primaryLocationId: fallbackLocationId,
			enabledLocationIds,
			customFieldValues: customFieldValuesFromMembership(selectedClient.record)
		};
		editError = '';
		editOpen = true;
	}

	function toggleEditLocation(locationId: string, enabled: boolean) {
		const nextEnabled = enabled
			? Array.from(new Set([...editFormState.enabledLocationIds, locationId]))
			: editFormState.enabledLocationIds.filter((value) => value !== locationId);

		editFormState = {
			...editFormState,
			enabledLocationIds: nextEnabled,
			primaryLocationId: nextEnabled.includes(editFormState.primaryLocationId)
				? editFormState.primaryLocationId
				: (nextEnabled[0] ?? '')
		};
	}

	async function refreshAll() {
		if (!center.selectedGymId) {
			return;
		}

		await Promise.all([membershipsQuery.refetch(), customFieldsQuery.refetch()]);
	}

	async function handleCreateClient(event: Event) {
		event.preventDefault();
		createError = '';
		createSuccess = '';

		if (!center.selectedGymId) {
			createError = 'Seleziona prima un tenant per invitare un cliente.';
			return;
		}

		if (!formState.email.trim()) {
			createError = "Inserisci almeno l'email del cliente.";
			return;
		}

		createSubmitting = true;

		try {
			const response = await api.client.apiGymsGymIdMembershipsPost({
				gymId: center.selectedGymId,
				assignUserToGymRequest: {
					email: formState.email.trim(),
					locationIds: formState.primaryLocationId ? [formState.primaryLocationId] : [],
					primaryLocationId: formState.primaryLocationId || null,
					status: GymMembershipStatusValue.PendingClaim,
					source: GymMembershipSourceValue.StaffInvite,
					taxCode: formState.taxCode.trim() || null,
					notes: formState.notes.trim() || null,
					customFields: buildCustomFieldInputs(formState.customFieldValues, activeCustomFields),
					profile: {
						firstName: formState.firstName.trim() || null,
						lastName: formState.lastName.trim() || null
					}
				}
			});

			if (!response.success || !response.data) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile creare il cliente.'
				);
			}

			await membershipsQuery.refetch();
			selectedClientId = response.data.membershipId ?? selectedClientId;
			createSuccess =
				'Cliente preregistrato con successo. Ora puoi inviare il link di attivazione.';
			createOpen = false;
			resetCreateForm();
		} catch (error: unknown) {
			createError = error instanceof Error ? error.message : 'Impossibile creare il cliente.';
		} finally {
			createSubmitting = false;
		}
	}

	async function handleEditClient(event: Event) {
		event.preventDefault();
		editError = '';
		createSuccess = '';

		if (!center.selectedGymId || !selectedClient) {
			editError = 'Seleziona prima un cliente del tenant.';
			return;
		}

		if (editFormState.enabledLocationIds.length === 0) {
			editError = 'Seleziona almeno una sede abilitata per la membership.';
			return;
		}

		if (!editFormState.primaryLocationId) {
			editError = 'Seleziona una sede primaria valida.';
			return;
		}

		if (!editFormState.enabledLocationIds.includes(editFormState.primaryLocationId)) {
			editError = 'La sede primaria deve essere compresa tra le sedi abilitate.';
			return;
		}

		editSubmitting = true;

		try {
			const payload = {
				userId: selectedClient.record.userId ?? null,
				email: selectedClient.record.userId ? null : selectedClient.email,
				locationIds: editFormState.enabledLocationIds,
				primaryLocationId: editFormState.primaryLocationId,
				status: editFormState.status,
				source: selectedClient.record.source ?? GymMembershipSourceValue.StaffInvite,
				taxCode: editFormState.taxCode.trim() || null,
				notes: editFormState.notes.trim() || null,
				customFields: buildCustomFieldInputs(editFormState.customFieldValues, activeCustomFields),
				profile: {
					firstName: editFormState.firstName.trim() || null,
					lastName: editFormState.lastName.trim() || null
				}
			} satisfies AssignUserToGymRequest;

			const response = await api.client.apiGymsGymIdMembershipsPost({
				gymId: center.selectedGymId,
				assignUserToGymRequest: payload
			});

			if (!response.success || !response.data) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile aggiornare il cliente.'
				);
			}

			await membershipsQuery.refetch();
			selectedClientId = response.data.membershipId ?? selectedClientId;
			createSuccess = `Cliente ${membershipDisplayName(response.data)} aggiornato con successo.`;
			editOpen = false;
			resetEditForm();
		} catch (error: unknown) {
			editError = error instanceof Error ? error.message : 'Impossibile aggiornare il cliente.';
		} finally {
			editSubmitting = false;
		}
	}
</script>

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	<section class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
		<div class="max-w-3xl space-y-2">
			<div class="flex flex-wrap items-center gap-2">
				<Badge variant="secondary" class="rounded-full px-3 py-1">Tenant memberships</Badge>
				{#if center.selectedGym}
					<Badge variant="outline" class="rounded-full px-3 py-1">
						{center.selectedGym.name}
					</Badge>
				{/if}
			</div>
			<div>
				<h2 class="text-2xl font-semibold tracking-tight">{pageMeta.title}</h2>
				<p class="max-w-2xl text-sm leading-relaxed text-muted-foreground">
					{pageMeta.description}
				</p>
			</div>
		</div>

		<div class="flex flex-wrap items-center gap-2">
			<Button variant="outline" size="sm" onclick={refreshAll} disabled={!hasSelectedGym || workspaceLoading}>
				<RefreshCwIcon class="size-4" />
				Aggiorna
			</Button>
			<Button variant="outline" size="sm" href="/users/expiring">
				<CalendarClockIcon class="size-4" />
				Scadenze
			</Button>
			<Button size="sm" onclick={() => (createOpen = true)} disabled={!hasSelectedGym || workspaceLoading}>
				<UserRoundPlusIcon class="size-4" />
				Nuovo cliente
			</Button>
		</div>
	</section>

	<section
		class="rounded-[24px] border border-border/70 bg-background [background-image:linear-gradient(135deg,rgba(23,105,255,0.08),rgba(255,255,255,0.96),rgba(184,242,29,0.12))] p-5 shadow-[var(--bf-shadow-card)]"
	>
		<div class="grid gap-3 lg:grid-cols-[1.5fr_1fr]">
			<div class="space-y-2">
				<div class="flex items-center gap-2 text-sm font-semibold">
					<CircleAlertIcon class="size-4 text-primary" />
					Modello clienti coerente con le specifiche docs
				</div>
				<p class="text-sm leading-relaxed text-muted-foreground">
					Questa area lavora sulle <strong>membership della palestra corrente</strong>, non
					sull'account globale Betterfit. Lo stesso account puo avere piu membership su tenant
					diversi, con stato, sedi e note operative indipendenti.
				</p>
			</div>

			<div class="grid gap-2 text-sm text-muted-foreground sm:grid-cols-2 lg:grid-cols-1">
				<div class="rounded-[18px] border border-white/70 bg-background/80 px-4 py-3">
					<p class="font-semibold text-foreground">Tenant attivo</p>
					<p class="mt-1">{center.selectedGym?.name ?? 'Selezione in corso'}</p>
				</div>
				<div class="rounded-[18px] border border-white/70 bg-background/80 px-4 py-3">
					<p class="font-semibold text-foreground">Scope operativo</p>
					<p class="mt-1">
						{center.selectedLocation?.name ?? 'Tutte le sedi del tenant selezionato'}
					</p>
				</div>
			</div>
		</div>
	</section>

	<section class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Clienti censiti</CardDescription>
				<CardTitle class="text-3xl">{totalClients}</CardTitle>
			</CardHeader>
			<CardContent class="flex items-center gap-2 text-sm text-muted-foreground">
				<UsersIcon class="size-4 text-primary" />
				Membership presenti nel tenant corrente
			</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Attivi</CardDescription>
				<CardTitle class="text-3xl">{activeClients}</CardTitle>
			</CardHeader>
			<CardContent class="flex items-center gap-2 text-sm text-muted-foreground">
				<BellRingIcon class="size-4 text-[#166534]" />
				Clienti gia attivati e utilizzabili
			</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Da attivare</CardDescription>
				<CardTitle class="text-3xl">{pendingClients}</CardTitle>
			</CardHeader>
			<CardContent class="flex items-center gap-2 text-sm text-muted-foreground">
				<MailPlusIcon class="size-4 text-[#92400e]" />
				Inviti desk o preregistrazioni da completare
			</CardContent>
		</Card>

		<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
			<CardHeader class="space-y-1 pb-2">
				<CardDescription>Scadenze calde</CardDescription>
				<CardTitle class="text-3xl">{expiringClients}</CardTitle>
			</CardHeader>
			<CardContent class="flex items-center gap-2 text-sm text-muted-foreground">
				<Clock3Icon class="size-4 text-destructive" />
				Membership che scadono entro 30 giorni
			</CardContent>
		</Card>
	</section>

	{#if createSuccess}
		<section
			class="rounded-[20px] border border-[#bbf7d0] bg-[#f0fdf4] px-4 py-3 text-sm text-[#166534]"
		>
			{createSuccess}
		</section>
	{/if}

	{#if workspaceLoading}
		<Card class="border-dashed border-border/70 bg-muted/25">
			<CardHeader>
				<CardTitle>Caricamento area clienti</CardTitle>
				<CardDescription>
					Sto recuperando membership e campi personalizzati prima di mostrarti l anagrafica
					reale del tenant.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-dashed border-[#fecaca] bg-[#fff7f7]">
			<CardHeader>
				<CardTitle>Impossibile caricare l area clienti</CardTitle>
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
					Scegli prima la palestra dal selettore in alto a sinistra per vedere anagrafica,
					membership, sedi e campi personalizzati del tenant corretto.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
		<section class="grid gap-4 2xl:grid-cols-[1.2fr_0.8fr]">
			<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
				<CardHeader class="gap-4">
					<div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
						<div>
							<CardTitle>Registro clienti del tenant</CardTitle>
							<CardDescription>
								Ricerca rapida, stato membership, origine e filtraggio per sede.
							</CardDescription>
						</div>
						<Badge variant="outline" class="rounded-full px-3 py-1">
							{filteredClients.length} visibili su {clients.length}
						</Badge>
					</div>

					<div class="grid gap-3 lg:grid-cols-[1.5fr_1fr_1fr_1fr]">
						<div class="relative">
							<SearchIcon
								class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
							/>
							<Input
								class="pl-9"
								placeholder="Cerca per nome, email, codice fiscale o nota"
								bind:value={searchTerm}
							/>
						</div>

						<Select.Root type="single" bind:value={statusFilter}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{statusFilterLabel}</span>
							</Select.Trigger>
							<Select.Content>
								{#each statusOptions as option (option.value)}
									<Select.Item value={option.value} label={option.label}>{option.label}</Select.Item
									>
								{/each}
							</Select.Content>
						</Select.Root>

						<Select.Root type="single" bind:value={sourceFilter}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{sourceFilterLabel}</span>
							</Select.Trigger>
							<Select.Content>
								{#each sourceOptions as option (option.value)}
									<Select.Item value={option.value} label={option.label}>{option.label}</Select.Item
									>
								{/each}
							</Select.Content>
						</Select.Root>

						<Select.Root type="single" bind:value={locationFilter}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{locationFilterLabel}</span>
							</Select.Trigger>
							<Select.Content>
								<Select.Item value="all" label="Tutte le sedi">Tutte le sedi</Select.Item>
								{#each availableLocations as location (location.id)}
									<Select.Item value={location.id} label={location.name}
										>{location.name}</Select.Item
									>
								{/each}
							</Select.Content>
						</Select.Root>
					</div>
				</CardHeader>

				<CardContent>
					{#if filteredClients.length === 0}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="text-base font-semibold">
								{mode === 'expiring'
									? 'Nessun cliente in scadenza nei prossimi 30 giorni'
									: 'Nessun cliente trovato con i filtri correnti'}
							</p>
							<p class="mt-2 text-sm text-muted-foreground">
								{mode === 'new'
									? 'Puoi iniziare subito preregistrando un nuovo cliente dal drawer laterale.'
									: 'Prova a cambiare filtri oppure crea una nuova membership dal desk.'}
							</p>
							<div class="mt-4 flex flex-wrap justify-center gap-2">
								<Button variant="outline" size="sm" onclick={refreshAll}>
									<RefreshCwIcon class="size-4" />
									Aggiorna lista
								</Button>
								<Button size="sm" onclick={() => (createOpen = true)}>
									<UserRoundPlusIcon class="size-4" />
									Nuovo cliente
								</Button>
							</div>
						</div>
					{:else}
						<div class="overflow-hidden rounded-[20px] border border-border/70">
							<Table>
								<TableHeader>
									<TableRow class="bg-secondary/30 hover:bg-secondary/30">
										<TableHead>Cliente</TableHead>
										<TableHead>Stato</TableHead>
										<TableHead class="hidden xl:table-cell">Sede</TableHead>
										<TableHead class="hidden lg:table-cell">Origine</TableHead>
										<TableHead class="hidden md:table-cell">Scadenza</TableHead>
									</TableRow>
								</TableHeader>
								<TableBody>
									{#each filteredClients as client (client.id)}
										<TableRow
											class={`cursor-pointer transition-colors hover:bg-secondary/35 ${selectedClient?.id === client.id ? 'bg-[#eef4ff]' : ''}`}
											onclick={() => {
												selectedClientId = client.id;
												detailTab = 'overview';
											}}
										>
											<TableCell>
												<div class="flex items-center gap-3">
													<Avatar.Root class="size-10">
														<Avatar.Image src={client.avatar} alt={client.name} />
														<Avatar.Fallback class="bg-primary/10 text-primary">
															{client.initials}
														</Avatar.Fallback>
													</Avatar.Root>
													<div class="min-w-0">
														<p class="truncate font-semibold">{client.name}</p>
														<p class="truncate text-sm text-muted-foreground">{client.email}</p>
													</div>
												</div>
											</TableCell>
											<TableCell>
												<div class="flex flex-col gap-1">
													<Badge variant={client.statusVariant}>{client.statusLabel}</Badge>
													{#if client.record.status === GymMembershipStatusValue.Suspended}
														<span class="text-xs text-muted-foreground">
															Richiede verifica desk
														</span>
													{/if}
												</div>
											</TableCell>
											<TableCell class="hidden max-w-[180px] xl:table-cell">
												<p class="truncate text-sm text-muted-foreground">
													{client.locationLabel}
												</p>
											</TableCell>
											<TableCell class="hidden lg:table-cell">
												<p class="text-sm text-muted-foreground">{client.sourceLabel}</p>
											</TableCell>
											<TableCell class="hidden md:table-cell">
												<div class="space-y-1">
													<p
														class={`text-sm ${client.expiringSoon ? 'font-semibold text-destructive' : 'text-muted-foreground'}`}
													>
														{client.expiryLabel}
													</p>
													<p class="text-xs text-muted-foreground">
														Aggiornato {client.updatedLabel}
													</p>
												</div>
											</TableCell>
										</TableRow>
									{/each}
								</TableBody>
							</Table>
						</div>
					{/if}
				</CardContent>
			</Card>

			<Card class="border-border/70 shadow-[var(--bf-shadow-card)]">
				<CardHeader class="gap-4">
					<div class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
						{#if selectedClient}
							<div class="flex items-start gap-3">
								<Avatar.Root class="size-14 border-white bg-secondary">
									<Avatar.Image src={selectedClient.avatar} alt={selectedClient.name} />
									<Avatar.Fallback class="bg-primary/10 text-primary">
										{selectedClient.initials}
									</Avatar.Fallback>
								</Avatar.Root>
								<div class="min-w-0 space-y-1">
									<div class="flex flex-wrap items-center gap-2">
										<CardTitle class="truncate">{selectedClient.name}</CardTitle>
										<Badge variant={selectedClient.statusVariant}
											>{selectedClient.statusLabel}</Badge
										>
									</div>
									<CardDescription class="truncate">{selectedClient.email}</CardDescription>
									<p class="text-xs text-muted-foreground">
										Origine: {selectedClient.sourceLabel}
									</p>
								</div>
							</div>
							<div class="flex flex-wrap gap-2">
								<Button variant="outline" size="sm" onclick={openEditClient}>
									Modifica cliente
								</Button>
								<Button
									variant="outline"
									size="sm"
									href={`/users/${encodeURIComponent(selectedClient.record.membershipId ?? selectedClient.id)}`}
								>
									Apri scheda utente
								</Button>
							</div>
						{:else}
							<div class="space-y-1">
								<CardTitle>Profilo cliente</CardTitle>
								<CardDescription>
									Seleziona una riga per vedere il riepilogo operativo.
								</CardDescription>
							</div>
						{/if}
					</div>
				</CardHeader>

				<CardContent>
					{#if selectedClient}
						<Tabs.Root bind:value={detailTab} class="gap-4">
							<Tabs.List class="grid w-full grid-cols-3">
								<Tabs.Trigger value="overview">Overview</Tabs.Trigger>
								<Tabs.Trigger value="profile">Profilo</Tabs.Trigger>
								<Tabs.Trigger value="membership">Membership</Tabs.Trigger>
							</Tabs.List>

							<Tabs.Content value="overview" class="space-y-4">
								<div class="grid gap-3 sm:grid-cols-2">
									<div class="rounded-[18px] border border-border/70 bg-secondary/20 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Ingresso nel tenant
										</p>
										<p class="mt-2 text-sm font-semibold">{selectedClient.joinedLabel}</p>
										<p class="mt-1 text-sm text-muted-foreground">
											Claim account: {formatDate(
												selectedClient.record.claimedAtUtc,
												'Non ancora claimato'
											)}
										</p>
									</div>

									<div class="rounded-[18px] border border-border/70 bg-secondary/20 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Scadenza
										</p>
										<p
											class={`mt-2 text-sm font-semibold ${selectedClient.expiringSoon ? 'text-destructive' : ''}`}
										>
											{selectedClient.expiryLabel}
										</p>
										<p class="mt-1 text-sm text-muted-foreground">
											Stato operativo: {selectedClient.statusLabel}
										</p>
									</div>
								</div>

								<div class="rounded-[18px] border border-border/70 p-4">
									<p
										class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
									>
										Note desk
									</p>
									<p class="mt-2 text-sm leading-relaxed text-foreground">
										{selectedClient.notesPreview}
									</p>
								</div>

								<div class="rounded-[18px] border border-border/70 p-4">
									<p
										class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
									>
										Azioni consigliate
									</p>
									<div class="mt-3 flex flex-wrap gap-2">
										<Button variant="outline" size="sm" onclick={openEditClient}>
											Modifica cliente
										</Button>
										<Button variant="outline" size="sm" href={selectedClientSalesHref}>
											Nuova vendita
										</Button>
										{#if selectedClientCanStartRenewal}
											<Button variant="outline" size="sm" href={selectedClientRenewalHref}>
												Avvia rinnovo
											</Button>
										{/if}
										<Button
											variant="outline"
											size="sm"
											href={`/users/${encodeURIComponent(selectedClient.record.membershipId ?? selectedClient.id)}`}
										>
											Scheda utente
										</Button>
										{#if selectedClient.record.status === GymMembershipStatusValue.PendingClaim}
											<Badge variant="warning">Inviare reminder attivazione</Badge>
										{/if}
										{#if selectedClient.expiringSoon}
											<Badge variant="destructive">Contattare per rinnovo</Badge>
										{/if}
										{#if selectedClient.record.status === GymMembershipStatusValue.Active}
											<Badge variant="success">Cliente pronto per accesso e vendita</Badge>
										{/if}
										{#if selectedClient.record.status === GymMembershipStatusValue.Suspended}
											<Badge variant="outline">Verificare blocco amministrativo</Badge>
										{/if}
									</div>
								</div>
							</Tabs.Content>

							<Tabs.Content value="profile" class="space-y-4">
								<div class="grid gap-3 sm:grid-cols-2">
									<div class="rounded-[18px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Nome
										</p>
										<p class="mt-2 text-sm font-semibold">
											{selectedClient.fullNamePieces.firstName ?? 'Non disponibile'}
										</p>
									</div>
									<div class="rounded-[18px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Cognome
										</p>
										<p class="mt-2 text-sm font-semibold">
											{selectedClient.fullNamePieces.lastName ?? 'Non disponibile'}
										</p>
									</div>
									<div class="rounded-[18px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Codice fiscale
										</p>
										<p class="mt-2 text-sm font-semibold">
											{selectedClient.record.taxCode ?? 'Non registrato'}
										</p>
									</div>
									<div class="rounded-[18px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Email operativa
										</p>
										<p class="mt-2 text-sm font-semibold">{selectedClient.email}</p>
									</div>
								</div>

								<div class="rounded-[18px] border border-border/70 p-4">
									<p
										class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
									>
										Profilo membro
									</p>
									<div class="mt-3 grid gap-3 sm:grid-cols-2">
										<div>
											<p class="text-sm font-medium">Contatto emergenza</p>
											<p class="mt-1 text-sm text-muted-foreground">
												{selectedClient.record.memberProfile?.emergencyContactName ??
													'Non disponibile'}
											</p>
										</div>
										<div>
											<p class="text-sm font-medium">Telefono emergenza</p>
											<p class="mt-1 text-sm text-muted-foreground">
												{selectedClient.record.memberProfile?.emergencyContactPhoneNumber ??
													'Non disponibile'}
											</p>
										</div>
										<div>
											<p class="text-sm font-medium">Data nascita</p>
											<p class="mt-1 text-sm text-muted-foreground">
												{formatDate(
													selectedClient.record.memberProfile?.birthDate,
													'Non disponibile'
												)}
											</p>
										</div>
										<div>
											<p class="text-sm font-medium">Ultimo aggiornamento profilo</p>
											<p class="mt-1 text-sm text-muted-foreground">
												{formatDateTime(
													selectedClient.record.memberProfile?.updatedAtUtc ??
														selectedClient.record.memberProfile?.createdAtUtc,
													'Non disponibile'
												)}
											</p>
										</div>
									</div>
								</div>

								<div class="rounded-[18px] border border-border/70 p-4">
									<p
										class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
									>
										Campi personalizzati
									</p>
									{#if selectedClientCustomFields.length === 0}
										<p class="mt-2 text-sm text-muted-foreground">
											Nessun campo personalizzato compilato per questo cliente.
										</p>
									{:else}
										<div class="mt-3 grid gap-3 sm:grid-cols-2">
											{#each selectedClientCustomFields as field (field.fieldDefinitionId)}
												<div>
													<p class="text-sm font-medium">{field.label ?? field.key}</p>
													<p class="mt-1 text-sm text-muted-foreground">
														{formatCustomFieldValue(field.value, field.valueType)}
													</p>
												</div>
											{/each}
										</div>
									{/if}
								</div>
							</Tabs.Content>

							<Tabs.Content value="membership" class="space-y-4">
								<div class="rounded-[18px] border border-border/70 p-4">
									<p
										class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
									>
										Scope sedi
									</p>
									<p class="mt-2 text-sm font-semibold">{selectedClient.locationLabel}</p>
									<p class="mt-1 text-sm text-muted-foreground">
										Primary location id: {selectedClient.record.primaryLocationId ??
											'Non impostata'}
									</p>
									<div class="mt-4 flex flex-wrap gap-2">
										<Button variant="outline" size="sm" onclick={openEditClient}>
											Modifica membership
										</Button>
									</div>
								</div>

								<div class="grid gap-3 sm:grid-cols-2">
									<div class="rounded-[18px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Creata il
										</p>
										<p class="mt-2 text-sm font-semibold">
											{formatDateTime(selectedClient.record.createdAtUtc)}
										</p>
									</div>
									<div class="rounded-[18px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Ultimo update
										</p>
										<p class="mt-2 text-sm font-semibold">{selectedClient.updatedLabel}</p>
									</div>
									<div class="rounded-[18px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Gym id
										</p>
										<p class="mt-2 text-sm font-semibold break-all">
											{selectedClient.record.gymId ?? 'Non disponibile'}
										</p>
									</div>
									<div class="rounded-[18px] border border-border/70 p-4">
										<p
											class="text-xs font-semibold tracking-[0.18em] text-muted-foreground uppercase"
										>
											Membership id
										</p>
										<p class="mt-2 text-sm font-semibold break-all">
											{selectedClient.record.membershipId ?? 'Non disponibile'}
										</p>
									</div>
								</div>
							</Tabs.Content>
						</Tabs.Root>
					{:else}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="text-base font-semibold">Nessun cliente selezionato</p>
							<p class="mt-2 text-sm text-muted-foreground">
								Scegli una riga nella tabella a sinistra per aprire la scheda cliente.
							</p>
						</div>
					{/if}
				</CardContent>
			</Card>
		</section>
	{/if}
</main>

<Sheet.Root bind:open={editOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-2xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Modifica cliente</Sheet.Title>
			<Sheet.Description class="text-sm leading-relaxed text-muted-foreground">
				Aggiorna stato membership, sedi abilitate, anagrafica minima e note operative del cliente
				selezionato.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleEditClient}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<label class="space-y-2">
					<span class="text-sm font-medium">Email cliente</span>
					<Input type="email" bind:value={editFormState.email} disabled />
					<p class="text-xs text-muted-foreground">
						L identificativo account non viene cambiato da questo drawer.
					</p>
				</label>

				<div class="grid gap-4 sm:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Nome</span>
						<Input bind:value={editFormState.firstName} placeholder="Antonio" />
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Cognome</span>
						<Input bind:value={editFormState.lastName} placeholder="Rossi" />
					</label>
				</div>

				<div class="grid gap-4 sm:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Codice fiscale</span>
						<Input bind:value={editFormState.taxCode} placeholder="RSSMRA80A01H501U" />
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Stato membership</span>
						<Select.Root type="single" bind:value={editFormState.status}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{statusMeta(editFormState.status).label}
								</span>
							</Select.Trigger>
							<Select.Content>
								{#each editableStatusOptions as option (option.value)}
									<Select.Item value={option.value} label={option.label}>
										{option.label}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
				</div>

				<div class="rounded-[18px] border border-border/70 p-4">
					<div class="flex items-center justify-between gap-3">
						<div>
							<p class="font-semibold">Sedi abilitate</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Il cliente deve restare assegnato ad almeno una sede attiva del tenant.
							</p>
						</div>
						<Badge variant="secondary">{editFormState.enabledLocationIds.length} sedi</Badge>
					</div>

					<div class="mt-4 grid gap-3">
						{#each availableLocations as location (location.id)}
							<label class="flex items-start gap-3 rounded-[16px] border border-border/70 p-4">
								<Checkbox
									class="mt-1"
									checked={editFormState.enabledLocationIds.includes(location.id)}
									onCheckedChange={(checked) => toggleEditLocation(location.id, checked)}
								/>
								<div>
									<p class="font-medium">{location.name}</p>
									<p class="mt-1 text-sm text-muted-foreground">
										{location.city || 'Citta non impostata'} - {location.countryCode ||
											'Paese n.d.'}
									</p>
								</div>
							</label>
						{/each}
					</div>
				</div>

				<label class="space-y-2">
					<span class="text-sm font-medium">Sede primaria</span>
					<Select.Root type="single" bind:value={editFormState.primaryLocationId}>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{editPrimaryLocationLabel}</span>
						</Select.Trigger>
						<Select.Content>
							{#each editEnabledLocations as location (location.id)}
								<Select.Item value={location.id} label={location.name}>
									{location.name}
								</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
				</label>

				{#if activeCustomFields.length > 0}
					<div class="space-y-4 rounded-[18px] border border-border/70 p-4">
						<div>
							<p class="font-semibold">Campi personalizzati</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Campi aggiuntivi configurati dal tenant per la scheda cliente.
							</p>
						</div>
						<div class="grid gap-4 sm:grid-cols-2">
							{#each activeCustomFields as field (field.id)}
								<div class={`space-y-2 ${field.valueType === 'LongText' ? 'sm:col-span-2' : ''}`}>
									<span class="text-sm font-medium">
										{field.label}{field.isRequired ? ' *' : ''}
									</span>
									{#if field.valueType === 'LongText'}
										<Textarea
											class="min-h-24"
											value={editFormState.customFieldValues[field.id] ?? ''}
											oninput={(event) =>
												updateEditCustomFieldValue(
													field.id,
													(event.currentTarget as HTMLTextAreaElement).value
												)}
										/>
									{:else if field.valueType === 'Boolean'}
										<label class="flex items-start gap-3 rounded-[16px] border border-border/70 p-3">
											<Checkbox
												class="mt-1"
												checked={editFormState.customFieldValues[field.id] === 'true'}
												onCheckedChange={(checked) =>
													updateEditCustomFieldValue(field.id, checked ? 'true' : 'false')}
											/>
											<span class="text-sm text-muted-foreground">
												Attiva se il valore deve risultare positivo.
											</span>
										</label>
									{:else if field.valueType === 'Select'}
										<Select.Root
											type="single"
											value={editFormState.customFieldValues[field.id] || CLIENTS_EMPTY_CUSTOM_FIELD_SELECT_VALUE}
											onValueChange={(value) =>
												updateEditCustomFieldValue(
													field.id,
													value === CLIENTS_EMPTY_CUSTOM_FIELD_SELECT_VALUE ? '' : value
												)}
										>
											<Select.Trigger class="w-full">
												<span data-slot="select-value">
													{editFormState.customFieldValues[field.id] || 'Seleziona un valore'}
												</span>
											</Select.Trigger>
											<Select.Content>
												<Select.Item
													value={CLIENTS_EMPTY_CUSTOM_FIELD_SELECT_VALUE}
													label="Seleziona un valore"
												>
													Seleziona un valore
												</Select.Item>
												{#each field.options as option}
													<Select.Item value={option} label={option}>{option}</Select.Item>
												{/each}
											</Select.Content>
										</Select.Root>
									{:else}
										<Input
											type={field.valueType === 'Number' ? 'number' : field.valueType === 'Date' ? 'date' : 'text'}
											value={editFormState.customFieldValues[field.id] ?? ''}
											oninput={(event) =>
												updateEditCustomFieldValue(
													field.id,
													(event.currentTarget as HTMLInputElement).value
												)}
										/>
									{/if}
								</div>
							{/each}
						</div>
					</div>
				{/if}

				<label class="space-y-2">
					<span class="text-sm font-medium">Note operative</span>
					<Textarea
						class="min-h-28"
						bind:value={editFormState.notes}
						placeholder="Es. contattato per rinnovo, sospensione amministrativa, cliente multisede."
					/>
				</label>

				<div
					class="rounded-[18px] border border-border/70 bg-secondary/20 p-4 text-sm text-muted-foreground"
				>
					<p class="font-semibold text-foreground">Regole applicate</p>
					<p class="mt-2">
						Le membership non ancora claimate restano in stato <strong>PendingClaim</strong>. Per i
						clienti gia collegati a un account Betterfit puoi invece gestire
						<strong>Attivo</strong>, <strong>Sospeso</strong> o <strong>Archiviato</strong>.
					</p>
				</div>

				{#if editError}
					<div
						class="rounded-[18px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]"
					>
						{editError}
					</div>
				{/if}
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button
					type="button"
					variant="outline"
					onclick={() => {
						editOpen = false;
						editError = '';
					}}
				>
					Annulla
				</Button>
				<Button type="submit" disabled={editSubmitting}>
					{#if editSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<UserRoundPlusIcon class="size-4" />
						Salva modifiche
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={createOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-2xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Nuovo cliente</Sheet.Title>
			<Sheet.Description class="text-sm leading-relaxed text-muted-foreground">
				Crea una membership in stato <strong>PendingClaim</strong> pronta per attivazione dal
				cliente.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateClient}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4 sm:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Nome</span>
						<Input bind:value={formState.firstName} placeholder="Antonio" />
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Cognome</span>
						<Input bind:value={formState.lastName} placeholder="Rossi" />
					</label>
				</div>

				<label class="space-y-2">
					<span class="text-sm font-medium">Email cliente</span>
					<Input
						type="email"
						required
						bind:value={formState.email}
						placeholder="cliente@esempio.it"
					/>
					<p class="text-xs text-muted-foreground">
						L'email viene usata per claim account o collegamento a un account Betterfit gia
						esistente.
					</p>
				</label>

				<div class="grid gap-4 sm:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Codice fiscale</span>
						<Input bind:value={formState.taxCode} placeholder="RSSMRA80A01H501U" />
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Sede primaria</span>
						<Select.Root
							type="single"
							value={formState.primaryLocationId || CLIENTS_ALL_TENANT_LOCATIONS_SELECT_VALUE}
							onValueChange={(value) =>
								(formState.primaryLocationId =
									value === CLIENTS_ALL_TENANT_LOCATIONS_SELECT_VALUE ? '' : value)}
						>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{primaryLocationLabel}</span>
							</Select.Trigger>
							<Select.Content>
								<Select.Item
									value={CLIENTS_ALL_TENANT_LOCATIONS_SELECT_VALUE}
									label="Tutte le sedi del tenant"
									>Tutte le sedi del tenant</Select.Item
								>
								{#each availableLocations as location (location.id)}
									<Select.Item value={location.id} label={location.name}
										>{location.name}</Select.Item
									>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
				</div>

				{#if activeCustomFields.length > 0}
					<div class="space-y-4 rounded-[18px] border border-border/70 p-4">
						<div>
							<p class="font-semibold">Campi personalizzati</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Compila qui i campi aggiuntivi richiesti dal tenant.
							</p>
						</div>
						<div class="grid gap-4 sm:grid-cols-2">
							{#each activeCustomFields as field (field.id)}
								<div class={`space-y-2 ${field.valueType === 'LongText' ? 'sm:col-span-2' : ''}`}>
									<span class="text-sm font-medium">
										{field.label}{field.isRequired ? ' *' : ''}
									</span>
									{#if field.valueType === 'LongText'}
										<Textarea
											class="min-h-24"
											value={formState.customFieldValues[field.id] ?? ''}
											oninput={(event) =>
												updateCreateCustomFieldValue(
													field.id,
													(event.currentTarget as HTMLTextAreaElement).value
												)}
										/>
									{:else if field.valueType === 'Boolean'}
										<label class="flex items-start gap-3 rounded-[16px] border border-border/70 p-3">
											<Checkbox
												class="mt-1"
												checked={formState.customFieldValues[field.id] === 'true'}
												onCheckedChange={(checked) =>
													updateCreateCustomFieldValue(field.id, checked ? 'true' : 'false')}
											/>
											<span class="text-sm text-muted-foreground">
												Attiva se il valore deve risultare positivo.
											</span>
										</label>
									{:else if field.valueType === 'Select'}
										<Select.Root
											type="single"
											value={formState.customFieldValues[field.id] || CLIENTS_EMPTY_CUSTOM_FIELD_SELECT_VALUE}
											onValueChange={(value) =>
												updateCreateCustomFieldValue(
													field.id,
													value === CLIENTS_EMPTY_CUSTOM_FIELD_SELECT_VALUE ? '' : value
												)}
										>
											<Select.Trigger class="w-full">
												<span data-slot="select-value">
													{formState.customFieldValues[field.id] || 'Seleziona un valore'}
												</span>
											</Select.Trigger>
											<Select.Content>
												<Select.Item
													value={CLIENTS_EMPTY_CUSTOM_FIELD_SELECT_VALUE}
													label="Seleziona un valore"
												>
													Seleziona un valore
												</Select.Item>
												{#each field.options as option}
													<Select.Item value={option} label={option}>{option}</Select.Item>
												{/each}
											</Select.Content>
										</Select.Root>
									{:else}
										<Input
											type={field.valueType === 'Number' ? 'number' : field.valueType === 'Date' ? 'date' : 'text'}
											value={formState.customFieldValues[field.id] ?? ''}
											oninput={(event) =>
												updateCreateCustomFieldValue(
													field.id,
													(event.currentTarget as HTMLInputElement).value
												)}
										/>
									{/if}
								</div>
							{/each}
						</div>
					</div>
				{/if}

				<label class="space-y-2">
					<span class="text-sm font-medium">Note operative</span>
					<Textarea
						class="min-h-28"
						bind:value={formState.notes}
						placeholder="Es. cliente interessato a prova mensile, richiamare entro venerdi."
					/>
				</label>

				<div
					class="rounded-[18px] border border-border/70 bg-secondary/20 p-4 text-sm text-muted-foreground"
				>
					<p class="font-semibold text-foreground">Stato iniziale</p>
					<p class="mt-2">
						Il cliente verra salvato come <strong>PendingClaim</strong> con origine
						<strong>StaffInvite</strong>, pronto per il completamento dell attivazione.
					</p>
				</div>

				{#if createError}
					<div
						class="rounded-[18px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]"
					>
						{createError}
					</div>
				{/if}
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button
					type="button"
					variant="outline"
					onclick={() => {
						createOpen = false;
						createError = '';
					}}
				>
					Annulla
				</Button>
				<Button type="submit" disabled={createSubmitting}>
					{#if createSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Creazione...
					{:else}
						<UserRoundPlusIcon class="size-4" />
						Crea cliente
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>
