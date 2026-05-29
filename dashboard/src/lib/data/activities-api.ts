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

export type ActivitySessionStatus = 'Scheduled' | 'Cancelled' | 'Completed';
export type ActivityBookingStatus = 'Booked' | 'CheckedIn' | 'Cancelled' | 'NoShow';

export type GymActivityTemplate = {
	templateId: string;
	gymId: string;
	locationId: string;
	locationName: string;
	instructorAssignmentId: string | null;
	instructorName: string;
	name: string;
	category: string;
	description: string | null;
	colorHex: string | null;
	capacity: number;
	durationMinutes: number;
	requiresBooking: boolean;
	isActive: boolean;
	createdAtUtc: Date;
	updatedAtUtc: Date;
};

export type GymActivityBooking = {
	bookingId: string;
	sessionId: string;
	membershipId: string;
	memberName: string;
	memberEmail: string;
	status: ActivityBookingStatus;
	bookedAtUtc: Date;
	checkedInAtUtc: Date | null;
	cancelledAtUtc: Date | null;
	notes: string | null;
};

export type GymActivitySession = {
	sessionId: string;
	templateId: string;
	gymId: string;
	locationId: string;
	locationName: string;
	instructorAssignmentId: string | null;
	instructorName: string;
	title: string;
	category: string;
	colorHex: string | null;
	capacity: number;
	activeBookingsCount: number;
	checkedInCount: number;
	remainingSpots: number;
	startsAtUtc: Date;
	endsAtUtc: Date;
	status: ActivitySessionStatus;
	notes: string | null;
	bookings: GymActivityBooking[];
};

export type GymActivitiesOverview = {
	sessionsNext7DaysCount: number;
	bookingsNext7DaysCount: number;
	checkedInTodayCount: number;
	noShowLast30DaysCount: number;
	upcomingSessions: GymActivitySession[];
	generatedAtUtc: Date;
};

export type CreateGymActivityTemplateRequest = {
	locationId: string;
	instructorAssignmentId?: string | null;
	name: string;
	category?: string;
	description?: string | null;
	colorHex?: string | null;
	capacity: number;
	durationMinutes: number;
	requiresBooking: boolean;
};

export type UpdateGymActivityTemplateActivationRequest = {
	isActive: boolean;
};

export type CreateGymActivitySessionRequest = {
	templateId: string;
	startsAtUtc: string;
	endsAtUtc?: string | null;
	notes?: string | null;
};

export type CreateGymActivityBookingRequest = {
	membershipId: string;
	notes?: string | null;
};

export type UpdateGymActivitySessionStatusRequest = {
	status: ActivitySessionStatus;
};

export type UpdateGymActivityBookingStatusRequest = {
	status: ActivityBookingStatus;
	notes?: string | null;
};

