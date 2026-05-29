<script lang="ts">
	import { goto } from '$app/navigation';
	import { createQuery } from '@tanstack/svelte-query';
	import Building2Icon from '@lucide/svelte/icons/building-2';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import ShieldCheckIcon from '@lucide/svelte/icons/shield-check';
	import UserCogIcon from '@lucide/svelte/icons/user-cog';
	import type {
		CreateRoleRequest,
		CreateGymLocationRequest,
		CreateStaffAssignmentRequest,
		GymAuthenticationPolicyResponse,
		GymLocationResponse,
		GymStaffAssignmentResponse,
		PermissionCatalogItemResponse,
		RoleResponse,
		TenantRoleAssignmentScopeType,
		TenantRoleAssignmentStatus,
		UpdateGymAuthenticationPolicyRequest
	} from '$lib/api';
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
		createGymCustomField,
		fetchGymCustomFields,
		type GymCustomFieldDefinition,
		type GymCustomFieldValueType,
		updateGymCustomField
	} from '$lib/data/custom-fields-api.js';
	import {
		getPermissionGroupLabel,
		getPermissionTranslation
	} from '$lib/i18n/permission-catalog.js';
	import { getAuthenticatedSession } from '$lib/utils/auth-session-storage.js';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
	type RoleSheetMode = 'create' | 'edit' | 'clone';
	type StaffSheetMode = 'create' | 'edit';
	type CustomFieldSheetMode = 'create' | 'edit';

	const api = useApiClient();
	const center = useCenterSelectionStore();
	const API_BASE_URL = 'http://localhost:5299';
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});

	const scopeOptions: Array<{ value: TenantRoleAssignmentScopeType; label: string }> = [
		{ value: 'Tenant', label: 'Tutto il tenant' },
		{ value: 'Location', label: 'Solo una sede' }
	];

	const statusOptions: Array<{ value: TenantRoleAssignmentStatus; label: string }> = [
		{ value: 'Active', label: 'Attivo' },
		{ value: 'Suspended', label: 'Sospeso' },
		{ value: 'Revoked', label: 'Revocato' }
	];
	const customFieldValueTypeOptions: Array<{ value: GymCustomFieldValueType; label: string }> = [
		{ value: 'Text', label: 'Testo breve' },
		{ value: 'LongText', label: 'Testo lungo' },
		{ value: 'Number', label: 'Numero' },
		{ value: 'Date', label: 'Data' },
		{ value: 'Boolean', label: 'Si / No' },
		{ value: 'Select', label: 'Scelta da elenco' }
	];

	let feedbackMessage = $state('');
	let feedbackError = $state('');
	let locationSheetOpen = $state(false);
	let staffSheetOpen = $state(false);
	let roleSheetOpen = $state(false);
	let customFieldSheetOpen = $state(false);
	let locationSubmitting = $state(false);
	let staffSubmitting = $state(false);
	let roleSubmitting = $state(false);
	let policySubmitting = $state(false);
	let customFieldSubmitting = $state(false);
	let selectedRoleId = $state<string | null>(null);
	let roleSheetMode = $state<RoleSheetMode>('create');
	let editingRoleId = $state<string | null>(null);
	let staffSheetMode = $state<StaffSheetMode>('create');
	let editingStaffAssignmentId = $state<string | null>(null);
	let customFieldSheetMode = $state<CustomFieldSheetMode>('create');
	let editingCustomFieldId = $state<string | null>(null);

	let locationForm = $state({
		name: '',
		code: '',
		addressLine1: '',
		city: '',
		countryCode: 'IT'
	});

	let staffForm = $state({
		email: '',
		roleId: '',
		scopeType: 'Tenant' as TenantRoleAssignmentScopeType,
		scopeLocationId: '',
		status: 'Active' as TenantRoleAssignmentStatus,
		displayName: '',
		jobTitle: '',
		internalCode: ''
	});

	let policyForm = $state({
		requireTwoFactorForStaff: false,
		requireTwoFactorForMembers: false
	});

	let roleForm = $state({
		name: '',
		description: '',
		selectedPermissionIds: [] as string[]
	});

	let customFieldForm = $state({
		key: '',
		label: '',
		description: '',
		valueType: 'Text' as GymCustomFieldValueType,
		optionsText: '',
		isRequired: false,
		isActive: true,
		sortOrder: 0
	});

	const locationsQuery = createQuery(() => ({
		queryKey: ['settings-locations', center.selectedGymId],
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

	const staffQuery = createQuery(() => ({
		queryKey: ['settings-staff', center.selectedGymId],
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
			return (response.data ?? []) as GymStaffAssignmentResponse[];
		}
	}));

	const rolesQuery = createQuery(() => ({
		queryKey: ['settings-roles', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdRolesGet({ gymId: center.selectedGymId! });
			if (!response.success) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile caricare i ruoli.'
				);
			}
			return (response.data ?? []) as RoleResponse[];
		}
	}));

	const permissionCatalogQuery = createQuery(() => ({
		queryKey: ['settings-permissions-catalog', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdPermissionsCatalogGet({
				gymId: center.selectedGymId!
			});
			if (!response.success) {
				throw new Error(
					response.error?.message ??
						response.message ??
						'Impossibile caricare il catalogo permessi.'
				);
			}
			return (response.data ?? []) as PermissionCatalogItemResponse[];
		}
	}));

	const policyQuery = createQuery(() => ({
		queryKey: ['settings-policy', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdSecurityAuthenticationPolicyGet({
				gymId: center.selectedGymId!
			});
			if (!response.success || !response.data) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile caricare la policy sicurezza.'
				);
			}
			return response.data as GymAuthenticationPolicyResponse;
		}
	}));
	const customFieldsQuery = createQuery(() => ({
		queryKey: ['settings-custom-fields', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => fetchGymCustomFields(center.selectedGymId!)
	}));

	const locations = $derived(locationsQuery.data ?? []);
	const staffAssignments = $derived(staffQuery.data ?? []);
	const roles = $derived(rolesQuery.data ?? []);
	const permissionCatalog = $derived(permissionCatalogQuery.data ?? []);
	const locationsError = $derived(
		locationsQuery.error instanceof Error
			? locationsQuery.error.message
			: locationsQuery.error
				? 'Impossibile caricare le sedi.'
				: null
	);
	const staffError = $derived(
		staffQuery.error instanceof Error
			? staffQuery.error.message
			: staffQuery.error
				? 'Impossibile caricare il team.'
				: null
	);
	const rolesError = $derived(
		rolesQuery.error instanceof Error
			? rolesQuery.error.message
			: rolesQuery.error
				? 'Impossibile caricare i ruoli.'
				: null
	);
	const permissionCatalogError = $derived(
		permissionCatalogQuery.error instanceof Error
			? permissionCatalogQuery.error.message
			: permissionCatalogQuery.error
				? 'Impossibile caricare il catalogo permessi.'
				: null
	);
	const policyError = $derived(
		policyQuery.error instanceof Error
			? policyQuery.error.message
			: policyQuery.error
				? 'Impossibile caricare la policy sicurezza.'
				: null
	);
	const customFieldsError = $derived(
		customFieldsQuery.error instanceof Error
			? customFieldsQuery.error.message
			: customFieldsQuery.error
				? 'Impossibile caricare i campi personalizzati.'
				: null
	);
	const customFields = $derived(
		(customFieldsQuery.data ?? []).slice().sort((left, right) => left.sortOrder - right.sortOrder)
	);
	const selectedRole = $derived(
		roles.find((role) => role.id === selectedRoleId) ?? roles[0] ?? null
	);
	const editingStaffAssignment = $derived(
		staffAssignments.find((assignment) => assignment.assignmentId === editingStaffAssignmentId) ??
			null
	);
	const customRolesCount = $derived(roles.filter((role) => !role.isDefault).length);
	const activeStaffCount = $derived(
		staffAssignments.filter((assignment) => assignment.status === 'Active').length
	);
	const securityEnabledCount = $derived(
		Number(policyForm.requireTwoFactorForStaff) + Number(policyForm.requireTwoFactorForMembers)
	);
	const activeCustomFieldsCount = $derived(customFields.filter((field) => field.isActive).length);
	const allowedPermissions = $derived(
		(selectedRole?.permissions ?? []).filter((permission) => permission.isAllowed)
	);
	const availableScopeLocations = $derived(
		locations.filter((location) => location.isActive !== false)
	);
	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms
			|| (!!center.selectedGymId
				&& (locationsQuery.isPending
					|| staffQuery.isPending
					|| rolesQuery.isPending
					|| permissionCatalogQuery.isPending
					|| policyQuery.isPending
					|| customFieldsQuery.isPending))
	);
	const workspaceError = $derived(
		center.gymsError
			?? locationsError
			?? staffError
			?? rolesError
			?? permissionCatalogError
			?? policyError
			?? customFieldsError
			?? null
	);
	const groupedPermissionCatalog = $derived.by(() => {
		const groups = new Map<
			string,
			{ resource: string; label: string; permissions: PermissionCatalogItemResponse[] }
		>();

		for (const permission of permissionCatalog) {
			const resource = permission.resource?.trim() || 'other';
			const key = resource.toLowerCase();
			const existing = groups.get(key);

			if (existing) {
				existing.permissions.push(permission);
				continue;
			}

			groups.set(key, {
				resource,
				label: getPermissionTranslation(permission).resourceLabel,
				permissions: [permission]
			});
		}

		return Array.from(groups.values()).map((group) => ({
			...group,
			permissions: [...group.permissions].sort((left, right) =>
				`${left.action ?? ''}`.localeCompare(`${right.action ?? ''}`, 'it')
			)
		}));
	});
	const roleSheetTitle = $derived(
		roleSheetMode === 'edit'
			? 'Modifica ruolo custom'
			: roleSheetMode === 'clone'
				? 'Clona ruolo'
				: 'Nuovo ruolo custom'
	);
	const roleSheetDescription = $derived(
		roleSheetMode === 'edit'
			? 'Aggiorna nome, descrizione e permessi del ruolo custom selezionato.'
			: roleSheetMode === 'clone'
				? 'Crea un nuovo ruolo partendo dalla configurazione del ruolo selezionato.'
				: 'Crea un ruolo operativo del tenant scegliendo esattamente i permessi da assegnare.'
	);
	const roleSubmitLabel = $derived(
		roleSheetMode === 'edit'
			? 'Salva modifiche'
			: roleSheetMode === 'clone'
				? 'Crea clone'
				: 'Crea ruolo'
	);
	const staffSheetTitle = $derived(
		staffSheetMode === 'edit' ? 'Modifica assegnazione staff' : 'Assegna staff'
	);
	const staffSheetDescription = $derived(
		staffSheetMode === 'edit'
			? 'Aggiorna ruolo, scope, stato e profilo operativo dell account gia assegnato.'
			: 'Collega un account Betterfit esistente a ruolo, scope e profilo operativo.'
	);
	const staffSubmitLabel = $derived(
		staffSheetMode === 'edit' ? 'Salva modifiche' : 'Salva assegnazione'
	);
	const customFieldSheetTitle = $derived(
		customFieldSheetMode === 'edit' ? 'Modifica campo personalizzato' : 'Nuovo campo personalizzato'
	);
	const customFieldSubmitLabel = $derived(
		customFieldSheetMode === 'edit' ? 'Salva campo' : 'Crea campo'
	);
	const selectedStaffRoleLabel = $derived(
		roles.find((role) => role.id === staffForm.roleId)?.name ?? 'Seleziona un ruolo'
	);
	const selectedStaffStatusLabel = $derived(
		statusOptions.find((option) => option.value === staffForm.status)?.label ?? 'Seleziona uno stato'
	);
	const selectedStaffScopeLabel = $derived(
		scopeOptions.find((option) => option.value === staffForm.scopeType)?.label ?? 'Seleziona uno scope'
	);
	const selectedStaffScopeLocationLabel = $derived(
		availableScopeLocations.find((location) => location.id === staffForm.scopeLocationId)?.name ??
			'Seleziona una sede'
	);
	const selectedCustomFieldTypeLabel = $derived(
		customFieldValueTypeOptions.find((option) => option.value === customFieldForm.valueType)?.label ??
			'Tipo campo'
	);

	$effect(() => {
		if (
			roles.length > 0 &&
			(!selectedRoleId || !roles.some((role) => role.id === selectedRoleId))
		) {
			selectedRoleId = roles[0].id ?? null;
		}
	});

	$effect(() => {
		if (policyQuery.data) {
			policyForm = {
				requireTwoFactorForStaff: policyQuery.data.requireTwoFactorForStaff ?? false,
				requireTwoFactorForMembers: policyQuery.data.requireTwoFactorForMembers ?? false
			};
		}
	});

	$effect(() => {
		if (!staffForm.roleId && roles.length > 0) {
			staffForm = {
				...staffForm,
				roleId: roles[0].id ?? ''
			};
		}
	});

	$effect(() => {
		if (
			staffForm.scopeType === 'Location' &&
			!staffForm.scopeLocationId &&
			availableScopeLocations.length > 0
		) {
			staffForm = {
				...staffForm,
				scopeLocationId: availableScopeLocations[0].id ?? ''
			};
		}
	});

	function clearFeedback() {
		feedbackMessage = '';
		feedbackError = '';
	}

	function resetLocationForm() {
		locationForm = {
			name: '',
			code: '',
			addressLine1: '',
			city: '',
			countryCode: 'IT'
		};
	}

	function resetStaffForm() {
		staffForm = {
			email: '',
			roleId: roles[0]?.id ?? '',
			scopeType: 'Tenant',
			scopeLocationId: '',
			status: 'Active',
			displayName: '',
			jobTitle: '',
			internalCode: ''
		};
	}

	function fillStaffFormFromAssignment(assignment: GymStaffAssignmentResponse) {
		staffForm = {
			email: assignment.userEmail || '',
			roleId: assignment.roleId || roles[0]?.id || '',
			scopeType: assignment.scopeType ?? 'Tenant',
			scopeLocationId: assignment.scopeLocationId || '',
			status: assignment.status ?? 'Active',
			displayName: assignment.staffProfile?.displayName || '',
			jobTitle: assignment.staffProfile?.jobTitle || '',
			internalCode: assignment.staffProfile?.internalCode || ''
		};
	}

	function resetRoleForm() {
		roleForm = {
			name: '',
			description: '',
			selectedPermissionIds: []
		};
	}

	function fillRoleFormFromRole(role: RoleResponse, mode: RoleSheetMode) {
		const selectedPermissionIds = (role.permissions ?? [])
			.filter((permission) => permission.isAllowed && permission.permissionId)
			.map((permission) => permission.permissionId!)
			.filter(Boolean);

		roleForm = {
			name: mode === 'clone' ? `${role.name || 'Ruolo'} copia` : role.name || '',
			description: role.description || '',
			selectedPermissionIds
		};
	}

	function openLocationSheet() {
		clearFeedback();
		resetLocationForm();
		locationSheetOpen = true;
	}

	function openStaffSheet() {
		clearFeedback();
		resetStaffForm();
		staffSheetMode = 'create';
		editingStaffAssignmentId = null;
		staffSheetOpen = true;
	}

	function openEditStaffSheet(assignment: GymStaffAssignmentResponse) {
		clearFeedback();
		staffSheetMode = 'edit';
		editingStaffAssignmentId = assignment.assignmentId ?? null;
		fillStaffFormFromAssignment(assignment);
		staffSheetOpen = true;
	}

	function openRoleSheet() {
		clearFeedback();
		resetRoleForm();
		roleSheetMode = 'create';
		editingRoleId = null;
		roleSheetOpen = true;
	}

	function openEditRoleSheet() {
		if (!selectedRole || selectedRole.isDefault) {
			return;
		}

		clearFeedback();
		roleSheetMode = 'edit';
		editingRoleId = selectedRole.id ?? null;
		fillRoleFormFromRole(selectedRole, 'edit');
		roleSheetOpen = true;
	}

	function openCloneRoleSheet() {
		if (!selectedRole) {
			return;
		}

		clearFeedback();
		roleSheetMode = 'clone';
		editingRoleId = null;
		fillRoleFormFromRole(selectedRole, 'clone');
		roleSheetOpen = true;
	}

	function scopeLabel(assignment: GymStaffAssignmentResponse) {
		if (assignment.scopeType === 'Tenant') {
			return 'Tutto il tenant';
		}

		return assignment.scopeLocationName?.trim() || 'Sede specifica';
	}

	function assignmentStatusMeta(status: TenantRoleAssignmentStatus | undefined): {
		label: string;
		variant: BadgeVariant;
	} {
		if (status === 'Suspended') return { label: 'Sospeso', variant: 'warning' };
		if (status === 'Revoked') return { label: 'Revocato', variant: 'outline' };
		return { label: 'Attivo', variant: 'success' };
	}

	function assignmentLooksProtected(assignment: GymStaffAssignmentResponse) {
		return (assignment.roleName || '').trim().toLowerCase() === 'owner';
	}

	function formatCustomFieldType(valueType: GymCustomFieldValueType) {
		return (
			customFieldValueTypeOptions.find((option) => option.value === valueType)?.label ?? valueType
		);
	}

	function permissionSelected(permissionId: string | undefined) {
		return !!permissionId && roleForm.selectedPermissionIds.includes(permissionId);
	}

	function resetCustomFieldForm() {
		customFieldForm = {
			key: '',
			label: '',
			description: '',
			valueType: 'Text',
			optionsText: '',
			isRequired: false,
			isActive: true,
			sortOrder: customFields.length
		};
	}

	function buildDefaultCustomFieldKey(label: string) {
		return label
			.trim()
			.toLowerCase()
			.normalize('NFD')
			.replaceAll(/[\u0300-\u036f]/g, '')
			.replaceAll(/[^a-z0-9]+/g, '_')
			.replaceAll(/^_+|_+$/g, '')
			.slice(0, 64);
	}

	function optionsToText(options: string[]) {
		return options.join('\n');
	}

	function parseCustomFieldOptions() {
		return customFieldForm.optionsText
			.split(/\r?\n|,/)
			.map((option) => option.trim())
			.filter(Boolean);
	}

	function openCustomFieldSheet() {
		clearFeedback();
		customFieldSheetMode = 'create';
		editingCustomFieldId = null;
		resetCustomFieldForm();
		customFieldSheetOpen = true;
	}

	function openEditCustomFieldSheet(field: GymCustomFieldDefinition) {
		clearFeedback();
		customFieldSheetMode = 'edit';
		editingCustomFieldId = field.id;
		customFieldForm = {
			key: field.key,
			label: field.label,
			description: field.description ?? '',
			valueType: field.valueType,
			optionsText: optionsToText(field.options),
			isRequired: field.isRequired,
			isActive: field.isActive,
			sortOrder: field.sortOrder
		};
		customFieldSheetOpen = true;
	}

	function toggleRolePermission(permissionId: string | undefined, enabled: boolean) {
		if (!permissionId) {
			return;
		}

		const selected = new Set(roleForm.selectedPermissionIds);
		if (enabled) {
			selected.add(permissionId);
		} else {
			selected.delete(permissionId);
		}

		roleForm = {
			...roleForm,
			selectedPermissionIds: Array.from(selected)
		};
	}

	function buildRolePayload() {
		return {
			name: roleForm.name.trim(),
			description: roleForm.description.trim() || null,
			permissions: roleForm.selectedPermissionIds.map((permissionId) => ({
				permissionId,
				isAllowed: true
			}))
		} satisfies CreateRoleRequest;
	}

	async function updateRoleRequest(gymId: string, roleId: string, payload: CreateRoleRequest) {
		const response = await fetch(`${API_BASE_URL}/api/gyms/${gymId}/roles/${roleId}`, {
			method: 'PUT',
			headers: {
				'Content-Type': 'application/json',
				Authorization: `Bearer ${getAuthenticatedSession()?.token ?? ''}`
			},
			body: JSON.stringify(payload)
		});

		const json = await response.json();
		if (!response.ok || !json.success || !json.data) {
			throw new Error(json.error?.message ?? json.message ?? 'Impossibile aggiornare il ruolo.');
		}

		return json.data as RoleResponse;
	}

	async function updateStaffAssignmentRequest(
		gymId: string,
		assignmentId: string,
		payload: {
			roleId: string;
			scopeType: TenantRoleAssignmentScopeType;
			scopeLocationId: string | null;
			status: TenantRoleAssignmentStatus;
			profile: {
				displayName: string | null;
				jobTitle: string | null;
				internalCode: string | null;
			};
		}
	) {
		const response = await fetch(
			`${API_BASE_URL}/api/gyms/${gymId}/staff-assignments/${assignmentId}`,
			{
				method: 'PUT',
				headers: {
					'Content-Type': 'application/json',
					Authorization: `Bearer ${getAuthenticatedSession()?.token ?? ''}`
				},
				body: JSON.stringify(payload)
			}
		);

		const json = await response.json();
		if (!response.ok || !json.success || !json.data) {
			throw new Error(
				json.error?.message ?? json.message ?? 'Impossibile aggiornare l assegnazione staff.'
			);
		}

		return json.data as GymStaffAssignmentResponse;
	}

	async function refreshAll() {
		if (!center.selectedGymId) {
			return;
		}

		await Promise.all([
			locationsQuery.refetch(),
			staffQuery.refetch(),
			rolesQuery.refetch(),
			permissionCatalogQuery.refetch(),
			policyQuery.refetch(),
			customFieldsQuery.refetch()
		]);
	}

	async function handleCreateLocation(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !locationForm.name.trim()) {
			feedbackError = 'Inserisci almeno il nome della sede.';
			return;
		}

		locationSubmitting = true;
		try {
			const response = await api.client.apiGymsGymIdLocationsPost({
				gymId: center.selectedGymId,
				createGymLocationRequest: {
					name: locationForm.name.trim(),
					code: locationForm.code.trim() || null,
					addressLine1: locationForm.addressLine1.trim() || null,
					city: locationForm.city.trim() || null,
					countryCode: locationForm.countryCode.trim().toUpperCase() || null
				} satisfies CreateGymLocationRequest
			});

			if (!response.success || !response.data) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile creare la sede.'
				);
			}

			await Promise.all([locationsQuery.refetch(), center.refetchGyms()]);
			locationSheetOpen = false;
			feedbackMessage = `Sede ${response.data.name ?? locationForm.name.trim()} creata con successo.`;
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile creare la sede.';
		} finally {
			locationSubmitting = false;
		}
	}

	async function handleSaveStaffAssignment(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !staffForm.roleId) {
			feedbackError = 'Inserisci almeno ruolo e palestra di riferimento.';
			return;
		}

		if (staffSheetMode === 'create' && !staffForm.email.trim()) {
			feedbackError = 'Inserisci email account Betterfit e ruolo.';
			return;
		}

		if (staffForm.scopeType === 'Location' && !staffForm.scopeLocationId) {
			feedbackError = 'Se scegli scope per sede devi selezionare una location.';
			return;
		}

		staffSubmitting = true;
		try {
			const payload = {
				roleId: staffForm.roleId,
				scopeType: staffForm.scopeType,
				scopeLocationId:
					staffForm.scopeType === 'Location' ? staffForm.scopeLocationId || null : null,
				status: staffForm.status,
				profile: {
					displayName: staffForm.displayName.trim() || null,
					jobTitle: staffForm.jobTitle.trim() || null,
					internalCode: staffForm.internalCode.trim() || null
				}
			};

			if (staffSheetMode === 'edit') {
				if (!editingStaffAssignmentId) {
					throw new Error('Assegnazione staff da modificare non disponibile.');
				}

				const updatedAssignment = await updateStaffAssignmentRequest(
					center.selectedGymId,
					editingStaffAssignmentId,
					payload
				);
				await staffQuery.refetch();
				staffSheetOpen = false;
				feedbackMessage = `Assegnazione aggiornata per ${updatedAssignment.userEmail || staffForm.email.trim()}.`;
			} else {
				const response = await api.client.apiGymsGymIdStaffAssignmentsPost({
					gymId: center.selectedGymId,
					createStaffAssignmentRequest: {
						email: staffForm.email.trim(),
						...payload
					} satisfies CreateStaffAssignmentRequest
				});

				if (!response.success || !response.data) {
					throw new Error(
						response.error?.message ??
							response.message ??
							'Impossibile salvare l assegnazione staff.'
					);
				}

				await staffQuery.refetch();
				staffSheetOpen = false;
				feedbackMessage = `Assegnazione creata per ${response.data.userEmail ?? staffForm.email.trim()}.`;
			}
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error
					? error.message
					: staffSheetMode === 'edit'
						? 'Impossibile aggiornare l assegnazione staff.'
						: 'Impossibile salvare l assegnazione staff.';
		} finally {
			staffSubmitting = false;
		}
	}

	async function handleSavePolicy() {
		clearFeedback();

		if (!center.selectedGymId) {
			feedbackError = 'Seleziona una palestra prima di aggiornare la policy.';
			return;
		}

		policySubmitting = true;
		try {
			const response = await api.client.apiGymsGymIdSecurityAuthenticationPolicyPut({
				gymId: center.selectedGymId,
				updateGymAuthenticationPolicyRequest: {
					requireTwoFactorForStaff: policyForm.requireTwoFactorForStaff,
					requireTwoFactorForMembers: policyForm.requireTwoFactorForMembers
				} satisfies UpdateGymAuthenticationPolicyRequest
			});

			if (!response.success || !response.data) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile aggiornare la policy.'
				);
			}

			await policyQuery.refetch();
			feedbackMessage = 'Policy sicurezza aggiornata con successo.';
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile aggiornare la policy.';
		} finally {
			policySubmitting = false;
		}
	}

	async function handleCreateRole(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId) {
			feedbackError = 'Seleziona una palestra prima di creare un ruolo.';
			return;
		}

		if (!roleForm.name.trim()) {
			feedbackError = 'Inserisci il nome del ruolo.';
			return;
		}

		if (roleForm.selectedPermissionIds.length === 0) {
			feedbackError = 'Seleziona almeno un permesso per il nuovo ruolo.';
			return;
		}

		roleSubmitting = true;
		try {
			const payload = buildRolePayload();

			if (roleSheetMode === 'edit') {
				if (!editingRoleId) {
					throw new Error('Ruolo da modificare non disponibile.');
				}

				const updatedRole = await updateRoleRequest(center.selectedGymId, editingRoleId, payload);
				await rolesQuery.refetch();
				selectedRoleId = updatedRole.id ?? editingRoleId;
				roleSheetOpen = false;
				feedbackMessage = `Ruolo ${updatedRole.name ?? roleForm.name.trim()} aggiornato con successo.`;
			} else {
				const response = await api.client.apiGymsGymIdRolesPost({
					gymId: center.selectedGymId,
					createRoleRequest: payload
				});

				if (!response.success || !response.data) {
					throw new Error(
						response.error?.message ?? response.message ?? 'Impossibile creare il ruolo.'
					);
				}

				await rolesQuery.refetch();
				selectedRoleId = response.data.id ?? null;
				roleSheetOpen = false;
				feedbackMessage =
					roleSheetMode === 'clone'
						? `Clone creato per ${response.data.name ?? roleForm.name.trim()}.`
						: `Ruolo ${response.data.name ?? roleForm.name.trim()} creato con successo.`;
			}
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error
					? error.message
					: roleSheetMode === 'edit'
						? 'Impossibile aggiornare il ruolo.'
						: 'Impossibile creare il ruolo.';
		} finally {
			roleSubmitting = false;
		}
	}

	async function handleSaveCustomField(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId) {
			feedbackError = 'Seleziona un tenant prima di salvare un campo personalizzato.';
			return;
		}

		if (!customFieldForm.label.trim()) {
			feedbackError = 'Inserisci l etichetta del campo.';
			return;
		}

		const key = (customFieldForm.key.trim() || buildDefaultCustomFieldKey(customFieldForm.label)).trim();
		if (!key) {
			feedbackError = 'Inserisci una chiave valida per il campo.';
			return;
		}

		const options = customFieldForm.valueType === 'Select' ? parseCustomFieldOptions() : [];
		if (customFieldForm.valueType === 'Select' && options.length === 0) {
			feedbackError = 'Per un campo a scelta devi indicare almeno un opzione.';
			return;
		}

		customFieldSubmitting = true;
		try {
			const payload = {
				key,
				label: customFieldForm.label.trim(),
				description: customFieldForm.description.trim() || null,
				valueType: customFieldForm.valueType,
				options,
				isRequired: customFieldForm.isRequired,
				isActive: customFieldForm.isActive,
				sortOrder: Number(customFieldForm.sortOrder) || 0
			};

			if (customFieldSheetMode === 'edit') {
				if (!editingCustomFieldId) {
					throw new Error('Campo personalizzato non disponibile.');
				}

				const updatedField = await updateGymCustomField(
					center.selectedGymId,
					editingCustomFieldId,
					payload
				);
				await customFieldsQuery.refetch();
				customFieldSheetOpen = false;
				feedbackMessage = `Campo ${updatedField.label} aggiornato con successo.`;
			} else {
				const createdField = await createGymCustomField(center.selectedGymId, payload);
				await customFieldsQuery.refetch();
				customFieldSheetOpen = false;
				feedbackMessage = `Campo ${createdField.label} creato con successo.`;
			}
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile salvare il campo personalizzato.';
		} finally {
			customFieldSubmitting = false;
		}
	}

	async function handleToggleCustomFieldActive(field: GymCustomFieldDefinition) {
		if (!center.selectedGymId) {
			feedbackError = 'Seleziona un tenant prima di aggiornare il campo.';
			return;
		}

		clearFeedback();
		try {
			await updateGymCustomField(center.selectedGymId, field.id, {
				key: field.key,
				label: field.label,
				description: field.description,
				valueType: field.valueType,
				options: field.options,
				isRequired: field.isRequired,
				isActive: !field.isActive,
				sortOrder: field.sortOrder
			});
			await customFieldsQuery.refetch();
			feedbackMessage = field.isActive
				? `Campo ${field.label} disattivato.`
				: `Campo ${field.label} riattivato.`;
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile aggiornare lo stato del campo.';
		}
	}
