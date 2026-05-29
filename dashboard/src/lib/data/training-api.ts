import { getAuthenticatedSession } from '$lib/utils/auth-session-storage';

const API_BASE_URL = 'http://localhost:5299';

type ApiEnvelope<T> = {
	success: boolean;
	data: T | null;
	message?: string | null;
	error?: {
		code?: string | null;
		message?: string | null;
		details?: Record<string, string[]>;
	} | null;
};

export type WorkoutTemplateLevel = 'Beginner' | 'Intermediate' | 'Advanced' | 'Mixed';
export type WorkoutAssignmentStatus = 'Active' | 'Completed' | 'Archived';

export type GymWorkoutExercise = {
	exerciseId: string;
	gymId: string;
	name: string;
	category: string | null;
	muscleGroup: string | null;
	equipment: string | null;
	instructions: string | null;
	videoUrl: string | null;
	isActive: boolean;
	createdAtUtc: Date;
	updatedAtUtc: Date;
};

export type GymWorkoutTemplateItem = {
	templateItemId: string;
	exerciseId: string | null;
	exerciseName: string;
	dayNumber: number;
	sortOrder: number;
	setsPrescription: string;
	repetitionsPrescription: string;
	restSeconds: number | null;
	tempo: string | null;
	notes: string | null;
};

export type GymWorkoutTemplate = {
	templateId: string;
	gymId: string;
	locationId: string;
	locationName: string;
	coachAssignmentId: string | null;
	coachName: string;
	name: string;
	goal: string | null;
	level: WorkoutTemplateLevel;
	description: string | null;
	daysPerWeek: number;
	isActive: boolean;
	createdAtUtc: Date;
	updatedAtUtc: Date;
	items: GymWorkoutTemplateItem[];
};

export type GymWorkoutAssignmentItem = {
	assignmentItemId: string;
	exerciseId: string | null;
	exerciseName: string;
	dayNumber: number;
	sortOrder: number;
	setsPrescription: string;
	repetitionsPrescription: string;
	restSeconds: number | null;
	tempo: string | null;
	notes: string | null;
};

export type GymWorkoutAssignment = {
	assignmentId: string;
	gymId: string;
	membershipId: string;
	locationId: string;
	locationName: string;
	templateId: string | null;
	templateName: string | null;
	coachAssignmentId: string | null;
	coachName: string;
	memberName: string;
	memberEmail: string;
	title: string;
	goal: string | null;
	status: WorkoutAssignmentStatus;
	assignedAtUtc: Date;
	startsAtUtc: Date | null;
	revisionDueAtUtc: Date | null;
	completedAtUtc: Date | null;
	notes: string | null;
	items: GymWorkoutAssignmentItem[];
};

export type GymWorkoutAssessment = {
	assessmentId: string;
	gymId: string;
	membershipId: string;
	locationId: string;
	locationName: string;
	coachAssignmentId: string | null;
	coachName: string;
	memberName: string;
	memberEmail: string;
	recordedByUserId: string;
	recordedByUserName: string;
	recordedAtUtc: Date;
	weightKg: number | null;
	bodyFatPercentage: number | null;
	leanMassKg: number | null;
	chestCm: number | null;
	waistCm: number | null;
	hipsCm: number | null;
	armCm: number | null;
	thighCm: number | null;
	calfCm: number | null;
	restingHeartRateBpm: number | null;
	notes: string | null;
};

export type GymTrainingOverview = {
	exercisesCount: number;
	templatesCount: number;
	activeAssignmentsCount: number;
	revisionsDueCount: number;
	recentAssignments: GymWorkoutAssignment[];
	generatedAtUtc: Date;
};

export type CreateGymWorkoutExerciseRequest = {
	name: string;
	category?: string | null;
	muscleGroup?: string | null;
	equipment?: string | null;
	instructions?: string | null;
	videoUrl?: string | null;
};

export type CreateGymWorkoutTemplateItemRequest = {
	exerciseId?: string | null;
	exerciseName: string;
	dayNumber: number;
	sortOrder: number;
	setsPrescription: string;
	repetitionsPrescription: string;
	restSeconds?: number | null;
	tempo?: string | null;
	notes?: string | null;
};

export type CreateGymWorkoutTemplateRequest = {
	locationId: string;
	coachAssignmentId?: string | null;
	name: string;
	goal?: string | null;
	level: WorkoutTemplateLevel;
	description?: string | null;
	daysPerWeek: number;
	items: CreateGymWorkoutTemplateItemRequest[];
};

