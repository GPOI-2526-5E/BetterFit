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

export type GymIntegrationType =
	| 'EmailDelivery'
	| 'WhatsAppMessaging'
	| 'AccessControl'
	| 'AccountingExport';

export type GymIntegrationStatus = 'Draft' | 'Active' | 'Disabled';

export type GymIntegration = {
	id: string;
	gymId: string;
	locationId: string | null;
	locationName: string | null;
	type: GymIntegrationType;
	displayName: string;
	providerName: string;
	status: GymIntegrationStatus;
	endpointUrl: string | null;
	username: string | null;
	externalAccountId: string | null;
	senderIdentity: string | null;
	notes: string | null;
	hasCredentialConfigured: boolean;
	lastSyncAttemptAtUtc: Date | null;
	lastSyncSucceeded: boolean | null;
	lastSyncMessage: string | null;
	createdAtUtc: Date;
	updatedAtUtc: Date;
};

export type UpsertGymIntegrationRequest = {
	locationId?: string | null;
	displayName: string;
	providerName: string;
	status: GymIntegrationStatus;
	endpointUrl?: string | null;
	username?: string | null;
	apiKey?: string | null;
	externalAccountId?: string | null;
	senderIdentity?: string | null;
	notes?: string | null;
};

function getAccessToken() {
	return getAuthenticatedSession()?.token ?? '';
}

function parseDate(value: string | null | undefined) {
	if (!value) {
		return null;
	}

	const parsed = new Date(value);
	return Number.isNaN(parsed.getTime()) ? null : parsed;
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

function mapIntegration(item: { [id: string]: unknown }) {
	return {
		id: String(item.id),
		gymId: String(item.gymId),
		locationId:
			item.locationId === null || item.locationId === undefined ? null : String(item.locationId),
		locationName:
			item.locationName === null || item.locationName === undefined ? null : String(item.locationName),
		type: String(item.type ?? 'EmailDelivery') as GymIntegrationType,
		displayName: String(item.displayName ?? ''),
		providerName: String(item.providerName ?? ''),
		status: String(item.status ?? 'Draft') as GymIntegrationStatus,
		endpointUrl: (item.endpointUrl as string | null | undefined) ?? null,
		username: (item.username as string | null | undefined) ?? null,
		externalAccountId: (item.externalAccountId as string | null | undefined) ?? null,
		senderIdentity: (item.senderIdentity as string | null | undefined) ?? null,
		notes: (item.notes as string | null | undefined) ?? null,
		hasCredentialConfigured: Boolean(item.hasCredentialConfigured ?? false),
		lastSyncAttemptAtUtc: parseDate(item.lastSyncAttemptAtUtc as string | null | undefined),
		lastSyncSucceeded:
			item.lastSyncSucceeded === null || item.lastSyncSucceeded === undefined
				? null
				: Boolean(item.lastSyncSucceeded),
		lastSyncMessage: (item.lastSyncMessage as string | null | undefined) ?? null,
		createdAtUtc: parseDate(item.createdAtUtc as string | null | undefined) ?? new Date(),
		updatedAtUtc: parseDate(item.updatedAtUtc as string | null | undefined) ?? new Date()
	} satisfies GymIntegration;
}

export async function fetchGymIntegrations(gymId: string, locationId?: string | null) {
	const params = new URLSearchParams();
	if (locationId) {
		params.set('locationId', locationId);
	}

	const query = params.toString();
	const payload = await apiRequest<Array<{ [id: string]: unknown }>>(
		`/api/gyms/${gymId}/integrations${query ? `?${query}` : ''}`
	);

	return payload.map((item) => mapIntegration(item));
}

export async function upsertGymIntegration(
	gymId: string,
	integrationType: GymIntegrationType,
	request: UpsertGymIntegrationRequest
) {
	const payload = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/integrations/${integrationType}`,
		{
			method: 'PUT',
			body: JSON.stringify(request)
		}
	);

	return mapIntegration(payload);
}

export async function testGymIntegration(gymId: string, integrationType: GymIntegrationType) {
	const payload = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/integrations/${integrationType}/test`,
		{
			method: 'POST'
		}
	);

	return mapIntegration(payload);
}
