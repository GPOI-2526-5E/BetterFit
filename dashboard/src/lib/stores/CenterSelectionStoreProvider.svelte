<script module>
	export type FitnessCenter = {
		id: string;
		name: string;
		city: string;
		code: string;
		status: 'operational' | 'reduced';
	};

	export const fitnessCenters: FitnessCenter[] = [
		{
			id: 'caserta-centro',
			name: 'Betterfit Caserta Centro',
			city: 'Caserta',
			code: 'CAS-C',
			status: 'operational'
		},
		{
			id: 'caserta-nord',
			name: 'Betterfit Caserta Nord',
			city: 'Caserta',
			code: 'CAS-N',
			status: 'operational'
		},
		{
			id: 'napoli-vomero',
			name: 'Betterfit Vomero',
			city: 'Napoli',
			code: 'NAP-V',
			status: 'reduced'
		}
	];

	import { getContext } from 'svelte';

	export const useCenterSelectionStore = () => {
		return getContext('betterfitCenterStore') as {
			selectedId: string;
			selected: FitnessCenter | null;
			centers: FitnessCenter[];
		};
	};
</script>

<script lang="ts">
	import { onDestroy, setContext, type Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();

	let selectedCenterId: string | null = $state(fitnessCenters[0].id);

	const selectedCenter = $derived(
		fitnessCenters.find((center) => center.id === selectedCenterId) ?? null
	);

	let availableCenters = $state(fitnessCenters);

	setContext('betterfitCenterStore', {
		get selectedId() {
			return selectedCenterId;
		},
		set selectedId(centerId: string | null) {
			selectedCenterId = centerId;
		},
		get selected() {
			return selectedCenter;
		},
		get centers() {
			return availableCenters;
		}
	});

	onDestroy(() => {
		//dndType.set(null);

		selectedCenterId = null;
	});
</script>

{@render children()}
