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

export type ReportLeadStage = 'New' | 'Contacted' | 'TrialBooked' | 'Negotiation' | 'Won' | 'Lost';
export type ReportSessionStatus = 'Scheduled' | 'Cancelled' | 'Completed';

export type GymKpiLocationRevenue = {
	locationId: string;
	locationName: string;
	salesCount: number;
	revenueMonthAmount: number;
	pendingCollectionsAmount: number;
};

export type GymKpiLocationAccess = {
	locationId: string;
	locationName: string;
	grantedTodayCount: number;
	deniedTodayCount: number;
};

export type GymKpiUpcomingActivity = {
	sessionId: string;
	locationId: string;
	locationName: string;
	title: string;
	instructorName: string;
	startsAtUtc: Date;
	capacity: number;
	bookedCount: number;
	occupancyRatePercentage: number;
	status: ReportSessionStatus;
};

export type GymKpiLeadStage = {
	stage: ReportLeadStage;
	leadsCount: number;
};

export type GymKpiTrainingSummary = {
	locationId: string;
	locationName: string;
	activeAssignmentsCount: number;
	revisionDueCount: number;
	assessmentsLast30DaysCount: number;
};

export type GymKpiDashboard = {
	activeMembersCount: number;
	revenueMonthAmount: number;
	pendingCollectionsAmount: number;
	leadsInPipelineCount: number;
	leadConversionRatePercentage: number;
	accessesTodayCount: number;
	upcomingBookingsCount: number;
	activeWorkoutAssignmentsCount: number;
	revenueByLocation: GymKpiLocationRevenue[];
	accessByLocation: GymKpiLocationAccess[];
	upcomingActivities: GymKpiUpcomingActivity[];
	leadPipeline: GymKpiLeadStage[];
	trainingByLocation: GymKpiTrainingSummary[];
	generatedAtUtc: Date;
};

export type GymRetentionLocationSummary = {
	locationId: string;
	locationName: string;
	activeMembersCount: number;
	expiringNext30DaysCount: number;
	atRiskMembersCount: number;
	renewedLast90DaysCount: number;
	churnedLast90DaysCount: number;
	retentionRatePercentage: number;
	churnRatePercentage: number;
};

export type GymRetentionRiskMember = {
	membershipId: string;
	locationId: string | null;
	locationName: string;
	memberName: string;
	memberEmail: string;
	status: 'PendingClaim' | 'Active' | 'Suspended' | 'Archived';
	membershipEndsAtUtc: Date | null;
	lastAccessAtUtc: Date | null;
	daysUntilExpiry: number;
	hasRenewalSale: boolean;
};

export type GymRetentionChurn = {
	activeMembersCount: number;
	expiringNext30DaysCount: number;
	atRiskMembersCount: number;
	renewedLast90DaysCount: number;
	churnedLast90DaysCount: number;
	retentionRatePercentage: number;
	churnRatePercentage: number;
	locations: GymRetentionLocationSummary[];
	atRiskMembers: GymRetentionRiskMember[];
	generatedAtUtc: Date;
};

export type GymReportExportKey = 'revenue-by-location' | 'at-risk-members' | 'lead-pipeline';

export type GymReportExportSummary = {
	key: GymReportExportKey;
	recordsCount: number;
};

export type GymReportExportCatalog = {
	exports: GymReportExportSummary[];
	generatedAtUtc: Date;
};

