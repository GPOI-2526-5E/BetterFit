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

let selectedCenterId = $state(fitnessCenters[0].id);

const selectedCenter = $derived(
	fitnessCenters.find((center) => center.id === selectedCenterId) ?? fitnessCenters[0]
);

export const center = {
	get selectedId() {
		return selectedCenterId;
	},
	set selectedId(centerId: string) {
		selectedCenterId = centerId;
	},
	get selected() {
		return selectedCenter;
	}
};