export type CreateGymWorkoutAssignmentRequest = {
	membershipId: string;
	templateId: string;
	startsAtUtc?: string | null;
	revisionDueAtUtc?: string | null;
	notes?: string | null;
};

export type UpdateGymWorkoutTemplateActivationRequest = {
	isActive: boolean;
};

export type UpdateGymWorkoutAssignmentStatusRequest = {
	status: WorkoutAssignmentStatus;
	completedAtUtc?: string | null;
};

export type CreateGymWorkoutAssessmentRequest = {
	membershipId: string;
	locationId: string;
	coachAssignmentId?: string | null;
	recordedAtUtc?: string | null;
	weightKg?: number | null;
	bodyFatPercentage?: number | null;
	leanMassKg?: number | null;
	chestCm?: number | null;
	waistCm?: number | null;
	hipsCm?: number | null;
	armCm?: number | null;
	thighCm?: number | null;
	calfCm?: number | null;
	restingHeartRateBpm?: number | null;
	notes?: string | null;
};

type TrainingFilters = {
	locationId?: string | null;
	membershipId?: string | null;
	search?: string | null;
	category?: string | null;
};

function parseDate(value: string | null | undefined) {
	if (!value) {
		return null;
	}

	const parsed = new Date(value);
	return Number.isNaN(parsed.getTime()) ? null : parsed;
}

function getAccessToken() {
	return getAuthenticatedSession()?.token ?? '';
}

async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
	const response = await fetch(`${API_BASE_URL}${path}`, {
		...init,
		headers: {
			'Content-Type': 'application/json',
			Authorization: `Bearer ${getAccessToken()}`,
			...(init?.headers ?? {})
		}
	});

	const payload = (await response.json()) as ApiEnvelope<T>;
	if (!response.ok || !payload.success || payload.data === null || payload.data === undefined) {
		throw new Error(payload.error?.message ?? payload.message ?? 'Richiesta API non riuscita.');
	}

	return payload.data;
}

function buildFiltersQuery(filters: TrainingFilters = {}) {
	const params = new URLSearchParams();
	if (filters.locationId) {
		params.set('locationId', filters.locationId);
	}
	if (filters.membershipId) {
		params.set('membershipId', filters.membershipId);
	}
	if (filters.search?.trim()) {
		params.set('search', filters.search.trim());
	}
	if (filters.category?.trim()) {
		params.set('category', filters.category.trim());
	}

	const query = params.toString();
	return query ? `?${query}` : '';
}

function mapExercise(exercise: {
	[id: string]: unknown;
	createdAtUtc?: string | null;
	updatedAtUtc?: string | null;
}) {
	return {
		exerciseId: String(exercise.exerciseId),
		gymId: String(exercise.gymId),
		name: String(exercise.name),
		category: (exercise.category as string | null | undefined) ?? null,
		muscleGroup: (exercise.muscleGroup as string | null | undefined) ?? null,
		equipment: (exercise.equipment as string | null | undefined) ?? null,
		instructions: (exercise.instructions as string | null | undefined) ?? null,
		videoUrl: (exercise.videoUrl as string | null | undefined) ?? null,
		isActive: Boolean(exercise.isActive),
		createdAtUtc: parseDate(exercise.createdAtUtc) ?? new Date(),
		updatedAtUtc: parseDate(exercise.updatedAtUtc) ?? new Date()
	} satisfies GymWorkoutExercise;
}

function mapTemplateItem(item: { [id: string]: unknown }) {
	return {
		templateItemId: String(item.templateItemId),
		exerciseId: (item.exerciseId as string | null | undefined) ?? null,
		exerciseName: String(item.exerciseName),
		dayNumber: Number(item.dayNumber),
		sortOrder: Number(item.sortOrder),
		setsPrescription: String(item.setsPrescription),
		repetitionsPrescription: String(item.repetitionsPrescription),
		restSeconds:
			item.restSeconds === null || item.restSeconds === undefined ? null : Number(item.restSeconds),
		tempo: (item.tempo as string | null | undefined) ?? null,
		notes: (item.notes as string | null | undefined) ?? null
	} satisfies GymWorkoutTemplateItem;
}