export type GymReportExportRows = {
	fileName: string;
	headers: string[];
	rows: string[][];
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

function buildHeaders(init?: HeadersInit) {
	const headers = new Headers(init);
	headers.set('Authorization', `Bearer ${getAccessToken()}`);
	return headers;
}

async function apiRequest<T>(path: string): Promise<T> {
	const response = await fetch(`${API_BASE_URL}${path}`, {
		headers: buildHeaders({ 'Content-Type': 'application/json' })
	});

	const payload = (await response.json()) as ApiEnvelope<T>;
	if (!response.ok || !payload.success || payload.data === null || payload.data === undefined) {
		throw new Error(payload.error?.message ?? payload.message ?? 'Richiesta API non riuscita.');
	}

	return payload.data;
}

async function fileRequest(path: string) {
	const response = await fetch(`${API_BASE_URL}${path}`, {
		headers: buildHeaders()
	});

	if (!response.ok) {
		let errorMessage = 'Download report non riuscito.';

		try {
			const payload = (await response.json()) as ApiEnvelope<unknown>;
			errorMessage = payload.error?.message ?? payload.message ?? errorMessage;
		} catch {
			// Ignore JSON parsing failures and keep the fallback message.
		}

		throw new Error(errorMessage);
	}

	const blob = await response.blob();
	const contentDisposition = response.headers.get('content-disposition');
	const fileName = extractFileName(contentDisposition) ?? 'betterfit-report.csv';

	return { blob, fileName };
}

function extractFileName(contentDisposition: string | null) {
	if (!contentDisposition) {
		return null;
	}

	const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
	if (utf8Match?.[1]) {
		return decodeURIComponent(utf8Match[1]);
	}

	const asciiMatch = contentDisposition.match(/filename="?([^"]+)"?/i);
	return asciiMatch?.[1] ?? null;
}

function parseDelimitedRows(text: string) {
	const rows: string[][] = [];
	let currentRow: string[] = [];
	let currentValue = '';
	let inQuotes = false;
	const content = text.replace(/^\uFEFF/, '');

	for (let index = 0; index < content.length; index += 1) {
		const char = content[index];
		const next = content[index + 1];

		if (char === '"') {
			if (inQuotes && next === '"') {
				currentValue += '"';
				index += 1;
			} else {
				inQuotes = !inQuotes;
			}
			continue;
		}

		if (char === ';' && !inQuotes) {
			currentRow.push(currentValue);
			currentValue = '';
			continue;
		}

		if ((char === '\n' || char === '\r') && !inQuotes) {
			if (char === '\r' && next === '\n') {
				index += 1;
			}
			currentRow.push(currentValue);
			if (currentRow.some((value) => value.trim() !== '')) {
				rows.push(currentRow);
			}
			currentRow = [];
			currentValue = '';
			continue;
		}

		currentValue += char;
	}

	if (currentValue || currentRow.length > 0) {
		currentRow.push(currentValue);
		if (currentRow.some((value) => value.trim() !== '')) {
			rows.push(currentRow);
		}
	}

	return rows;
}

