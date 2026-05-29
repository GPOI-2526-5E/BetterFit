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

export type SaleItemType =
	| 'MembershipPeriodic'
	| 'MembershipEntries'
	| 'Package'
	| 'Service'
	| 'Product'
	| 'CreditRecharge';

export type SaleStatus = 'PendingPayment' | 'Paid' | 'Cancelled' | 'Refunded';

export type SalePaymentMethod =
	| 'Cash'
	| 'Card'
	| 'BankTransfer'
	| 'DirectDebit'
	| 'DigitalWallet'
	| 'Other';

export type SalePaymentStatus = 'Pending' | 'Paid' | 'Failed' | 'Refunded' | 'PartiallyPaid';

export type GymSaleLine = {
	id: string;
	itemType: SaleItemType;
	name: string;
	quantity: number;
	unitPriceAmount: number;
	discountAmount: number;
	lineTotalAmount: number;
	servicePeriodStartUtc: Date | null;
	servicePeriodEndUtc: Date | null;
	creditsGranted: number | null;
	notes: string | null;
};

export type GymSalePayment = {
	id: string;
	amount: number;
	method: SalePaymentMethod;
	status: SalePaymentStatus;
	dueAtUtc: Date | null;
	paidAtUtc: Date | null;
	receiptCode: string | null;
	receiptIssuedAtUtc: Date | null;
	notes: string | null;
	createdAtUtc: Date;
};

export type GymSale = {
	saleId: string;
	gymId: string;
	membershipId: string;
	locationId: string;
	referenceCode: string;
	memberName: string;
	memberEmail: string;
	locationName: string;
	soldAtUtc: Date;
	createdAtUtc: Date;
	updatedAtUtc: Date;
	subtotalAmount: number;
	discountAmount: number;
	totalAmount: number;
	paidAmount: number;
	remainingAmount: number;
	status: SaleStatus;
	paymentStatus: SalePaymentStatus;
	notes: string | null;
	lines: GymSaleLine[];
	payments: GymSalePayment[];
};

export type GymSaleCatalogItem = {
	catalogItemId: string;
	gymId: string;
	locationId: string;
	locationName: string;
	itemType: SaleItemType;
	name: string;
	unitPriceAmount: number;
	defaultQuantity: number;
	defaultDiscountAmount: number;
	defaultCreditsGranted: number | null;
	servicePeriodDays: number | null;
	notes: string | null;
	isActive: boolean;
	createdAtUtc: Date;
	updatedAtUtc: Date;
};

export type GymSalesOverview = {
	salesTodayCount: number;
	revenueTodayAmount: number;
	revenueMonthAmount: number;
	pendingCollectionAmount: number;
	renewalCandidatesCount: number;
	failedPaymentsCount: number;
};

export type CreateGymSaleLineRequest = {
	itemType: SaleItemType;
	name: string;
	quantity: number;
	unitPriceAmount: number;
	discountAmount: number;
	servicePeriodStartUtc?: string | null;
	servicePeriodEndUtc?: string | null;
	creditsGranted?: number | null;
	notes?: string | null;
};

export type CreateGymSalePaymentRequest = {
	amount: number;
	method: SalePaymentMethod;
	status: SalePaymentStatus;
	dueAtUtc?: string | null;
	paidAtUtc?: string | null;
	notes?: string | null;
};

export type CreateGymSaleRequest = {
	membershipId: string;
	locationId?: string | null;
	soldAtUtc?: string | null;
	notes?: string | null;
	lines: CreateGymSaleLineRequest[];
	payments: CreateGymSalePaymentRequest[];
};

export type AddGymSalePaymentRequest = {
	amount: number;
	method: SalePaymentMethod;
	status: SalePaymentStatus;
	dueAtUtc?: string | null;
	paidAtUtc?: string | null;
	notes?: string | null;
};

export type UpdateGymSalePaymentRequest = {
	amount: number;
	method: SalePaymentMethod;
	status: SalePaymentStatus;
	dueAtUtc?: string | null;
	paidAtUtc?: string | null;
	notes?: string | null;
};

export type UpsertGymSaleCatalogItemRequest = {
	locationId: string;
	itemType: SaleItemType;
	name: string;
	unitPriceAmount: number;
	defaultQuantity: number;
	defaultDiscountAmount: number;
	defaultCreditsGranted?: number | null;
	servicePeriodDays?: number | null;
	notes?: string | null;
	isActive: boolean;
};

export type UpdateGymSaleCatalogItemActivationRequest = {
	isActive: boolean;
};

