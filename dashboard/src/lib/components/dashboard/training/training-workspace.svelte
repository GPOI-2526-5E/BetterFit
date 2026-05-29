<script lang="ts">
	import { page } from '$app/state';
	import ArchiveIcon from '@lucide/svelte/icons/archive';
	import CheckCircle2Icon from '@lucide/svelte/icons/check-circle-2';
	import { createQuery } from '@tanstack/svelte-query';
	import ClipboardListIcon from '@lucide/svelte/icons/clipboard-list';
	import DumbbellIcon from '@lucide/svelte/icons/dumbbell';
	import LibraryBigIcon from '@lucide/svelte/icons/library-big';
	import PlusCircleIcon from '@lucide/svelte/icons/plus-circle';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import RotateCcwIcon from '@lucide/svelte/icons/rotate-ccw';
	import SearchIcon from '@lucide/svelte/icons/search';
	import SparklesIcon from '@lucide/svelte/icons/sparkles';
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
		type CreateGymWorkoutAssessmentRequest,
		type CreateGymWorkoutAssignmentRequest,
		type CreateGymWorkoutExerciseRequest,
		type CreateGymWorkoutTemplateRequest,
		type CreateGymWorkoutTemplateItemRequest,
		type GymWorkoutAssessment,
		type GymWorkoutAssignment,
		type WorkoutAssignmentStatus,
		type WorkoutTemplateLevel,
		createGymWorkoutAssessment,
		createGymWorkoutAssignment,
		createGymWorkoutExercise,
		createGymWorkoutTemplate,
		fetchGymTrainingOverview,
		fetchGymWorkoutAssessments,
		fetchGymWorkoutAssignments,
		fetchGymWorkoutExercises,
		fetchGymWorkoutTemplates,
		updateGymWorkoutTemplateActivation,
		updateGymWorkoutAssignmentStatus
	} from '$lib/data/training-api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
	type ExerciseItemState = {
		exerciseId: string;
		exerciseName: string;
		dayNumber: number;
		sortOrder: number;
		setsPrescription: string;
		repetitionsPrescription: string;
		restSeconds: string;
		tempo: string;
		notes: string;
	};
	type MembershipOption = {
		id: string;
		label: string;
		email: string;
		locationIds: string[];
		status: string;
	};
	type CoachOption = {
		id: string;
		label: string;
		scopeLocationId: string | null;
	};
	type TemplateOption = {
		id: string;
		label: string;
		locationId: string;
	};
	type MeasurementField = {
		label: string;
		value: string;
	};
	const TRAINING_ALL_LOCATIONS_SELECT_VALUE = '__all_locations__';
	const TRAINING_ALL_MEMBERS_SELECT_VALUE = '__all_members__';
	const TRAINING_NO_COACH_SELECT_VALUE = '__no_coach__';
	const TRAINING_CUSTOM_EXERCISE_SELECT_VALUE = '__custom_exercise__';

	let {
		presetMembershipId = null
	}: {
		presetMembershipId?: string | null;
	} = $props();

	const api = useApiClient();
	const center = useCenterSelectionStore();
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});
	const date = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		year: 'numeric'
	});
	const currentHash = $derived(page.url.hash || '#assigned');

	const levelOptions: Array<{ value: WorkoutTemplateLevel; label: string }> = [
		{ value: 'Beginner', label: 'Beginner' },
		{ value: 'Intermediate', label: 'Intermediate' },
		{ value: 'Advanced', label: 'Advanced' },
		{ value: 'Mixed', label: 'Mixed' }
	];

	const toInputDate = (date = new Date()) => {
		const next = new Date(date);
		next.setSeconds(0, 0);
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

	const coachLabel = (assignment: GymStaffAssignmentResponse) =>
		assignment.staffProfile?.displayName?.trim() ||
		assignment.userEmail?.trim() ||
		assignment.roleName?.trim() ||
		'Coach';

	const assignmentStatusMeta = (
		status: WorkoutAssignmentStatus
	): { label: string; variant: BadgeVariant } => {
		if (status === 'Completed') return { label: 'Completato', variant: 'success' };
		if (status === 'Archived') return { label: 'Archiviato', variant: 'outline' };
		return { label: 'Attivo', variant: 'secondary' };
	};
	const templateStatusMeta = (isActive: boolean): { label: string; variant: BadgeVariant } =>
		isActive
			? { label: 'Attivo', variant: 'success' }
			: { label: 'Disattivato', variant: 'outline' };
	const sectionNavClass = (hash: string) =>
		`inline-flex items-center rounded-full border px-3 py-1.5 text-sm transition ${
			currentHash === hash
				? 'border-primary/40 bg-primary/10 text-foreground shadow-sm'
				: 'border-border/70 bg-background text-muted-foreground hover:border-border hover:bg-secondary/20 hover:text-foreground'
		}`;
	const sectionCardClass = (hash: string) =>
		`scroll-mt-24 ${
			currentHash === hash ? 'border-primary/40 bg-primary/[0.03] shadow-sm' : 'border-border/70'
		}`;

	const newExerciseItem = (sortOrder = 1): ExerciseItemState => ({
		exerciseId: '',
		exerciseName: '',
		dayNumber: 1,
		sortOrder,
		setsPrescription: '4',
		repetitionsPrescription: '8-10',
		restSeconds: '90',
		tempo: '',
		notes: ''
	});

	let locationFilter = $state(center.selectedLocationId ?? '');
	let membershipFilter = $state('');
	let exerciseSearch = $state('');
	let feedbackMessage = $state('');
	let feedbackError = $state('');
	let selectedAssignmentId = $state<string | null>(null);
	let selectedMeasurementId = $state<string | null>(null);
	let exerciseSheetOpen = $state(false);
	let templateSheetOpen = $state(false);
	let assignSheetOpen = $state(false);
	let measurementSheetOpen = $state(false);
	let exerciseSubmitting = $state(false);
	let templateSubmitting = $state(false);
	let assignmentSubmitting = $state(false);
	let templateActionPendingId = $state<string | null>(null);
	let assignmentActionPending = $state<string | null>(null);
	let measurementSubmitting = $state(false);

	let exerciseForm = $state({
		name: '',
		category: '',
		muscleGroup: '',
		equipment: '',
		instructions: '',
		videoUrl: ''
	});
	let templateForm = $state({
		locationId: center.selectedLocationId ?? '',
		coachAssignmentId: '',
		name: '',
		goal: '',
		level: 'Mixed' as WorkoutTemplateLevel,
		description: '',
		daysPerWeek: '3',
		items: [newExerciseItem()]
	});
	let assignForm = $state({
		membershipId: '',
		templateId: '',
		startsAtLocal: toInputDate(),
		revisionDueAtLocal: '',
		notes: ''
	});
	let measurementForm = $state({
		membershipId: '',
		locationId: center.selectedLocationId ?? '',
		coachAssignmentId: '',
		recordedAtLocal: toInputDate(),
		weightKg: '',
		bodyFatPercentage: '',
		leanMassKg: '',
		chestCm: '',
		waistCm: '',
		hipsCm: '',
		armCm: '',
		thighCm: '',
		calfCm: '',
		restingHeartRateBpm: '',
		notes: ''
	});

	const membershipsQuery = createQuery(() => ({
		queryKey: ['training-memberships', center.selectedGymId],
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
		queryKey: ['training-staff', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdStaffAssignmentsGet({
				gymId: center.selectedGymId!
			});
			if (!response.success) {
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile caricare i coach.'
				);
			}
			return response.data ?? [];
		}
	}));

	const overviewQuery = createQuery(() => ({
		queryKey: ['training-overview', center.selectedGymId, locationFilter],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymTrainingOverview(center.selectedGymId!, {
				locationId: locationFilter || null
			})
	}));

	const exercisesQuery = createQuery(() => ({
		queryKey: ['training-exercises', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: () => fetchGymWorkoutExercises(center.selectedGymId!)
	}));

	const templatesQuery = createQuery(() => ({
		queryKey: ['training-templates', center.selectedGymId, locationFilter],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymWorkoutTemplates(center.selectedGymId!, {
				locationId: locationFilter || null
			})
	}));

	const assignmentsQuery = createQuery(() => ({
		queryKey: ['training-assignments', center.selectedGymId, locationFilter, membershipFilter],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymWorkoutAssignments(center.selectedGymId!, {
				locationId: locationFilter || null,
				membershipId: membershipFilter || null
			})
	}));

	const measurementsQuery = createQuery(() => ({
		queryKey: ['training-measurements', center.selectedGymId, locationFilter, membershipFilter],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymWorkoutAssessments(center.selectedGymId!, {
				locationId: locationFilter || null,
				membershipId: membershipFilter || null
			})
	}));

	const locationOptions = $derived(
		(center.locations ?? [])
			.filter((location) => location.id && location.name && location.isActive !== false)
			.map((location) => ({ id: location.id!, name: location.name! }))
	);

	const membershipOptions = $derived.by(() => {
		return (membershipsQuery.data ?? [])
			.filter((membership) => membership.membershipId)
			.map((membership) => ({
				id: membership.membershipId ?? '',
				label: membershipLabel(membership),
				email: membership.userEmail ?? membership.invitationEmail ?? '',
				locationIds: (membership.locations ?? [])
					.map((location) => location.id ?? '')
					.filter(Boolean),
				status: membership.status ?? 'PendingClaim'
			})) satisfies MembershipOption[];
	});

	const coachOptions = $derived.by(() => {
		return (staffQuery.data ?? [])
			.filter((assignment) => assignment.assignmentId && assignment.status === 'Active')
			.map((assignment) => ({
				id: assignment.assignmentId ?? '',
				label: coachLabel(assignment),
				scopeLocationId: assignment.scopeLocationId ?? null
			})) satisfies CoachOption[];
	});

	const filteredCoachOptions = $derived.by(() => {
		if (!templateForm.locationId) {
			return coachOptions;
		}

		return coachOptions.filter(
			(coach) => !coach.scopeLocationId || coach.scopeLocationId === templateForm.locationId
		);
	});

	const filteredMeasurementCoachOptions = $derived.by(() => {
		if (!measurementForm.locationId) {
			return coachOptions;
		}

		return coachOptions.filter(
			(coach) => !coach.scopeLocationId || coach.scopeLocationId === measurementForm.locationId
		);
	});

	const filteredExercises = $derived.by(() => {
		const search = exerciseSearch.trim().toLowerCase();
		if (!search) {
			return exercisesQuery.data ?? [];
		}

		return (exercisesQuery.data ?? []).filter((exercise) =>
			[exercise.name, exercise.category, exercise.muscleGroup, exercise.equipment]
				.filter(Boolean)
				.join(' ')
				.toLowerCase()
				.includes(search)
		);
	});

	const selectedAssignment = $derived.by(() => {
		const assignments = assignmentsQuery.data ?? [];
		if (!selectedAssignmentId) {
			return assignments[0] ?? null;
		}

		return (
			assignments.find((assignment) => assignment.assignmentId === selectedAssignmentId) ??
			assignments[0] ??
			null
		);
	});

	const selectedAssignmentDays = $derived.by(() => {
		if (!selectedAssignment) {
			return [] as Array<{ dayNumber: number; items: GymWorkoutAssignment['items'] }>;
		}

		return Array.from(
			selectedAssignment.items
				.reduce((map, item) => {
					const current = map.get(item.dayNumber) ?? [];
					current.push(item);
					map.set(item.dayNumber, current);
					return map;
				}, new Map<number, GymWorkoutAssignment['items']>())
				.entries()
		)
			.sort(([left], [right]) => left - right)
			.map(([dayNumber, items]) => ({
				dayNumber,
				items: items.sort((left, right) => left.sortOrder - right.sortOrder)
			}));
	});
	const revisionFocusAssignment = $derived.by(() => {
		const activeAssignments = (assignmentsQuery.data ?? [])
			.filter((assignment) => assignment.status === 'Active')
			.sort((left, right) => {
				const leftRevision = left.revisionDueAtUtc?.getTime() ?? Number.MAX_SAFE_INTEGER;
				const rightRevision = right.revisionDueAtUtc?.getTime() ?? Number.MAX_SAFE_INTEGER;
				return leftRevision - rightRevision;
			});

		return activeAssignments[0] ?? overviewQuery.data?.recentAssignments[0] ?? null;
	});

	const activeTemplates = $derived.by(() => {
		return (templatesQuery.data ?? []).filter((template) => template.isActive);
	});

	const templateOptions = $derived.by(() => {
		return activeTemplates.map((template) => ({
			id: template.templateId,
			label: `${template.name} - ${template.locationName}`,
			locationId: template.locationId
		})) satisfies TemplateOption[];
	});

	const assignableMembershipOptions = $derived.by(() => {
		const template = activeTemplates.find((item) => item.templateId === assignForm.templateId);
		if (!template) {
			return membershipOptions.filter((membership) => membership.status === 'Active');
		}

		return membershipOptions.filter(
			(membership) =>
				membership.status === 'Active' && membership.locationIds.includes(template.locationId)
		);
	});

	const measurableMembershipOptions = $derived.by(() => {
		if (!measurementForm.locationId) {
			return membershipOptions.filter((membership) => membership.status !== 'Archived');
		}

		return membershipOptions.filter(
			(membership) =>
				membership.status !== 'Archived' &&
				membership.locationIds.includes(measurementForm.locationId)
		);
	});

	function selectedLocationLabel(locationId: string, fallback = 'Seleziona una sede') {
		return locationOptions.find((location) => location.id === locationId)?.name ?? fallback;
	}

	function selectedMembershipLabel(
		options: MembershipOption[],
		membershipId: string,
		fallback = 'Seleziona un membro'
	) {
		return options.find((membership) => membership.id === membershipId)?.label ?? fallback;
	}

	function selectedCoachLabel(
		coachId: string,
		options: CoachOption[],
		fallback = 'Nessun coach fisso'
	) {
		return options.find((coach) => coach.id === coachId)?.label ?? fallback;
	}

	function selectedTemplateLabel(templateId: string, fallback = 'Seleziona un modello') {
		return templateOptions.find((template) => template.id === templateId)?.label ?? fallback;
	}

	function selectedLevelLabel(level: WorkoutTemplateLevel) {
		return levelOptions.find((option) => option.value === level)?.label ?? level;
	}

	function selectedExerciseLibraryLabel(exerciseId: string) {
		return (
			(exercisesQuery.data ?? []).find((exercise) => exercise.exerciseId === exerciseId)?.name ??
			'Custom / manuale'
		);
	}
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
				? 'Impossibile caricare i coach.'
				: null
	);
	const overviewQueryError = $derived(
		overviewQuery.error instanceof Error
			? overviewQuery.error.message
			: overviewQuery.error
				? 'Impossibile caricare l overview training.'
				: null
	);
	const exercisesQueryError = $derived(
		exercisesQuery.error instanceof Error
			? exercisesQuery.error.message
			: exercisesQuery.error
				? 'Impossibile caricare la libreria esercizi.'
				: null
	);
	const templatesQueryError = $derived(
		templatesQuery.error instanceof Error
			? templatesQuery.error.message
			: templatesQuery.error
				? 'Impossibile caricare i modelli training.'
				: null
	);
	const assignmentsQueryError = $derived(
		assignmentsQuery.error instanceof Error
			? assignmentsQuery.error.message
			: assignmentsQuery.error
				? 'Impossibile caricare i piani assegnati.'
				: null
	);
	const measurementsQueryError = $derived(
		measurementsQuery.error instanceof Error
			? measurementsQuery.error.message
			: measurementsQuery.error
				? 'Impossibile caricare le misurazioni.'
				: null
	);
	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms ||
			(!!center.selectedGymId &&
				((!membershipsQuery.data && membershipsQuery.isPending) ||
					(!staffQuery.data && staffQuery.isPending) ||
					(!overviewQuery.data && overviewQuery.isPending) ||
					(!exercisesQuery.data && exercisesQuery.isPending) ||
					(!templatesQuery.data && templatesQuery.isPending) ||
					(!assignmentsQuery.data && assignmentsQuery.isPending) ||
					(!measurementsQuery.data && measurementsQuery.isPending)))
	);
	const workspaceError = $derived(
		center.gymsError ??
			membershipsQueryError ??
			staffQueryError ??
			overviewQueryError ??
			exercisesQueryError ??
			templatesQueryError ??
			assignmentsQueryError ??
			measurementsQueryError ??
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
		if (templateOptions.length === 0) {
			if (assignForm.templateId) {
				assignForm = {
					...assignForm,
					templateId: ''
				};
			}
			return;
		}

		if (!templateOptions.some((template) => template.id === assignForm.templateId)) {
			assignForm = {
				...assignForm,
				templateId: templateOptions[0].id
			};
		}
	});

	$effect(() => {
		if (
			presetMembershipId &&
			membershipOptions.some((membership) => membership.id === presetMembershipId)
		) {
			if (membershipFilter !== presetMembershipId) {
				membershipFilter = presetMembershipId;
			}
		}
	});

	$effect(() => {
		if (assignableMembershipOptions.length === 0) {
			if (assignForm.membershipId) {
				assignForm = {
					...assignForm,
					membershipId: ''
				};
			}
			return;
		}

		if (
			!assignForm.membershipId ||
			!assignableMembershipOptions.some((membership) => membership.id === assignForm.membershipId)
		) {
			assignForm = {
				...assignForm,
				membershipId:
					presetMembershipId &&
					assignableMembershipOptions.some((membership) => membership.id === presetMembershipId)
						? presetMembershipId
						: assignableMembershipOptions[0].id
			};
		}
	});

	$effect(() => {
		if (!measurementForm.locationId && locationOptions.length > 0) {
			measurementForm = {
				...measurementForm,
				locationId: locationFilter || locationOptions[0].id
			};
		}
	});

	$effect(() => {
		if (
			presetMembershipId &&
			membershipOptions.some((membership) => membership.id === presetMembershipId)
		) {
			if (measurementForm.membershipId !== presetMembershipId) {
				measurementForm = {
					...measurementForm,
					membershipId: presetMembershipId
				};
			}
		}
	});

	$effect(() => {
		if (!measurementForm.membershipId && measurableMembershipOptions.length > 0) {
			measurementForm = {
				...measurementForm,
				membershipId: measurableMembershipOptions[0].id
			};
		}
	});

	$effect(() => {
		const assignments = assignmentsQuery.data ?? [];
		if (assignments.length === 0) {
			selectedAssignmentId = null;
			return;
		}

		if (
			!selectedAssignmentId ||
			!assignments.some((assignment) => assignment.assignmentId === selectedAssignmentId)
		) {
			selectedAssignmentId = assignments[0].assignmentId;
		}
	});

	$effect(() => {
		const measurements = measurementsQuery.data ?? [];
		if (measurements.length === 0) {
			selectedMeasurementId = null;
			return;
		}

		if (
			!selectedMeasurementId ||
			!measurements.some((measurement) => measurement.assessmentId === selectedMeasurementId)
		) {
			selectedMeasurementId = measurements[0].assessmentId;
		}
	});

	const selectedMeasurement = $derived.by(() => {
		const measurements = measurementsQuery.data ?? [];
		if (!selectedMeasurementId) {
			return measurements[0] ?? null;
		}

		return (
			measurements.find((measurement) => measurement.assessmentId === selectedMeasurementId) ??
			measurements[0] ??
			null
		);
	});
	const selectedAssignmentLatestMeasurement = $derived.by(() => {
		if (!selectedAssignment) {
			return null;
		}

		return (
			(measurementsQuery.data ?? []).find(
				(measurement) => measurement.membershipId === selectedAssignment.membershipId
			) ?? null
		);
	});
	const revisionFocusLatestMeasurement = $derived.by(() => {
		if (!revisionFocusAssignment) {
			return null;
		}

		return (
			(measurementsQuery.data ?? []).find(
				(measurement) => measurement.membershipId === revisionFocusAssignment.membershipId
			) ?? null
		);
	});

	const selectedMeasurementFields = $derived.by(() => {
		if (!selectedMeasurement) {
			return [] as MeasurementField[];
		}

		const formatMetric = (value: number | null, suffix: string) =>
			value === null ? 'Non rilevato' : `${value.toLocaleString('it-IT')} ${suffix}`;

		return [
			{ label: 'Peso', value: formatMetric(selectedMeasurement.weightKg, 'kg') },
			{ label: 'Body fat', value: formatMetric(selectedMeasurement.bodyFatPercentage, '%') },
			{ label: 'Massa magra', value: formatMetric(selectedMeasurement.leanMassKg, 'kg') },
			{ label: 'Torace', value: formatMetric(selectedMeasurement.chestCm, 'cm') },
			{ label: 'Vita', value: formatMetric(selectedMeasurement.waistCm, 'cm') },
			{ label: 'Fianchi', value: formatMetric(selectedMeasurement.hipsCm, 'cm') },
			{ label: 'Braccio', value: formatMetric(selectedMeasurement.armCm, 'cm') },
			{ label: 'Coscia', value: formatMetric(selectedMeasurement.thighCm, 'cm') },
			{ label: 'Polpaccio', value: formatMetric(selectedMeasurement.calfCm, 'cm') },
			{
				label: 'FC a riposo',
				value:
					selectedMeasurement.restingHeartRateBpm === null
						? 'Non rilevata'
						: `${selectedMeasurement.restingHeartRateBpm} bpm`
			}
		] satisfies MeasurementField[];
	});

	function clearFeedback() {
		feedbackMessage = '';
		feedbackError = '';
	}

	async function handleAssignmentStatus(status: WorkoutAssignmentStatus) {
		if (!center.selectedGymId || !selectedAssignment) {
			return;
		}

		clearFeedback();
		assignmentActionPending = `${selectedAssignment.assignmentId}-${status}`;

		try {
			await updateGymWorkoutAssignmentStatus(
				center.selectedGymId,
				selectedAssignment.assignmentId,
				{
					status,
					completedAtUtc: status === 'Completed' ? new Date().toISOString() : null
				}
			);
			await Promise.all([overviewQuery.refetch(), assignmentsQuery.refetch()]);
			feedbackMessage =
				status === 'Completed'
					? 'Scheda segnata come completata.'
					: status === 'Archived'
						? 'Scheda archiviata.'
						: 'Scheda riattivata.';
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile aggiornare lo stato della scheda.';
		} finally {
			assignmentActionPending = null;
		}
	}

	async function handleTemplateActivation(templateId: string, isActive: boolean, label: string) {
		if (!center.selectedGymId) {
			return;
		}

		clearFeedback();
		templateActionPendingId = templateId;

		try {
			await updateGymWorkoutTemplateActivation(center.selectedGymId, templateId, {
				isActive
			});
			await Promise.all([templatesQuery.refetch(), overviewQuery.refetch()]);
			if (!isActive && assignForm.templateId === templateId) {
				assignForm = {
					...assignForm,
					templateId: ''
				};
			}
			feedbackMessage = label;
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile aggiornare lo stato del modello.';
		} finally {
			templateActionPendingId = null;
		}
	}

	function resetExerciseForm() {
		exerciseForm = {
			name: '',
			category: '',
			muscleGroup: '',
			equipment: '',
			instructions: '',
			videoUrl: ''
		};
	}

	function resetTemplateForm() {
		templateForm = {
			locationId: locationFilter || locationOptions[0]?.id || '',
			coachAssignmentId: '',
			name: '',
			goal: '',
			level: 'Mixed',
			description: '',
			daysPerWeek: '3',
			items: [newExerciseItem()]
		};
	}

	function resetAssignForm(templateId = templateOptions[0]?.id ?? '') {
		const template = activeTemplates.find((item) => item.templateId === templateId);
		const availableMemberships = template
			? membershipOptions.filter(
					(membership) =>
						membership.status === 'Active' && membership.locationIds.includes(template.locationId)
				)
			: membershipOptions.filter((membership) => membership.status === 'Active');

		assignForm = {
			membershipId:
				presetMembershipId &&
				availableMemberships.some((membership) => membership.id === presetMembershipId)
					? presetMembershipId
					: (availableMemberships[0]?.id ?? ''),
			templateId,
			startsAtLocal: toInputDate(),
			revisionDueAtLocal: '',
			notes: ''
		};
	}

	function resetMeasurementForm() {
		measurementForm = {
			membershipId: presetMembershipId || measurableMembershipOptions[0]?.id || '',
			locationId: locationFilter || locationOptions[0]?.id || '',
			coachAssignmentId: '',
			recordedAtLocal: toInputDate(),
			weightKg: '',
			bodyFatPercentage: '',
			leanMassKg: '',
			chestCm: '',
			waistCm: '',
			hipsCm: '',
			armCm: '',
			thighCm: '',
			calfCm: '',
			restingHeartRateBpm: '',
			notes: ''
		};
	}

	function openExerciseSheet() {
		clearFeedback();
		resetExerciseForm();
		exerciseSheetOpen = true;
	}

	function openTemplateSheet() {
		clearFeedback();
		resetTemplateForm();
		templateSheetOpen = true;
	}

	function openAssignSheet(templateId = templateOptions[0]?.id ?? '') {
		clearFeedback();

		const nextTemplateId = templateOptions.some((template) => template.id === templateId)
			? templateId
			: (templateOptions[0]?.id ?? '');

		if (!nextTemplateId) {
			feedbackError = 'Serve almeno un modello attivo per creare una nuova assegnazione.';
			assignSheetOpen = false;
			return;
		}

		resetAssignForm(nextTemplateId);
		assignSheetOpen = true;
	}

	function openMeasurementSheet() {
		clearFeedback();
		resetMeasurementForm();
		measurementSheetOpen = true;
	}

	function openMeasurementSheetForAssignment(assignment: GymWorkoutAssignment) {
		clearFeedback();
		measurementForm = {
			membershipId: assignment.membershipId,
			locationId: assignment.locationId,
			coachAssignmentId: assignment.coachAssignmentId ?? '',
			recordedAtLocal: toInputDate(),
			weightKg: '',
			bodyFatPercentage: '',
			leanMassKg: '',
			chestCm: '',
			waistCm: '',
			hipsCm: '',
			armCm: '',
			thighCm: '',
			calfCm: '',
			restingHeartRateBpm: '',
			notes: ''
		};
		measurementSheetOpen = true;
	}

	function focusAssignment(assignmentId: string) {
		selectedAssignmentId = assignmentId;
	}

	function focusMeasurementsForMembership(membershipId: string) {
		membershipFilter = membershipId;
	}

	function addTemplateItem() {
		templateForm = {
			...templateForm,
			items: [...templateForm.items, newExerciseItem(templateForm.items.length + 1)]
		};
	}

	function updateTemplateItemExercise(index: number, exerciseId: string) {
		const selectedExercise = (exercisesQuery.data ?? []).find(
			(exercise) => exercise.exerciseId === exerciseId
		);

		templateForm = {
			...templateForm,
			items: templateForm.items.map((item, itemIndex) =>
				itemIndex === index
					? {
							...item,
							exerciseId,
							exerciseName: selectedExercise?.name ?? item.exerciseName
						}
					: item
			)
		};
	}

	async function refreshAll() {
		if (!center.selectedGymId) {
			return;
		}

		await Promise.all([
			membershipsQuery.refetch(),
			staffQuery.refetch(),
			overviewQuery.refetch(),
			exercisesQuery.refetch(),
			templatesQuery.refetch(),
			assignmentsQuery.refetch(),
			measurementsQuery.refetch()
		]);
	}

	function parseDecimalField(value: string) {
		const normalized = value.trim().replace(',', '.');
		if (!normalized) {
			return null;
		}

		const parsed = Number.parseFloat(normalized);
		return Number.isFinite(parsed) ? parsed : null;
	}

	function parseIntegerField(value: string) {
		const normalized = value.trim();
		if (!normalized) {
			return null;
		}

		const parsed = Number.parseInt(normalized, 10);
		return Number.isFinite(parsed) ? parsed : null;
	}

	async function handleCreateExercise(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !exerciseForm.name.trim()) {
			feedbackError = "Inserisci almeno il nome dell'esercizio.";
			return;
		}

		exerciseSubmitting = true;
		try {
			const created = await createGymWorkoutExercise(center.selectedGymId, {
				name: exerciseForm.name.trim(),
				category: exerciseForm.category.trim() || null,
				muscleGroup: exerciseForm.muscleGroup.trim() || null,
				equipment: exerciseForm.equipment.trim() || null,
				instructions: exerciseForm.instructions.trim() || null,
				videoUrl: exerciseForm.videoUrl.trim() || null
			} satisfies CreateGymWorkoutExerciseRequest);

			await Promise.all([exercisesQuery.refetch(), overviewQuery.refetch()]);
			exerciseSheetOpen = false;
			feedbackMessage = `Esercizio ${created.name} aggiunto alla libreria.`;
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : "Impossibile creare l'esercizio.";
		} finally {
			exerciseSubmitting = false;
		}
	}

	async function handleCreateTemplate(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !templateForm.locationId || !templateForm.name.trim()) {
			feedbackError = 'Compila sede e nome del modello.';
			return;
		}

		if (templateForm.items.some((item) => !item.exerciseName.trim())) {
			feedbackError = 'Ogni riga del builder deve avere un esercizio.';
			return;
		}

		templateSubmitting = true;
		try {
			const created = await createGymWorkoutTemplate(center.selectedGymId, {
				locationId: templateForm.locationId,
				coachAssignmentId: templateForm.coachAssignmentId || null,
				name: templateForm.name.trim(),
				goal: templateForm.goal.trim() || null,
				level: templateForm.level,
				description: templateForm.description.trim() || null,
				daysPerWeek: Number.parseInt(templateForm.daysPerWeek, 10) || 3,
				items: templateForm.items.map((item) => ({
					exerciseId: item.exerciseId || null,
					exerciseName: item.exerciseName.trim(),
					dayNumber: item.dayNumber,
					sortOrder: item.sortOrder,
					setsPrescription: item.setsPrescription.trim(),
					repetitionsPrescription: item.repetitionsPrescription.trim(),
					restSeconds: item.restSeconds ? Number.parseInt(item.restSeconds, 10) : null,
					tempo: item.tempo.trim() || null,
					notes: item.notes.trim() || null
				})) satisfies CreateGymWorkoutTemplateItemRequest[]
			} satisfies CreateGymWorkoutTemplateRequest);

			await Promise.all([templatesQuery.refetch(), overviewQuery.refetch()]);
			templateSheetOpen = false;
			assignForm = {
				...assignForm,
				templateId: created.templateId
			};
			feedbackMessage = `Modello ${created.name} creato con successo.`;
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile creare il modello.';
		} finally {
			templateSubmitting = false;
		}
	}

	async function handleCreateAssignment(event: Event) {
		event.preventDefault();
		clearFeedback();

		if (!center.selectedGymId || !assignForm.templateId || !assignForm.membershipId) {
			feedbackError = 'Seleziona membro e modello prima di assegnare il piano.';
			return;
		}

		assignmentSubmitting = true;
		try {
			const created = await createGymWorkoutAssignment(center.selectedGymId, {
				membershipId: assignForm.membershipId,
				templateId: assignForm.templateId,
				startsAtUtc: assignForm.startsAtLocal
					? new Date(assignForm.startsAtLocal).toISOString()
					: null,
				revisionDueAtUtc: assignForm.revisionDueAtLocal
					? new Date(assignForm.revisionDueAtLocal).toISOString()
					: null,
				notes: assignForm.notes.trim() || null
			} satisfies CreateGymWorkoutAssignmentRequest);

			await Promise.all([assignmentsQuery.refetch(), overviewQuery.refetch()]);
			assignSheetOpen = false;
			selectedAssignmentId = created.assignmentId;
			feedbackMessage = `Piano ${created.title} assegnato a ${created.memberName}.`;
		} catch (error: unknown) {
			feedbackError = error instanceof Error ? error.message : 'Impossibile assegnare il piano.';
		} finally {
			assignmentSubmitting = false;
		}
	}

	async function handleCreateMeasurement(event: Event) {
		event.preventDefault();
		clearFeedback();

		const request = {
			membershipId: measurementForm.membershipId,
			locationId: measurementForm.locationId,
			coachAssignmentId: measurementForm.coachAssignmentId || null,
			recordedAtUtc: measurementForm.recordedAtLocal
				? new Date(measurementForm.recordedAtLocal).toISOString()
				: null,
			weightKg: parseDecimalField(measurementForm.weightKg),
			bodyFatPercentage: parseDecimalField(measurementForm.bodyFatPercentage),
			leanMassKg: parseDecimalField(measurementForm.leanMassKg),
			chestCm: parseDecimalField(measurementForm.chestCm),
			waistCm: parseDecimalField(measurementForm.waistCm),
			hipsCm: parseDecimalField(measurementForm.hipsCm),
			armCm: parseDecimalField(measurementForm.armCm),
			thighCm: parseDecimalField(measurementForm.thighCm),
			calfCm: parseDecimalField(measurementForm.calfCm),
			restingHeartRateBpm: parseIntegerField(measurementForm.restingHeartRateBpm),
			notes: measurementForm.notes.trim() || null
		} satisfies CreateGymWorkoutAssessmentRequest;

		const hasMetrics =
			request.weightKg !== null ||
			request.bodyFatPercentage !== null ||
			request.leanMassKg !== null ||
			request.chestCm !== null ||
			request.waistCm !== null ||
			request.hipsCm !== null ||
			request.armCm !== null ||
			request.thighCm !== null ||
			request.calfCm !== null ||
			request.restingHeartRateBpm !== null ||
			request.notes !== null;

		if (!center.selectedGymId || !request.membershipId || !request.locationId) {
			feedbackError = 'Seleziona membro e sede prima di salvare la misurazione.';
			return;
		}

		if (!hasMetrics) {
			feedbackError = 'Inserisci almeno una misura o una nota valutativa.';
			return;
		}

		measurementSubmitting = true;
		try {
			const created = await createGymWorkoutAssessment(center.selectedGymId, request);

			await measurementsQuery.refetch();
			measurementSheetOpen = false;
			selectedMeasurementId = created.assessmentId;
			feedbackMessage = `Misurazione registrata per ${created.memberName}.`;
		} catch (error: unknown) {
			feedbackError =
				error instanceof Error ? error.message : 'Impossibile registrare la misurazione.';
		} finally {
			measurementSubmitting = false;
		}
	}