function mapDashboard(payload: { [id: string]: unknown }) {
	return {
		activeMembersCount: Number(payload.activeMembersCount ?? 0),
		revenueMonthAmount: Number(payload.revenueMonthAmount ?? 0),
		pendingCollectionsAmount: Number(payload.pendingCollectionsAmount ?? 0),
		leadsInPipelineCount: Number(payload.leadsInPipelineCount ?? 0),
		leadConversionRatePercentage: Number(payload.leadConversionRatePercentage ?? 0),
		accessesTodayCount: Number(payload.accessesTodayCount ?? 0),
		upcomingBookingsCount: Number(payload.upcomingBookingsCount ?? 0),
		activeWorkoutAssignmentsCount: Number(payload.activeWorkoutAssignmentsCount ?? 0),
		revenueByLocation: Array.isArray(payload.revenueByLocation)
			? payload.revenueByLocation.map((item) => ({
					locationId: String((item as { [id: string]: unknown }).locationId),
					locationName: String((item as { [id: string]: unknown }).locationName ?? ''),
					salesCount: Number((item as { [id: string]: unknown }).salesCount ?? 0),
					revenueMonthAmount: Number((item as { [id: string]: unknown }).revenueMonthAmount ?? 0),
					pendingCollectionsAmount: Number(
						(item as { [id: string]: unknown }).pendingCollectionsAmount ?? 0
					)
				}))
			: [],
		accessByLocation: Array.isArray(payload.accessByLocation)
			? payload.accessByLocation.map((item) => ({
					locationId: String((item as { [id: string]: unknown }).locationId),
					locationName: String((item as { [id: string]: unknown }).locationName ?? ''),
					grantedTodayCount: Number((item as { [id: string]: unknown }).grantedTodayCount ?? 0),
					deniedTodayCount: Number((item as { [id: string]: unknown }).deniedTodayCount ?? 0)
				}))
			: [],
		upcomingActivities: Array.isArray(payload.upcomingActivities)
			? payload.upcomingActivities.map((item) => ({
					sessionId: String((item as { [id: string]: unknown }).sessionId),
					locationId: String((item as { [id: string]: unknown }).locationId),
					locationName: String((item as { [id: string]: unknown }).locationName ?? ''),
					title: String((item as { [id: string]: unknown }).title ?? ''),
					instructorName: String((item as { [id: string]: unknown }).instructorName ?? ''),
					startsAtUtc:
						parseDate(
							(item as { [id: string]: unknown }).startsAtUtc as string | null | undefined
						) ?? new Date(),
					capacity: Number((item as { [id: string]: unknown }).capacity ?? 0),
					bookedCount: Number((item as { [id: string]: unknown }).bookedCount ?? 0),
					occupancyRatePercentage: Number(
						(item as { [id: string]: unknown }).occupancyRatePercentage ?? 0
					),
					status: String(
						(item as { [id: string]: unknown }).status ?? 'Scheduled'
					) as ReportSessionStatus
				}))
			: [],
		leadPipeline: Array.isArray(payload.leadPipeline)
			? payload.leadPipeline.map((item) => ({
					stage: String((item as { [id: string]: unknown }).stage ?? 'New') as ReportLeadStage,
					leadsCount: Number((item as { [id: string]: unknown }).leadsCount ?? 0)
				}))
			: [],
		trainingByLocation: Array.isArray(payload.trainingByLocation)
			? payload.trainingByLocation.map((item) => ({
					locationId: String((item as { [id: string]: unknown }).locationId),
					locationName: String((item as { [id: string]: unknown }).locationName ?? ''),
					activeAssignmentsCount: Number(
						(item as { [id: string]: unknown }).activeAssignmentsCount ?? 0
					),
					revisionDueCount: Number((item as { [id: string]: unknown }).revisionDueCount ?? 0),
					assessmentsLast30DaysCount: Number(
						(item as { [id: string]: unknown }).assessmentsLast30DaysCount ?? 0
					)
				}))
			: [],
		generatedAtUtc: parseDate(payload.generatedAtUtc as string | null | undefined) ?? new Date()
	} satisfies GymKpiDashboard;
}

