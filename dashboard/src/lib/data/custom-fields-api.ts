import { getLocale } from '$lib/paraglide/runtime';
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

export type GymCustomFieldEntityType = 'Member';

export type GymCustomFieldValueType = 'Text' | 'LongText' | 'Number' | 'Date' | 'Boolean' | 'Select';

export type GymCustomFieldDefinition = {
	id: string;
	gymId: string;
	entityType: GymCustomFieldEntityType;
	key: string;
	label: string;
	description: string | null;
	valueType: GymCustomFieldValueType;
	options: string[];
	isRequired: boolean;
	isActive: boolean;
	sortOrder: number;
	createdAtUtc: Date;
	updatedAtUtc: Date;
};

export type UpsertGymCustomFieldDefinitionRequest = {
	key: string;
	label: string;
	description?: string | null;
	valueType: GymCustomFieldValueType;
	options: string[];
	isRequired: boolean;
	isActive: boolean;
	sortOrder: number;
};

function parseDate(value: string | null | undefined) {
	if (!value) {
		return new Date();
	}

	const parsed = new Date(value);
	return Number.isNaN(parsed.getTime()) ? new Date() : parsed;
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
			'Accept-Language': getLocale(),
			...(init?.headers ?? {})
		}
	});

	const payload = (await response.json()) as ApiEnvelope<T>;
	if (!response.ok || !payload.success || payload.data == null) {
		throw new Error(payload.error?.message ?? payload.message ?? 'Richiesta API non riuscita.');
	}

	return payload.data;
}

function mapDefinition(definition: Record<string, unknown>) {
	return {
		id: String(definition.id),
		gymId: String(definition.gymId),
		entityType: String(definition.entityType) as GymCustomFieldEntityType,
		key: String(definition.key),
		label: String(definition.label),
		description: (definition.description as string | null | undefined) ?? null,
		valueType: String(definition.valueType) as GymCustomFieldValueType,
		options: Array.isArray(definition.options)
			? definition.options.map((option) => String(option))
			: [],
		isRequired: Boolean(definition.isRequired),
		isActive: Boolean(definition.isActive),
		sortOrder: Number(definition.sortOrder ?? 0),
		createdAtUtc: parseDate(definition.createdAtUtc as string | null | undefined),
		updatedAtUtc: parseDate(definition.updatedAtUtc as string | null | undefined)
	} satisfies GymCustomFieldDefinition;
}

export async function fetchGymCustomFields(gymId: string, entityType: GymCustomFieldEntityType = 'Member') {
	const params = new URLSearchParams({ entityType });
	const data = await apiRequest<Array<Record<string, unknown>>>(
		`/api/gyms/${gymId}/custom-fields?${params.toString()}`
	);

	return data.map(mapDefinition);
}

export async function createGymCustomField(
	gymId: string,
	request: UpsertGymCustomFieldDefinitionRequest
) {
	const data = await apiRequest<Record<string, unknown>>(`/api/gyms/${gymId}/custom-fields`, {
		method: 'POST',
		body: JSON.stringify({
			entityType: 'Member',
			...request
		})
	});

	return mapDefinition(data);
}

export async function updateGymCustomField(
	gymId: string,
	fieldDefinitionId: string,
	request: UpsertGymCustomFieldDefinitionRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/custom-fields/${fieldDefinitionId}`,
		{
			method: 'PUT',
			body: JSON.stringify(request)
		}
	);

	return mapDefinition(data);
}
