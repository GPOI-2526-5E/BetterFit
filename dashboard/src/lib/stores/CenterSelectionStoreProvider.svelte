<script lang="ts" module>
	import { getContext } from 'svelte';

	export type TenantGym = {
		id: string;
		name: string;
		createdAtUtc: Date | null;
	};

	export type TenantLocation = {
		id: string;
		gymId: string;
		name: string;
		code: string | null;
		addressLine1: string | null;
		city: string | null;
		countryCode: string | null;
		isActive: boolean;
	};

	export type CenterSelectionStore = {
		selectedId: string | null;
		selectedGymId: string | null;
		selectedLocationId: string | null;
		selectedGym: TenantGym | null;
		selectedLocation: TenantLocation | null;
		gyms: TenantGym[];
		locations: TenantLocation[];
		isLoadingGyms: boolean;
		isLoadingLocations: boolean;
		gymsError: string | null;
		locationsError: string | null;
		selectGym: (gymId: string) => void;
		selectLocation: (locationId: string | null) => void;
		refetchGyms: () => Promise<unknown>;
		refetchLocations: () => Promise<unknown>;
	};

	export const useCenterSelectionStore = () => {
		return getContext('betterfitCenterStore') as CenterSelectionStore;
	};
</script>

<script lang="ts">
	import { createQuery } from '@tanstack/svelte-query';
	import type { GymLocationResponse, GymResponse } from '$lib/api';
	import { useUserAuthenticationStore } from '$lib/stores/AuthenticationStoreProvider.svelte';
	import {
		clearDashboardScopeSelection,
		getDashboardScopeSelection,
		saveDashboardScopeSelection,
		type DashboardScopeSelection
	} from '$lib/utils/dashboard-scope-storage.js';
	import { setContext, type Snippet } from 'svelte';
	import { useApiClient } from './ApiClientProvider.svelte';

	let { children }: { children: Snippet } = $props();

	const api = useApiClient();
	const auth = useUserAuthenticationStore();

	function mapGym(gym: GymResponse): TenantGym | null {
		if (!gym.id || !gym.name) {
			return null;
		}

		return {
			id: gym.id,
			name: gym.name,
			createdAtUtc: gym.createdAtUtc ?? null
		};
	}

	function mapLocation(location: GymLocationResponse): TenantLocation | null {
		if (!location.id || !location.gymId || !location.name) {
			return null;
		}

		return {
			id: location.id,
			gymId: location.gymId,
			name: location.name,
			code: location.code ?? null,
			addressLine1: location.addressLine1 ?? null,
			city: location.city ?? null,
			countryCode: location.countryCode ?? null,
			isActive: location.isActive ?? true
		};
	}

	async function fetchGyms() {
		const response = await api.client.apiGymsGet();

		if (!response.success) {
			throw new Error(response.message ?? 'Unable to load available tenants.');
		}

		return (response.data ?? []).map(mapGym).filter((gym): gym is TenantGym => gym !== null);
	}

	async function fetchLocations(gymId: string) {
		const response = await api.client.apiGymsGymIdLocationsGet({ gymId });

		if (!response.success) {
			throw new Error(response.message ?? 'Unable to load locations for this tenant.');
		}

		return (response.data ?? [])
			.map(mapLocation)
			.filter((location): location is TenantLocation => location !== null);
	}

	const initialSelection = getDashboardScopeSelection();
	let savedSelection: DashboardScopeSelection | null = initialSelection;
	let selectedGymId = $state<string | null>(initialSelection?.gymId ?? null);
	let selectedLocationId = $state<string | null>(initialSelection?.locationId ?? null);

	const gymsQuery = createQuery(() => ({
		queryKey: ['dashboard', 'available-gyms', auth.user?.user ?? null],
		enabled: auth.ready && !!auth.user,
		queryFn: fetchGyms
	}));

	const locationsQuery = createQuery(() => ({
		queryKey: ['dashboard', 'available-locations', selectedGymId],
		enabled: auth.ready && !!auth.user && !!selectedGymId,
		queryFn: () => fetchLocations(selectedGymId!)
	}));

	const availableGyms = $derived(gymsQuery.data ?? []);
	const availableLocations = $derived(locationsQuery.data ?? []);
	const selectedGym = $derived(availableGyms.find((gym) => gym.id === selectedGymId) ?? null);
	const selectedLocation = $derived(
		availableLocations.find((location) => location.id === selectedLocationId) ?? null
	);
	const gymsError = $derived(
		gymsQuery.error instanceof Error
			? gymsQuery.error.message
			: gymsQuery.error
				? 'Unable to load tenants.'
				: null
	);
	const locationsError = $derived(
		locationsQuery.error instanceof Error
			? locationsQuery.error.message
			: locationsQuery.error
				? 'Unable to load locations.'
				: null
	);

	function selectGym(gymId: string) {
		if (selectedGymId === gymId) {
			return;
		}

		selectedGymId = gymId;
		selectedLocationId = null;
		persistSelection(gymId, null);
	}

	function selectLocation(locationId: string | null) {
		selectedLocationId = locationId;
		persistSelection(selectedGymId, locationId);
	}

	function persistSelection(gymId: string | null, locationId: string | null) {
		if (!gymId) {
			if (savedSelection !== null) {
				clearDashboardScopeSelection();
				savedSelection = null;
			}
			return;
		}

		if (savedSelection?.gymId === gymId && savedSelection.locationId === locationId) {
			return;
		}

		const nextSelection = {
			gymId,
			locationId
		} satisfies DashboardScopeSelection;

		saveDashboardScopeSelection(nextSelection);
		savedSelection = nextSelection;
	}

	$effect(() => {
		if (!gymsQuery.isSuccess) {
			return;
		}

		if (availableGyms.length === 0) {
			if (selectedGymId !== null) {
				selectedGymId = null;
			}
			if (selectedLocationId !== null) {
				selectedLocationId = null;
			}
			persistSelection(null, null);
			return;
		}

		if (selectedGymId && availableGyms.some((gym) => gym.id === selectedGymId)) {
			return;
		}

		const nextGymId =
			savedSelection?.gymId && availableGyms.some((gym) => gym.id === savedSelection?.gymId)
				? savedSelection.gymId
				: (availableGyms[0]?.id ?? null);

		if (selectedGymId !== nextGymId) {
			selectedGymId = nextGymId;
		}
		if (savedSelection?.gymId !== nextGymId && selectedLocationId !== null) {
			selectedLocationId = null;
		}
		persistSelection(nextGymId, null);
	});

	$effect(() => {
		if (!selectedGymId) {
			if (selectedLocationId !== null) {
				selectedLocationId = null;
			}
			return;
		}

		if (!locationsQuery.isSuccess) {
			return;
		}

		const validLocationIds = new Set(availableLocations.map((location) => location.id));
		if (selectedLocationId && validLocationIds.has(selectedLocationId)) {
			return;
		}

		const savedLocationId =
			savedSelection?.gymId === selectedGymId ? savedSelection.locationId : null;
		if (savedLocationId && validLocationIds.has(savedLocationId)) {
			if (selectedLocationId !== savedLocationId) {
				selectedLocationId = savedLocationId;
			}
			persistSelection(selectedGymId, savedLocationId);
			return;
		}

		if (selectedLocationId !== null) {
			selectedLocationId = null;
		}
		persistSelection(selectedGymId, null);
	});

	setContext('betterfitCenterStore', {
		get selectedId() {
			return selectedLocationId ?? selectedGymId;
		},
		get selectedGymId() {
			return selectedGymId;
		},
		get selectedLocationId() {
			return selectedLocationId;
		},
		get selectedGym() {
			return selectedGym;
		},
		get selectedLocation() {
			return selectedLocation;
		},
		get gyms() {
			return availableGyms;
		},
		get locations() {
			return availableLocations;
		},
		get isLoadingGyms() {
			return gymsQuery.isPending;
		},
		get isLoadingLocations() {
			return !!selectedGymId && locationsQuery.isPending;
		},
		get gymsError() {
			return gymsError;
		},
		get locationsError() {
			return locationsError;
		},
		selectGym,
		selectLocation,
		refetchGyms() {
			return gymsQuery.refetch();
		},
		refetchLocations() {
			return locationsQuery.refetch();
		}
	});
</script>

{@render children()}