function mapTemplate(template: {
	[id: string]: unknown;
	createdAtUtc?: string | null;
	updatedAtUtc?: string | null;
	items?: Array<Record<string, unknown>>;
}) {
	return {
		templateId: String(template.templateId),
		gymId: String(template.gymId),
		locationId: String(template.locationId),
		locationName: String(template.locationName),
		coachAssignmentId: (template.coachAssignmentId as string | null | undefined) ?? null,
		coachName: String(template.coachName),
		name: String(template.name),
		goal: (template.goal as string | null | undefined) ?? null,
		level: template.level as WorkoutTemplateLevel,
		description: (template.description as string | null | undefined) ?? null,
		daysPerWeek: Number(template.daysPerWeek),
		isActive: Boolean(template.isActive),
		createdAtUtc: parseDate(template.createdAtUtc) ?? new Date(),
		updatedAtUtc: parseDate(template.updatedAtUtc) ?? new Date(),
		items: Array.isArray(template.items) ? template.items.map(mapTemplateItem) : []
	} satisfies GymWorkoutTemplate;
}

function mapAssignmentItem(item: { [id: string]: unknown }) {
	return {
		assignmentItemId: String(item.assignmentItemId),
		exerciseId: (item.exerciseId as string | null | undefined) ?? null,
		exerciseName: String(item.exerciseName),
		dayNumber: Number(item.dayNumber),
		sortOrder: Number(item.sortOrder),
		setsPrescription: String(item.setsPrescription),
		repetitionsPrescription: String(item.repetitionsPrescription),
		restSeconds:
			item.restSeconds === null || item.restSeconds === undefined ? null : Number(item.restSeconds),
		tempo: (item.tempo as string | null | undefined) ?? null,
		notes: (item.notes as string | null | undefined) ?? null
	} satisfies GymWorkoutAssignmentItem;
}

function mapAssignment(assignment: {
	[id: string]: unknown;
	assignedAtUtc?: string | null;
	startsAtUtc?: string | null;
	revisionDueAtUtc?: string | null;
	completedAtUtc?: string | null;
	items?: Array<Record<string, unknown>>;
}) {
	return {
		assignmentId: String(assignment.assignmentId),
		gymId: String(assignment.gymId),
		membershipId: String(assignment.membershipId),
		locationId: String(assignment.locationId),
		locationName: String(assignment.locationName),
		templateId: (assignment.templateId as string | null | undefined) ?? null,
		templateName: (assignment.templateName as string | null | undefined) ?? null,
		coachAssignmentId: (assignment.coachAssignmentId as string | null | undefined) ?? null,
		coachName: String(assignment.coachName),
		memberName: String(assignment.memberName),
		memberEmail: String(assignment.memberEmail),
		title: String(assignment.title),
		goal: (assignment.goal as string | null | undefined) ?? null,
		status: assignment.status as WorkoutAssignmentStatus,
		assignedAtUtc: parseDate(assignment.assignedAtUtc) ?? new Date(),
		startsAtUtc: parseDate(assignment.startsAtUtc),
		revisionDueAtUtc: parseDate(assignment.revisionDueAtUtc),
		completedAtUtc: parseDate(assignment.completedAtUtc),
		notes: (assignment.notes as string | null | undefined) ?? null,
		items: Array.isArray(assignment.items) ? assignment.items.map(mapAssignmentItem) : []
	} satisfies GymWorkoutAssignment;
}

function mapAssessment(assessment: { [id: string]: unknown; recordedAtUtc?: string | null }) {
	return {
		assessmentId: String(assessment.assessmentId),
		gymId: String(assessment.gymId),
		membershipId: String(assessment.membershipId),
		locationId: String(assessment.locationId),
		locationName: String(assessment.locationName),
		coachAssignmentId: (assessment.coachAssignmentId as string | null | undefined) ?? null,
		coachName: String(assessment.coachName),
		memberName: String(assessment.memberName),
		memberEmail: String(assessment.memberEmail),
		recordedByUserId: String(assessment.recordedByUserId),
		recordedByUserName: String(assessment.recordedByUserName),
		recordedAtUtc: parseDate(assessment.recordedAtUtc) ?? new Date(),
		weightKg:
			assessment.weightKg === null || assessment.weightKg === undefined
				? null
				: Number(assessment.weightKg),
		bodyFatPercentage:
			assessment.bodyFatPercentage === null || assessment.bodyFatPercentage === undefined
				? null
				: Number(assessment.bodyFatPercentage),
		leanMassKg:
			assessment.leanMassKg === null || assessment.leanMassKg === undefined
				? null
				: Number(assessment.leanMassKg),
		chestCm:
			assessment.chestCm === null || assessment.chestCm === undefined
				? null
				: Number(assessment.chestCm),
		waistCm:
			assessment.waistCm === null || assessment.waistCm === undefined
				? null
				: Number(assessment.waistCm),
		hipsCm:
			assessment.hipsCm === null || assessment.hipsCm === undefined
				? null
				: Number(assessment.hipsCm),
		armCm:
			assessment.armCm === null || assessment.armCm === undefined ? null : Number(assessment.armCm),
		thighCm:
			assessment.thighCm === null || assessment.thighCm === undefined
				? null
				: Number(assessment.thighCm),
		calfCm:
			assessment.calfCm === null || assessment.calfCm === undefined
				? null
				: Number(assessment.calfCm),
		restingHeartRateBpm:
			assessment.restingHeartRateBpm === null || assessment.restingHeartRateBpm === undefined
				? null
				: Number(assessment.restingHeartRateBpm),
		notes: (assessment.notes as string | null | undefined) ?? null
	} satisfies GymWorkoutAssessment;
}