function mapRetentionChurn(payload: { [id: string]: unknown }) {
	return {
		activeMembersCount: Number(payload.activeMembersCount ?? 0),
		expiringNext30DaysCount: Number(payload.expiringNext30DaysCount ?? 0),
		atRiskMembersCount: Number(payload.atRiskMembersCount ?? 0),
		renewedLast90DaysCount: Number(payload.renewedLast90DaysCount ?? 0),
		churnedLast90DaysCount: Number(payload.churnedLast90DaysCount ?? 0),
		retentionRatePercentage: Number(payload.retentionRatePercentage ?? 0),
		churnRatePercentage: Number(payload.churnRatePercentage ?? 0),
		locations: Array.isArray(payload.locations)
			? payload.locations.map((item) => ({
					locationId: String((item as { [id: string]: unknown }).locationId),
					locationName: String((item as { [id: string]: unknown }).locationName ?? ''),
					activeMembersCount: Number((item as { [id: string]: unknown }).activeMembersCount ?? 0),
					expiringNext30DaysCount: Number(
						(item as { [id: string]: unknown }).expiringNext30DaysCount ?? 0
					),
					atRiskMembersCount: Number((item as { [id: string]: unknown }).atRiskMembersCount ?? 0),
					renewedLast90DaysCount: Number(
						(item as { [id: string]: unknown }).renewedLast90DaysCount ?? 0
					),
					churnedLast90DaysCount: Number(
						(item as { [id: string]: unknown }).churnedLast90DaysCount ?? 0
					),
					retentionRatePercentage: Number(
						(item as { [id: string]: unknown }).retentionRatePercentage ?? 0
					),
					churnRatePercentage: Number((item as { [id: string]: unknown }).churnRatePercentage ?? 0)
				}))
			: [],
		atRiskMembers: Array.isArray(payload.atRiskMembers)
			? payload.atRiskMembers.map((item) => ({
					membershipId: String((item as { [id: string]: unknown }).membershipId),
					locationId:
						(item as { [id: string]: unknown }).locationId === null ||
						(item as { [id: string]: unknown }).locationId === undefined
							? null
							: String((item as { [id: string]: unknown }).locationId),
					locationName: String((item as { [id: string]: unknown }).locationName ?? ''),
					memberName: String((item as { [id: string]: unknown }).memberName ?? ''),
					memberEmail: String((item as { [id: string]: unknown }).memberEmail ?? ''),
					status: String((item as { [id: string]: unknown }).status ?? 'Active') as
						| 'PendingClaim'
						| 'Active'
						| 'Suspended'
						| 'Archived',
					membershipEndsAtUtc: parseDate(
						(item as { [id: string]: unknown }).membershipEndsAtUtc as string | null | undefined
					),
					lastAccessAtUtc: parseDate(
						(item as { [id: string]: unknown }).lastAccessAtUtc as string | null | undefined
					),
					daysUntilExpiry: Number((item as { [id: string]: unknown }).daysUntilExpiry ?? 0),
					hasRenewalSale: Boolean((item as { [id: string]: unknown }).hasRenewalSale ?? false)
				}))
			: [],
		generatedAtUtc: parseDate(payload.generatedAtUtc as string | null | undefined) ?? new Date()
	} satisfies GymRetentionChurn;
}

function mapExportCatalog(payload: { [id: string]: unknown }) {
	return {
		exports: Array.isArray(payload.exports)
			? payload.exports.map((item) => ({
					key: String(
						(item as { [id: string]: unknown }).key ?? 'revenue-by-location'
					) as GymReportExportKey,
					recordsCount: Number((item as { [id: string]: unknown }).recordsCount ?? 0)
				}))
			: [],
		generatedAtUtc: parseDate(payload.generatedAtUtc as string | null | undefined) ?? new Date()
	} satisfies GymReportExportCatalog;
}

export async function fetchGymKpiDashboard(gymId: string, locationId?: string | null) {
	const params = new URLSearchParams();
	if (locationId) {
		params.set('locationId', locationId);
	}

	const query = params.toString();
	const payload = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/reports/kpi${query ? `?${query}` : ''}`
	);
	return mapDashboard(payload);
}

export async function fetchGymRetentionChurn(gymId: string, locationId?: string | null) {
	const params = new URLSearchParams();
	if (locationId) {
		params.set('locationId', locationId);
	}

	const query = params.toString();
	const payload = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/reports/churn${query ? `?${query}` : ''}`
	);
	return mapRetentionChurn(payload);
}

export async function fetchGymReportExportCatalog(gymId: string, locationId?: string | null) {
	const params = new URLSearchParams();
	if (locationId) {
		params.set('locationId', locationId);
	}

	const query = params.toString();
	const payload = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/reports/exports${query ? `?${query}` : ''}`
	);
	return mapExportCatalog(payload);
}

export async function fetchGymReportExportRows(
	gymId: string,
	exportKey: GymReportExportKey,
	locationId?: string | null
): Promise<GymReportExportRows> {
	const params = new URLSearchParams();
	if (locationId) {
		params.set('locationId', locationId);
	}

	const query = params.toString();
	const { blob, fileName } = await fileRequest(
		`/api/gyms/${gymId}/reports/exports/${exportKey}/csv${query ? `?${query}` : ''}`
	);
	const rows = parseDelimitedRows(await blob.text());
	const [headers = [], ...bodyRows] = rows;

	return {
		fileName,
		headers,
		rows: bodyRows
	};
}