type FetchActivitiesFilters = {
	locationId?: string | null;
	membershipId?: string | null;
	fromUtc?: string | null;
	toUtc?: string | null;
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

function mapBooking(booking: {
	[id: string]: unknown;
	bookedAtUtc?: string | null;
	checkedInAtUtc?: string | null;
	cancelledAtUtc?: string | null;
}) {
	return {
		bookingId: String(booking.bookingId),
		sessionId: String(booking.sessionId),
		membershipId: String(booking.membershipId),
		memberName: String(booking.memberName),
		memberEmail: String(booking.memberEmail),
		status: booking.status as ActivityBookingStatus,
		bookedAtUtc: parseDate(booking.bookedAtUtc) ?? new Date(),
		checkedInAtUtc: parseDate(booking.checkedInAtUtc),
		cancelledAtUtc: parseDate(booking.cancelledAtUtc),
		notes: (booking.notes as string | null | undefined) ?? null
	} satisfies GymActivityBooking;
}

function mapSession(session: {
	[id: string]: unknown;
	startsAtUtc?: string | null;
	endsAtUtc?: string | null;
	bookings?: Array<Record<string, unknown>>;
}) {
	return {
		sessionId: String(session.sessionId),
		templateId: String(session.templateId),
		gymId: String(session.gymId),
		locationId: String(session.locationId),
		locationName: String(session.locationName),
		instructorAssignmentId: (session.instructorAssignmentId as string | null | undefined) ?? null,
		instructorName: String(session.instructorName),
		title: String(session.title),
		category: String(session.category),
		colorHex: (session.colorHex as string | null | undefined) ?? null,
		capacity: Number(session.capacity),
		activeBookingsCount: Number(session.activeBookingsCount),
		checkedInCount: Number(session.checkedInCount),
		remainingSpots: Number(session.remainingSpots),
		startsAtUtc: parseDate(session.startsAtUtc) ?? new Date(),
		endsAtUtc: parseDate(session.endsAtUtc) ?? new Date(),
		status: session.status as ActivitySessionStatus,
		notes: (session.notes as string | null | undefined) ?? null,
		bookings: Array.isArray(session.bookings)
			? session.bookings.map((item) => mapBooking(item))
			: []
	} satisfies GymActivitySession;
}

function mapTemplate(template: {
	[id: string]: unknown;
	createdAtUtc?: string | null;
	updatedAtUtc?: string | null;
}) {
	return {
		templateId: String(template.templateId),
		gymId: String(template.gymId),
		locationId: String(template.locationId),
		locationName: String(template.locationName),
		instructorAssignmentId: (template.instructorAssignmentId as string | null | undefined) ?? null,
		instructorName: String(template.instructorName),
		name: String(template.name),
		category: String(template.category),
		description: (template.description as string | null | undefined) ?? null,
		colorHex: (template.colorHex as string | null | undefined) ?? null,
		capacity: Number(template.capacity),
		durationMinutes: Number(template.durationMinutes),
		requiresBooking: Boolean(template.requiresBooking),
		isActive: Boolean(template.isActive),
		createdAtUtc: parseDate(template.createdAtUtc) ?? new Date(),
		updatedAtUtc: parseDate(template.updatedAtUtc) ?? new Date()
	} satisfies GymActivityTemplate;
}

function mapOverview(data: Record<string, unknown>) {
	return {
		sessionsNext7DaysCount: Number(data.sessionsNext7DaysCount),
		bookingsNext7DaysCount: Number(data.bookingsNext7DaysCount),
		checkedInTodayCount: Number(data.checkedInTodayCount),
		noShowLast30DaysCount: Number(data.noShowLast30DaysCount),
		upcomingSessions: Array.isArray(data.upcomingSessions)
			? data.upcomingSessions.map((item) => mapSession(item as Record<string, unknown>))
			: [],
		generatedAtUtc: parseDate(data.generatedAtUtc as string | null | undefined) ?? new Date()
	} satisfies GymActivitiesOverview;
}

function buildFiltersQuery(filters: FetchActivitiesFilters = {}) {
	const params = new URLSearchParams();
	if (filters.locationId) {
		params.set('locationId', filters.locationId);
	}
	if (filters.membershipId) {
		params.set('membershipId', filters.membershipId);
	}
	if (filters.fromUtc) {
		params.set('fromUtc', filters.fromUtc);
	}
	if (filters.toUtc) {
		params.set('toUtc', filters.toUtc);
	}

	const query = params.toString();
	return query ? `?${query}` : '';
}

export async function fetchGymActivitiesOverview(
	gymId: string,
	filters: FetchActivitiesFilters = {}
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/activities/overview${buildFiltersQuery(filters)}`
	);

	return mapOverview(data);
}

export async function fetchGymActivityTemplates(
	gymId: string,
	filters: FetchActivitiesFilters = {}
) {
	const data = await apiRequest<Array<Record<string, unknown>>>(
		`/api/gyms/${gymId}/activities/templates${buildFiltersQuery(filters)}`
	);

	return data.map(mapTemplate);
}

export async function createGymActivityTemplate(
	gymId: string,
	request: CreateGymActivityTemplateRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/activities/templates`,
		{
			method: 'POST',
			body: JSON.stringify(request)
		}
	);

	return mapTemplate(data);
}

export async function updateGymActivityTemplateActivation(
	gymId: string,
	templateId: string,
	request: UpdateGymActivityTemplateActivationRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/activities/templates/${templateId}/activation`,
		{
			method: 'PATCH',
			body: JSON.stringify(request)
		}
	);

	return mapTemplate(data);
}

export async function fetchGymActivitySessions(
	gymId: string,
	filters: FetchActivitiesFilters = {}
) {
	const data = await apiRequest<Array<Record<string, unknown>>>(
		`/api/gyms/${gymId}/activities/sessions${buildFiltersQuery(filters)}`
	);

	return data.map(mapSession);
}

export async function createGymActivitySession(
	gymId: string,
	request: CreateGymActivitySessionRequest
) {
	const data = await apiRequest<Record<string, unknown>>(`/api/gyms/${gymId}/activities/sessions`, {
		method: 'POST',
		body: JSON.stringify(request)
	});

	return mapSession(data);
}

export async function updateGymActivitySessionStatus(
	gymId: string,
	sessionId: string,
	request: UpdateGymActivitySessionStatusRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/activities/sessions/${sessionId}/status`,
		{
			method: 'PATCH',
			body: JSON.stringify(request)
		}
	);

	return mapSession(data);
}

export async function createGymActivityBooking(
	gymId: string,
	sessionId: string,
	request: CreateGymActivityBookingRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/activities/sessions/${sessionId}/bookings`,
		{
			method: 'POST',
			body: JSON.stringify(request)
		}
	);

	return mapSession(data);
}

export async function updateGymActivityBookingStatus(
	gymId: string,
	bookingId: string,
	request: UpdateGymActivityBookingStatusRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/activities/bookings/${bookingId}/status`,
		{
			method: 'POST',
			body: JSON.stringify(request)
		}
	);

	return mapSession(data);
}