</script>

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	<section class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
		<div class="space-y-2">
			<div class="flex flex-wrap items-center gap-2">
				<Badge variant="secondary" class="rounded-full px-3 py-1">Configurazione operativa</Badge>
				{#if center.selectedGym}
					<Badge variant="outline" class="rounded-full px-3 py-1">{center.selectedGym.name}</Badge>
				{/if}
			</div>
			<div>
				<h2 class="text-2xl font-semibold tracking-tight">Sedi, team, ruoli e sicurezza</h2>
				<p class="text-sm text-muted-foreground">
					Impostazioni base operative per far girare Betterfit in una palestra vera.
				</p>
			</div>
		</div>
		<div class="flex flex-wrap items-center gap-2">
			<Button variant="outline" size="sm" onclick={() => goto('/settings/integrations')}>
				<ShieldCheckIcon class="size-4" />
				Integrazioni
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
			<Button
				variant="outline"
				size="sm"
				onclick={openLocationSheet}
				disabled={!hasSelectedGym || workspaceLoading}
			>
				<Building2Icon class="size-4" />
				Nuova sede
			</Button>
			<Button
				variant="outline"
				size="sm"
				onclick={openRoleSheet}
				disabled={!hasSelectedGym || workspaceLoading}
			>
				<ShieldCheckIcon class="size-4" />
				Nuovo ruolo
			</Button>
			<Button size="sm" onclick={openStaffSheet} disabled={!hasSelectedGym || workspaceLoading}>
				<UserCogIcon class="size-4" />
				Assegna staff
			</Button>
		</div>
	</section>

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
				<CardTitle>Caricamento configurazione tenant</CardTitle>
				<CardDescription>
					Sto recuperando sedi, team, ruoli, policy e campi personalizzati prima di mostrarti la
					configurazione reale.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-dashed border-[#fecaca] bg-[#fff7f7]">
			<CardHeader>
				<CardTitle>Impossibile caricare le impostazioni</CardTitle>
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
					Scegli prima la palestra dal selettore in alto a sinistra per vedere sedi, team, ruoli e
					policy realmente collegate al tenant.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
		<section class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
			<Card>
				<CardHeader class="pb-2">
					<CardDescription>Sedi visibili</CardDescription>
					<CardTitle class="text-3xl">{locations.length}</CardTitle>
				</CardHeader>
				<CardContent class="text-sm text-muted-foreground"
					>Anagrafica filiali e desk operativi.</CardContent
				>
			</Card>
			<Card>
				<CardHeader class="pb-2">
					<CardDescription>Staff attivo</CardDescription>
					<CardTitle class="text-3xl">{activeStaffCount}</CardTitle>
				</CardHeader>
				<CardContent class="text-sm text-muted-foreground"
					>Operatori e coach attivi nel tenant.</CardContent
				>
			</Card>
			<Card>
				<CardHeader class="pb-2">
					<CardDescription>Ruoli custom</CardDescription>
					<CardTitle class="text-3xl">{customRolesCount}</CardTitle>
				</CardHeader>
				<CardContent class="text-sm text-muted-foreground"
					>Ruoli non di default disponibili per il team.</CardContent
				>
			</Card>
			<Card>
				<CardHeader class="pb-2">
					<CardDescription>Campi personalizzati attivi</CardDescription>
					<CardTitle class="text-3xl">{activeCustomFieldsCount}</CardTitle>
				</CardHeader>
				<CardContent class="text-sm text-muted-foreground"
					>Campi extra usabili nella scheda cliente.</CardContent
				>
			</Card>
		</section>

		<section class="grid gap-4 xl:grid-cols-[0.95fr_1.05fr]">
			<Card id="locations">
				<CardHeader class="gap-4">
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>Sedi</CardTitle>
							<CardDescription>
								Elenco sedi attive per desk, training, corsi e vendite.
							</CardDescription>
						</div>
						<Badge variant="outline">{locations.length} sedi</Badge>
					</div>
				</CardHeader>
				<CardContent class="space-y-3">
					{#if locations.length === 0}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessuna sede configurata</p>
							<p class="mt-2 text-sm text-muted-foreground">
								Crea almeno una location reale per lavorare con membri e team.
							</p>
						</div>
					{:else}
						{#each locations as location (location.id)}
							<div class="rounded-[18px] border border-border/70 p-4">
								<div class="flex items-start justify-between gap-3">
									<div>
										<p class="font-semibold">{location.name}</p>
										<p class="mt-1 text-sm text-muted-foreground">
											{location.city || 'Citta non impostata'} - {location.countryCode ||
												'Nazione n.d.'}
										</p>
									</div>
									<Badge variant={location.isActive === false ? 'outline' : 'success'}>
										{location.isActive === false ? 'Inattiva' : 'Attiva'}
									</Badge>
								</div>
								<div class="mt-3 space-y-1 text-sm text-muted-foreground">
									<p>Codice: {location.code || 'Non assegnato'}</p>
									<p>Indirizzo: {location.addressLine1 || 'Non impostato'}</p>
									<p>
										Creata: {location.createdAtUtc
											? dateTime.format(location.createdAtUtc)
											: 'Data non disponibile'}
									</p>
								</div>
							</div>
						{/each}
					{/if}
				</CardContent>
			</Card>

			<Card id="team">
				<CardHeader class="gap-4">
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>Team e assegnazioni</CardTitle>
							<CardDescription>
								Account staff collegati a ruoli e scope reali del tenant.
							</CardDescription>
						</div>
						<Badge variant="outline">{staffAssignments.length} assegnazioni</Badge>
					</div>
				</CardHeader>
				<CardContent class="space-y-3">
					{#if staffAssignments.length === 0}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessuna assegnazione staff</p>
							<p class="mt-2 text-sm text-muted-foreground">
								Collega operatori e coach esistenti a ruoli e scope tenant.
							</p>
						</div>
					{:else}
						{#each staffAssignments as assignment (assignment.assignmentId)}
							<div class="rounded-[18px] border border-border/70 p-4">
								<div class="flex items-start justify-between gap-3">
									<div>
										<div class="flex flex-wrap items-center gap-2">
											<p class="font-semibold">
												{assignment.staffProfile?.displayName ||
													assignment.userEmail ||
													'Staff account'}
											</p>
											{#if assignmentLooksProtected(assignment)}
												<Badge variant="secondary">Owner</Badge>
											{/if}
										</div>
										<p class="mt-1 text-sm text-muted-foreground">
											{assignment.roleName || 'Ruolo non disponibile'} - {scopeLabel(assignment)}
										</p>
									</div>
									<div class="flex items-center gap-2">
										<Badge variant={assignmentStatusMeta(assignment.status).variant}>
											{assignmentStatusMeta(assignment.status).label}
										</Badge>
										<Button
											variant="outline"
											size="sm"
											onclick={() => openEditStaffSheet(assignment)}
										>
											Modifica
										</Button>
									</div>
								</div>
								<div class="mt-3 space-y-1 text-sm text-muted-foreground">
									<p>Email: {assignment.userEmail || 'Non disponibile'}</p>
									<p>Job title: {assignment.staffProfile?.jobTitle || 'Non impostato'}</p>
									<p>Codice interno: {assignment.staffProfile?.internalCode || 'Non impostato'}</p>
									<p>
										Assegnato:
										{assignment.grantedAtUtc
											? dateTime.format(assignment.grantedAtUtc)
											: 'Data non disponibile'}
									</p>
									{#if assignment.revokedAtUtc}
										<p>Revocato: {dateTime.format(assignment.revokedAtUtc)}</p>
									{/if}
								</div>
							</div>
						{/each}
					{/if}
				</CardContent>
			</Card>
		</section>

		<section class="grid gap-4 xl:grid-cols-[0.9fr_1.1fr]">
		<Card id="security">
			<CardHeader>
				<CardTitle>Sicurezza tenant</CardTitle>
				<CardDescription>
					Policy di autenticazione applicate a staff e membri del tenant corrente.
				</CardDescription>
			</CardHeader>
			<CardContent>
				{#if policyQuery.isPending}
					<div
						class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
					>
						<p class="font-semibold">Carico la policy sicurezza...</p>
					</div>
				{:else}
					<div class="space-y-4">
						<label class="flex items-start gap-3 rounded-[18px] border border-border/70 p-4">
							<Checkbox class="mt-1" bind:checked={policyForm.requireTwoFactorForStaff} />
							<div>
								<p class="font-semibold">Richiedi 2FA allo staff</p>
								<p class="mt-1 text-sm text-muted-foreground">
									Owner, manager e operatori devono completare il secondo fattore.
								</p>
							</div>
						</label>
						<label class="flex items-start gap-3 rounded-[18px] border border-border/70 p-4">
							<Checkbox class="mt-1" bind:checked={policyForm.requireTwoFactorForMembers} />
							<div>
								<p class="font-semibold">Richiedi 2FA ai membri</p>
								<p class="mt-1 text-sm text-muted-foreground">
									Attiva un controllo piu forte sugli account clienti Betterfit.
								</p>
							</div>
						</label>
						<div class="rounded-[18px] border border-border/70 p-4 text-sm text-muted-foreground">
							<p>
								Ultimo aggiornamento:
								{policyQuery.data?.updatedAtUtc
									? dateTime.format(policyQuery.data.updatedAtUtc)
									: 'Non disponibile'}
							</p>
						</div>
						<Button onclick={handleSavePolicy} disabled={policySubmitting}>
							{#if policySubmitting}
								<RefreshCwIcon class="size-4 animate-spin" />
								Salvataggio...
							{:else}
								<ShieldCheckIcon class="size-4" />
								Salva policy
							{/if}
						</Button>
					</div>
				{/if}
			</CardContent>
		</Card>

		<Card id="custom-fields">
			<CardHeader class="gap-4">
				<div class="flex items-start justify-between gap-3">
					<div>
						<CardTitle>Campi personalizzati clienti</CardTitle>
						<CardDescription>
							Il tenant puo aggiungere campi specifici da usare nella scheda cliente.
						</CardDescription>
					</div>
					<Badge variant="outline">{customFields.length} campi</Badge>
				</div>
				<div class="flex flex-wrap gap-2">
					<Button variant="outline" size="sm" onclick={openCustomFieldSheet}>
						Nuovo campo
					</Button>
				</div>
			</CardHeader>
			<CardContent class="space-y-3">
				{#if customFieldsQuery.isPending}
					<div
						class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
					>
						<p class="font-semibold">Carico i campi personalizzati...</p>
					</div>
				{:else if customFields.length === 0}
					<div
						class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
					>
						<p class="font-semibold">Nessun campo personalizzato</p>
						<p class="mt-2 text-sm text-muted-foreground">
							Aggiungi campi come obiettivo, scadenza certificato o provenienza lead.
						</p>
					</div>
				{:else}
					{#each customFields as field (field.id)}
						<div class="rounded-[18px] border border-border/70 p-4">
							<div class="flex flex-wrap items-start justify-between gap-3">
								<div class="space-y-2">
									<div class="flex flex-wrap items-center gap-2">
										<p class="font-semibold">{field.label}</p>
										<Badge variant={field.isActive ? 'success' : 'outline'}>
											{field.isActive ? 'Attivo' : 'Inattivo'}
										</Badge>
										{#if field.isRequired}
											<Badge variant="secondary">Obbligatorio</Badge>
										{/if}
										<Badge variant="outline">{formatCustomFieldType(field.valueType)}</Badge>
									</div>
									<p class="text-sm text-muted-foreground">Chiave: {field.key}</p>
									{#if field.description}
										<p class="text-sm text-muted-foreground">{field.description}</p>
									{/if}
									{#if field.options.length > 0}
										<p class="text-sm text-muted-foreground">
											Opzioni: {field.options.join(', ')}
										</p>
									{/if}
								</div>
								<div class="flex flex-wrap gap-2">
									<Button variant="outline" size="sm" onclick={() => openEditCustomFieldSheet(field)}>
										Modifica
									</Button>
									<Button
										variant="outline"
										size="sm"
										onclick={() => handleToggleCustomFieldActive(field)}
									>
										{field.isActive ? 'Disattiva' : 'Riattiva'}
									</Button>
								</div>
							</div>
						</div>
					{/each}
				{/if}
			</CardContent>
		</Card>
		</section>

		<section class="grid gap-4">
		<Card id="roles">
			<CardHeader class="gap-4">
				<div class="flex items-start justify-between gap-3">
					<div>
						<CardTitle>Ruoli e permessi</CardTitle>
						<CardDescription>
							Catalogo ruoli del tenant da usare per il team settings e operativo.
						</CardDescription>
					</div>
					<Badge variant="outline">{roles.length} ruoli</Badge>
				</div>
				<div class="flex flex-wrap gap-2">
					{#each roles as role (role.id)}
						<Button
							variant={selectedRole?.id === role.id ? 'default' : 'outline'}
							size="sm"
							onclick={() => (selectedRoleId = role.id ?? null)}
						>
							{role.name || 'Ruolo'}
						</Button>
					{/each}
					<Button variant="outline" size="sm" onclick={openRoleSheet}>Nuovo ruolo custom</Button>
					{#if selectedRole}
						<Button variant="outline" size="sm" onclick={openCloneRoleSheet}>Clona ruolo</Button>
					{/if}
					{#if selectedRole && !selectedRole.isDefault}
						<Button variant="outline" size="sm" onclick={openEditRoleSheet}>Modifica ruolo</Button>
					{/if}
				</div>
			</CardHeader>
			<CardContent>
				{#if rolesQuery.isPending}
					<div
						class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
					>
						<p class="font-semibold">Carico i ruoli...</p>
					</div>
				{:else if !selectedRole}
					<div
						class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
					>
						<p class="font-semibold">Nessun ruolo disponibile</p>
					</div>
				{:else}
					<div class="space-y-4">
						<div class="rounded-[18px] border border-border/70 p-4">
							<div class="flex items-start justify-between gap-3">
								<div>
									<p class="font-semibold">{selectedRole.name || 'Ruolo'}</p>
									<p class="mt-1 text-sm text-muted-foreground">
										{selectedRole.description || 'Ruolo operativo senza descrizione aggiuntiva.'}
									</p>
								</div>
								<Badge variant={selectedRole.isDefault ? 'secondary' : 'outline'}>
									{selectedRole.isDefault ? 'Default' : 'Custom'}
								</Badge>
							</div>
						</div>

						<div class="grid gap-3 sm:grid-cols-2">
							<div class="rounded-[18px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
									Permessi attivi
								</p>
								<p class="mt-2 text-lg font-semibold">{allowedPermissions.length}</p>
							</div>
							<div class="rounded-[18px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
									Totale permessi
								</p>
								<p class="mt-2 text-lg font-semibold">{selectedRole.permissions?.length ?? 0}</p>
							</div>
						</div>

						<div class="space-y-2 rounded-[18px] border border-border/70 p-4">
							<p class="font-semibold">Permessi concessi</p>
							{#if allowedPermissions.length === 0}
								<p class="text-sm text-muted-foreground">Nessun permesso attivo su questo ruolo.</p>
							{:else}
								{#each allowedPermissions as permission, index (`${permission.resource}-${permission.action}-${index}`)}
									{@const translation = getPermissionTranslation(permission)}
									<div
										class="rounded-[14px] border border-border/60 bg-secondary/15 px-3 py-2 text-sm"
									>
										<p class="font-medium">{translation.title}</p>
										<p class="mt-1 text-muted-foreground">{translation.description}</p>
									</div>
								{/each}
							{/if}
						</div>
					</div>
				{/if}
			</CardContent>
		</Card>
		</section>
	{/if}
</main>

<Sheet.Root bind:open={locationSheetOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Nuova sede</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				Configura una filiale reale da usare in utenti, accessi, vendite e training.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateLocation}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2 md:col-span-2">
						<span class="text-sm font-medium">Nome sede</span>
						<Input bind:value={locationForm.name} placeholder="Betterfit Milano Centro" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Codice</span>
						<Input bind:value={locationForm.code} placeholder="MI-CENTRO" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Paese</span>
						<Input bind:value={locationForm.countryCode} placeholder="IT" maxlength={2} />
					</label>
					<label class="space-y-2 md:col-span-2">
						<span class="text-sm font-medium">Indirizzo</span>
						<Input bind:value={locationForm.addressLine1} placeholder="Via Roma 20" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Citta</span>
						<Input bind:value={locationForm.city} placeholder="Milano" />
					</label>
				</div>
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (locationSheetOpen = false)}
					>Annulla</Button
				>
				<Button type="submit" disabled={locationSubmitting}>
					{#if locationSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<Building2Icon class="size-4" />
						Salva sede
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={staffSheetOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-2xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">{staffSheetTitle}</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				{staffSheetDescription}
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleSaveStaffAssignment}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2 md:col-span-2">
						<span class="text-sm font-medium">Email account Betterfit</span>
						<Input
							bind:value={staffForm.email}
							placeholder="coach@betterfit.local"
							disabled={staffSheetMode === 'edit'}
						/>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Ruolo</span>
						<Select.Root type="single" bind:value={staffForm.roleId}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{selectedStaffRoleLabel}</span>
							</Select.Trigger>
							<Select.Content>
								{#each roles as role (role.id)}
									{#if role.id}
										<Select.Item value={role.id} label={role.name || 'Ruolo'}>
											{role.name || 'Ruolo'}
										</Select.Item>
									{/if}
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Stato</span>
						<Select.Root type="single" bind:value={staffForm.status}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{selectedStaffStatusLabel}</span>
							</Select.Trigger>
							<Select.Content>
								{#each statusOptions as option (option.value)}
									<Select.Item value={option.value} label={option.label}>
										{option.label}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Scope</span>
						<Select.Root type="single" bind:value={staffForm.scopeType}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{selectedStaffScopeLabel}</span>
							</Select.Trigger>
							<Select.Content>
								{#each scopeOptions as option (option.value)}
									<Select.Item value={option.value} label={option.label}>
										{option.label}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
					{#if staffForm.scopeType === 'Location'}
						<label class="space-y-2">
							<span class="text-sm font-medium">Sede scope</span>
							<Select.Root type="single" bind:value={staffForm.scopeLocationId}>
								<Select.Trigger class="w-full">
									<span data-slot="select-value">{selectedStaffScopeLocationLabel}</span>
								</Select.Trigger>
								<Select.Content>
									{#each availableScopeLocations as location (location.id)}
										{#if location.id}
											<Select.Item value={location.id} label={location.name ?? 'Sede'}>
												{location.name ?? 'Sede'}
											</Select.Item>
										{/if}
									{/each}
								</Select.Content>
							</Select.Root>
						</label>
					{/if}
					<label class="space-y-2">
						<span class="text-sm font-medium">Display name</span>
						<Input bind:value={staffForm.displayName} placeholder="Marco Rossi" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Job title</span>
						<Input bind:value={staffForm.jobTitle} placeholder="Personal trainer" />
					</label>
					<label class="space-y-2 md:col-span-2">
						<span class="text-sm font-medium">Codice interno</span>
						<Input bind:value={staffForm.internalCode} placeholder="PT-014" />
					</label>
				</div>

				<div class="rounded-[18px] border border-border/70 p-4 text-sm text-muted-foreground">
					{#if staffSheetMode === 'edit'}
						L account Betterfit non cambia da qui: puoi aggiornare ruolo, stato, scope e profilo
						operativo del membro staff.
					{:else}
						Servono account Betterfit gia esistenti: il form non crea un nuovo utente globale, ma
						assegna un account al tenant.
					{/if}
				</div>
				{#if staffSheetMode === 'edit' && editingStaffAssignment && assignmentLooksProtected(editingStaffAssignment)}
					<div
						class="rounded-[18px] border border-border/70 bg-secondary/20 p-4 text-sm text-muted-foreground"
					>
						Le assegnazioni owner hanno vincoli protetti: devono restare attive e con scope tenant.
					</div>
				{/if}
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (staffSheetOpen = false)}
					>Annulla</Button
				>
				<Button type="submit" disabled={staffSubmitting}>
					{#if staffSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<UserCogIcon class="size-4" />
						{staffSubmitLabel}
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={roleSheetOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-3xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">{roleSheetTitle}</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				{roleSheetDescription}
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateRole}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4">
					<label class="space-y-2">
						<span class="text-sm font-medium">Nome ruolo</span>
						<Input
							bind:value={roleForm.name}
							placeholder="Sales manager, Front desk senior, Marketing locale"
						/>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Descrizione</span>
						<Input
							bind:value={roleForm.description}
							placeholder="Ruolo operativo per il team commerciale della sede"
						/>
					</label>
				</div>

				<div class="rounded-[18px] border border-border/70 p-4">
					<div class="flex flex-wrap items-center justify-between gap-3">
						<div>
							<p class="font-semibold">Permessi selezionati</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Scegli solo i permessi necessari al lavoro reale di questo ruolo.
							</p>
						</div>
						<Badge variant="secondary">{roleForm.selectedPermissionIds.length} attivi</Badge>
					</div>
				</div>

				{#if permissionCatalogQuery.isPending}
					<div
						class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
					>
						<p class="font-semibold">Carico il catalogo permessi...</p>
					</div>
				{:else if groupedPermissionCatalog.length === 0}
					<div
						class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
					>
						<p class="font-semibold">Nessun permesso disponibile</p>
					</div>
				{:else}
					<div class="space-y-4">
						{#each groupedPermissionCatalog as group (group.resource)}
							<section class="rounded-[18px] border border-border/70 p-4">
								<div class="mb-4 flex items-center justify-between gap-3">
									<div>
										<p class="font-semibold">{group.label}</p>
										<p class="mt-1 text-sm text-muted-foreground">
											Permessi operativi dell area {getPermissionGroupLabel(group.permissions).toLowerCase()}.
										</p>
									</div>
									<Badge variant="outline">{group.permissions.length} permessi</Badge>
								</div>

								<div class="grid gap-3 md:grid-cols-2">
									{#each group.permissions as permission (permission.id)}
										{@const translation = getPermissionTranslation(permission)}
										<label
											class="flex items-start gap-3 rounded-[16px] border border-border/70 p-4"
										>
											<Checkbox
												class="mt-1"
												checked={permissionSelected(permission.id)}
												onCheckedChange={(checked) =>
													toggleRolePermission(permission.id, checked)
												}
											/>
											<div>
												<p class="font-medium">{translation.title}</p>
												<p class="mt-1 text-sm text-muted-foreground">
													{translation.description}
												</p>
											</div>
										</label>
									{/each}
								</div>
							</section>
						{/each}
					</div>
				{/if}
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (roleSheetOpen = false)}
					>Annulla</Button
				>
				<Button type="submit" disabled={roleSubmitting || permissionCatalogQuery.isPending}>
					{#if roleSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<ShieldCheckIcon class="size-4" />
						{roleSubmitLabel}
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={customFieldSheetOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-2xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">{customFieldSheetTitle}</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				Configura un campo aggiuntivo da usare nella scheda cliente del tenant.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleSaveCustomField}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2 md:col-span-2">
						<span class="text-sm font-medium">Etichetta</span>
						<Input bind:value={customFieldForm.label} placeholder="Obiettivo principale" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Chiave</span>
						<Input bind:value={customFieldForm.key} placeholder="obiettivo_principale" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Ordine</span>
						<Input type="number" bind:value={customFieldForm.sortOrder} min="0" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Tipo campo</span>
						<Select.Root type="single" bind:value={customFieldForm.valueType}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{selectedCustomFieldTypeLabel}</span>
							</Select.Trigger>
							<Select.Content>
								{#each customFieldValueTypeOptions as option (option.value)}
									<Select.Item value={option.value} label={option.label}>
										{option.label}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
					<label class="space-y-2 md:col-span-2">
						<span class="text-sm font-medium">Descrizione interna</span>
						<Textarea
							class="min-h-24"
							bind:value={customFieldForm.description}
							placeholder="Aiuta il team a capire quando compilare questo campo."
						/>
					</label>
					{#if customFieldForm.valueType === 'Select'}
						<label class="space-y-2 md:col-span-2">
							<span class="text-sm font-medium">Opzioni</span>
							<Textarea
								class="min-h-28"
								bind:value={customFieldForm.optionsText}
								placeholder="Dimagrimento&#10;Tonificazione&#10;Preparazione gara"
							/>
							<p class="text-xs text-muted-foreground">
								Una voce per riga oppure separate da virgola.
							</p>
						</label>
					{/if}
				</div>

				<div class="grid gap-3">
					<label class="flex items-start gap-3 rounded-[16px] border border-border/70 p-4">
						<Checkbox class="mt-1" bind:checked={customFieldForm.isRequired} />
						<div>
							<p class="font-medium">Campo obbligatorio</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Il desk dovra compilarlo in creazione o modifica cliente.
							</p>
						</div>
					</label>
					<label class="flex items-start gap-3 rounded-[16px] border border-border/70 p-4">
						<Checkbox class="mt-1" bind:checked={customFieldForm.isActive} />
						<div>
							<p class="font-medium">Campo attivo</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Se disattivato resta nello storico ma non compare piu nei form.
							</p>
						</div>
					</label>
				</div>
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (customFieldSheetOpen = false)}>
					Annulla
				</Button>
				<Button type="submit" disabled={customFieldSubmitting}>
					{#if customFieldSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<ShieldCheckIcon class="size-4" />
						{customFieldSubmitLabel}
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>
