import type {
	HomeAlert,
	HomeData,
	HomeDevice,
	HomeKpi,
	HomeLocationSnapshot,
	HomeRecentCollection,
	HomeStatusCard,
	HomeTask,
	HomeTimelineEvent,
	TaskStatus
} from '$lib/components/dashboard/home/types';
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

type DashboardOverviewResponse = {
	accessesTodayCount: number;
	revenueTodayAmount: number;
	expiringMembershipsCount: number;
	pendingActivationsCount: number;
	pendingCollectionsAmount: number;
	deniedTodayCount: number;
	failedPaymentsCount: number;
	canReadMembers: boolean;
	canReadBilling: boolean;
	canReadCheckins: boolean;
	tasks: Array<{
		title: string;
		memberName: string;
		status: TaskStatus;
		dueAtUtc?: string | null;
		detail: string;
	}>;
	alerts: Array<{
		title: string;
		description: string;
		status: HomeAlert['status'];
	}>;
	statusCards: Array<{
		id: HomeStatusCard['id'];
		label: string;
		value: string;
		description?: string | null;
		tone: HomeStatusCard['tone'];
	}>;
	devices: Array<{
		name: string;
		status: HomeDevice['status'];
		lastEvent: string;
	}>;
	timeline: Array<{
		occurredAtUtc: string;
		text: string;
	}>;
	locations: Array<{
		locationId: string;
		locationName: string;
		accessesTodayCount: number;
		revenueTodayAmount: number;
		expiringMembershipsCount: number;
		pendingActivationsCount: number;
	}>;
	recentCollections: Array<{
		paymentId: string;
		paidAtUtc: string;
		referenceCode: string;
		receiptCode: string;
		memberName: string;
		locationName: string;
		amount: number;
		method: string;
	}>;
	generatedAtUtc: string;
};

function getAccessToken() {
	return getAuthenticatedSession()?.token ?? '';
}

async function apiRequest<T>(path: string): Promise<T> {
	const response = await fetch(`${API_BASE_URL}${path}`, {
		headers: {
			'Content-Type': 'application/json',
			Authorization: `Bearer ${getAccessToken()}`
		}
	});

	const payload = (await response.json()) as ApiEnvelope<T>;
	if (!response.ok || !payload.success || !payload.data) {
		throw new Error(payload.error?.message ?? payload.message ?? 'Richiesta API non riuscita.');
	}

	return payload.data;
}

function parseDate(value: string | null | undefined) {
	if (!value) {
		return null;
	}

	const parsed = new Date(value);
	return Number.isNaN(parsed.getTime()) ? null : parsed;
}

function formatCurrency(value: number) {
	return new Intl.NumberFormat('it-IT', {
		style: 'currency',
		currency: 'EUR',
		maximumFractionDigits: 2
	}).format(value);
}

function formatDueDate(value: string | null | undefined) {
	const parsed = parseDate(value);
	if (!parsed) {
		return 'Da verificare';
	}

	return new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	}).format(parsed);
}

function formatTimelineDate(value: string) {
	const parsed = parseDate(value);
	if (!parsed) {
		return 'Ora non disponibile';
	}

	const now = new Date();
	const isToday =
		now.getFullYear() === parsed.getFullYear() &&
		now.getMonth() === parsed.getMonth() &&
		now.getDate() === parsed.getDate();

	if (isToday) {
		return new Intl.DateTimeFormat('it-IT', {
			hour: '2-digit',
			minute: '2-digit'
		}).format(parsed);
	}

	return new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	}).format(parsed);
}

function buildCoverageNotice(data: DashboardOverviewResponse) {
	const hiddenAreas: string[] = [];

	if (!data.canReadMembers) {
		hiddenAreas.push('utenti');
	}

	if (!data.canReadBilling) {
		hiddenAreas.push('vendite');
	}

	if (!data.canReadCheckins) {
		hiddenAreas.push('accessi');
	}

	if (hiddenAreas.length === 0) {
		return null;
	}

	return `La dashboard mostra solo i dati consentiti dai permessi correnti. Moduli non inclusi: ${hiddenAreas.join(', ')}.`;
}