export async function fetchGymTrainingOverview(gymId: string, filters: TrainingFilters = {}) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/training/overview${buildFiltersQuery(filters)}`
	);

	return {
		exercisesCount: Number(data.exercisesCount),
		templatesCount: Number(data.templatesCount),
		activeAssignmentsCount: Number(data.activeAssignmentsCount),
		revisionsDueCount: Number(data.revisionsDueCount),
		recentAssignments: Array.isArray(data.recentAssignments)
			? data.recentAssignments.map((item) => mapAssignment(item as Record<string, unknown>))
			: [],
		generatedAtUtc: parseDate(data.generatedAtUtc as string | null | undefined) ?? new Date()
	} satisfies GymTrainingOverview;
}

export async function fetchGymWorkoutExercises(gymId: string, filters: TrainingFilters = {}) {
	const data = await apiRequest<Array<Record<string, unknown>>>(
		`/api/gyms/${gymId}/training/exercises${buildFiltersQuery(filters)}`
	);

	return data.map(mapExercise);
}

export async function createGymWorkoutExercise(
	gymId: string,
	request: CreateGymWorkoutExerciseRequest
) {
	const data = await apiRequest<Record<string, unknown>>(`/api/gyms/${gymId}/training/exercises`, {
		method: 'POST',
		body: JSON.stringify(request)
	});

	return mapExercise(data);
}

export async function fetchGymWorkoutTemplates(gymId: string, filters: TrainingFilters = {}) {
	const data = await apiRequest<Array<Record<string, unknown>>>(
		`/api/gyms/${gymId}/training/templates${buildFiltersQuery(filters)}`
	);

	return data.map(mapTemplate);
}

export async function createGymWorkoutTemplate(
	gymId: string,
	request: CreateGymWorkoutTemplateRequest
) {
	const data = await apiRequest<Record<string, unknown>>(`/api/gyms/${gymId}/training/templates`, {
		method: 'POST',
		body: JSON.stringify(request)
	});

	return mapTemplate(data);
}

export async function updateGymWorkoutTemplateActivation(
	gymId: string,
	templateId: string,
	request: UpdateGymWorkoutTemplateActivationRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/training/templates/${templateId}/activation`,
		{
			method: 'PATCH',
			body: JSON.stringify(request)
		}
	);

	return mapTemplate(data);
}

export async function fetchGymWorkoutAssignments(gymId: string, filters: TrainingFilters = {}) {
	const data = await apiRequest<Array<Record<string, unknown>>>(
		`/api/gyms/${gymId}/training/assignments${buildFiltersQuery(filters)}`
	);

	return data.map(mapAssignment);
}

export async function createGymWorkoutAssignment(
	gymId: string,
	request: CreateGymWorkoutAssignmentRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/training/assignments`,
		{
			method: 'POST',
			body: JSON.stringify(request)
		}
	);

	return mapAssignment(data);
}

export async function updateGymWorkoutAssignmentStatus(
	gymId: string,
	assignmentId: string,
	request: UpdateGymWorkoutAssignmentStatusRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/training/assignments/${assignmentId}/status`,
		{
			method: 'PATCH',
			body: JSON.stringify(request)
		}
	);

	return mapAssignment(data);
}

export async function fetchGymWorkoutAssessments(gymId: string, filters: TrainingFilters = {}) {
	const data = await apiRequest<Array<Record<string, unknown>>>(
		`/api/gyms/${gymId}/training/measurements${buildFiltersQuery(filters)}`
	);

	return data.map(mapAssessment);
}

export async function createGymWorkoutAssessment(
	gymId: string,
	request: CreateGymWorkoutAssessmentRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/training/measurements`,
		{
			method: 'POST',
			body: JSON.stringify(request)
		}
	);

	return mapAssessment(data);
}