type FetchSalesFilters = {
	status?: SaleStatus | 'all';
	paymentStatus?: SalePaymentStatus | 'all';
	membershipId?: string | null;
	onlyRenewals?: boolean;
	search?: string;
};

type FetchSaleCatalogFilters = {
	locationId?: string | null;
	includeInactive?: boolean;
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

function mapSaleLine(line: {
	[id: string]: unknown;
	servicePeriodStartUtc?: string | null;
	servicePeriodEndUtc?: string | null;
}) {
	return {
		id: String(line.id),
		itemType: line.itemType as SaleItemType,
		name: String(line.name),
		quantity: Number(line.quantity),
		unitPriceAmount: Number(line.unitPriceAmount),
		discountAmount: Number(line.discountAmount),
		lineTotalAmount: Number(line.lineTotalAmount),
		servicePeriodStartUtc: parseDate(line.servicePeriodStartUtc),
		servicePeriodEndUtc: parseDate(line.servicePeriodEndUtc),
		creditsGranted:
			line.creditsGranted === null || line.creditsGranted === undefined
				? null
				: Number(line.creditsGranted),
		notes: (line.notes as string | null | undefined) ?? null
	} satisfies GymSaleLine;
}

function mapSalePayment(payment: {
	[id: string]: unknown;
	dueAtUtc?: string | null;
	paidAtUtc?: string | null;
	receiptIssuedAtUtc?: string | null;
	createdAtUtc?: string | null;
}) {
	return {
		id: String(payment.id),
		amount: Number(payment.amount),
		method: payment.method as SalePaymentMethod,
		status: payment.status as SalePaymentStatus,
		dueAtUtc: parseDate(payment.dueAtUtc),
		paidAtUtc: parseDate(payment.paidAtUtc),
		receiptCode: (payment.receiptCode as string | null | undefined) ?? null,
		receiptIssuedAtUtc: parseDate(payment.receiptIssuedAtUtc),
		notes: (payment.notes as string | null | undefined) ?? null,
		createdAtUtc: parseDate(payment.createdAtUtc) ?? new Date()
	} satisfies GymSalePayment;
}

function mapSale(sale: {
	[id: string]: unknown;
	soldAtUtc?: string | null;
	createdAtUtc?: string | null;
	updatedAtUtc?: string | null;
	lines?: Array<Record<string, unknown>>;
	payments?: Array<Record<string, unknown>>;
}) {
	return {
		saleId: String(sale.saleId),
		gymId: String(sale.gymId),
		membershipId: String(sale.membershipId),
		locationId: String(sale.locationId),
		referenceCode: String(sale.referenceCode),
		memberName: String(sale.memberName),
		memberEmail: String(sale.memberEmail),
		locationName: String(sale.locationName),
		soldAtUtc: parseDate(sale.soldAtUtc) ?? new Date(),
		createdAtUtc: parseDate(sale.createdAtUtc) ?? new Date(),
		updatedAtUtc: parseDate(sale.updatedAtUtc) ?? new Date(),
		subtotalAmount: Number(sale.subtotalAmount),
		discountAmount: Number(sale.discountAmount),
		totalAmount: Number(sale.totalAmount),
		paidAmount: Number(sale.paidAmount),
		remainingAmount: Number(sale.remainingAmount),
		status: sale.status as SaleStatus,
		paymentStatus: sale.paymentStatus as SalePaymentStatus,
		notes: (sale.notes as string | null | undefined) ?? null,
		lines: (sale.lines ?? []).map(mapSaleLine),
		payments: (sale.payments ?? []).map(mapSalePayment)
	} satisfies GymSale;
}

function mapSaleCatalogItem(item: {
	[id: string]: unknown;
	createdAtUtc?: string | null;
	updatedAtUtc?: string | null;
}) {
	return {
		catalogItemId: String(item.catalogItemId),
		gymId: String(item.gymId),
		locationId: String(item.locationId),
		locationName: String(item.locationName),
		itemType: item.itemType as SaleItemType,
		name: String(item.name),
		unitPriceAmount: Number(item.unitPriceAmount),
		defaultQuantity: Number(item.defaultQuantity),
		defaultDiscountAmount: Number(item.defaultDiscountAmount),
		defaultCreditsGranted:
			item.defaultCreditsGranted === null || item.defaultCreditsGranted === undefined
				? null
				: Number(item.defaultCreditsGranted),
		servicePeriodDays:
			item.servicePeriodDays === null || item.servicePeriodDays === undefined
				? null
				: Number(item.servicePeriodDays),
		notes: (item.notes as string | null | undefined) ?? null,
		isActive: Boolean(item.isActive),
		createdAtUtc: parseDate(item.createdAtUtc) ?? new Date(),
		updatedAtUtc: parseDate(item.updatedAtUtc) ?? new Date()
	} satisfies GymSaleCatalogItem;
}

export async function fetchGymSalesOverview(gymId: string) {
	const data = await apiRequest<Record<string, unknown>>(`/api/gyms/${gymId}/sales/overview`);

	return {
		salesTodayCount: Number(data.salesTodayCount),
		revenueTodayAmount: Number(data.revenueTodayAmount),
		revenueMonthAmount: Number(data.revenueMonthAmount),
		pendingCollectionAmount: Number(data.pendingCollectionAmount),
		renewalCandidatesCount: Number(data.renewalCandidatesCount),
		failedPaymentsCount: Number(data.failedPaymentsCount)
	} satisfies GymSalesOverview;
}

export async function fetchGymSales(gymId: string, filters: FetchSalesFilters = {}) {
	const params = new URLSearchParams();
	if (filters.status && filters.status !== 'all') {
		params.set('status', filters.status);
	}
	if (filters.paymentStatus && filters.paymentStatus !== 'all') {
		params.set('paymentStatus', filters.paymentStatus);
	}
	if (filters.membershipId) {
		params.set('membershipId', filters.membershipId);
	}
	if (filters.onlyRenewals) {
		params.set('onlyRenewals', 'true');
	}
	if (filters.search?.trim()) {
		params.set('search', filters.search.trim());
	}

	const query = params.toString();
	const data = await apiRequest<Array<Record<string, unknown>>>(
		`/api/gyms/${gymId}/sales${query ? `?${query}` : ''}`
	);

	return data.map(mapSale);
}

export async function fetchGymSaleCatalog(gymId: string, filters: FetchSaleCatalogFilters = {}) {
	const params = new URLSearchParams();
	if (filters.locationId) {
		params.set('locationId', filters.locationId);
	}
	if (filters.includeInactive) {
		params.set('includeInactive', 'true');
	}

	const query = params.toString();
	const data = await apiRequest<Array<Record<string, unknown>>>(
		`/api/gyms/${gymId}/sales/catalog${query ? `?${query}` : ''}`
	);

	return data.map(mapSaleCatalogItem);
}

export async function createGymSale(gymId: string, request: CreateGymSaleRequest) {
	const data = await apiRequest<Record<string, unknown>>(`/api/gyms/${gymId}/sales`, {
		method: 'POST',
		body: JSON.stringify(request)
	});

	return mapSale(data);
}

export async function addGymSalePayment(
	gymId: string,
	saleId: string,
	request: AddGymSalePaymentRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/sales/${saleId}/payments`,
		{
			method: 'POST',
			body: JSON.stringify(request)
		}
	);

	return mapSale(data);
}

export async function updateGymSalePayment(
	gymId: string,
	saleId: string,
	paymentId: string,
	request: UpdateGymSalePaymentRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/sales/${saleId}/payments/${paymentId}`,
		{
			method: 'PATCH',
			body: JSON.stringify(request)
		}
	);

	return mapSale(data);
}

