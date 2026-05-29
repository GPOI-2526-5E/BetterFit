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

export type AccessEventResult = 'Granted' | 'Denied' | 'ManualOverride';
export type AccessOrigin = 'Badge' | 'AppQr' | 'Desk' | 'Unknown';

export type GymAccessEvent = {
	eventId: string;
	gymId: string;
	membershipId: string;
	locationId: string;
	memberName: string;
	memberEmail: string;
	locationName: string;
	gateName: string;
	result: AccessEventResult;
	origin: AccessOrigin;
	reason: string | null;
	occurredAtUtc: Date;
};

export type GymAccessDeniedAttempt = {
	memberName: string;
	reason: string;
	attempts: number;
	lastAttemptAtUtc: Date;
};

export type GymAccessOverview = {
	peoplePresentTodayCount: number;
	checkinsLast30MinutesCount: number;
	deniedTodayCount: number;
	deskApprovalsTodayCount: number;
	recentEvents: GymAccessEvent[];
	deniedAttempts: GymAccessDeniedAttempt[];
	lastSyncUtc: Date;
};

export type RecordGymCheckinRequest = {
	membershipId: string;
	locationId?: string | null;
	gateName?: string | null;
};

type FetchAccessEventFilters = {
	result?: AccessEventResult | 'all';
	membershipId?: string | null;
	search?: string;
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
	if (!response.ok || !payload.success || !payload.data) {
		throw new Error(payload.error?.message ?? payload.message ?? 'Richiesta API non riuscita.');
	}

	return payload.data;
}

function mapAccessEvent(event: {
	[id: string]: unknown;
	occurredAtUtc?: string | null;
}) {
	return {
		eventId: String(event.eventId),
		gymId: String(event.gymId),
		membershipId: String(event.membershipId),
		locationId: String(event.locationId),
		memberName: String(event.memberName),
		memberEmail: String(event.memberEmail),
		locationName: String(event.locationName),
		gateName: String(event.gateName),
		result: event.result as AccessEventResult,
		origin: event.origin as AccessOrigin,
		reason: (event.reason as string | null | undefined) ?? null,
		occurredAtUtc: parseDate(event.occurredAtUtc) ?? new Date()
	} satisfies GymAccessEvent;
}

function mapDeniedAttempt(attempt: {
	[id: string]: unknown;
	lastAttemptAtUtc?: string | null;
}) {
	return {
		memberName: String(attempt.memberName),
		reason: String(attempt.reason),
		attempts: Number(attempt.attempts),
		lastAttemptAtUtc: parseDate(attempt.lastAttemptAtUtc) ?? new Date()
	} satisfies GymAccessDeniedAttempt;
}

export async function fetchGymAccessOverview(gymId: string) {
	const data = await apiRequest<Record<string, unknown>>(`/api/gyms/${gymId}/access/overview`);

	return {
		peoplePresentTodayCount: Number(data.peoplePresentTodayCount),
		checkinsLast30MinutesCount: Number(data.checkinsLast30MinutesCount),
		deniedTodayCount: Number(data.deniedTodayCount),
		deskApprovalsTodayCount: Number(data.deskApprovalsTodayCount),
		recentEvents: Array.isArray(data.recentEvents)
			? data.recentEvents.map((item) => mapAccessEvent(item as Record<string, unknown>))
			: [],
		deniedAttempts: Array.isArray(data.deniedAttempts)
			? data.deniedAttempts.map((item) => mapDeniedAttempt(item as Record<string, unknown>))
			: [],
		lastSyncUtc: parseDate(data.lastSyncUtc as string | null | undefined) ?? new Date()
	} satisfies GymAccessOverview;
}

export async function fetchGymAccessEvents(gymId: string, filters: FetchAccessEventFilters = {}) {
	const params = new URLSearchParams();
	if (filters.result && filters.result !== 'all') {
		params.set('result', filters.result);
	}
	if (filters.membershipId) {
		params.set('membershipId', filters.membershipId);
	}
	if (filters.search?.trim()) {
		params.set('search', filters.search.trim());
	}

	const query = params.toString();
	const data = await apiRequest<Array<Record<string, unknown>>>(
		`/api/gyms/${gymId}/access/events${query ? `?${query}` : ''}`
	);

	return data.map(mapAccessEvent);
}

export async function recordGymCheckin(gymId: string, request: RecordGymCheckinRequest) {
	const data = await apiRequest<Record<string, unknown>>(`/api/gyms/${gymId}/access/checkins`, {
		method: 'POST',
		body: JSON.stringify(request)
	});

	return mapAccessEvent(data);
}