</script>

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	<section class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
		<div class="space-y-2">
			<div class="flex flex-wrap items-center gap-2">
				<Badge variant="secondary" class="rounded-full px-3 py-1">Area coaching</Badge>
				{#if center.selectedGym}
					<Badge variant="outline" class="rounded-full px-3 py-1">{center.selectedGym.name}</Badge>
				{/if}
				{#if presetMembershipId}
					<Badge variant="outline" class="rounded-full px-3 py-1">Contesto membro attivo</Badge>
				{/if}
			</div>
			<div>
				<h2 class="text-2xl font-semibold tracking-tight">
					Schede, builder, assegnazioni e misure
				</h2>
				<p class="text-sm text-muted-foreground">
					Libreria esercizi, modelli multi-giorno, piani assegnati ai membri e valutazioni storiche.
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
				value={locationFilter || TRAINING_ALL_LOCATIONS_SELECT_VALUE}
				onValueChange={(value) =>
					(locationFilter = value === TRAINING_ALL_LOCATIONS_SELECT_VALUE ? '' : value)}
			>
				<Select.Trigger class="min-w-[180px]" disabled={!hasSelectedGym || workspaceLoading}>
					<span data-slot="select-value">
						{selectedLocationLabel(locationFilter, 'Tutte le sedi')}
					</span>
				</Select.Trigger>
				<Select.Content>
					<Select.Item value={TRAINING_ALL_LOCATIONS_SELECT_VALUE} label="Tutte le sedi">
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
				onclick={openExerciseSheet}
				disabled={!hasSelectedGym || workspaceLoading}
			>
				<LibraryBigIcon class="size-4" />
				Nuovo esercizio
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
				onclick={() => openAssignSheet()}
				disabled={!hasSelectedGym || workspaceLoading || templateOptions.length === 0}
			>
				<ClipboardListIcon class="size-4" />
				Assegna piano
			</Button>
			<Button
				variant="outline"
				size="sm"
				onclick={openMeasurementSheet}
				disabled={!hasSelectedGym || workspaceLoading}
			>
				<PlusCircleIcon class="size-4" />
				Nuova misurazione
			</Button>
		</div>
	</section>

	<nav
		class="flex flex-wrap gap-2 rounded-[20px] border border-border/70 bg-muted/20 p-2"
		aria-label="Sezioni training"
	>
		<a href="#assigned" class={sectionNavClass('#assigned')}> Piani assegnati </a>
		<a href="#templates" class={sectionNavClass('#templates')}> Template </a>
		<a href="#builder" class={sectionNavClass('#builder')}> Libreria esercizi </a>
		<a href="#measures" class={sectionNavClass('#measures')}> Misure </a>
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
				<CardTitle>Caricamento area training</CardTitle>
				<CardDescription>
					Sto recuperando membership, coach, overview, esercizi, modelli, piani e misurazioni prima
					di mostrarti l area coaching reale del tenant.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-dashed border-[#fecaca] bg-[#fff7f7]">
			<CardHeader>
				<CardTitle>Impossibile caricare l area training</CardTitle>
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
					Scegli prima la palestra dal selettore in alto a sinistra per vedere schede, assegnazioni,
					libreria esercizi e misurazioni del tenant corretto.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
		{#if overviewQuery.data}
			<section class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
				<Card>
					<CardHeader class="pb-2">
						<CardDescription>Esercizi in libreria</CardDescription>
						<CardTitle class="text-3xl">{overviewQuery.data.exercisesCount}</CardTitle>
					</CardHeader>
					<CardContent class="text-sm text-muted-foreground">
						Esercizi disponibili per il builder schede.
					</CardContent>
				</Card>
				<Card>
					<CardHeader class="pb-2">
						<CardDescription>Modelli attivi</CardDescription>
						<CardTitle class="text-3xl">{overviewQuery.data.templatesCount}</CardTitle>
					</CardHeader>
					<CardContent class="text-sm text-muted-foreground">
						Template riutilizzabili filtrati per sede.
					</CardContent>
				</Card>
				<Card>
					<CardHeader class="pb-2">
						<CardDescription>Piani assegnati</CardDescription>
						<CardTitle class="text-3xl">{overviewQuery.data.activeAssignmentsCount}</CardTitle>
					</CardHeader>
					<CardContent class="text-sm text-muted-foreground">
						Piani attivi sui membri del tenant.
					</CardContent>
				</Card>
				<Card>
					<CardHeader class="pb-2">
						<CardDescription>Revisioni da fare</CardDescription>
						<CardTitle class="text-3xl">{overviewQuery.data.revisionsDueCount}</CardTitle>
					</CardHeader>
					<CardContent class="text-sm text-muted-foreground">
						Piani attivi con revisione scaduta o in scadenza.
					</CardContent>
				</Card>
			</section>
		{/if}

		{#if revisionFocusAssignment}
			<section>
				<Card class="border-border/70">
					<CardHeader>
						<div class="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
							<div>
								<CardTitle>Desk coaching</CardTitle>
								<CardDescription>
									Il piano piu urgente da revisionare o seguire adesso.
								</CardDescription>
							</div>
							<div class="flex flex-wrap gap-2">
								<Button
									variant="outline"
									size="sm"
									onclick={() => focusAssignment(revisionFocusAssignment.assignmentId)}
								>
									Apri piano
								</Button>
								<Button
									size="sm"
									onclick={() => openMeasurementSheetForAssignment(revisionFocusAssignment)}
								>
									<PlusCircleIcon class="size-4" />
									Nuova misurazione
								</Button>
							</div>
						</div>
					</CardHeader>
					<CardContent class="grid gap-4 lg:grid-cols-[1.1fr_0.9fr]">
						<div class="rounded-[18px] border border-border/70 p-4">
							<div class="flex flex-wrap items-center gap-2">
								<p class="font-semibold">{revisionFocusAssignment.title}</p>
								<Badge variant={assignmentStatusMeta(revisionFocusAssignment.status).variant}>
									{assignmentStatusMeta(revisionFocusAssignment.status).label}
								</Badge>
							</div>
							<p class="mt-2 text-sm text-muted-foreground">
								{revisionFocusAssignment.memberName} - {revisionFocusAssignment.locationName}
							</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Coach: {revisionFocusAssignment.coachName}
							</p>
							{#if revisionFocusAssignment.notes}
								<p class="mt-3 text-sm text-muted-foreground">
									{revisionFocusAssignment.notes}
								</p>
							{/if}
						</div>
						<div class="grid gap-3 sm:grid-cols-3 lg:grid-cols-1 xl:grid-cols-3">
							<div class="rounded-[18px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Assegnato</p>
								<p class="mt-2 text-lg font-semibold">
									{date.format(revisionFocusAssignment.assignedAtUtc)}
								</p>
							</div>
							<div class="rounded-[18px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Revisione</p>
								<p class="mt-2 text-lg font-semibold">
									{revisionFocusAssignment.revisionDueAtUtc
										? date.format(revisionFocusAssignment.revisionDueAtUtc)
										: 'Non impostata'}
								</p>
							</div>
							<div class="rounded-[18px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
									Ultima misura
								</p>
								<p class="mt-2 text-lg font-semibold">
									{revisionFocusLatestMeasurement
										? date.format(revisionFocusLatestMeasurement.recordedAtUtc)
										: 'Assente'}
								</p>
							</div>
						</div>
					</CardContent>
				</Card>
			</section>
		{/if}

		<section class="grid gap-4">
			<Card id="assigned" class={sectionCardClass('#assigned')}>
				<CardHeader class="gap-4">
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>Piani assegnati</CardTitle>
							<CardDescription>
								Snapshot dei piani consegnati ai membri, con filtro per persona.
							</CardDescription>
						</div>
						<Badge variant="outline">{assignmentsQuery.data?.length ?? 0} piani</Badge>
					</div>
					<Select.Root
						type="single"
						value={membershipFilter || TRAINING_ALL_MEMBERS_SELECT_VALUE}
						onValueChange={(value) =>
							(membershipFilter = value === TRAINING_ALL_MEMBERS_SELECT_VALUE ? '' : value)}
					>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">
								{selectedMembershipLabel(membershipOptions, membershipFilter, 'Tutti i membri')}
							</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item value={TRAINING_ALL_MEMBERS_SELECT_VALUE} label="Tutti i membri">
								Tutti i membri
							</Select.Item>
							{#each membershipOptions as membership (membership.id)}
								<Select.Item value={membership.id} label={membership.label}>
									{membership.label}
								</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
				</CardHeader>
				<CardContent class="space-y-3">
					{#if assignmentsQuery.isPending}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Carico i piani assegnati...</p>
						</div>
					{:else if (assignmentsQuery.data ?? []).length === 0}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessun piano assegnato</p>
							<p class="mt-2 text-sm text-muted-foreground">
								Usa l'assegnazione per collegare un modello a un membro attivo.
							</p>
						</div>
					{:else}
						{#each assignmentsQuery.data ?? [] as assignment (assignment.assignmentId)}
							<button
								type="button"
								class={`w-full rounded-[18px] border border-border/70 p-4 text-left transition hover:border-border hover:bg-secondary/20 ${selectedAssignment?.assignmentId === assignment.assignmentId ? 'bg-[#eef4ff]' : ''}`}
								onclick={() => (selectedAssignmentId = assignment.assignmentId)}
							>
								<div class="flex items-start justify-between gap-3">
									<div>
										<p class="font-semibold">{assignment.title}</p>
										<p class="mt-1 text-sm text-muted-foreground">
											{assignment.memberName} - {assignment.locationName}
										</p>
									</div>
									<Badge variant={assignmentStatusMeta(assignment.status).variant}>
										{assignmentStatusMeta(assignment.status).label}
									</Badge>
								</div>
								<div class="mt-3 flex flex-wrap gap-3 text-sm text-muted-foreground">
									<span>{assignment.items.length} righe</span>
									<span>Assegnato {date.format(assignment.assignedAtUtc)}</span>
									{#if assignment.revisionDueAtUtc}
										<span>Revisione {date.format(assignment.revisionDueAtUtc)}</span>
									{/if}
								</div>
							</button>
						{/each}
					{/if}
				</CardContent>
			</Card>

			<Card id="templates" class={sectionCardClass('#templates')}>
				<CardHeader class="gap-4">
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>Modelli scheda</CardTitle>
							<CardDescription>
								Builder multi-giorno con coach, sede e prescrizione esercizi.
							</CardDescription>
						</div>
						<Badge variant="outline">{templatesQuery.data?.length ?? 0} modelli</Badge>
					</div>
				</CardHeader>
				<CardContent class="space-y-3">
					{#if templatesQuery.isPending}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Carico i modelli training...</p>
						</div>
					{:else if (templatesQuery.data ?? []).length === 0}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessun modello disponibile</p>
							<p class="mt-2 text-sm text-muted-foreground">
								Crea il primo template per iniziare con assegnazioni reali.
							</p>
						</div>
					{:else}
						{#each templatesQuery.data ?? [] as template (template.templateId)}
							<div class="rounded-[18px] border border-border/70 p-4">
								<div class="flex items-start justify-between gap-3">
									<div>
										<div class="flex flex-wrap items-center gap-2">
											<p class="font-semibold">{template.name}</p>
											<Badge variant="outline">{template.level}</Badge>
											<Badge variant={templateStatusMeta(template.isActive).variant}>
												{templateStatusMeta(template.isActive).label}
											</Badge>
										</div>
										<p class="mt-1 text-sm text-muted-foreground">
											{template.locationName} - {template.coachName}
										</p>
									</div>
									<div class="flex flex-wrap gap-2">
										<Button
											variant="outline"
											size="sm"
											onclick={() => openAssignSheet(template.templateId)}
											disabled={!template.isActive}
										>
											<SparklesIcon class="size-4" />
											Assegna
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
								<div class="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-3">
									<p>{template.daysPerWeek} giorni/settimana</p>
									<p>{template.items.length} righe esercizio</p>
									<p>{template.goal || 'Goal non specificato'}</p>
								</div>
								{#if !template.isActive}
									<p class="mt-3 text-sm text-muted-foreground">
										Template disattivato: resta nello storico ma non puo essere assegnato finche non
										viene riattivato.
									</p>
								{/if}
								{#if template.description}
									<p class="mt-3 text-sm text-muted-foreground">{template.description}</p>
								{/if}
							</div>
						{/each}
					{/if}
				</CardContent>
			</Card>
		</section>

		<section class="grid gap-4 xl:grid-cols-[0.9fr_1.1fr]">
			<Card id="builder" class={sectionCardClass('#builder')}>
				<CardHeader class="gap-4">
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>Libreria esercizi</CardTitle>
							<CardDescription>
								Base comune per costruire schede coerenti e riutilizzabili.
							</CardDescription>
						</div>
						<Badge variant="outline">{filteredExercises.length} esercizi</Badge>
					</div>
					<div class="relative">
						<SearchIcon
							class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
						/>
						<Input
							class="pl-9"
							placeholder="Cerca per nome, gruppo muscolare o attrezzo"
							bind:value={exerciseSearch}
						/>
					</div>
				</CardHeader>
				<CardContent>
					{#if exercisesQuery.isPending}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Carico la libreria esercizi...</p>
						</div>
					{:else if filteredExercises.length === 0}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessun esercizio trovato</p>
						</div>
					{:else}
						<div class="overflow-hidden rounded-[20px] border border-border/70">
							<Table>
								<TableHeader>
									<TableRow class="bg-secondary/30 hover:bg-secondary/30">
										<TableHead>Esercizio</TableHead>
										<TableHead>Focus</TableHead>
										<TableHead class="hidden md:table-cell">Attrezzo</TableHead>
									</TableRow>
								</TableHeader>
								<TableBody>
									{#each filteredExercises as exercise (exercise.exerciseId)}
										<TableRow>
											<TableCell>
												<div>
													<p class="font-medium">{exercise.name}</p>
													{#if exercise.category}
														<p class="text-sm text-muted-foreground">{exercise.category}</p>
													{/if}
												</div>
											</TableCell>
											<TableCell>{exercise.muscleGroup || 'Non specificato'}</TableCell>
											<TableCell class="hidden md:table-cell"
												>{exercise.equipment || 'Corpo libero'}</TableCell
											>
										</TableRow>
									{/each}
								</TableBody>
							</Table>
						</div>
					{/if}
				</CardContent>
			</Card>

			<Card>
				<CardHeader>
					<CardTitle>{selectedAssignment ? selectedAssignment.title : 'Dettaglio piano'}</CardTitle>
					<CardDescription>
						{#if selectedAssignment}
							{selectedAssignment.memberName} - {selectedAssignment.coachName} - {selectedAssignment.locationName}
						{:else}
							Seleziona un piano assegnato per vedere la scheda completa.
						{/if}
					</CardDescription>
				</CardHeader>
				<CardContent>
					{#if selectedAssignment}
						<div class="space-y-4">
							<div class="grid gap-3 sm:grid-cols-3">
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Stato</p>
									<div class="mt-2">
										<Badge variant={assignmentStatusMeta(selectedAssignment.status).variant}>
											{assignmentStatusMeta(selectedAssignment.status).label}
										</Badge>
									</div>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
										Assegnato il
									</p>
									<p class="mt-2 text-lg font-semibold">
										{date.format(selectedAssignment.assignedAtUtc)}
									</p>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Revisione</p>
									<p class="mt-2 text-lg font-semibold">
										{selectedAssignment.revisionDueAtUtc
											? date.format(selectedAssignment.revisionDueAtUtc)
											: 'Non impostata'}
									</p>
								</div>
							</div>

							<div class="rounded-[18px] border border-border/70 p-4">
								<div class="flex flex-wrap items-center justify-between gap-3">
									<div>
										<p class="text-sm font-semibold">Azioni scheda</p>
										<p class="text-sm text-muted-foreground">
											Chiudi, archivia o riattiva il piano senza perdere lo storico del cliente.
										</p>
									</div>
									<div class="flex flex-wrap gap-2">
										<Button
											size="sm"
											variant="outline"
											onclick={() => openMeasurementSheetForAssignment(selectedAssignment)}
										>
											<PlusCircleIcon class="size-4" />
											Misurazione
										</Button>
										<Button
											size="sm"
											variant="outline"
											onclick={() =>
												focusMeasurementsForMembership(selectedAssignment.membershipId)}
										>
											Vedi storico misure
										</Button>
										{#if selectedAssignment.status === 'Active'}
											<Button
												size="sm"
												onclick={() => handleAssignmentStatus('Completed')}
												disabled={assignmentActionPending ===
													`${selectedAssignment.assignmentId}-Completed`}
											>
												<CheckCircle2Icon class="size-4" />
												Completa
											</Button>
											<Button
												size="sm"
												variant="outline"
												onclick={() => handleAssignmentStatus('Archived')}
												disabled={assignmentActionPending ===
													`${selectedAssignment.assignmentId}-Archived`}
											>
												<ArchiveIcon class="size-4" />
												Archivia
											</Button>
										{:else if selectedAssignment.status === 'Completed'}
											<Button
												size="sm"
												variant="outline"
												onclick={() => handleAssignmentStatus('Active')}
												disabled={assignmentActionPending ===
													`${selectedAssignment.assignmentId}-Active`}
											>
												<RotateCcwIcon class="size-4" />
												Riattiva
											</Button>
											<Button
												size="sm"
												variant="outline"
												onclick={() => handleAssignmentStatus('Archived')}
												disabled={assignmentActionPending ===
													`${selectedAssignment.assignmentId}-Archived`}
											>
												<ArchiveIcon class="size-4" />
												Archivia
											</Button>
										{:else}
											<Button
												size="sm"
												variant="outline"
												onclick={() => handleAssignmentStatus('Active')}
												disabled={assignmentActionPending ===
													`${selectedAssignment.assignmentId}-Active`}
											>
												<RotateCcwIcon class="size-4" />
												Riattiva
											</Button>
										{/if}
									</div>
								</div>
								{#if selectedAssignment.completedAtUtc}
									<p class="mt-3 text-sm text-muted-foreground">
										Completata il {dateTime.format(selectedAssignment.completedAtUtc)}.
									</p>
								{/if}
							</div>

							<div class="rounded-[18px] border border-border/70 p-4 text-sm text-muted-foreground">
								<p>
									<span class="font-medium text-foreground">Template origine:</span>
									{selectedAssignment.templateName || 'Custom snapshot'}
								</p>
								<p class="mt-2">
									<span class="font-medium text-foreground">Goal:</span>
									{selectedAssignment.goal || 'Non specificato'}
								</p>
								<p class="mt-2">
									<span class="font-medium text-foreground">Ultima misurazione:</span>
									{selectedAssignmentLatestMeasurement
										? date.format(selectedAssignmentLatestMeasurement.recordedAtUtc)
										: 'Nessuna rilevazione collegata'}
								</p>
								{#if selectedAssignment.notes}
									<p class="mt-2">
										<span class="font-medium text-foreground">Note:</span>
										{selectedAssignment.notes}
									</p>
								{/if}
							</div>

							<div class="space-y-3">
								{#each selectedAssignmentDays as day (day.dayNumber)}
									<div class="rounded-[18px] border border-border/70 p-4">
										<div class="mb-3 flex items-center justify-between gap-3">
											<p class="font-semibold">Giorno {day.dayNumber}</p>
											<Badge variant="outline">{day.items.length} esercizi</Badge>
										</div>
										<div class="space-y-3">
											{#each day.items as item (item.assignmentItemId)}
												<div class="rounded-[14px] border border-border/60 bg-secondary/15 p-3">
													<div class="flex items-start justify-between gap-3">
														<div>
															<p class="font-medium">{item.exerciseName}</p>
															<p class="mt-1 text-sm text-muted-foreground">
																{item.setsPrescription} serie - {item.repetitionsPrescription}
																{#if item.restSeconds}
																	- recupero {item.restSeconds}s
																{/if}
																{#if item.tempo}
																	- tempo {item.tempo}
																{/if}
															</p>
														</div>
														<Badge variant="secondary">#{item.sortOrder}</Badge>
													</div>
													{#if item.notes}
														<p class="mt-2 text-xs text-muted-foreground">{item.notes}</p>
													{/if}
												</div>
											{/each}
										</div>
									</div>
								{/each}
							</div>
						</div>
					{:else}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessun piano selezionato</p>
						</div>
					{/if}
				</CardContent>
			</Card>
		</section>

		<section class="grid gap-4 xl:grid-cols-[0.95fr_1.05fr]">
			<Card id="measures" class={sectionCardClass('#measures')}>
				<CardHeader class="gap-4">
					<div class="flex items-start justify-between gap-3">
						<div>
							<CardTitle>Misure e valutazioni</CardTitle>
							<CardDescription>
								Storico rilevazioni corporee e note tecniche dei membri.
							</CardDescription>
						</div>
						<Badge variant="outline">{measurementsQuery.data?.length ?? 0} rilevazioni</Badge>
					</div>
					<div class="flex flex-col gap-3 sm:flex-row">
						<Select.Root
							type="single"
							value={membershipFilter || TRAINING_ALL_MEMBERS_SELECT_VALUE}
							onValueChange={(value) =>
								(membershipFilter = value === TRAINING_ALL_MEMBERS_SELECT_VALUE ? '' : value)}
						>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{selectedMembershipLabel(membershipOptions, membershipFilter, 'Tutti i membri')}
								</span>
							</Select.Trigger>
							<Select.Content>
								<Select.Item value={TRAINING_ALL_MEMBERS_SELECT_VALUE} label="Tutti i membri">
									Tutti i membri
								</Select.Item>
								{#each membershipOptions as membership (membership.id)}
									<Select.Item value={membership.id} label={membership.label}>
										{membership.label}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
						<Button variant="outline" size="sm" onclick={openMeasurementSheet}>
							<PlusCircleIcon class="size-4" />
							Registra
						</Button>
					</div>
				</CardHeader>
				<CardContent class="space-y-3">
					{#if measurementsQuery.isPending}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Carico le misure...</p>
						</div>
					{:else if (measurementsQuery.data ?? []).length === 0}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessuna misurazione registrata</p>
							<p class="mt-2 text-sm text-muted-foreground">
								Inizia con peso, body fat, circonferenze o note valutative del coach.
							</p>
						</div>
					{:else}
						{#each measurementsQuery.data ?? [] as measurement (measurement.assessmentId)}
							<button
								type="button"
								class={`w-full rounded-[18px] border border-border/70 p-4 text-left transition hover:border-border hover:bg-secondary/20 ${selectedMeasurement?.assessmentId === measurement.assessmentId ? 'bg-[#eef4ff]' : ''}`}
								onclick={() => (selectedMeasurementId = measurement.assessmentId)}
							>
								<div class="flex items-start justify-between gap-3">
									<div>
										<p class="font-semibold">{measurement.memberName}</p>
										<p class="mt-1 text-sm text-muted-foreground">
											{measurement.locationName} - {measurement.coachName}
										</p>
									</div>
									<Badge variant="secondary">{dateTime.format(measurement.recordedAtUtc)}</Badge>
								</div>
								<div class="mt-3 flex flex-wrap gap-3 text-sm text-muted-foreground">
									<span
										>{measurement.weightKg !== null
											? `${measurement.weightKg} kg`
											: 'Peso n.d.'}</span
									>
									<span>
										{measurement.bodyFatPercentage !== null
											? `${measurement.bodyFatPercentage}% body fat`
											: 'Body fat n.d.'}
									</span>
									{#if measurement.waistCm !== null}
										<span>Vita {measurement.waistCm} cm</span>
									{/if}
								</div>
							</button>
						{/each}
					{/if}
				</CardContent>
			</Card>

			<Card>
				<CardHeader>
					<CardTitle
						>{selectedMeasurement
							? selectedMeasurement.memberName
							: 'Dettaglio misurazione'}</CardTitle
					>
					<CardDescription>
						{#if selectedMeasurement}
							Registrata il {dateTime.format(selectedMeasurement.recordedAtUtc)} da {selectedMeasurement.recordedByUserName}
						{:else}
							Seleziona una rilevazione per vedere i valori salvati.
						{/if}
					</CardDescription>
				</CardHeader>
				<CardContent>
					{#if selectedMeasurement}
						<div class="space-y-4">
							<div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
								{#each selectedMeasurementFields as field (field.label)}
									<div class="rounded-[18px] border border-border/70 p-4">
										<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
											{field.label}
										</p>
										<p class="mt-2 text-lg font-semibold">{field.value}</p>
									</div>
								{/each}
							</div>

							<div class="rounded-[18px] border border-border/70 p-4 text-sm text-muted-foreground">
								<p>
									<span class="font-medium text-foreground">Membro:</span>
									{selectedMeasurement.memberName}
								</p>
								<p class="mt-2">
									<span class="font-medium text-foreground">Email:</span>
									{selectedMeasurement.memberEmail}
								</p>
								<p class="mt-2">
									<span class="font-medium text-foreground">Sede:</span>
									{selectedMeasurement.locationName}
								</p>
								<p class="mt-2">
									<span class="font-medium text-foreground">Coach:</span>
									{selectedMeasurement.coachName}
								</p>
								{#if selectedMeasurement.notes}
									<p class="mt-2">
										<span class="font-medium text-foreground">Note:</span>
										{selectedMeasurement.notes}
									</p>
								{/if}
							</div>
						</div>
					{:else}
						<div
							class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
						>
							<p class="font-semibold">Nessuna misurazione selezionata</p>
						</div>
					{/if}
				</CardContent>
			</Card>
		</section>
	{/if}
</main>

<Sheet.Root bind:open={exerciseSheetOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Nuovo esercizio</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				Aggiungi un esercizio alla libreria training del tenant.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateExercise}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Nome</span>
						<Input bind:value={exerciseForm.name} placeholder="Panca piana bilanciere" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Categoria</span>
						<Input bind:value={exerciseForm.category} placeholder="Push, Lower, Core..." />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Gruppo muscolare</span>
						<Input bind:value={exerciseForm.muscleGroup} placeholder="Petto, gambe, dorsali..." />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Attrezzo</span>
						<Input bind:value={exerciseForm.equipment} placeholder="Bilanciere, manubri, cavo..." />
					</label>
				</div>
				<label class="space-y-2">
					<span class="text-sm font-medium">Istruzioni</span>
					<Textarea
						bind:value={exerciseForm.instructions}
						rows={5}
						placeholder="Setup, cue tecnici, errori da evitare..."
					/>
				</label>
				<label class="space-y-2">
					<span class="text-sm font-medium">Video URL</span>
					<Input bind:value={exerciseForm.videoUrl} placeholder="https://..." />
				</label>
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (exerciseSheetOpen = false)}
					>Annulla</Button
				>
				<Button type="submit" disabled={exerciseSubmitting}>
					{#if exerciseSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<DumbbellIcon class="size-4" />
						Salva esercizio
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={templateSheetOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-3xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Nuovo modello scheda</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				Builder multi-giorno con esercizi, serie e prescrizioni operative.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateTemplate}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Sede</span>
						<Select.Root type="single" bind:value={templateForm.locationId}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{selectedLocationLabel(templateForm.locationId)}
								</span>
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
						<span class="text-sm font-medium">Coach</span>
						<Select.Root
							type="single"
							value={templateForm.coachAssignmentId || TRAINING_NO_COACH_SELECT_VALUE}
							onValueChange={(value) =>
								(templateForm.coachAssignmentId =
									value === TRAINING_NO_COACH_SELECT_VALUE ? '' : value)}
						>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{selectedCoachLabel(templateForm.coachAssignmentId, filteredCoachOptions)}
								</span>
							</Select.Trigger>
							<Select.Content>
								<Select.Item value={TRAINING_NO_COACH_SELECT_VALUE} label="Nessun coach fisso">
									Nessun coach fisso
								</Select.Item>
								{#each filteredCoachOptions as coach (coach.id)}
									<Select.Item value={coach.id} label={coach.label}>{coach.label}</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Nome modello</span>
						<Input bind:value={templateForm.name} placeholder="Full body base 3 giorni" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Goal</span>
						<Input
							bind:value={templateForm.goal}
							placeholder="Ipertrofia, ricondizionamento, dimagrimento..."
						/>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Livello</span>
						<Select.Root type="single" bind:value={templateForm.level}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{selectedLevelLabel(templateForm.level)}</span>
							</Select.Trigger>
							<Select.Content>
								{#each levelOptions as level (level.value)}
									<Select.Item value={level.value} label={level.label}>{level.label}</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Giorni / settimana</span>
						<Input type="number" min="1" max="14" bind:value={templateForm.daysPerWeek} />
					</label>
				</div>

				<label class="space-y-2">
					<span class="text-sm font-medium">Descrizione</span>
					<Textarea
						bind:value={templateForm.description}
						rows={4}
						placeholder="Contesto della scheda, progressione o note coach..."
					/>
				</label>

				<div class="space-y-3 rounded-[18px] border border-border/70 p-4">
					<div class="flex items-center justify-between gap-3">
						<p class="font-semibold">Righe builder</p>
						<Button type="button" variant="outline" size="sm" onclick={addTemplateItem}
							>Aggiungi esercizio</Button
						>
					</div>
					{#each templateForm.items as item, index (index)}
						<div
							class="grid gap-3 rounded-[16px] border border-border/70 bg-secondary/20 p-4 md:grid-cols-2"
						>
							<label class="space-y-2">
								<span class="text-sm font-medium">Esercizio da libreria</span>
								<Select.Root
									type="single"
									value={item.exerciseId || TRAINING_CUSTOM_EXERCISE_SELECT_VALUE}
									onValueChange={(value) =>
										updateTemplateItemExercise(
											index,
											value === TRAINING_CUSTOM_EXERCISE_SELECT_VALUE ? '' : value
										)}
								>
									<Select.Trigger class="w-full">
										<span data-slot="select-value">
											{selectedExerciseLibraryLabel(item.exerciseId)}
										</span>
									</Select.Trigger>
									<Select.Content>
										<Select.Item
											value={TRAINING_CUSTOM_EXERCISE_SELECT_VALUE}
											label="Custom / manuale"
										>
											Custom / manuale
										</Select.Item>
										{#each exercisesQuery.data ?? [] as exercise (exercise.exerciseId)}
											<Select.Item value={exercise.exerciseId} label={exercise.name}>
												{exercise.name}
											</Select.Item>
										{/each}
									</Select.Content>
								</Select.Root>
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Nome esercizio</span>
								<Input
									bind:value={item.exerciseName}
									placeholder="Se lasci la libreria vuota, inserisci qui il nome"
								/>
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Giorno</span>
								<Input
									type="number"
									min="1"
									max={templateForm.daysPerWeek}
									bind:value={item.dayNumber}
								/>
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Ordine</span>
								<Input type="number" min="1" bind:value={item.sortOrder} />
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Serie</span>
								<Input bind:value={item.setsPrescription} placeholder="4" />
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Ripetizioni</span>
								<Input bind:value={item.repetitionsPrescription} placeholder="8-10" />
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Recupero (sec)</span>
								<Input bind:value={item.restSeconds} placeholder="90" />
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Tempo</span>
								<Input bind:value={item.tempo} placeholder="3-1-1-0" />
							</label>
							<label class="space-y-2 md:col-span-2">
								<span class="text-sm font-medium">Note</span>
								<Input bind:value={item.notes} placeholder="Cue tecnici, variante, superset..." />
							</label>
							<div class="flex items-end justify-end md:col-span-2">
								<Button
									type="button"
									variant="outline"
									size="sm"
									disabled={templateForm.items.length === 1}
									onclick={() =>
										(templateForm = {
											...templateForm,
											items: templateForm.items.filter((_, currentIndex) => currentIndex !== index)
										})}
								>
									Rimuovi
								</Button>
							</div>
						</div>
					{/each}
				</div>
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (templateSheetOpen = false)}
					>Annulla</Button
				>
				<Button type="submit" disabled={templateSubmitting}>
					{#if templateSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<ClipboardListIcon class="size-4" />
						Salva modello
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={assignSheetOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Assegna piano</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				Collega un modello training a un membro attivo, salvando una copia snapshot.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateAssignment}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<label class="space-y-2">
					<span class="text-sm font-medium">Modello</span>
					<Select.Root type="single" bind:value={assignForm.templateId}>
						<Select.Trigger class="w-full" disabled={templateOptions.length === 0}>
							<span data-slot="select-value">
								{selectedTemplateLabel(
									assignForm.templateId,
									templateOptions.length === 0
										? 'Nessun modello attivo disponibile'
										: 'Seleziona un modello'
								)}
							</span>
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

				{#if templateOptions.length === 0}
					<div
						class="rounded-[18px] border border-dashed border-border bg-secondary/15 px-4 py-6 text-sm text-muted-foreground"
					>
						Non ci sono modelli attivi nella selezione corrente. Riattiva un template oppure creane
						uno nuovo prima di assegnare una scheda.
					</div>
				{/if}

				<label class="space-y-2">
					<span class="text-sm font-medium">Membro</span>
					<Select.Root type="single" bind:value={assignForm.membershipId}>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">
								{selectedMembershipLabel(
									assignableMembershipOptions,
									assignForm.membershipId,
									'Seleziona un membro'
								)}
							</span>
						</Select.Trigger>
						<Select.Content>
							{#each assignableMembershipOptions as membership (membership.id)}
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

				{#if assignableMembershipOptions.length === 0}
					<div
						class="rounded-[18px] border border-dashed border-border bg-secondary/15 px-4 py-6 text-sm text-muted-foreground"
					>
						Nessun membro attivo disponibile sulla sede del template selezionato.
					</div>
				{/if}

				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Inizio piano</span>
						<input
							class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
							type="datetime-local"
							bind:value={assignForm.startsAtLocal}
						/>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Revisione prevista</span>
						<input
							class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
							type="datetime-local"
							bind:value={assignForm.revisionDueAtLocal}
						/>
					</label>
				</div>

				<label class="space-y-2">
					<span class="text-sm font-medium">Note assegnazione</span>
					<Textarea
						bind:value={assignForm.notes}
						rows={4}
						placeholder="Focus della prima fase, limiti fisici, indicazioni coach..."
					/>
				</label>
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (assignSheetOpen = false)}
					>Annulla</Button
				>
				<Button
					type="submit"
					disabled={assignmentSubmitting ||
						templateOptions.length === 0 ||
						!assignForm.templateId ||
						assignableMembershipOptions.length === 0}
				>
					{#if assignmentSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<SparklesIcon class="size-4" />
						Assegna piano
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={measurementSheetOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-2xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Nuova misurazione</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				Registra peso, circonferenze e note tecniche nel profilo training del membro.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateMeasurement}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Membro</span>
						<Select.Root type="single" bind:value={measurementForm.membershipId}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{selectedMembershipLabel(
										measurableMembershipOptions,
										measurementForm.membershipId,
										'Seleziona un membro'
									)}
								</span>
							</Select.Trigger>
							<Select.Content>
								{#each measurableMembershipOptions as membership (membership.id)}
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
					<label class="space-y-2">
						<span class="text-sm font-medium">Sede</span>
						<Select.Root type="single" bind:value={measurementForm.locationId}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{selectedLocationLabel(measurementForm.locationId)}
								</span>
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
						<span class="text-sm font-medium">Coach</span>
						<Select.Root
							type="single"
							value={measurementForm.coachAssignmentId || TRAINING_NO_COACH_SELECT_VALUE}
							onValueChange={(value) =>
								(measurementForm.coachAssignmentId =
									value === TRAINING_NO_COACH_SELECT_VALUE ? '' : value)}
						>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{selectedCoachLabel(
										measurementForm.coachAssignmentId,
										filteredMeasurementCoachOptions,
										'Coach non specificato'
									)}
								</span>
							</Select.Trigger>
							<Select.Content>
								<Select.Item value={TRAINING_NO_COACH_SELECT_VALUE} label="Coach non specificato">
									Coach non specificato
								</Select.Item>
								{#each filteredMeasurementCoachOptions as coach (coach.id)}
									<Select.Item value={coach.id} label={coach.label}>{coach.label}</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Data rilevazione</span>
						<input
							class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
							type="datetime-local"
							bind:value={measurementForm.recordedAtLocal}
						/>
					</label>
				</div>

				<div class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
					<label class="space-y-2">
						<span class="text-sm font-medium">Peso (kg)</span>
						<Input bind:value={measurementForm.weightKg} placeholder="72.4" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Body fat (%)</span>
						<Input bind:value={measurementForm.bodyFatPercentage} placeholder="18.5" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Massa magra (kg)</span>
						<Input bind:value={measurementForm.leanMassKg} placeholder="58.3" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Torace (cm)</span>
						<Input bind:value={measurementForm.chestCm} placeholder="101" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Vita (cm)</span>
						<Input bind:value={measurementForm.waistCm} placeholder="84" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Fianchi (cm)</span>
						<Input bind:value={measurementForm.hipsCm} placeholder="96" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Braccio (cm)</span>
						<Input bind:value={measurementForm.armCm} placeholder="34" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Coscia (cm)</span>
						<Input bind:value={measurementForm.thighCm} placeholder="58" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Polpaccio (cm)</span>
						<Input bind:value={measurementForm.calfCm} placeholder="37" />
					</label>
					<label class="space-y-2 md:col-span-2 xl:col-span-1">
						<span class="text-sm font-medium">FC a riposo (bpm)</span>
						<Input bind:value={measurementForm.restingHeartRateBpm} placeholder="62" />
					</label>
				</div>

				<label class="space-y-2">
					<span class="text-sm font-medium">Note valutative</span>
					<Textarea
						bind:value={measurementForm.notes}
						rows={5}
						placeholder="Asimmetrie, mobilita, focus del prossimo blocco, osservazioni coach..."
					/>
				</label>

				{#if measurableMembershipOptions.length === 0}
					<div
						class="rounded-[18px] border border-dashed border-border bg-secondary/15 px-4 py-6 text-sm text-muted-foreground"
					>
						Nessun membro disponibile sulla sede selezionata per registrare una misurazione.
					</div>
				{/if}
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button type="button" variant="outline" onclick={() => (measurementSheetOpen = false)}
					>Annulla</Button
				>
				<Button
					type="submit"
					disabled={measurementSubmitting || measurableMembershipOptions.length === 0}
				>
					{#if measurementSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<PlusCircleIcon class="size-4" />
						Salva misurazione
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>