export async function createGymSaleCatalogItem(
	gymId: string,
	request: UpsertGymSaleCatalogItemRequest
) {
	const data = await apiRequest<Record<string, unknown>>(`/api/gyms/${gymId}/sales/catalog`, {
		method: 'POST',
		body: JSON.stringify(request)
	});

	return mapSaleCatalogItem(data);
}

export async function updateGymSaleCatalogItem(
	gymId: string,
	catalogItemId: string,
	request: UpsertGymSaleCatalogItemRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/sales/catalog/${catalogItemId}`,
		{
			method: 'PUT',
			body: JSON.stringify(request)
		}
	);

	return mapSaleCatalogItem(data);
}

export async function updateGymSaleCatalogItemActivation(
	gymId: string,
	catalogItemId: string,
	request: UpdateGymSaleCatalogItemActivationRequest
) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/sales/catalog/${catalogItemId}/activation`,
		{
			method: 'PATCH',
			body: JSON.stringify(request)
		}
	);

	return mapSaleCatalogItem(data);
}

export async function cancelGymSale(gymId: string, saleId: string) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/sales/${saleId}/cancel`,
		{
			method: 'POST'
		}
	);

	return mapSale(data);
}

export async function refundGymSale(gymId: string, saleId: string) {
	const data = await apiRequest<Record<string, unknown>>(
		`/api/gyms/${gymId}/sales/${saleId}/refund`,
		{
			method: 'POST'
		}
	);

	return mapSale(data);
}