function mapKpis(data: DashboardOverviewResponse): HomeKpi[] {
	return [
		data.canReadCheckins
			? {
					label: 'Accessi consentiti oggi',
					value: data.accessesTodayCount.toString(),
					trend:
						data.deniedTodayCount > 0
							? `${data.deniedTodayCount} accessi negati da verificare`
							: 'Nessun accesso negato registrato oggi',
					positive: data.deniedTodayCount === 0
				}
			: {
					label: 'Accessi consentiti oggi',
					value: 'N/D',
					trend: 'Dato non incluso nei permessi correnti',
					positive: true,
					disabled: true
				},
		data.canReadBilling
			? {
					label: 'Incassi di oggi',
					value: formatCurrency(data.revenueTodayAmount),
					trend:
						data.pendingCollectionsAmount > 0
							? `${formatCurrency(data.pendingCollectionsAmount)} ancora da incassare`
							: 'Tutti gli incassi risultano chiusi',
					positive: data.pendingCollectionsAmount === 0
				}
			: {
					label: 'Incassi di oggi',
					value: 'N/D',
					trend: 'Dato non incluso nei permessi correnti',
					positive: true,
					disabled: true
				},
		data.canReadMembers
			? {
					label: 'Rinnovi in scadenza',
					value: data.expiringMembershipsCount.toString(),
					trend:
						data.expiringMembershipsCount > 0
							? 'Clienti da contattare entro 30 giorni'
							: 'Nessuna scadenza critica nella finestra corrente',
					positive: data.expiringMembershipsCount === 0
				}
			: {
					label: 'Rinnovi in scadenza',
					value: 'N/D',
					trend: 'Dato non incluso nei permessi correnti',
					positive: true,
					disabled: true
				},
		data.canReadMembers
			? {
					label: 'Attivazioni da chiudere',
					value: data.pendingActivationsCount.toString(),
					trend:
						data.pendingActivationsCount > 0
							? 'Preregistrazioni desk ancora aperte'
							: 'Nessuna attivazione pendente',
					positive: data.pendingActivationsCount === 0
				}
			: {
					label: 'Attivazioni da chiudere',
					value: 'N/D',
					trend: 'Dato non incluso nei permessi correnti',
					positive: true,
					disabled: true
				}
	];
}

function mapTasks(data: DashboardOverviewResponse): HomeTask[] {
	return data.tasks.map((task) => ({
		task: task.title,
		member: task.memberName,
		due: `${formatDueDate(task.dueAtUtc)} - ${task.detail}`,
		status: task.status
	}));
}

function mapTimeline(data: DashboardOverviewResponse): HomeTimelineEvent[] {
	return data.timeline.map((event) => ({
		time: formatTimelineDate(event.occurredAtUtc),
		text: event.text
	}));
}

function mapLocations(data: DashboardOverviewResponse): HomeLocationSnapshot[] {
	return data.locations.map((location) => ({
		locationId: location.locationId,
		locationName: location.locationName,
		accessesTodayCount: location.accessesTodayCount,
		revenueTodayLabel: formatCurrency(location.revenueTodayAmount),
		expiringMembershipsCount: location.expiringMembershipsCount,
		pendingActivationsCount: location.pendingActivationsCount
	}));
}

function mapRecentCollections(data: DashboardOverviewResponse): HomeRecentCollection[] {
	return data.recentCollections.map((collection) => ({
		paymentId: collection.paymentId,
		paidAt: formatTimelineDate(collection.paidAtUtc),
		referenceCode: collection.referenceCode,
		receiptCode: collection.receiptCode,
		memberName: collection.memberName,
		locationName: collection.locationName,
		amountLabel: formatCurrency(collection.amount),
		method: collection.method
	}));
}

function mapStatusCards(data: DashboardOverviewResponse): HomeStatusCard[] {
	return data.statusCards.map((card) => ({
		id: card.id,
		label: card.label,
		value: card.value,
		description: card.description,
		tone: card.tone
	}));
}

function mapDevices(data: DashboardOverviewResponse): HomeDevice[] {
	return data.devices.map((device) => ({
		name: device.name,
		status: device.status,
		lastEvent: device.lastEvent
	}));
}

function buildOperationalMessage(data: DashboardOverviewResponse) {
	const criticalAlert = data.alerts.find((alert) => alert.status === 'critical');
	if (criticalAlert) {
		return `${criticalAlert.title}: ${criticalAlert.description}`;
	}

	const offlineDevices = data.devices.filter((device) => device.status === 'offline').length;
	if (offlineDevices > 0) {
		return `${offlineDevices} sistemi risultano offline e richiedono verifica tecnica o operativa.`;
	}

	return null;
}

export async function fetchDashboardHomeData({
	gymId,
	locationId
}: {
	gymId: string;
	locationId?: string | null;
}): Promise<HomeData> {
	const params = new URLSearchParams();
	if (locationId) {
		params.set('locationId', locationId);
	}

	const query = params.toString();
	const data = await apiRequest<DashboardOverviewResponse>(
		`/api/gyms/${gymId}/dashboard/overview${query ? `?${query}` : ''}`
	);

	return {
		kpis: mapKpis(data),
		statusCards: mapStatusCards(data),
		tasks: mapTasks(data),
		alerts: data.alerts,
		devices: mapDevices(data),
		timeline: mapTimeline(data),
		locations: mapLocations(data),
		recentCollections: mapRecentCollections(data),
		operationalMessage: buildOperationalMessage(data),
		coverageNotice: buildCoverageNotice(data)
	};
}
