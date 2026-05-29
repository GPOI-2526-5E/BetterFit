<script lang="ts">
	import { goto } from '$app/navigation';
	import { createQuery } from '@tanstack/svelte-query';
	import CreditCardIcon from '@lucide/svelte/icons/credit-card';
	import DownloadIcon from '@lucide/svelte/icons/download';
	import PencilIcon from '@lucide/svelte/icons/pencil';
	import PlusCircleIcon from '@lucide/svelte/icons/plus-circle';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import SearchIcon from '@lucide/svelte/icons/search';
	import type { GymMembershipResponse } from '$lib/api';
	import * as AlertDialog from '$lib/components/ui/alert-dialog/index.js';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Checkbox } from '$lib/components/ui/checkbox/index.js';
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
		type AddGymSalePaymentRequest,
		type CreateGymSaleRequest,
		type GymSaleCatalogItem,
		type GymSale,
		type GymSalePayment,
		type UpsertGymSaleCatalogItemRequest,
		type SaleItemType,
		type SalePaymentMethod,
		type SalePaymentStatus,
		type SaleStatus,
		addGymSalePayment,
		cancelGymSale,
		createGymSaleCatalogItem,
		createGymSale,
		fetchGymSaleCatalog,
		fetchGymSales,
		fetchGymSalesOverview,
		refundGymSale,
		updateGymSaleCatalogItem,
		updateGymSaleCatalogItemActivation,
		updateGymSalePayment
	} from '$lib/data/sales-api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';
	import { membershipDisplayName } from '$lib/utils/membership-presenters.js';

	type Mode = 'all' | 'new' | 'payments' | 'renewals' | 'catalog';
	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
	type PendingSaleAction = 'cancel' | 'refund' | null;
	type StatusFilter = SaleStatus | 'all';
	type PaymentFilter = SalePaymentStatus | 'all';
	type PaymentEntryStatus = 'Pending' | 'Paid' | 'Failed';
	type CatalogFilter = 'all' | 'active' | 'inactive';
	type MembershipOption = {
		id: string;
		label: string;
		email: string;
		primaryLocationId: string;
		locations: GymMembershipResponse['locations'];
		status: GymMembershipResponse['status'];
		endedAtUtc: GymMembershipResponse['endedAtUtc'];
	};
	type RenewalCandidate = {
		id: string;
		name: string;
		email: string;
		locationLabel: string;
		primaryLocationId: string;
		locations: GymMembershipResponse['locations'];
		endedAtUtc: Date;
		daysToExpiry: number;
	};
	type SaleActivity = {
		id: string;
		happenedAt: Date;
		title: string;
		description: string;
		variant: BadgeVariant;
	};
	type LineState = {
		itemType: SaleItemType;
		name: string;
		quantity: number;
		unitPriceAmount: string;
		discountAmount: string;
		servicePeriodStartLocal: string;
		servicePeriodEndLocal: string;
		creditsGranted: string;
		notes: string;
	};
	type PaymentState = {
		amount: string;
		method: SalePaymentMethod;
		status: PaymentEntryStatus;
		dueAtLocal: string;
		paidAtLocal: string;
		notes: string;
	};
	type PaymentEntryState = {
		amount: string;
		method: SalePaymentMethod;
		status: PaymentEntryStatus;
		dueAtLocal: string;
		paidAtLocal: string;
		notes: string;
	};
	type CollectionDeskItem = {
		saleId: string;
		paymentId: string;
		referenceCode: string;
		memberName: string;
		memberEmail: string;
		locationName: string;
		amount: number;
		method: SalePaymentMethod;
		status: 'Pending' | 'Failed';
		dueAtUtc: Date | null;
		priority: number;
		timingLabel: string;
		badgeVariant: BadgeVariant;
	};
	type RecentReceiptItem = {
		saleId: string;
		paymentId: string;
		referenceCode: string;
		receiptCode: string;
		memberName: string;
		locationName: string;
		amount: number;
		method: SalePaymentMethod;
		paidAtUtc: Date;
	};
	type PaymentMovementItem = {
		saleId: string;
		paymentId: string;
		referenceCode: string;
		memberName: string;
		memberEmail: string;
		locationName: string;
		amount: number;
		method: SalePaymentMethod;
		status: SalePaymentStatus;
		saleStatus: SaleStatus;
		salePaymentStatus: SalePaymentStatus;
		dueAtUtc: Date | null;
		paidAtUtc: Date | null;
		receiptCode: string | null;
		receiptIssuedAtUtc: Date | null;
		notes: string | null;
		createdAtUtc: Date;
		sortPriority: number;
		sortAt: Date;
	};
	type CatalogItemFormState = {
		locationId: string;
		itemType: SaleItemType;
		name: string;
		unitPriceAmount: string;
		defaultQuantity: number;
		defaultDiscountAmount: string;
		defaultCreditsGranted: string;
		servicePeriodDays: string;
		notes: string;
		isActive: boolean;
	};

	let {
		mode = 'all',
		autoOpenCreateSheet = false,
		presetMembershipId = null,
		presetSaleId = null
	}: {
		mode?: Mode;
		autoOpenCreateSheet?: boolean;
		presetMembershipId?: string | null;
		presetSaleId?: string | null;
	} = $props();

	const api = useApiClient();
	const center = useCenterSelectionStore();
	const money = new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR' });
	const date = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		year: 'numeric'
	});
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});

	const itemTypes: Array<{ value: SaleItemType; label: string }> = [
		{ value: 'MembershipPeriodic', label: 'Abbonamento periodico' },
		{ value: 'MembershipEntries', label: 'Abbonamento ingressi' },
		{ value: 'Package', label: 'Pacchetto' },
		{ value: 'Service', label: 'Servizio' },
		{ value: 'Product', label: 'Prodotto' },
		{ value: 'CreditRecharge', label: 'Ricarica crediti' }
	];
	const paymentMethods: Array<{ value: SalePaymentMethod; label: string }> = [
		{ value: 'Cash', label: 'Contanti' },
		{ value: 'Card', label: 'Carta' },
		{ value: 'BankTransfer', label: 'Bonifico' },
		{ value: 'DirectDebit', label: 'RID' },
		{ value: 'DigitalWallet', label: 'Wallet' },
		{ value: 'Other', label: 'Altro' }
	];
	const saleStatuses: Array<{ value: StatusFilter; label: string }> = [
		{ value: 'all', label: 'Tutti gli stati vendita' },
		{ value: 'PendingPayment', label: 'Da incassare' },
		{ value: 'Paid', label: 'Pagate' },
		{ value: 'Cancelled', label: 'Annullate' },
		{ value: 'Refunded', label: 'Rimborsate' }
	];
	const paymentStatuses: Array<{ value: PaymentFilter; label: string }> = [
		{ value: 'all', label: 'Tutti gli stati pagamento' },
		{ value: 'Pending', label: 'In attesa' },
		{ value: 'Paid', label: 'Pagato' },
		{ value: 'PartiallyPaid', label: 'Parziale' },
		{ value: 'Failed', label: 'Fallito' },
		{ value: 'Refunded', label: 'Rimborsato' }
	];
	const paymentEntryStatuses: Array<{ value: PaymentEntryStatus; label: string }> = [
		{ value: 'Paid', label: 'Pagato' },
		{ value: 'Pending', label: 'Da incassare' },
		{ value: 'Failed', label: 'Fallito' }
	];

	const newLine = (): LineState => ({
		itemType: 'MembershipPeriodic',
		name: '',
		quantity: 1,
		unitPriceAmount: '50',
		discountAmount: '0',
		servicePeriodStartLocal: '',
		servicePeriodEndLocal: '',
		creditsGranted: '',
		notes: ''
	});
	const newPayment = (): PaymentState => ({
		amount: '50',
		method: 'Cash',
		status: 'Paid',
		dueAtLocal: '',
		paidAtLocal: toInputDate(),
		notes: ''
	});
	const toInputDate = (date = new Date()) =>
		`${date.getFullYear()}-${`${date.getMonth() + 1}`.padStart(2, '0')}-${`${date.getDate()}`.padStart(2, '0')}T${`${date.getHours()}`.padStart(2, '0')}:${`${date.getMinutes()}`.padStart(2, '0')}`;
	const formatMoneyInput = (value: number) => value.toFixed(2).replace('.', ',');
	const parseMoney = (value: string) => Number.parseFloat(value.replace(',', '.')) || 0;
	const parseInteger = (value: string) => {
		const parsed = Number.parseInt(value.trim(), 10);
		return Number.isFinite(parsed) ? parsed : null;
	};
	const parseInputDateTime = (value: string) => {
		if (!value.trim()) {
			return null;
		}

		const parsed = new Date(value);
		return Number.isNaN(parsed.getTime()) ? null : parsed;
	};
	const escapeHtml = (value: string) =>
		value
			.replaceAll('&', '&amp;')
			.replaceAll('<', '&lt;')
			.replaceAll('>', '&gt;')
			.replaceAll('"', '&quot;')
			.replaceAll("'", '&#39;');
	const paymentMethodLabel = (method: SalePaymentMethod) =>
		paymentMethods.find((option) => option.value === method)?.label ?? method;
	const saleItemTypeLabel = (itemType: SaleItemType) =>
		itemTypes.find((option) => option.value === itemType)?.label ?? itemType;
	const paymentEntryStatusLabel = (status: PaymentEntryStatus) =>
		paymentEntryStatuses.find((option) => option.value === status)?.label ?? status;
	const paymentDocumentTimingLabel = (payment: GymSalePayment) =>
		payment.status === 'Refunded'
			? `Rimborsato ${dateTime.format(payment.createdAtUtc)}`
			: payment.paidAtUtc
				? `Incassato ${dateTime.format(payment.paidAtUtc)}`
				: payment.dueAtUtc
					? `Scadenza ${dateTime.format(payment.dueAtUtc)}`
					: 'Nessuna data';
	const paymentReceiptCode = (sale: GymSale, payment: GymSalePayment) =>
		payment.receiptCode ?? `RIC-${sale.referenceCode}-${payment.id.slice(0, 6).toUpperCase()}`;
	const paymentReceiptIssuedAt = (payment: GymSalePayment) =>
		payment.receiptIssuedAtUtc ?? payment.paidAtUtc ?? payment.createdAtUtc;
	const movementPaymentSnapshot = (item: PaymentMovementItem): GymSalePayment => ({
		id: item.paymentId,
		amount: item.amount,
		method: item.method,
		status: item.status,
		dueAtUtc: item.dueAtUtc,
		paidAtUtc: item.paidAtUtc,
		receiptCode: item.receiptCode,
		receiptIssuedAtUtc: item.receiptIssuedAtUtc,
		notes: item.notes,
		createdAtUtc: item.createdAtUtc
	});
	const findSaleById = (saleId: string) => sales.find((sale) => sale.saleId === saleId) ?? null;
	const findPaymentByIds = (saleId: string, paymentId: string) =>
		findSaleById(saleId)?.payments.find((payment) => payment.id === paymentId) ?? null;
	const movementPaymentRecord = (item: PaymentMovementItem) =>
		findPaymentByIds(item.saleId, item.paymentId) ?? movementPaymentSnapshot(item);
	const movementTimingLabel = (item: PaymentMovementItem) =>
		paymentDocumentTimingLabel(movementPaymentRecord(item));
	const movementReceiptCode = (item: PaymentMovementItem) => {
		const sale = findSaleById(item.saleId);
		const payment = movementPaymentRecord(item);
		return sale
			? paymentReceiptCode(sale, payment)
			: (payment.receiptCode ??
					`RIC-${item.referenceCode}-${item.paymentId.slice(0, 6).toUpperCase()}`);
	};
	const movementReceiptIssuedAt = (item: PaymentMovementItem) =>
		paymentReceiptIssuedAt(movementPaymentRecord(item));
	const printDocumentStyles = `
		:root {
			color-scheme: light;
			--print-ink: #0f172a;
			--print-muted: #475569;
			--print-soft: #64748b;
			--print-line: #dbe4f0;
			--print-panel: #f8fafc;
			--print-panel-strong: #eff6ff;
			--print-accent: #1769ff;
			--print-accent-dark: #0a4fd4;
		}
		* { box-sizing: border-box; }
		html, body { margin: 0; padding: 0; background: #eef2f7; color: var(--print-ink); }
		body { font-family: Arial, sans-serif; }
		@page { size: A4; margin: 14mm; }
		@media print {
			html, body { background: #ffffff; }
			.print-shell { padding: 0; }
			.print-page { box-shadow: none; border: 0; margin: 0; }
		}
		.print-shell { padding: 24px; }
		.print-page {
			max-width: 980px;
			margin: 0 auto;
			background: #ffffff;
			border: 1px solid var(--print-line);
			border-radius: 28px;
			overflow: hidden;
			box-shadow: 0 24px 50px rgba(15, 23, 42, 0.08);
		}
		.document-hero {
			display: flex;
			justify-content: space-between;
			gap: 24px;
			padding: 28px 32px;
			background: linear-gradient(135deg, var(--print-accent-dark) 0%, var(--print-accent) 58%, #31b8ff 100%);
			color: #ffffff;
		}
		.hero-copy { min-width: 0; }
		.eyebrow {
			font-size: 11px;
			font-weight: 700;
			letter-spacing: 0.22em;
			text-transform: uppercase;
			opacity: 0.82;
		}
		.document-hero h1 {
			margin: 10px 0 0;
			font-size: 30px;
			line-height: 1.1;
		}
		.hero-subtitle {
			margin: 10px 0 0;
			max-width: 620px;
			font-size: 14px;
			line-height: 1.55;
			opacity: 0.92;
		}
		.hero-reference {
			align-self: flex-start;
			padding: 12px 16px;
			border: 1px solid rgba(255, 255, 255, 0.28);
			border-radius: 18px;
			background: rgba(255, 255, 255, 0.14);
			font-size: 12px;
			line-height: 1.5;
			text-align: right;
			white-space: nowrap;
		}
		.hero-reference strong {
			display: block;
			margin-top: 4px;
			font-size: 16px;
		}
		.document-body { padding: 28px 32px 32px; }
		.top-grid,
		.document-grid {
			display: grid;
			gap: 18px;
		}
		.top-grid { grid-template-columns: minmax(0, 1.1fr) minmax(280px, 0.9fr); }
		.document-grid {
			grid-template-columns: minmax(0, 1.15fr) minmax(280px, 0.85fr);
			margin-top: 18px;
		}
		.surface {
			border: 1px solid var(--print-line);
			border-radius: 22px;
			background: #ffffff;
			padding: 18px 20px;
		}
		.surface.tint { background: var(--print-panel); }
		.surface-title {
			margin: 0;
			font-size: 17px;
			font-weight: 700;
		}
		.surface-subtitle {
			margin: 6px 0 0;
			font-size: 13px;
			line-height: 1.55;
			color: var(--print-muted);
		}
		.brand-name {
			margin: 0;
			font-size: 22px;
			font-weight: 700;
		}
		.brand-copy {
			margin: 8px 0 0;
			font-size: 14px;
			line-height: 1.6;
			color: var(--print-muted);
		}
		.meta-list {
			display: grid;
			gap: 10px;
			margin-top: 4px;
		}
		.meta-row {
			display: grid;
			grid-template-columns: 130px minmax(0, 1fr);
			gap: 12px;
			align-items: start;
		}
		.meta-label {
			font-size: 11px;
			font-weight: 700;
			letter-spacing: 0.18em;
			text-transform: uppercase;
			color: var(--print-soft);
		}
		.meta-value {
			font-size: 14px;
			line-height: 1.55;
			color: var(--print-ink);
			word-break: break-word;
		}
		.status-pills {
			display: flex;
			flex-wrap: wrap;
			gap: 10px;
			margin-top: 18px;
		}
		.status-pill {
			padding: 8px 12px;
			border-radius: 999px;
			border: 1px solid var(--print-line);
			background: var(--print-panel);
			font-size: 12px;
			color: var(--print-muted);
		}
		.status-pill strong { color: var(--print-ink); }
		.stat-grid {
			display: grid;
			grid-template-columns: repeat(5, minmax(0, 1fr));
			gap: 14px;
			margin-top: 18px;
		}
		.stat-card {
			padding: 16px;
			border-radius: 20px;
			border: 1px solid var(--print-line);
			background: var(--print-panel);
		}
		.stat-card.emphasis { background: var(--print-panel-strong); }
		.stat-label {
			font-size: 11px;
			font-weight: 700;
			letter-spacing: 0.18em;
			text-transform: uppercase;
			color: var(--print-soft);
		}
		.stat-value {
			margin-top: 10px;
			font-size: 24px;
			font-weight: 700;
			line-height: 1.1;
		}
		.stat-note {
			margin-top: 8px;
			font-size: 12px;
			line-height: 1.5;
			color: var(--print-muted);
		}
		table {
			width: 100%;
			border-collapse: collapse;
			margin-top: 16px;
		}
		th, td {
			padding: 12px 10px;
			border-bottom: 1px solid var(--print-line);
			text-align: left;
			vertical-align: top;
			font-size: 13px;
		}
		th {
			font-size: 11px;
			font-weight: 700;
			letter-spacing: 0.16em;
			text-transform: uppercase;
			color: var(--print-soft);
		}
		tbody tr:last-child td { border-bottom: 0; }
		.amount-cell { text-align: right; white-space: nowrap; }
		.center-cell { text-align: center; }
		.row-note {
			margin-top: 4px;
			font-size: 12px;
			line-height: 1.5;
			color: var(--print-muted);
		}
		.empty-state {
			padding: 18px;
			border: 1px dashed var(--print-line);
			border-radius: 18px;
			background: var(--print-panel);
			font-size: 13px;
			color: var(--print-muted);
		}
		.totals-box {
			display: grid;
			gap: 10px;
			margin-top: 18px;
			padding-top: 14px;
			border-top: 1px solid var(--print-line);
		}
		.total-row {
			display: flex;
			justify-content: space-between;
			gap: 12px;
			font-size: 13px;
			color: var(--print-muted);
		}
		.total-row strong {
			font-size: 15px;
			color: var(--print-ink);
		}
		.note-block {
			margin-top: 16px;
			padding: 16px 18px;
			border: 1px solid var(--print-line);
			border-radius: 18px;
			background: var(--print-panel);
			font-size: 13px;
			line-height: 1.65;
			white-space: pre-wrap;
			color: var(--print-muted);
		}
		.footer-note {
			margin-top: 18px;
			font-size: 11px;
			line-height: 1.6;
			color: var(--print-soft);
			text-align: right;
		}
	`;
	const renderPrintMetaRows = (rows: Array<{ label: string; value: string }>) =>
		rows
			.map(
				(row) =>
					`<div class="meta-row"><div class="meta-label">${escapeHtml(row.label)}</div><div class="meta-value">${escapeHtml(row.value)}</div></div>`
			)
			.join('');
	const renderPrintStatCards = (
		rows: Array<{ label: string; value: string; note?: string; emphasis?: boolean }>
	) =>
		rows
			.map(
				(row) =>
					`<div class="stat-card${row.emphasis ? ' emphasis' : ''}"><div class="stat-label">${escapeHtml(row.label)}</div><div class="stat-value">${escapeHtml(row.value)}</div>${row.note ? `<div class="stat-note">${escapeHtml(row.note)}</div>` : ''}</div>`
			)
			.join('');
	const buildPrintDocument = (title: string, body: string) => `<!DOCTYPE html>
<html lang="it">
<head>
	<meta charset="utf-8" />
	<title>${escapeHtml(title)}</title>
	<style>${printDocumentStyles}</style>
</head>
<body>
	<div class="print-shell">
		<div class="print-page">${body}</div>
	</div>
</body>
</html>`;
	const normalizeDownloadFileName = (value: string) =>
		value
			.toLowerCase()
			.replace(/[^a-z0-9-_]+/gi, '-')
			.replace(/-+/g, '-')
			.replace(/^-|-$/g, '') || 'documento-betterfit';
	const movementCanCollect = (item: PaymentMovementItem) => {
		const sale = findSaleById(item.saleId);
		return (
			!!sale &&
			(item.status === 'Pending' || item.status === 'Failed') &&
			sale.status !== 'Cancelled' &&
			sale.status !== 'Refunded'
		);
	};
	const lineNeedsServicePeriod = (itemType: SaleItemType) =>
		itemType === 'MembershipPeriodic' || itemType === 'Service';
	const lineSupportsCredits = (itemType: SaleItemType) =>
		itemType === 'MembershipEntries' || itemType === 'Package' || itemType === 'CreditRecharge';
	const getDaysToExpiry = (value: Date | null | undefined) => {
		if (!value) return null;
		const now = new Date();
		const midnightNow = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
		const midnightExpiry = new Date(
			value.getFullYear(),
			value.getMonth(),
			value.getDate()
		).getTime();
		return Math.ceil((midnightExpiry - midnightNow) / 86_400_000);
	};
	const renewalTimingMeta = (daysToExpiry: number): { label: string; variant: BadgeVariant } => {
		if (daysToExpiry < 0) {
			return { label: `Scaduta da ${Math.abs(daysToExpiry)}g`, variant: 'destructive' };
		}

		if (daysToExpiry === 0) {
			return { label: 'Scade oggi', variant: 'destructive' };
		}

		if (daysToExpiry <= 3) {
			return { label: `Scade in ${daysToExpiry}g`, variant: 'warning' };
		}

		return { label: `Tra ${daysToExpiry}g`, variant: 'secondary' };
	};
	const paymentTimingLabel = (status: PaymentEntryStatus, value: string) => {
		if (!value) return '';
		const parsed = new Date(value);
		return Number.isNaN(parsed.getTime())
			? ''
			: `${status === 'Paid' ? 'Incassato' : 'Scadenza'} ${dateTime.format(parsed)}`;
	};
	const paymentDescription = (sale: GymSale, payment: GymSale['payments'][number]) => {
		const methodLabel =
			paymentMethods.find((option) => option.value === payment.method)?.label ?? payment.method;
		if (payment.status === 'Paid') {
			return `${methodLabel} - ${money.format(payment.amount)} incassati`;
		}
		if (payment.status === 'Pending') {
			return `${methodLabel} - ${money.format(payment.amount)} da incassare`;
		}
		if (payment.status === 'Failed') {
			return `${methodLabel} - tentativo fallito da ${money.format(payment.amount)}`;
		}
		return `${methodLabel} - rimborso di ${money.format(payment.amount)} su vendita ${sale.referenceCode}`;
	};
	const saleDisplayStatus = (sale: GymSale) =>
		sale.status === 'Cancelled' || sale.status === 'Refunded' ? sale.status : sale.paymentStatus;
	const statusMeta = (
		status: SaleStatus | SalePaymentStatus
	): { label: string; variant: BadgeVariant } => {
		if (status === 'Paid') return { label: 'Pagato', variant: 'success' };
		if (status === 'Pending' || status === 'PendingPayment')
			return { label: 'In attesa', variant: 'warning' };
		if (status === 'PartiallyPaid') return { label: 'Parziale', variant: 'secondary' };
		if (status === 'Failed') return { label: 'Fallito', variant: 'destructive' };
		if (status === 'Cancelled') return { label: 'Annullato', variant: 'outline' };
		return { label: 'Rimborsato', variant: 'outline' };
	};
	const saleActionLabel = (action: Exclude<PendingSaleAction, null>) =>
		action === 'cancel' ? 'annullo' : 'rimborso';
	const saleConfirmTitle = (action: PendingSaleAction) =>
		action === 'cancel'
			? 'Annullare questa vendita?'
			: action === 'refund'
				? 'Rimborsare questa vendita?'
				: 'Confermare questa operazione?';
	const saleConfirmDescription = (action: PendingSaleAction, sale: GymSale | null) => {
		if (!sale) {
			return 'Seleziona una vendita valida prima di confermare l operazione.';
		}

		if (action === 'cancel') {
			return `La vendita ${sale.referenceCode} verra chiusa senza incasso e non restera piu attiva nel flusso operativo.`;
		}

		if (action !== 'refund') {
			return `Controlla i dati della vendita ${sale.referenceCode} prima di procedere con una modifica irreversibile.`;
		}

		return `Il rimborso completo della vendita ${sale.referenceCode} aggiornera incassi e stato amministrativo del movimento.`;
	};

	let searchTerm = $state('');
	let catalogSearchTerm = $state('');
	let catalogFilter = $state<CatalogFilter>('active');
	let saleStatusFilter = $state<StatusFilter>('all');
	let paymentStatusFilter = $state<PaymentFilter>('all');
	let selectedSaleId = $state<string | null>(null);
	let saleConfirmOpen = $state(false);
	let pendingSaleAction = $state<PendingSaleAction>(null);
	let createOpen = $state(false);
	let catalogOpen = $state(false);
	let paymentOpen = $state(false);
	let editingPaymentId = $state<string | null>(null);
	let selectedPaymentId = $state<string | null>(null);
	let editingCatalogItemId = $state<string | null>(null);
	let hasAutoOpened = $state(false);
	let handledRenewalPresetId = $state<string | null>(null);
	let catalogError = $state('');
	let catalogSuccess = $state('');
	let createError = $state('');
	let createSuccess = $state('');
	let createSubmitting = $state(false);
	let catalogSubmitting = $state(false);
	let paymentError = $state('');
	let paymentSuccess = $state('');
	let paymentSubmitting = $state(false);
	let saleActionSubmitting = $state(false);
	let formState = $state({
		membershipId: '',
		locationId: center.selectedLocationId ?? '',
		soldAtLocal: toInputDate(),
		notes: '',
		lines: [newLine()],
		payments: [newPayment()]
	});
	let paymentFormState = $state<PaymentEntryState>({
		amount: '',
		method: 'Cash',
		status: 'Paid',
		dueAtLocal: '',
		paidAtLocal: toInputDate(),
		notes: ''
	});
	let catalogFormState = $state<CatalogItemFormState>({
		locationId: center.selectedLocationId ?? '',
		itemType: 'Service',
		name: '',
		unitPriceAmount: '50',
		defaultQuantity: 1,
		defaultDiscountAmount: '0',
		defaultCreditsGranted: '',
		servicePeriodDays: '',
		notes: '',
		isActive: true
	});

	const membershipsQuery = createQuery(() => ({
		queryKey: ['sales-memberships', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdMembershipsGet({
				gymId: center.selectedGymId!
			});
			if (!response.success)
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile caricare le membership.'
				);
			return response.data ?? [];
		}
	}));
	const overviewQuery = createQuery(() => ({
		queryKey: ['sales-overview', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: () => fetchGymSalesOverview(center.selectedGymId!)
	}));
	const catalogQuery = createQuery(() => ({
		queryKey: ['sales-catalog', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: () => fetchGymSaleCatalog(center.selectedGymId!, { includeInactive: true })
	}));
	const salesQuery = createQuery(() => ({
		queryKey: [
			'sales-list',
			center.selectedGymId,
			mode,
			saleStatusFilter,
			paymentStatusFilter,
			searchTerm
		],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymSales(center.selectedGymId!, {
				status: saleStatusFilter,
				paymentStatus: paymentStatusFilter,
				search: searchTerm,
				onlyRenewals: mode === 'renewals'
			})
	}));
	const membershipsQueryError = $derived(
		membershipsQuery.error instanceof Error
			? membershipsQuery.error.message
			: membershipsQuery.error
				? 'Impossibile caricare le membership.'
				: null
	);
	const overviewQueryError = $derived(
		overviewQuery.error instanceof Error
			? overviewQuery.error.message
			: overviewQuery.error
				? 'Impossibile caricare l overview vendite.'
				: null
	);
	const catalogQueryError = $derived(
		catalogQuery.error instanceof Error
			? catalogQuery.error.message
			: catalogQuery.error
				? 'Impossibile caricare il listino.'
				: null
	);
	const salesQueryError = $derived(
		salesQuery.error instanceof Error
			? salesQuery.error.message
			: salesQuery.error
				? 'Impossibile caricare il registro vendite.'
				: null
	);

	const memberships = $derived(membershipsQuery.data ?? []);
	const catalogItems = $derived(catalogQuery.data ?? []);
	const membershipOptions = $derived(
		memberships
			.map((m) => ({
				id: m.membershipId ?? '',
				label: membershipDisplayName(m),
				email: m.userEmail ?? m.invitationEmail ?? '',
				primaryLocationId: m.primaryLocationId ?? '',
				locations: m.locations ?? [],
				status: m.status,
				endedAtUtc: m.endedAtUtc ?? null
			}))
			.filter((m) => m.id) satisfies MembershipOption[]
	);
	const renewalCandidates = $derived.by(() => {
		return membershipOptions
			.map((membership) => {
				const daysToExpiry = getDaysToExpiry(membership.endedAtUtc);
				if (
					membership.status !== 'Active' ||
					!membership.endedAtUtc ||
					daysToExpiry === null ||
					daysToExpiry < -30 ||
					daysToExpiry > 30
				) {
					return null;
				}

				return {
					id: membership.id,
					name: membership.label,
					email: membership.email,
					locationLabel:
						membership.locations
							.map((location) => location.name)
							.filter(Boolean)
							.join(', ') || 'Tutte le sedi abilitate',
					primaryLocationId: membership.primaryLocationId,
					locations: membership.locations,
					endedAtUtc: membership.endedAtUtc,
					daysToExpiry
				} satisfies RenewalCandidate;
			})
			.filter((membership) => membership !== null)
			.sort((left, right) => left.daysToExpiry - right.daysToExpiry);
	});
	const filteredLocations = $derived.by(() => {
		const current = membershipOptions.find((m) => m.id === formState.membershipId);
		if (!current) return center.locations;
		const ids = new Set(current.locations.map((location) => location.id));
		return center.locations.filter((location) => ids.has(location.id));
	});
	const sales = $derived(salesQuery.data ?? []);
	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms ||
			(!!center.selectedGymId &&
				((!membershipsQuery.data && membershipsQuery.isPending) ||
					(!overviewQuery.data && overviewQuery.isPending) ||
					(!catalogQuery.data && catalogQuery.isPending) ||
					(!salesQuery.data && salesQuery.isPending)))
	);
	const workspaceError = $derived(
		center.gymsError ??
			membershipsQueryError ??
			overviewQueryError ??
			catalogQueryError ??
			salesQueryError ??
			null
	);
	const selectedSale = $derived(
		sales.find((sale) => sale.saleId === selectedSaleId) ?? sales[0] ?? null
	);
	const collectionDeskItems = $derived.by(() => {
		const startOfToday = new Date();
		startOfToday.setHours(0, 0, 0, 0);

		return sales
			.flatMap((sale) => {
				if (sale.status === 'Cancelled' || sale.status === 'Refunded') {
					return [] satisfies CollectionDeskItem[];
				}

				return sale.payments.flatMap((payment) => {
					if (payment.status !== 'Pending' && payment.status !== 'Failed') {
						return [] satisfies CollectionDeskItem[];
					}

					const dueAt = payment.dueAtUtc;
					const daysFromToday =
						dueAt !== null
							? Math.floor((dueAt.getTime() - startOfToday.getTime()) / 86_400_000)
							: null;

					let timingLabel = 'Senza scadenza';
					let priority = 4;
					let badgeVariant: BadgeVariant = 'secondary';

					if (payment.status === 'Failed') {
						timingLabel = 'Pagamento fallito';
						priority = 0;
						badgeVariant = 'destructive';
					} else if (daysFromToday !== null && daysFromToday < 0) {
						timingLabel = `Scaduto da ${Math.abs(daysFromToday)}g`;
						priority = 1;
						badgeVariant = 'destructive';
					} else if (daysFromToday === 0) {
						timingLabel = 'Scade oggi';
						priority = 2;
						badgeVariant = 'warning';
					} else if (daysFromToday !== null) {
						timingLabel = `Scade tra ${daysFromToday}g`;
						priority = 3;
						badgeVariant = 'secondary';
					}

					return [
						{
							saleId: sale.saleId,
							paymentId: payment.id,
							referenceCode: sale.referenceCode,
							memberName: sale.memberName,
							memberEmail: sale.memberEmail,
							locationName: sale.locationName,
							amount: payment.amount,
							method: payment.method,
							status: payment.status,
							dueAtUtc: dueAt,
							priority,
							timingLabel,
							badgeVariant
						} satisfies CollectionDeskItem
					];
				});
			})
			.sort((left, right) => {
				if (left.priority !== right.priority) {
					return left.priority - right.priority;
				}

				const leftDueAt = left.dueAtUtc?.getTime() ?? Number.MAX_SAFE_INTEGER;
				const rightDueAt = right.dueAtUtc?.getTime() ?? Number.MAX_SAFE_INTEGER;
				if (leftDueAt !== rightDueAt) {
					return leftDueAt - rightDueAt;
				}

				return left.referenceCode.localeCompare(right.referenceCode);
			});
	});
	const collectionDeskSummary = $derived.by(() => {
		let pendingCount = 0;
		let pendingAmount = 0;
		let overdueCount = 0;
		let overdueAmount = 0;
		let failedCount = 0;

		for (const item of collectionDeskItems) {
			if (item.status === 'Pending') {
				pendingCount += 1;
				pendingAmount += item.amount;
			}

			if (item.status === 'Pending' && item.badgeVariant === 'destructive') {
				overdueCount += 1;
				overdueAmount += item.amount;
			}

			if (item.status === 'Failed') {
				failedCount += 1;
			}
		}

		return {
			pendingCount,
			pendingAmount,
			overdueCount,
			overdueAmount,
			failedCount
		};
	});
	const recentReceiptItems = $derived.by(() => {
		return sales
			.flatMap((sale) =>
				sale.payments
					.filter((payment) => payment.status === 'Paid' && payment.paidAtUtc)
					.map(
						(payment) =>
							({
								saleId: sale.saleId,
								paymentId: payment.id,
								referenceCode: sale.referenceCode,
								receiptCode: paymentReceiptCode(sale, payment),
								memberName: sale.memberName,
								locationName: sale.locationName,
								amount: payment.amount,
								method: payment.method,
								paidAtUtc: payment.paidAtUtc!
							}) satisfies RecentReceiptItem
					)
			)
			.sort((left, right) => right.paidAtUtc.getTime() - left.paidAtUtc.getTime())
			.slice(0, 6);
	});
	const paymentMovementItems = $derived.by(() => {
		return sales
			.flatMap((sale) =>
				sale.payments.map((payment) => {
					const sortPriority =
						payment.status === 'Failed'
							? 0
							: payment.status === 'Pending'
								? 1
								: payment.status === 'Paid'
									? 2
									: payment.status === 'Refunded'
										? 3
										: 4;
					const sortAt = payment.dueAtUtc ?? payment.paidAtUtc ?? payment.createdAtUtc;

					return {
						saleId: sale.saleId,
						paymentId: payment.id,
						referenceCode: sale.referenceCode,
						memberName: sale.memberName,
						memberEmail: sale.memberEmail,
						locationName: sale.locationName,
						amount: payment.amount,
						method: payment.method,
						status: payment.status,
						saleStatus: sale.status,
						salePaymentStatus: sale.paymentStatus,
						dueAtUtc: payment.dueAtUtc,
						paidAtUtc: payment.paidAtUtc,
						receiptCode: payment.receiptCode,
						receiptIssuedAtUtc: payment.receiptIssuedAtUtc,
						notes: payment.notes,
						createdAtUtc: payment.createdAtUtc,
						sortPriority,
						sortAt
					} satisfies PaymentMovementItem;
				})
			)
			.sort((left, right) => {
				if (left.sortPriority !== right.sortPriority) {
					return left.sortPriority - right.sortPriority;
				}

				return right.sortAt.getTime() - left.sortAt.getTime();
			});
	});
	const paymentMovementSummary = $derived.by(() => {
		let paidCount = 0;
		let paidAmount = 0;
		let pendingCount = 0;
		let pendingAmount = 0;
		let failedCount = 0;
		let refundedCount = 0;

		for (const item of paymentMovementItems) {
			if (item.status === 'Paid') {
				paidCount += 1;
				paidAmount += item.amount;
			} else if (item.status === 'Pending') {
				pendingCount += 1;
				pendingAmount += item.amount;
			} else if (item.status === 'Failed') {
				failedCount += 1;
			} else if (item.status === 'Refunded') {
				refundedCount += 1;
			}
		}

		return {
			paidCount,
			paidAmount,
			pendingCount,
			pendingAmount,
			failedCount,
			refundedCount
		};
	});
	const selectedCatalogItem = $derived(
		catalogItems.find((item) => item.catalogItemId === editingCatalogItemId) ?? null
	);
	const filteredCatalogItems = $derived.by(() => {
		const needle = catalogSearchTerm.trim().toLowerCase();

		return catalogItems.filter((item) => {
			if (catalogFilter === 'active' && !item.isActive) {
				return false;
			}

			if (catalogFilter === 'inactive' && item.isActive) {
				return false;
			}

			if (center.selectedLocationId && item.locationId !== center.selectedLocationId) {
				return false;
			}

			if (!needle) {
				return true;
			}

			return (
				item.name.toLowerCase().includes(needle) ||
				item.locationName.toLowerCase().includes(needle) ||
				item.itemType.toLowerCase().includes(needle) ||
				(item.notes?.toLowerCase().includes(needle) ?? false)
			);
		});
	});
	const catalogItemsForCurrentSaleLocation = $derived.by(() => {
		if (!formState.locationId) {
			return [] satisfies GymSaleCatalogItem[];
		}

		return catalogItems.filter((item) => item.isActive && item.locationId === formState.locationId);
	});
	const catalogSummary = $derived.by(() => ({
		total: catalogItems.length,
		active: catalogItems.filter((item) => item.isActive).length,
		inactive: catalogItems.filter((item) => !item.isActive).length
	}));
	const editingPayment = $derived(
		selectedSale?.payments.find((payment) => payment.id === editingPaymentId) ?? null
	);
	const selectedPaymentMovement = $derived(
		paymentMovementItems.find((payment) => payment.paymentId === selectedPaymentId) ?? null
	);
	const selectedPaymentSale = $derived(
		selectedPaymentMovement ? findSaleById(selectedPaymentMovement.saleId) : null
	);
	const selectedPaymentRecord = $derived(
		selectedPaymentMovement
			? findPaymentByIds(selectedPaymentMovement.saleId, selectedPaymentMovement.paymentId)
			: null
	);
	const selectedSaleActivities = $derived.by(() => {
		if (!selectedSale) {
			return [] satisfies SaleActivity[];
		}

		const activities: SaleActivity[] = [
			{
				id: `${selectedSale.saleId}-created`,
				happenedAt: selectedSale.createdAtUtc,
				title: 'Vendita registrata',
				description: `Creata come ${selectedSale.referenceCode} per ${money.format(selectedSale.totalAmount)}`,
				variant: 'secondary'
			}
		];

		for (const payment of selectedSale.payments) {
			activities.push({
				id: payment.id,
				happenedAt: payment.paidAtUtc ?? payment.dueAtUtc ?? payment.createdAtUtc,
				title:
					payment.status === 'Refunded'
						? 'Rimborso registrato'
						: payment.status === 'Paid'
							? 'Pagamento incassato'
							: payment.status === 'Failed'
								? 'Pagamento fallito'
								: 'Rata pianificata',
				description: paymentDescription(selectedSale, payment),
				variant: statusMeta(payment.status).variant
			});
		}

		if (selectedSale.status === 'Cancelled') {
			activities.push({
				id: `${selectedSale.saleId}-cancelled`,
				happenedAt: selectedSale.updatedAtUtc,
				title: 'Vendita annullata',
				description: `La vendita ${selectedSale.referenceCode} e stata chiusa senza incasso.`,
				variant: 'outline'
			});
		}

		return activities.sort((left, right) => right.happenedAt.getTime() - left.happenedAt.getTime());
	});
	const totalPreview = $derived(
		Math.max(
			0,
			formState.lines.reduce(
				(sum, line) =>
					sum + line.quantity * parseMoney(line.unitPriceAmount) - parseMoney(line.discountAmount),
				0
			)
		)
	);
	const paidPreview = $derived(
		formState.payments.reduce(
			(sum, payment) => sum + (payment.status === 'Paid' ? parseMoney(payment.amount) : 0),
			0
		)
	);
	const pageTitle = $derived(
		mode === 'new'
			? 'Nuova vendita'
			: mode === 'payments'
				? 'Pagamenti e ricevute'
				: mode === 'catalog'
					? 'Listino vendite'
					: mode === 'renewals'
						? 'Rinnovi'
						: 'Vendite'
	);

	$effect(() => {
		if (
			sales.length > 0 &&
			(!selectedSaleId || !sales.some((sale) => sale.saleId === selectedSaleId))
		)
			selectedSaleId = sales[0].saleId;
	});
	$effect(() => {
		if (paymentMovementItems.length === 0) {
			selectedPaymentId = null;
			return;
		}

		if (
			!selectedPaymentId ||
			!paymentMovementItems.some((payment) => payment.paymentId === selectedPaymentId)
		) {
			selectedPaymentId = paymentMovementItems[0].paymentId;
		}
	});
	$effect(() => {
		if (autoOpenCreateSheet && !hasAutoOpened) {
			createOpen = true;
			hasAutoOpened = true;
		}
	});
	$effect(() => {
		if (!formState.membershipId && membershipOptions.length > 0) {
			const first = membershipOptions[0];
			formState = {
				...formState,
				membershipId: first.id,
				locationId:
					center.selectedLocationId || first.primaryLocationId || first.locations[0]?.id || ''
			};
		}
	});
	$effect(() => {
		if (!presetMembershipId) {
			return;
		}

		const preset = membershipOptions.find((membership) => membership.id === presetMembershipId);
		if (!preset || formState.membershipId === preset.id) {
			return;
		}

		formState = {
			...formState,
			membershipId: preset.id,
			locationId:
				center.selectedLocationId || preset.primaryLocationId || preset.locations[0]?.id || ''
		};
	});
	$effect(() => {
		if (!presetMembershipId) {
			handledRenewalPresetId = null;
			return;
		}

		if (mode !== 'renewals' || handledRenewalPresetId === presetMembershipId) {
			return;
		}

		const candidate = renewalCandidates.find((item) => item.id === presetMembershipId);
		if (!candidate) {
			return;
		}

		openRenewalCreate(candidate.id);
		handledRenewalPresetId = candidate.id;
	});
	$effect(() => {
		if (mode !== 'all' || !presetSaleId) {
			return;
		}

		const sale = sales.find((item) => item.saleId === presetSaleId);
		if (!sale || selectedSaleId === sale.saleId) {
			return;
		}

		selectedSaleId = sale.saleId;
		selectedPaymentId = sale.payments[0]?.id ?? null;
	});
	$effect(() => {
		if (!paymentOpen && editingPaymentId) {
			editingPaymentId = null;
		}
	});
	$effect(() => {
		if (!catalogOpen && editingCatalogItemId) {
			editingCatalogItemId = null;
		}
	});
	$effect(() => {
		if (catalogOpen && !catalogFormState.locationId) {
			catalogFormState = {
				...catalogFormState,
				locationId: center.selectedLocationId ?? center.locations[0]?.id ?? ''
			};
		}
	});

	function resetForm() {
		const first = membershipOptions[0];
		formState = {
			membershipId: first?.id ?? '',
			locationId:
				center.selectedLocationId || first?.primaryLocationId || first?.locations[0]?.id || '',
			soldAtLocal: toInputDate(),
			notes: '',
			lines: [newLine()],
			payments: [newPayment()]
		};
		createError = '';
	}

	function resetCatalogForm() {
		catalogFormState = {
			locationId: center.selectedLocationId ?? center.locations[0]?.id ?? '',
			itemType: 'Service',
			name: '',
			unitPriceAmount: '50',
			defaultQuantity: 1,
			defaultDiscountAmount: '0',
			defaultCreditsGranted: '',
			servicePeriodDays: '',
			notes: '',
			isActive: true
		};
		catalogError = '';
	}

	function openCreateCatalogItem() {
		editingCatalogItemId = null;
		resetCatalogForm();
		catalogSuccess = '';
		catalogOpen = true;
	}

	function openEditCatalogItem(item: GymSaleCatalogItem) {
		editingCatalogItemId = item.catalogItemId;
		catalogFormState = {
			locationId: item.locationId,
			itemType: item.itemType,
			name: item.name,
			unitPriceAmount: formatMoneyInput(item.unitPriceAmount),
			defaultQuantity: item.defaultQuantity,
			defaultDiscountAmount: formatMoneyInput(item.defaultDiscountAmount),
			defaultCreditsGranted:
				item.defaultCreditsGranted !== null ? String(item.defaultCreditsGranted) : '',
			servicePeriodDays: item.servicePeriodDays !== null ? String(item.servicePeriodDays) : '',
			notes: item.notes ?? '',
			isActive: item.isActive
		};
		catalogError = '';
		catalogSuccess = '';
		catalogOpen = true;
	}

	function buildLineFromCatalogItem(item: GymSaleCatalogItem): LineState {
		const soldAt = parseInputDateTime(formState.soldAtLocal) ?? new Date();
		const servicePeriodStart = lineNeedsServicePeriod(item.itemType) ? toInputDate(soldAt) : '';
		let servicePeriodEnd = '';

		if (
			lineNeedsServicePeriod(item.itemType) &&
			item.servicePeriodDays &&
			item.servicePeriodDays > 0
		) {
			const endDate = new Date(soldAt);
			endDate.setDate(endDate.getDate() + item.servicePeriodDays);
			servicePeriodEnd = toInputDate(endDate);
		}

		return {
			itemType: item.itemType,
			name: item.name,
			quantity: item.defaultQuantity,
			unitPriceAmount: formatMoneyInput(item.unitPriceAmount),
			discountAmount: formatMoneyInput(item.defaultDiscountAmount),
			servicePeriodStartLocal: servicePeriodStart,
			servicePeriodEndLocal: servicePeriodEnd,
			creditsGranted: item.defaultCreditsGranted !== null ? String(item.defaultCreditsGranted) : '',
			notes: item.notes ?? ''
		};
	}

	function addCatalogItemToSale(item: GymSaleCatalogItem) {
		formState = {
			...formState,
			lines: [...formState.lines, buildLineFromCatalogItem(item)]
		};
		createSuccess = '';
		createError = '';
	}

	function validateCatalogForm() {
		if (!catalogFormState.locationId) {
			return 'Seleziona una sede del listino.';
		}

		if (!catalogFormState.name.trim()) {
			return 'Inserisci il nome dell elemento di listino.';
		}

		if (
			!Number.isFinite(catalogFormState.defaultQuantity) ||
			catalogFormState.defaultQuantity <= 0
		) {
			return 'La quantita predefinita deve essere maggiore di zero.';
		}

		const grossAmount =
			catalogFormState.defaultQuantity * parseMoney(catalogFormState.unitPriceAmount);
		const discountAmount = parseMoney(catalogFormState.defaultDiscountAmount);
		if (discountAmount > grossAmount) {
			return 'Lo sconto predefinito non puo superare il lordo della riga.';
		}

		if (
			catalogFormState.defaultCreditsGranted.trim() &&
			(parseInteger(catalogFormState.defaultCreditsGranted) ?? 0) <= 0
		) {
			return 'I crediti predefiniti devono essere un numero intero positivo.';
		}

		if (
			catalogFormState.servicePeriodDays.trim() &&
			(parseInteger(catalogFormState.servicePeriodDays) ?? 0) <= 0
		) {
			return 'I giorni di servizio devono essere un numero intero positivo.';
		}

		return '';
	}

	async function handleSaveCatalogItem(event: Event) {
		event.preventDefault();
		catalogError = '';
		catalogSuccess = '';

		if (!center.selectedGymId) {
			catalogError = 'Seleziona prima un tenant.';
			return;
		}

		const validationError = validateCatalogForm();
		if (validationError) {
			catalogError = validationError;
			return;
		}

		const request = {
			locationId: catalogFormState.locationId,
			itemType: catalogFormState.itemType,
			name: catalogFormState.name.trim(),
			unitPriceAmount: parseMoney(catalogFormState.unitPriceAmount),
			defaultQuantity: catalogFormState.defaultQuantity,
			defaultDiscountAmount: parseMoney(catalogFormState.defaultDiscountAmount),
			defaultCreditsGranted: catalogFormState.defaultCreditsGranted.trim()
				? parseInteger(catalogFormState.defaultCreditsGranted)
				: null,
			servicePeriodDays: catalogFormState.servicePeriodDays.trim()
				? parseInteger(catalogFormState.servicePeriodDays)
				: null,
			notes: catalogFormState.notes.trim() || null,
			isActive: catalogFormState.isActive
		} satisfies UpsertGymSaleCatalogItemRequest;

		catalogSubmitting = true;
		try {
			const saved = editingCatalogItemId
				? await updateGymSaleCatalogItem(center.selectedGymId, editingCatalogItemId, request)
				: await createGymSaleCatalogItem(center.selectedGymId, request);
			await catalogQuery.refetch();
			catalogSuccess = editingCatalogItemId
				? `Elemento ${saved.name} aggiornato nel listino.`
				: `Elemento ${saved.name} aggiunto al listino.`;
			catalogOpen = false;
			editingCatalogItemId = null;
			resetCatalogForm();
		} catch (error: unknown) {
			catalogError =
				error instanceof Error ? error.message : 'Impossibile salvare l elemento di listino.';
		} finally {
			catalogSubmitting = false;
		}
	}

	async function handleToggleCatalogItemActivation(item: GymSaleCatalogItem) {
		if (!center.selectedGymId) {
			return;
		}

		catalogError = '';
		catalogSuccess = '';
		try {
			const updated = await updateGymSaleCatalogItemActivation(
				center.selectedGymId,
				item.catalogItemId,
				{ isActive: !item.isActive }
			);
			await catalogQuery.refetch();
			catalogSuccess = updated.isActive
				? `${updated.name} riattivato nel listino.`
				: `${updated.name} disattivato nel listino.`;
		} catch (error: unknown) {
			catalogError =
				error instanceof Error
					? error.message
					: 'Impossibile aggiornare lo stato dell elemento di listino.';
		}
	}

	function openRenewalCreate(candidateId: string) {
		const candidate = renewalCandidates.find((item) => item.id === candidateId);
		if (!candidate) {
			return;
		}

		const targetLocationId =
			center.selectedLocationId || candidate.primaryLocationId || candidate.locations[0]?.id || '';

		formState = {
			membershipId: candidate.id,
			locationId: targetLocationId,
			soldAtLocal: toInputDate(),
			notes:
				candidate.daysToExpiry < 0
					? `Rinnovo membership scaduta il ${date.format(candidate.endedAtUtc)}.`
					: `Rinnovo membership con scadenza ${date.format(candidate.endedAtUtc)}.`,
			lines: [
				{
					itemType: 'MembershipPeriodic',
					name: 'Rinnovo abbonamento',
					quantity: 1,
					unitPriceAmount: '50',
					discountAmount: '0',
					servicePeriodStartLocal: '',
					servicePeriodEndLocal: '',
					creditsGranted: '',
					notes: ''
				}
			],
			payments: [newPayment()]
		};
		createError = '';
		createSuccess = '';
		paymentSuccess = '';
		createOpen = true;
	}

	function openPaymentForm() {
		if (!selectedSale || selectedSale.remainingAmount <= 0) {
			return;
		}

		editingPaymentId = null;
		paymentFormState = {
			amount: formatMoneyInput(selectedSale.remainingAmount),
			method: 'Cash',
			status: 'Paid',
			dueAtLocal: '',
			paidAtLocal: toInputDate(),
			notes: ''
		};
		paymentError = '';
		paymentOpen = true;
	}

	function openEditPaymentForm(paymentId: string) {
		if (!selectedSale) {
			return;
		}

		const payment = selectedSale.payments.find((item) => item.id === paymentId);
		if (!payment || payment.status === 'Refunded') {
			return;
		}

		editingPaymentId = payment.id;
		paymentFormState = {
			amount: formatMoneyInput(payment.amount),
			method: payment.method,
			status: payment.status === 'Pending' || payment.status === 'Failed' ? payment.status : 'Paid',
			dueAtLocal: payment.dueAtUtc ? toInputDate(payment.dueAtUtc) : '',
			paidAtLocal: payment.paidAtUtc ? toInputDate(payment.paidAtUtc) : toInputDate(),
			notes: payment.notes ?? ''
		};
		paymentError = '';
		paymentSuccess = '';
		paymentOpen = true;
	}

	function openCollectPaymentForm(paymentId: string) {
		if (
			!selectedSale ||
			selectedSale.status === 'Cancelled' ||
			selectedSale.status === 'Refunded'
		) {
			return;
		}

		const payment = selectedSale.payments.find((item) => item.id === paymentId);
		if (!payment || payment.status === 'Refunded') {
			return;
		}

		editingPaymentId = payment.id;
		paymentFormState = {
			amount: formatMoneyInput(payment.amount),
			method: payment.method,
			status: 'Paid',
			dueAtLocal: payment.dueAtUtc ? toInputDate(payment.dueAtUtc) : '',
			paidAtLocal: toInputDate(),
			notes: payment.notes ?? ''
		};
		paymentError = '';
		paymentSuccess = '';
		paymentOpen = true;
	}

	function openCollectPaymentShortcut(saleId: string, paymentId: string) {
		selectedSaleId = saleId;
		selectedPaymentId = paymentId;

		const sale = sales.find((item) => item.saleId === saleId);
		if (!sale || sale.status === 'Cancelled' || sale.status === 'Refunded') {
			return;
		}

		const payment = sale.payments.find((item) => item.id === paymentId);
		if (!payment || payment.status === 'Refunded' || payment.status === 'Paid') {
			return;
		}

		editingPaymentId = payment.id;
		paymentFormState = {
			amount: formatMoneyInput(payment.amount),
			method: payment.method,
			status: 'Paid',
			dueAtLocal: payment.dueAtUtc ? toInputDate(payment.dueAtUtc) : '',
			paidAtLocal: toInputDate(),
			notes: payment.notes ?? ''
		};
		paymentError = '';
		paymentSuccess = '';
		paymentOpen = true;
	}

	function selectPaymentMovement(item: PaymentMovementItem) {
		selectedSaleId = item.saleId;
		selectedPaymentId = item.paymentId;
	}

	function openSaleWorkspace(saleId: string) {
		if (mode === 'payments') {
			void goto(`/sales?saleId=${encodeURIComponent(saleId)}`);
			return;
		}

		selectedSaleId = saleId;
	}

	function validateCreateSaleForm() {
		if (!formState.locationId) {
			return 'Seleziona una sede valida per registrare la vendita.';
		}

		if (formState.lines.length === 0) {
			return 'Aggiungi almeno una riga vendita.';
		}

		if (formState.payments.length === 0) {
			return 'Aggiungi almeno un movimento di pagamento.';
		}

		let computedTotalAmount = 0;

		for (const [index, line] of formState.lines.entries()) {
			if (!line.name.trim()) {
				return `La riga ${index + 1} deve avere un nome.`;
			}

			if (!Number.isFinite(line.quantity) || line.quantity <= 0) {
				return `La quantita della riga ${index + 1} deve essere maggiore di zero.`;
			}

			const grossAmount = line.quantity * parseMoney(line.unitPriceAmount);
			const discountAmount = parseMoney(line.discountAmount);
			if (discountAmount > grossAmount) {
				return `Lo sconto della riga ${index + 1} supera il lordo della riga.`;
			}

			const servicePeriodStart = parseInputDateTime(line.servicePeriodStartLocal);
			const servicePeriodEnd = parseInputDateTime(line.servicePeriodEndLocal);
			if (servicePeriodStart && servicePeriodEnd && servicePeriodEnd < servicePeriodStart) {
				return `Il periodo di servizio della riga ${index + 1} non e valido.`;
			}

			if (line.creditsGranted.trim() && parseInteger(line.creditsGranted) === null) {
				return `I crediti della riga ${index + 1} devono essere un numero intero.`;
			}

			computedTotalAmount += grossAmount - discountAmount;
		}

		let paidAmount = 0;
		for (const [index, payment] of formState.payments.entries()) {
			const amount = parseMoney(payment.amount);
			if (amount <= 0) {
				return `Il pagamento ${index + 1} deve avere un importo maggiore di zero.`;
			}

			if (payment.status === 'Paid') {
				paidAmount += amount;
			}
		}

		if (paidAmount > computedTotalAmount) {
			return 'Gli incassi segnati come pagati superano il totale della vendita.';
		}

		return '';
	}

	function validatePaymentForm() {
		if (!selectedSale) {
			return 'Seleziona una vendita prima di registrare un incasso.';
		}

		const amount = parseMoney(paymentFormState.amount);
		if (amount <= 0) {
			return 'Inserisci un importo valido.';
		}

		if (paymentFormState.status === 'Paid') {
			const otherPaidAmount = selectedSale.payments
				.filter((payment) => payment.id !== editingPaymentId && payment.status === 'Paid')
				.reduce((sum, payment) => sum + payment.amount, 0);

			if (otherPaidAmount + amount > selectedSale.totalAmount) {
				return 'L importo incassato supera il totale ancora consentito per la vendita.';
			}
		}

		return '';
	}

	async function handleCreateSale(event: Event) {
		event.preventDefault();
		createError = '';
		createSuccess = '';
		paymentSuccess = '';
		if (!center.selectedGymId || !formState.membershipId) {
			createError = 'Seleziona tenant e cliente prima di registrare una vendita.';
			return;
		}

		const validationError = validateCreateSaleForm();
		if (validationError) {
			createError = validationError;
			return;
		}

		createSubmitting = true;
		try {
			const request = {
				membershipId: formState.membershipId,
				locationId: formState.locationId || null,
				soldAtUtc: formState.soldAtLocal ? new Date(formState.soldAtLocal).toISOString() : null,
				notes: formState.notes.trim() || null,
				lines: formState.lines.map((line) => ({
					itemType: line.itemType,
					name: line.name.trim(),
					quantity: line.quantity,
					unitPriceAmount: parseMoney(line.unitPriceAmount),
					discountAmount: parseMoney(line.discountAmount),
					servicePeriodStartUtc: line.servicePeriodStartLocal
						? new Date(line.servicePeriodStartLocal).toISOString()
						: null,
					servicePeriodEndUtc: line.servicePeriodEndLocal
						? new Date(line.servicePeriodEndLocal).toISOString()
						: null,
					creditsGranted: line.creditsGranted.trim() ? parseInteger(line.creditsGranted) : null,
					notes: line.notes.trim() || null
				})),
				payments: formState.payments.map((payment) => ({
					amount: parseMoney(payment.amount),
					method: payment.method,
					status: payment.status,
					dueAtUtc:
						payment.status === 'Pending' && payment.dueAtLocal
							? new Date(payment.dueAtLocal).toISOString()
							: null,
					paidAtUtc:
						payment.status === 'Paid' && payment.paidAtLocal
							? new Date(payment.paidAtLocal).toISOString()
							: null,
					notes: payment.notes.trim() || null
				}))
			} satisfies CreateGymSaleRequest;
			const created = await createGymSale(center.selectedGymId, request);
			await Promise.all([salesQuery.refetch(), overviewQuery.refetch()]);
			selectedSaleId = created.saleId;
			createOpen = false;
			createSuccess = `Vendita ${created.referenceCode} registrata con successo.`;
			resetForm();
		} catch (error: unknown) {
			createError = error instanceof Error ? error.message : 'Impossibile registrare la vendita.';
		} finally {
			createSubmitting = false;
		}
	}

	async function handleAddPayment(event: Event) {
		event.preventDefault();
		paymentError = '';
		paymentSuccess = '';
		createSuccess = '';

		if (!center.selectedGymId || !selectedSale) {
			paymentError = 'Seleziona una vendita prima di registrare un incasso.';
			return;
		}

		const paymentValidationError = validatePaymentForm();
		if (paymentValidationError) {
			paymentError = paymentValidationError;
			return;
		}

		const amount = parseMoney(paymentFormState.amount);
		paymentSubmitting = true;
		try {
			const request = {
				amount,
				method: paymentFormState.method,
				status: paymentFormState.status,
				dueAtUtc: paymentFormState.dueAtLocal
					? new Date(paymentFormState.dueAtLocal).toISOString()
					: null,
				paidAtUtc:
					paymentFormState.status === 'Paid'
						? new Date(paymentFormState.paidAtLocal || toInputDate()).toISOString()
						: null,
				notes: paymentFormState.notes.trim() || null
			} satisfies AddGymSalePaymentRequest;

			const updated = editingPaymentId
				? await updateGymSalePayment(
						center.selectedGymId,
						selectedSale.saleId,
						editingPaymentId,
						request
					)
				: await addGymSalePayment(center.selectedGymId, selectedSale.saleId, request);
			await Promise.all([salesQuery.refetch(), overviewQuery.refetch()]);
			selectedSaleId = updated.saleId;
			paymentOpen = false;
			paymentSuccess = editingPaymentId
				? `Movimento aggiornato su ${updated.referenceCode}.`
				: paymentFormState.status === 'Paid'
					? `Incasso registrato su ${updated.referenceCode}.`
					: `Rata aggiornata su ${updated.referenceCode}.`;
			editingPaymentId = null;
		} catch (error: unknown) {
			paymentError =
				error instanceof Error ? error.message : 'Impossibile registrare il pagamento.';
		} finally {
			paymentSubmitting = false;
		}
	}

	function closeSaleConfirm() {
		saleConfirmOpen = false;
		pendingSaleAction = null;
	}

	function openSaleConfirm(action: Exclude<PendingSaleAction, null>) {
		if (!selectedSale) {
			return;
		}

		pendingSaleAction = action;
		saleConfirmOpen = true;
	}

	async function handleConfirmSaleAction() {
		if (!center.selectedGymId || !selectedSale || !pendingSaleAction) {
			return;
		}

		const action = pendingSaleAction;
		saleActionSubmitting = true;
		createSuccess = '';
		paymentSuccess = '';
		paymentError = '';
		try {
			const updated =
				action === 'cancel'
					? await cancelGymSale(center.selectedGymId, selectedSale.saleId)
					: await refundGymSale(center.selectedGymId, selectedSale.saleId);
			await Promise.all([salesQuery.refetch(), overviewQuery.refetch()]);
			selectedSaleId = updated.saleId;
			closeSaleConfirm();
			paymentSuccess =
				action === 'cancel'
					? `Vendita ${updated.referenceCode} annullata.`
					: `Vendita ${updated.referenceCode} rimborsata.`;
		} catch (error: unknown) {
			paymentError =
				error instanceof Error
					? error.message
					: `Impossibile completare l ${saleActionLabel(action)} della vendita.`;
		} finally {
			saleActionSubmitting = false;
		}
	}

	function buildSalePrintHtml(sale: GymSale) {
		const gymName = center.selectedGym?.name ?? 'Betterfit';
		const printIssuedAt = dateTime.format(new Date());
		const saleStatusLabel = statusMeta(sale.status).label;
		const paymentStatusLabel = statusMeta(sale.paymentStatus).label;
		const displayStatusLabel = statusMeta(saleDisplayStatus(sale)).label;
		const soldLinesCount = sale.lines.reduce((total, line) => total + line.quantity, 0);
		const paidMovementsCount = sale.payments.filter((payment) => payment.status === 'Paid').length;

		const paymentRows =
			sale.payments
				.map((payment) => {
					const methodLabel = paymentMethodLabel(payment.method);
					const statusLabel = statusMeta(payment.status).label;
					const timing = paymentDocumentTimingLabel(payment);
					const notes = payment.notes
						? `<div class="row-note">${escapeHtml(payment.notes)}</div>`
						: '';
					return `<tr><td>${escapeHtml(methodLabel)}${notes}</td><td>${escapeHtml(statusLabel)}</td><td>${escapeHtml(timing)}</td><td class="amount-cell">${escapeHtml(money.format(payment.amount))}</td></tr>`;
				})
				.join('') ||
			`<tr><td colspan="4"><div class="empty-state">Nessun movimento registrato sulla vendita.</div></td></tr>`;

		const lineRows =
			sale.lines
				.map((line) => {
					const details = [
						line.servicePeriodStartUtc ? `Dal ${date.format(line.servicePeriodStartUtc)}` : '',
						line.servicePeriodEndUtc ? `Al ${date.format(line.servicePeriodEndUtc)}` : '',
						line.creditsGranted !== null ? `Crediti ${line.creditsGranted}` : '',
						line.notes ?? ''
					]
						.filter(Boolean)
						.join(' | ');

					return `<tr><td>${escapeHtml(line.name)}${details ? `<div class="row-note">${escapeHtml(details)}</div>` : ''}</td><td class="center-cell">${line.quantity}</td><td class="amount-cell">${escapeHtml(money.format(line.unitPriceAmount))}</td><td class="amount-cell">${escapeHtml(money.format(line.lineTotalAmount))}</td></tr>`;
				})
				.join('') ||
			`<tr><td colspan="4"><div class="empty-state">Nessuna riga presente nella vendita.</div></td></tr>`;

		const body = `
			<header class="document-hero">
				<div class="hero-copy">
					<div class="eyebrow">Betterfit sales desk</div>
					<h1>Riepilogo vendita</h1>
					<p class="hero-subtitle">Documento operativo per desk, amministrazione e controllo incassi.</p>
				</div>
				<div class="hero-reference">
					Riferimento
					<strong>${escapeHtml(sale.referenceCode)}</strong>
				</div>
			</header>
			<div class="document-body">
				<div class="top-grid">
					<section class="surface">
						<p class="brand-name">${escapeHtml(gymName)}</p>
						<p class="brand-copy">Vendita registrata su ${escapeHtml(sale.locationName)} per ${escapeHtml(sale.memberName)}.</p>
						<div class="status-pills">
							<div class="status-pill"><strong>Documento:</strong> ${escapeHtml(displayStatusLabel)}</div>
							<div class="status-pill"><strong>Vendita:</strong> ${escapeHtml(saleStatusLabel)}</div>
							<div class="status-pill"><strong>Pagamenti:</strong> ${escapeHtml(paymentStatusLabel)}</div>
						</div>
					</section>
					<section class="surface tint">
						<h2 class="surface-title">Dati operativi</h2>
						<div class="meta-list">
							${renderPrintMetaRows([
								{ label: 'Cliente', value: `${sale.memberName} - ${sale.memberEmail}` },
								{ label: 'Sede', value: sale.locationName },
								{ label: 'Data vendita', value: dateTime.format(sale.soldAtUtc) },
								{ label: 'Stampato il', value: printIssuedAt }
							])}
						</div>
					</section>
				</div>

				<section class="stat-grid">
					${renderPrintStatCards([
						{ label: 'Totale', value: money.format(sale.totalAmount), emphasis: true },
						{ label: 'Subtotale', value: money.format(sale.subtotalAmount) },
						{
							label: 'Sconto',
							value: money.format(sale.discountAmount),
							note: sale.discountAmount > 0 ? 'Applicato sul documento' : 'Nessuno sconto'
						},
						{
							label: 'Incassato',
							value: money.format(sale.paidAmount),
							note: `${paidMovementsCount} movimenti pagati`
						},
						{
							label: 'Residuo',
							value: money.format(sale.remainingAmount),
							note: `${soldLinesCount} quantita vendute`
						}
					])}
				</section>

				<div class="document-grid">
					<section class="surface">
						<h2 class="surface-title">Righe vendita</h2>
						<p class="surface-subtitle">Dettaglio delle voci incluse nella vendita, con periodo di servizio, crediti e note operative.</p>
						<table>
							<thead><tr><th>Voce</th><th>Qt</th><th>Prezzo</th><th>Totale</th></tr></thead>
							<tbody>${lineRows}</tbody>
						</table>
					</section>
					<aside class="surface tint">
						<h2 class="surface-title">Snapshot documento</h2>
						<div class="meta-list">
							${renderPrintMetaRows([
								{ label: 'Linee documento', value: `${sale.lines.length}` },
								{ label: 'Movimenti', value: `${sale.payments.length}` },
								{ label: 'Incasso aperto', value: money.format(sale.remainingAmount) },
								{ label: 'Ultimo aggiornamento', value: dateTime.format(sale.updatedAtUtc) }
							])}
						</div>
						<div class="note-block">${escapeHtml(sale.notes || 'Nessuna nota operativa registrata sulla vendita.')}</div>
					</aside>
				</div>

				<section class="surface" style="margin-top: 18px;">
					<h2 class="surface-title">Pagamenti</h2>
					<p class="surface-subtitle">Movimenti collegati alla vendita, con stato, data documento e importi utili per desk e amministrazione.</p>
					<table>
						<thead><tr><th>Metodo</th><th>Stato</th><th>Data</th><th>Importo</th></tr></thead>
						<tbody>${paymentRows}</tbody>
					</table>
					<div class="totals-box">
						<div class="total-row"><span>Totale vendita</span><strong>${escapeHtml(money.format(sale.totalAmount))}</strong></div>
						<div class="total-row"><span>Totale incassato</span><strong>${escapeHtml(money.format(sale.paidAmount))}</strong></div>
						<div class="total-row"><span>Residuo aperto</span><strong>${escapeHtml(money.format(sale.remainingAmount))}</strong></div>
					</div>
				</section>

				<div class="footer-note">Documento generato da Betterfit. Usa questo riepilogo per controllo desk, ristampa e verifica amministrativa.</div>
			</div>
		`;

		return buildPrintDocument(`Riepilogo vendita ${sale.referenceCode}`, body);
	}

	function downloadDocument(filename: string, html: string) {
		if (typeof window === 'undefined') {
			return;
		}

		const blob = new Blob([html], { type: 'text/html;charset=utf-8' });
		const url = URL.createObjectURL(blob);
		const anchor = document.createElement('a');
		anchor.href = url;
		anchor.download = filename;
		document.body.append(anchor);
		anchor.click();
		anchor.remove();
		window.setTimeout(() => URL.revokeObjectURL(url), 1000);
	}

	function buildPaymentReceiptHtml(sale: GymSale, payment: GymSalePayment) {
		const gymName = center.selectedGym?.name ?? 'Betterfit';
		const receiptCode = paymentReceiptCode(sale, payment);
		const issuedAt = dateTime.format(paymentReceiptIssuedAt(payment));
		const collectedAt = payment.paidAtUtc
			? dateTime.format(payment.paidAtUtc)
			: 'Data non disponibile';
		const receiptLineRows = sale.lines
			.map(
				(line) =>
					`<tr><td>${escapeHtml(line.name)}</td><td class="center-cell">${line.quantity}</td><td class="amount-cell">${escapeHtml(money.format(line.lineTotalAmount))}</td></tr>`
			)
			.join('');
		const body = `
			<header class="document-hero">
				<div class="hero-copy">
					<div class="eyebrow">Betterfit receipt</div>
					<h1>Ricevuta incasso</h1>
					<p class="hero-subtitle">Conferma di pagamento registrata dal desk Betterfit per il movimento collegato alla vendita.</p>
				</div>
				<div class="hero-reference">
					Ricevuta
					<strong>${escapeHtml(receiptCode)}</strong>
				</div>
			</header>
			<div class="document-body">
				<div class="top-grid">
					<section class="surface">
						<p class="brand-name">${escapeHtml(gymName)}</p>
						<p class="brand-copy">Incasso registrato su ${escapeHtml(sale.locationName)} per ${escapeHtml(sale.memberName)}.</p>
						<div class="status-pills">
							<div class="status-pill"><strong>Stato:</strong> ${escapeHtml(statusMeta(payment.status).label)}</div>
							<div class="status-pill"><strong>Metodo:</strong> ${escapeHtml(paymentMethodLabel(payment.method))}</div>
							<div class="status-pill"><strong>Vendita:</strong> ${escapeHtml(sale.referenceCode)}</div>
						</div>
					</section>
					<section class="surface tint">
						<h2 class="surface-title">Dati ricevuta</h2>
						<div class="meta-list">
							${renderPrintMetaRows([
								{ label: 'Cliente', value: `${sale.memberName} - ${sale.memberEmail}` },
								{ label: 'Sede', value: sale.locationName },
								{ label: 'Emessa il', value: issuedAt },
								{ label: 'Data incasso', value: collectedAt }
							])}
						</div>
					</section>
				</div>

				<section class="stat-grid" style="grid-template-columns: repeat(4, minmax(0, 1fr));">
					${renderPrintStatCards([
						{ label: 'Importo incassato', value: money.format(payment.amount), emphasis: true },
						{ label: 'Totale vendita', value: money.format(sale.totalAmount) },
						{ label: 'Incassato totale', value: money.format(sale.paidAmount) },
						{ label: 'Residuo aperto', value: money.format(sale.remainingAmount) }
					])}
				</section>

				<div class="document-grid" style="grid-template-columns: minmax(0, 1fr) minmax(280px, 0.9fr);">
					<section class="surface">
						<h2 class="surface-title">Voci collegate</h2>
						<p class="surface-subtitle">Riepilogo sintetico delle righe vendita coperte dal documento di incasso.</p>
						<table>
							<thead><tr><th>Voce</th><th>Qt</th><th>Totale</th></tr></thead>
							<tbody>${receiptLineRows || `<tr><td colspan="3"><div class="empty-state">Nessuna riga collegata alla vendita.</div></td></tr>`}</tbody>
						</table>
					</section>
					<aside class="surface tint">
						<h2 class="surface-title">Dettagli operazione</h2>
						<div class="meta-list">
							${renderPrintMetaRows([
								{ label: 'Riferimento', value: receiptCode },
								{ label: 'Pagamento', value: paymentMethodLabel(payment.method) },
								{ label: 'Movimento creato', value: dateTime.format(payment.createdAtUtc) },
								{ label: 'Stato documento', value: statusMeta(payment.status).label }
							])}
						</div>
						<div class="note-block">${escapeHtml(payment.notes || sale.notes || 'Nessuna nota operativa registrata.')}</div>
					</aside>
				</div>

				<div class="footer-note">Ricevuta generata da Betterfit per ristampa desk e riscontro amministrativo del pagamento.</div>
			</div>
		`;

		return buildPrintDocument(`Ricevuta incasso ${receiptCode}`, body);
	}

	function printSelectedSale() {
		if (!selectedSale || typeof window === 'undefined') {
			return;
		}

		downloadDocument(
			`${normalizeDownloadFileName(`vendita-${selectedSale.referenceCode}`)}.html`,
			buildSalePrintHtml(selectedSale)
		);
		paymentSuccess = `Riepilogo ${selectedSale.referenceCode} scaricato in locale.`;
	}

	function printPaymentReceipt(paymentId: string) {
		if (!selectedSale) {
			return;
		}

		const payment = selectedSale.payments.find((item) => item.id === paymentId);
		if (!payment || payment.status !== 'Paid' || !payment.paidAtUtc) {
			paymentError = 'La ricevuta e disponibile solo per movimenti gia incassati.';
			return;
		}

		downloadDocument(
			`${normalizeDownloadFileName(`ricevuta-${paymentReceiptCode(selectedSale, payment)}`)}.html`,
			buildPaymentReceiptHtml(selectedSale, payment)
		);
		paymentSuccess = `Ricevuta ${paymentReceiptCode(selectedSale, payment)} scaricata in locale.`;
	}

	function printPaymentReceiptByIds(saleId: string, paymentId: string) {
		const sale = findSaleById(saleId);
		const payment = findPaymentByIds(saleId, paymentId);
		if (!sale || !payment || payment.status !== 'Paid' || !payment.paidAtUtc) {
			paymentError = 'La ricevuta e disponibile solo per movimenti gia incassati.';
			return;
		}

		selectedSaleId = sale.saleId;
		selectedPaymentId = payment.id;
		downloadDocument(
			`${normalizeDownloadFileName(`ricevuta-${paymentReceiptCode(sale, payment)}`)}.html`,
			buildPaymentReceiptHtml(sale, payment)
		);
		paymentSuccess = `Ricevuta ${paymentReceiptCode(sale, payment)} scaricata in locale.`;
	}

	function printPaymentReceiptFromMovement(item: PaymentMovementItem) {
		printPaymentReceiptByIds(item.saleId, item.paymentId);
	}

	function printLatestReceipt() {
		if (!selectedSale) {
			return;
		}

		const latestPaidPayment = [...selectedSale.payments]
			.filter((payment) => payment.status === 'Paid' && payment.paidAtUtc)
			.sort((left, right) => {
				const leftTime = left.paidAtUtc ? left.paidAtUtc.getTime() : 0;
				const rightTime = right.paidAtUtc ? right.paidAtUtc.getTime() : 0;
				return rightTime - leftTime;
			})[0];

		if (!latestPaidPayment) {
			paymentError = 'Non ci sono ancora incassi stampabili per questa vendita.';
			return;
		}

		printPaymentReceipt(latestPaidPayment.id);
	}

	function legacyPrintBlock() {
		const paymentRows = selectedSale.payments
			.map((payment) => {
				const methodLabel =
					paymentMethods.find((option) => option.value === payment.method)?.label ?? payment.method;
				const statusLabel = statusMeta(payment.status).label;
				const timing =
					payment.status === 'Refunded'
						? `Rimborsato ${dateTime.format(payment.createdAtUtc)}`
						: payment.paidAtUtc
							? `Incassato ${dateTime.format(payment.paidAtUtc)}`
							: payment.dueAtUtc
								? `Scadenza ${dateTime.format(payment.dueAtUtc)}`
								: 'Nessuna data';
				return `<tr><td>${methodLabel}</td><td>${statusLabel}</td><td>${timing}</td><td style="text-align:right">${money.format(payment.amount)}</td></tr>`;
			})
			.join('');

		const lineRows = selectedSale.lines
			.map((line) => {
				const details = [
					line.servicePeriodStartUtc ? `Dal ${date.format(line.servicePeriodStartUtc)}` : '',
					line.servicePeriodEndUtc ? `Al ${date.format(line.servicePeriodEndUtc)}` : '',
					line.creditsGranted !== null ? `Crediti ${line.creditsGranted}` : '',
					line.notes ?? ''
				]
					.filter(Boolean)
					.join(' • ');

				return `<tr><td>${line.name}${details ? `<div style="margin-top:4px;font-size:12px;color:#475569">${details}</div>` : ''}</td><td style="text-align:center">${line.quantity}</td><td style="text-align:right">${money.format(line.unitPriceAmount)}</td><td style="text-align:right">${money.format(line.lineTotalAmount)}</td></tr>`;
			})
			.join('');

		const printWindow = window.open('', '_blank', 'width=900,height=700');
		if (!printWindow) {
			paymentError = "Popup bloccato: abilita l'apertura della finestra per stampare il riepilogo.";
			return;
		}

		printWindow.document.write(`<!DOCTYPE html>
<html lang="it">
<head>
	<meta charset="utf-8" />
	<title>Riepilogo vendita ${selectedSale.referenceCode}</title>
	<style>
		body { font-family: Arial, sans-serif; margin: 32px; color: #0f172a; }
		h1, h2 { margin: 0 0 12px; }
		section { margin-top: 24px; }
		table { width: 100%; border-collapse: collapse; margin-top: 12px; }
		th, td { border-bottom: 1px solid #cbd5e1; padding: 10px 8px; font-size: 14px; vertical-align: top; }
		th { text-align: left; color: #475569; }
		.summary { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }
		.card { border: 1px solid #cbd5e1; border-radius: 12px; padding: 14px; }
		.label { font-size: 12px; text-transform: uppercase; letter-spacing: 0.08em; color: #64748b; }
		.value { margin-top: 8px; font-size: 20px; font-weight: 700; }
		.note { font-size: 14px; white-space: pre-wrap; }
	</style>
</head>
<body>
	<h1>Riepilogo vendita ${selectedSale.referenceCode}</h1>
	<p>${selectedSale.memberName} - ${selectedSale.memberEmail}</p>
	<p>${selectedSale.locationName} - ${dateTime.format(selectedSale.soldAtUtc)}</p>

	<section class="summary">
		<div class="card"><div class="label">Totale</div><div class="value">${money.format(selectedSale.totalAmount)}</div></div>
		<div class="card"><div class="label">Incassato</div><div class="value">${money.format(selectedSale.paidAmount)}</div></div>
		<div class="card"><div class="label">Residuo</div><div class="value">${money.format(selectedSale.remainingAmount)}</div></div>
	</section>

	<section>
		<h2>Righe vendita</h2>
		<table>
			<thead><tr><th>Voce</th><th>Qt</th><th>Prezzo</th><th>Totale</th></tr></thead>
			<tbody>${lineRows}</tbody>
		</table>
	</section>

	<section>
		<h2>Pagamenti</h2>
		<table>
			<thead><tr><th>Metodo</th><th>Stato</th><th>Data</th><th>Importo</th></tr></thead>
			<tbody>${paymentRows}</tbody>
		</table>
	</section>

	<section>
		<h2>Note</h2>
		<p class="note">${selectedSale.notes || 'Nessuna nota operativa.'}</p>
	</section>
</body>
</html>`);
		printWindow.document.close();
		printWindow.focus();
		printWindow.print();
	}
	async function refreshAll() {
		if (!center.selectedGymId) {
			return;
		}

		await Promise.all([
			membershipsQuery.refetch(),
			overviewQuery.refetch(),
			catalogQuery.refetch(),
			salesQuery.refetch()
		]);
	}
</script>

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	<section class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
		<div class="space-y-2">
			<div class="flex flex-wrap items-center gap-2">
				<Badge variant="secondary" class="rounded-full px-3 py-1">Vendite e incassi</Badge>
				{#if center.selectedGym}<Badge variant="outline" class="rounded-full px-3 py-1"
						>{center.selectedGym.name}</Badge
					>{/if}
			</div>
			<div>
				<h2 class="text-2xl font-semibold tracking-tight">{pageTitle}</h2>
				<p class="text-sm text-muted-foreground">
					Gestione vendite reale: overview, incassi, residui e creazione vendita tenant-scoped.
				</p>
			</div>
		</div>
		<div class="flex flex-wrap items-center gap-2">
			<Button
				variant="outline"
				size="sm"
				onclick={refreshAll}
				disabled={!hasSelectedGym || workspaceLoading}
				><RefreshCwIcon class="size-4" />Aggiorna</Button
			>
			{#if mode === 'catalog'}
				<Button
					variant="outline"
					size="sm"
					onclick={openCreateCatalogItem}
					disabled={!hasSelectedGym || workspaceLoading}
					><PlusCircleIcon class="size-4" />Nuovo elemento</Button
				>
			{/if}
			<Button
				size="sm"
				onclick={() => (createOpen = true)}
				disabled={!hasSelectedGym || workspaceLoading}
				><PlusCircleIcon class="size-4" />Nuova vendita</Button
			>
		</div>
	</section>

	{#if createSuccess}<section
			class="rounded-[20px] border border-[#bbf7d0] bg-[#f0fdf4] px-4 py-3 text-sm text-[#166534]"
		>
			{createSuccess}
		</section>{/if}
	{#if paymentSuccess}<section
			class="rounded-[20px] border border-[#bfdbfe] bg-[#eff6ff] px-4 py-3 text-sm text-[#1d4ed8]"
		>
			{paymentSuccess}
		</section>{/if}
	{#if catalogSuccess}<section
			class="rounded-[20px] border border-[#dcfce7] bg-[#f0fdf4] px-4 py-3 text-sm text-[#166534]"
		>
			{catalogSuccess}
		</section>{/if}
	{#if paymentError && !paymentOpen}<section
			class="rounded-[20px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]"
		>
			{paymentError}
		</section>{/if}
	{#if catalogError && !catalogOpen}<section
			class="rounded-[20px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]"
		>
			{catalogError}
		</section>{/if}

	{#if workspaceLoading}
		<Card class="border-dashed border-border/70 bg-muted/25">
			<CardHeader>
				<CardTitle>Caricamento area vendite</CardTitle>
				<CardDescription>
					Sto recuperando membership, overview, listino e registro vendite prima di mostrarti i dati
					reali del tenant.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-dashed border-[#fecaca] bg-[#fff7f7]">
			<CardHeader>
				<CardTitle>Impossibile caricare l area vendite</CardTitle>
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
					Scegli prima la palestra dal selettore in alto a sinistra per vedere listino, vendite,
					incassi e rinnovi collegati al tenant corretto.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
		{#if overviewQuery.data}
			<section class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
				<Card
					><CardHeader class="pb-2"
						><CardDescription>Vendite oggi</CardDescription><CardTitle class="text-3xl"
							>{overviewQuery.data.salesTodayCount}</CardTitle
						></CardHeader
					><CardContent class="text-sm text-muted-foreground"
						>Operazioni registrate oggi</CardContent
					></Card
				>
				<Card
					><CardHeader class="pb-2"
						><CardDescription>Incassato oggi</CardDescription><CardTitle class="text-3xl"
							>{money.format(overviewQuery.data.revenueTodayAmount)}</CardTitle
						></CardHeader
					><CardContent class="text-sm text-muted-foreground">Pagamenti incassati oggi</CardContent
					></Card
				>
				<Card
					><CardHeader class="pb-2"
						><CardDescription>Residuo aperto</CardDescription><CardTitle class="text-3xl"
							>{money.format(overviewQuery.data.pendingCollectionAmount)}</CardTitle
						></CardHeader
					><CardContent class="text-sm text-muted-foreground"
						>Da recuperare sulle vendite</CardContent
					></Card
				>
				<Card
					><CardHeader class="pb-2"
						><CardDescription>Rinnovi caldi</CardDescription><CardTitle class="text-3xl"
							>{overviewQuery.data.renewalCandidatesCount}</CardTitle
						></CardHeader
					><CardContent class="text-sm text-muted-foreground">Membership in scadenza</CardContent
					></Card
				>
			</section>
		{/if}

		{#if mode === 'renewals'}
			<section>
				<Card>
					<CardHeader class="gap-4">
						<div class="flex items-start justify-between gap-3">
							<div>
								<CardTitle>Desk rinnovi</CardTitle>
								<CardDescription>
									Clienti attivi in scadenza nei prossimi 30 giorni o appena scaduti, con avvio
									rapido della vendita.
								</CardDescription>
							</div>
							<Badge variant="outline">{renewalCandidates.length} candidati</Badge>
						</div>
					</CardHeader>
					<CardContent>
						{#if membershipsQuery.isPending}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Carico i rinnovi da lavorare...</p>
							</div>
						{:else if renewalCandidates.length === 0}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Nessuna scadenza calda</p>
								<p class="mt-2 text-sm text-muted-foreground">
									Al momento non ci sono membership attive da rinnovare entro 30 giorni.
								</p>
							</div>
						{:else}
							<div class="grid gap-3 xl:grid-cols-2">
								{#each renewalCandidates as candidate (candidate.id)}
									<div class="rounded-[18px] border border-border/70 p-4">
										<div class="flex items-start justify-between gap-3">
											<div>
												<div class="flex flex-wrap items-center gap-2">
													<p class="font-semibold">{candidate.name}</p>
													<Badge variant={renewalTimingMeta(candidate.daysToExpiry).variant}>
														{renewalTimingMeta(candidate.daysToExpiry).label}
													</Badge>
												</div>
												<p class="mt-1 text-sm text-muted-foreground">{candidate.email}</p>
											</div>
											<Button size="sm" onclick={() => openRenewalCreate(candidate.id)}>
												<PlusCircleIcon class="size-4" />
												Avvia rinnovo
											</Button>
										</div>
										<div class="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-3">
											<p>Scadenza {date.format(candidate.endedAtUtc)}</p>
											<p>{candidate.locationLabel}</p>
											<p>
												{candidate.daysToExpiry < 0
													? 'Priorita alta'
													: candidate.daysToExpiry <= 3
														? 'Contattare subito'
														: 'Pianificabile'}
											</p>
										</div>
									</div>
								{/each}
							</div>
						{/if}
					</CardContent>
				</Card>
			</section>
		{/if}

		{#if mode === 'payments'}
			<section class="grid gap-4 xl:grid-cols-[1.5fr_1fr]">
				<Card>
					<CardHeader class="gap-4">
						<div class="flex items-start justify-between gap-3">
							<div>
								<CardTitle>Desk incassi</CardTitle>
								<CardDescription>
									Movimenti da lavorare per priorita, con focus su scaduti e pagamenti falliti.
								</CardDescription>
							</div>
							<Badge variant="outline">{collectionDeskItems.length} movimenti aperti</Badge>
						</div>
					</CardHeader>
					<CardContent class="space-y-4">
						<div class="grid gap-3 md:grid-cols-3">
							<div class="rounded-[16px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
									Da incassare
								</p>
								<p class="mt-2 text-lg font-semibold">
									{collectionDeskSummary.pendingCount} movimenti
								</p>
								<p class="mt-1 text-sm text-muted-foreground">
									{money.format(collectionDeskSummary.pendingAmount)}
								</p>
							</div>
							<div class="rounded-[16px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Scaduti</p>
								<p class="mt-2 text-lg font-semibold">
									{collectionDeskSummary.overdueCount} movimenti
								</p>
								<p class="mt-1 text-sm text-muted-foreground">
									{money.format(collectionDeskSummary.overdueAmount)}
								</p>
							</div>
							<div class="rounded-[16px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Falliti</p>
								<p class="mt-2 text-lg font-semibold">
									{collectionDeskSummary.failedCount} movimenti
								</p>
								<p class="mt-1 text-sm text-muted-foreground">
									Da verificare con il cliente o con il metodo di pagamento.
								</p>
							</div>
						</div>

						{#if collectionDeskItems.length === 0}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Nessun movimento da lavorare</p>
								<p class="mt-2 text-sm text-muted-foreground">
									Al momento non ci sono rate pendenti o tentativi falliti da gestire.
								</p>
							</div>
						{:else}
							<div class="space-y-3">
								{#each collectionDeskItems.slice(0, 8) as item (item.paymentId)}
									<div
										class="flex flex-col gap-3 rounded-[18px] border border-border/70 p-4 lg:flex-row lg:items-start lg:justify-between"
									>
										<div>
											<div class="flex flex-wrap items-center gap-2">
												<p class="font-semibold">{item.referenceCode}</p>
												<Badge variant={item.badgeVariant}>{item.timingLabel}</Badge>
											</div>
											<p class="mt-1 text-sm text-muted-foreground">
												{item.memberName} - {item.memberEmail}
											</p>
											<p class="mt-1 text-sm text-muted-foreground">
												{item.locationName} - {money.format(item.amount)}
											</p>
											<p class="mt-1 text-xs text-muted-foreground">
												Metodo {paymentMethods.find((option) => option.value === item.method)
													?.label ?? item.method}
												{#if item.dueAtUtc}
													- scadenza {dateTime.format(item.dueAtUtc)}
												{/if}
											</p>
										</div>
										<div class="flex shrink-0 flex-wrap gap-2">
											<Button
												variant="outline"
												size="sm"
												onclick={() => openSaleWorkspace(item.saleId)}
											>
												Apri vendita
											</Button>
											<Button
												size="sm"
												onclick={() => openCollectPaymentShortcut(item.saleId, item.paymentId)}
											>
												<CreditCardIcon class="size-4" />
												Incassa ora
											</Button>
										</div>
									</div>
								{/each}
							</div>
						{/if}
					</CardContent>
				</Card>

				<Card>
					<CardHeader class="gap-4">
						<div class="flex items-start justify-between gap-3">
							<div>
								<CardTitle>Ultimi incassi</CardTitle>
								<CardDescription>
									Movimenti gia saldati con ricevuta pronta da scaricare al desk.
								</CardDescription>
							</div>
							<Badge variant="outline">{recentReceiptItems.length} ricevute recenti</Badge>
						</div>
					</CardHeader>
					<CardContent>
						{#if recentReceiptItems.length === 0}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Nessun incasso recente</p>
								<p class="mt-2 text-sm text-muted-foreground">
									Appena registri nuovi pagamenti saldati li vedrai qui con la ricevuta pronta.
								</p>
							</div>
						{:else}
							<div class="space-y-3">
								{#each recentReceiptItems as item (item.paymentId)}
									<div class="rounded-[18px] border border-border/70 p-4">
										<div class="flex items-start justify-between gap-3">
											<div>
												<div class="flex flex-wrap items-center gap-2">
													<p class="font-semibold">{item.referenceCode}</p>
													<Badge variant="secondary">{item.receiptCode}</Badge>
												</div>
												<p class="mt-1 text-sm text-muted-foreground">
													{item.memberName} - {item.locationName}
												</p>
												<p class="mt-1 text-sm text-muted-foreground">
													{money.format(item.amount)} - {paymentMethodLabel(item.method)}
												</p>
												<p class="mt-1 text-xs text-muted-foreground">
													Incassato {dateTime.format(item.paidAtUtc)}
												</p>
											</div>
											<div class="flex shrink-0 flex-col gap-2">
												<Button
													variant="outline"
													size="sm"
													onclick={() => openSaleWorkspace(item.saleId)}
												>
													Apri vendita
												</Button>
												<Button
													variant="outline"
													size="sm"
													onclick={() => printPaymentReceiptByIds(item.saleId, item.paymentId)}
												>
													<DownloadIcon class="size-4" />
													Scarica
												</Button>
											</div>
										</div>
									</div>
								{/each}
							</div>
						{/if}
					</CardContent>
				</Card>
			</section>
		{/if}

		{#if mode === 'catalog'}
			<section>
				<Card>
					<CardHeader class="gap-4">
						<div class="flex items-start justify-between gap-3">
							<div>
								<CardTitle>Listino vendite</CardTitle>
								<CardDescription>
									Elementi riusabili per velocizzare le vendite al desk, distinti per sede.
								</CardDescription>
							</div>
							<Badge variant="outline">{filteredCatalogItems.length} elementi visibili</Badge>
						</div>
						<div class="grid gap-3 md:grid-cols-3">
							<div class="rounded-[16px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Totale</p>
								<p class="mt-2 text-lg font-semibold">{catalogSummary.total}</p>
							</div>
							<div class="rounded-[16px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Attivi</p>
								<p class="mt-2 text-lg font-semibold">{catalogSummary.active}</p>
							</div>
							<div class="rounded-[16px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Disattivi</p>
								<p class="mt-2 text-lg font-semibold">{catalogSummary.inactive}</p>
							</div>
						</div>
						<div class="grid gap-3 md:grid-cols-[1fr_220px]">
							<div class="relative">
								<SearchIcon
									class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
								/>
								<Input
									class="pl-9"
									placeholder="Cerca per nome, sede o tipo"
									bind:value={catalogSearchTerm}
								/>
							</div>
							<Select.Root type="single" bind:value={catalogFilter}>
								<Select.Trigger class="w-full">
									<span data-slot="select-value">
										{catalogFilter === 'active'
											? 'Solo attivi'
											: catalogFilter === 'inactive'
												? 'Solo disattivi'
												: 'Tutti'}
									</span>
								</Select.Trigger>
								<Select.Content>
									<Select.Item value="active" label="Solo attivi">Solo attivi</Select.Item>
									<Select.Item value="inactive" label="Solo disattivi">Solo disattivi</Select.Item>
									<Select.Item value="all" label="Tutti">Tutti</Select.Item>
								</Select.Content>
							</Select.Root>
						</div>
					</CardHeader>
					<CardContent>
						{#if catalogQuery.isPending}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Carico il listino...</p>
							</div>
						{:else if filteredCatalogItems.length === 0}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Nessun elemento di listino</p>
								<p class="mt-2 text-sm text-muted-foreground">
									Crea il primo elemento per precompilare velocemente le righe vendita.
								</p>
							</div>
						{:else}
							<div class="grid gap-3 xl:grid-cols-2">
								{#each filteredCatalogItems as item (item.catalogItemId)}
									<div class="rounded-[18px] border border-border/70 p-4">
										<div class="flex items-start justify-between gap-3">
											<div>
												<div class="flex flex-wrap items-center gap-2">
													<p class="font-semibold">{item.name}</p>
													<Badge variant={item.isActive ? 'success' : 'outline'}>
														{item.isActive ? 'Attivo' : 'Disattivo'}
													</Badge>
												</div>
												<p class="mt-1 text-sm text-muted-foreground">
													{item.locationName} - {money.format(item.unitPriceAmount)}
												</p>
											</div>
											<div class="flex flex-wrap justify-end gap-2">
												<Button
													variant="outline"
													size="sm"
													onclick={() => openEditCatalogItem(item)}
												>
													<PencilIcon class="size-4" />
													Modifica
												</Button>
												<Button
													variant="outline"
													size="sm"
													onclick={() => handleToggleCatalogItemActivation(item)}
												>
													{item.isActive ? 'Disattiva' : 'Riattiva'}
												</Button>
											</div>
										</div>
										<div class="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
											<p>
												Tipo {itemTypes.find((option) => option.value === item.itemType)?.label ??
													item.itemType}
											</p>
											<p>Quantita {item.defaultQuantity}</p>
											<p>Sconto {money.format(item.defaultDiscountAmount)}</p>
											<p>
												{item.defaultCreditsGranted !== null
													? `Crediti ${item.defaultCreditsGranted}`
													: 'Nessun credito'}
											</p>
											<p>
												{item.servicePeriodDays !== null
													? `Servizio ${item.servicePeriodDays} giorni`
													: 'Periodo libero'}
											</p>
											<p>Aggiornato {date.format(item.updatedAtUtc)}</p>
										</div>
										{#if item.notes}
											<p class="mt-3 text-sm text-muted-foreground">{item.notes}</p>
										{/if}
									</div>
								{/each}
							</div>
						{/if}
					</CardContent>
				</Card>
			</section>
		{/if}

		{#if mode === 'payments'}
			<section class="grid gap-4 xl:grid-cols-[1.35fr_1fr]">
				<Card>
					<CardHeader class="gap-4">
						<div class="flex items-center justify-between gap-3">
							<div>
								<CardTitle>Registro movimenti</CardTitle>
								<CardDescription>
									Tutti i pagamenti, le rate e le ricevute legate alle vendite del tenant.
								</CardDescription>
							</div>
							<Badge variant="outline">{paymentMovementItems.length} movimenti</Badge>
						</div>
						<div class="grid gap-3 md:grid-cols-3">
							<div class="relative">
								<SearchIcon
									class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
								/>
								<Input
									class="pl-9"
									placeholder="Cerca per cliente, riferimento o nota"
									bind:value={searchTerm}
								/>
							</div>
							<Select.Root type="single" bind:value={saleStatusFilter}>
								<Select.Trigger class="w-full">
									<span data-slot="select-value">
										{saleStatuses.find((option) => option.value === saleStatusFilter)?.label ||
											'Tutti gli stati vendita'}
									</span>
								</Select.Trigger>
								<Select.Content>
									{#each saleStatuses as option (option.value)}
										<Select.Item value={option.value} label={option.label}>
											{option.label}
										</Select.Item>
									{/each}
								</Select.Content>
							</Select.Root>
							<Select.Root type="single" bind:value={paymentStatusFilter}>
								<Select.Trigger class="w-full">
									<span data-slot="select-value">
										{paymentStatuses.find((option) => option.value === paymentStatusFilter)
											?.label || 'Tutti gli stati pagamento'}
									</span>
								</Select.Trigger>
								<Select.Content>
									{#each paymentStatuses as option (option.value)}
										<Select.Item value={option.value} label={option.label}>
											{option.label}
										</Select.Item>
									{/each}
								</Select.Content>
							</Select.Root>
						</div>
						<div class="grid gap-3 md:grid-cols-4">
							<div class="rounded-[16px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Incassati</p>
								<p class="mt-2 text-lg font-semibold">{paymentMovementSummary.paidCount}</p>
								<p class="mt-1 text-sm text-muted-foreground">
									{money.format(paymentMovementSummary.paidAmount)}
								</p>
							</div>
							<div class="rounded-[16px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
									Da incassare
								</p>
								<p class="mt-2 text-lg font-semibold">{paymentMovementSummary.pendingCount}</p>
								<p class="mt-1 text-sm text-muted-foreground">
									{money.format(paymentMovementSummary.pendingAmount)}
								</p>
							</div>
							<div class="rounded-[16px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Falliti</p>
								<p class="mt-2 text-lg font-semibold">{paymentMovementSummary.failedCount}</p>
								<p class="mt-1 text-sm text-muted-foreground">Richiedono ricontatto o verifica.</p>
							</div>
							<div class="rounded-[16px] border border-border/70 p-4">
								<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Rimborsati</p>
								<p class="mt-2 text-lg font-semibold">{paymentMovementSummary.refundedCount}</p>
								<p class="mt-1 text-sm text-muted-foreground">Movimenti chiusi lato cassa.</p>
							</div>
						</div>
					</CardHeader>
					<CardContent>
						{#if salesQuery.isPending}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Carico i movimenti di pagamento...</p>
							</div>
						{:else if paymentMovementItems.length === 0}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Nessun movimento trovato</p>
								<p class="mt-2 text-sm text-muted-foreground">
									Modifica i filtri o registra una nuova vendita con almeno un pagamento.
								</p>
							</div>
						{:else}
							<div class="overflow-hidden rounded-[20px] border border-border/70">
								<Table>
									<TableHeader>
										<TableRow class="bg-secondary/30 hover:bg-secondary/30">
											<TableHead>Movimento</TableHead>
											<TableHead>Cliente</TableHead>
											<TableHead class="hidden md:table-cell">Ricevuta</TableHead>
											<TableHead>Stato</TableHead>
										</TableRow>
									</TableHeader>
									<TableBody>
										{#each paymentMovementItems as item (item.paymentId)}
											<TableRow
												class={`cursor-pointer hover:bg-secondary/35 ${selectedPaymentMovement?.paymentId === item.paymentId ? 'bg-[#eef4ff]' : ''}`}
												onclick={() => selectPaymentMovement(item)}
											>
												<TableCell>
													<div>
														<p class="font-semibold">{item.referenceCode}</p>
														<p class="text-sm text-muted-foreground">
															{money.format(item.amount)} - {paymentMethodLabel(item.method)}
														</p>
														<p class="text-xs text-muted-foreground">
															{movementTimingLabel(item)}
														</p>
													</div>
												</TableCell>
												<TableCell>
													<div>
														<p class="font-medium">{item.memberName}</p>
														<p class="text-sm text-muted-foreground">{item.memberEmail}</p>
													</div>
												</TableCell>
												<TableCell class="hidden md:table-cell">
													<p class="font-medium">
														{item.status === 'Paid' ? movementReceiptCode(item) : '-'}
													</p>
													<p class="text-sm text-muted-foreground">{item.locationName}</p>
												</TableCell>
												<TableCell>
													<Badge variant={statusMeta(item.status).variant}>
														{statusMeta(item.status).label}
													</Badge>
												</TableCell>
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
						<div class="flex items-start justify-between gap-3">
							<div>
								<CardTitle>
									{selectedPaymentMovement
										? `Movimento ${selectedPaymentMovement.referenceCode}`
										: 'Dettaglio movimento'}
								</CardTitle>
								<CardDescription>
									{selectedPaymentMovement
										? `${selectedPaymentMovement.memberName} - ${selectedPaymentMovement.locationName}`
										: 'Seleziona un movimento dal registro.'}
								</CardDescription>
							</div>
							<div class="flex flex-wrap justify-end gap-2">
								{#if selectedPaymentMovement}
									<Button
										variant="outline"
										size="sm"
										onclick={() => openSaleWorkspace(selectedPaymentMovement.saleId)}
									>
										Apri vendita
									</Button>
									{#if movementCanCollect(selectedPaymentMovement)}
										<Button
											size="sm"
											onclick={() =>
												openCollectPaymentShortcut(
													selectedPaymentMovement.saleId,
													selectedPaymentMovement.paymentId
												)}
										>
											<CreditCardIcon class="size-4" />
											Incassa ora
										</Button>
									{/if}
									{#if selectedPaymentMovement.status === 'Paid'}
										<Button
											variant="outline"
											size="sm"
											onclick={() => printPaymentReceiptFromMovement(selectedPaymentMovement)}
										>
											<DownloadIcon class="size-4" />
											Scarica ricevuta
										</Button>
									{/if}
								{/if}
							</div>
						</div>
					</CardHeader>
					<CardContent>
						{#if selectedPaymentMovement && selectedPaymentSale}
							<div class="space-y-4">
								<div class="grid gap-3 sm:grid-cols-2">
									<div class="rounded-[18px] border border-border/70 p-4">
										<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Importo</p>
										<p class="mt-2 text-lg font-semibold">
											{money.format(selectedPaymentMovement.amount)}
										</p>
										<p class="mt-1 text-sm text-muted-foreground">
											Metodo {paymentMethodLabel(selectedPaymentMovement.method)}
										</p>
									</div>
									<div class="rounded-[18px] border border-border/70 p-4">
										<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Stato</p>
										<div class="mt-2">
											<Badge variant={statusMeta(selectedPaymentMovement.status).variant}>
												{statusMeta(selectedPaymentMovement.status).label}
											</Badge>
										</div>
										<p class="mt-2 text-sm text-muted-foreground">
											{movementTimingLabel(selectedPaymentMovement)}
										</p>
									</div>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Ricevuta</p>
									<p class="mt-2 text-sm font-medium">
										{selectedPaymentMovement.status === 'Paid'
											? movementReceiptCode(selectedPaymentMovement)
											: 'Disponibile solo dopo incasso'}
									</p>
									<p class="mt-1 text-sm text-muted-foreground">
										{selectedPaymentMovement.status === 'Paid'
											? `Emessa ${dateTime.format(selectedPaymentRecord ? paymentReceiptIssuedAt(selectedPaymentRecord) : movementReceiptIssuedAt(selectedPaymentMovement))}`
											: 'La ricevuta viene emessa quando il movimento viene segnato come pagato.'}
									</p>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
										Vendita collegata
									</p>
									<div class="mt-3 grid gap-2 text-sm sm:grid-cols-2">
										<p>
											<span class="font-medium">Riferimento:</span>
											{selectedPaymentSale.referenceCode}
										</p>
										<p>
											<span class="font-medium">Stato vendita:</span>
											{statusMeta(saleDisplayStatus(selectedPaymentSale)).label}
										</p>
										<p>
											<span class="font-medium">Totale:</span>
											{money.format(selectedPaymentSale.totalAmount)}
										</p>
										<p>
											<span class="font-medium">Residuo:</span>
											{money.format(selectedPaymentSale.remainingAmount)}
										</p>
									</div>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
										Note operative
									</p>
									<p class="mt-2 text-sm">
										{selectedPaymentMovement.notes ||
											selectedPaymentSale.notes ||
											'Nessuna nota operativa.'}
									</p>
								</div>
							</div>
						{:else}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Nessun movimento selezionato</p>
							</div>
						{/if}
					</CardContent>
				</Card>
			</section>
		{:else}
			<section class="grid gap-4 xl:grid-cols-[1.35fr_1fr]">
				<Card>
					<CardHeader class="gap-4">
						<div class="flex items-center justify-between gap-3">
							<div>
								<CardTitle>Registro vendite</CardTitle><CardDescription
									>Storico di vendite, rinnovi e pagamenti.</CardDescription
								>
							</div>
							<Badge variant="outline">{sales.length} elementi</Badge>
						</div>
						<div class="grid gap-3 md:grid-cols-3">
							<div class="relative">
								<SearchIcon
									class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
								/><Input
									class="pl-9"
									placeholder="Cerca per cliente, riferimento o nota"
									bind:value={searchTerm}
								/>
							</div>
							<Select.Root type="single" bind:value={saleStatusFilter}>
								<Select.Trigger class="w-full">
									<span data-slot="select-value">
										{saleStatuses.find((option) => option.value === saleStatusFilter)?.label ||
											'Tutti gli stati vendita'}
									</span>
								</Select.Trigger>
								<Select.Content>
									{#each saleStatuses as option (option.value)}
										<Select.Item value={option.value} label={option.label}>
											{option.label}
										</Select.Item>
									{/each}
								</Select.Content>
							</Select.Root>
							<Select.Root type="single" bind:value={paymentStatusFilter}>
								<Select.Trigger class="w-full">
									<span data-slot="select-value">
										{paymentStatuses.find((option) => option.value === paymentStatusFilter)
											?.label || 'Tutti gli stati pagamento'}
									</span>
								</Select.Trigger>
								<Select.Content>
									{#each paymentStatuses as option (option.value)}
										<Select.Item value={option.value} label={option.label}>
											{option.label}
										</Select.Item>
									{/each}
								</Select.Content>
							</Select.Root>
						</div>
					</CardHeader>
					<CardContent>
						{#if salesQuery.isPending}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Carico le vendite...</p>
							</div>
						{:else if sales.length === 0}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Nessuna vendita trovata</p>
								<p class="mt-2 text-sm text-muted-foreground">
									Registra la prima vendita dal desk.
								</p>
							</div>
						{:else}
							<div class="overflow-hidden rounded-[20px] border border-border/70">
								<Table>
									<TableHeader
										><TableRow class="bg-secondary/30 hover:bg-secondary/30"
											><TableHead>Vendita</TableHead><TableHead>Cliente</TableHead><TableHead
												class="hidden md:table-cell">Totale</TableHead
											><TableHead>Pagamento</TableHead></TableRow
										></TableHeader
									>
									<TableBody>
										{#each sales as sale (sale.saleId)}
											<TableRow
												class={`cursor-pointer hover:bg-secondary/35 ${selectedSale?.saleId === sale.saleId ? 'bg-[#eef4ff]' : ''}`}
												onclick={() => (selectedSaleId = sale.saleId)}
											>
												<TableCell
													><div>
														<p class="font-semibold">{sale.referenceCode}</p>
														<p class="text-sm text-muted-foreground">
															{dateTime.format(sale.soldAtUtc)} - {sale.locationName}
														</p>
													</div></TableCell
												>
												<TableCell
													><div>
														<p class="font-medium">{sale.memberName}</p>
														<p class="text-sm text-muted-foreground">{sale.memberEmail}</p>
													</div></TableCell
												>
												<TableCell class="hidden md:table-cell"
													><p class="font-medium">{money.format(sale.totalAmount)}</p>
													<p class="text-sm text-muted-foreground">
														Residuo {money.format(sale.remainingAmount)}
													</p></TableCell
												>
												<TableCell
													><Badge variant={statusMeta(saleDisplayStatus(sale)).variant}
														>{statusMeta(saleDisplayStatus(sale)).label}</Badge
													></TableCell
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
						<div class="flex items-start justify-between gap-3">
							<div>
								<CardTitle
									>{selectedSale ? selectedSale.referenceCode : 'Dettaglio vendita'}</CardTitle
								>
								<CardDescription
									>{selectedSale
										? `${selectedSale.memberName} - ${selectedSale.locationName}`
										: 'Seleziona una vendita dalla tabella.'}</CardDescription
								>
							</div>
							<div class="flex flex-wrap justify-end gap-2">
								{#if selectedSale}
									<Button variant="outline" size="sm" onclick={printSelectedSale}
										><DownloadIcon class="size-4" />Scarica riepilogo</Button
									>
								{/if}
								{#if selectedSale && selectedSale.payments.some((payment) => payment.status === 'Paid' && payment.paidAtUtc)}
									<Button variant="outline" size="sm" onclick={printLatestReceipt}
										><DownloadIcon class="size-4" />Scarica ultima ricevuta</Button
									>
								{/if}
								{#if selectedSale && selectedSale.remainingAmount > 0 && selectedSale.status !== 'Cancelled' && selectedSale.status !== 'Refunded'}
									<Button size="sm" onclick={openPaymentForm}
										><CreditCardIcon class="size-4" />Registra incasso</Button
									>
								{/if}
								{#if selectedSale && selectedSale.status === 'PendingPayment' && selectedSale.paidAmount === 0}
									<Button
										variant="outline"
										size="sm"
										disabled={saleActionSubmitting}
										onclick={() => openSaleConfirm('cancel')}>Annulla vendita</Button
									>
								{/if}
								{#if selectedSale && selectedSale.status === 'Paid' && selectedSale.paymentStatus === 'Paid'}
									<Button
										variant="outline"
										size="sm"
										disabled={saleActionSubmitting}
										onclick={() => openSaleConfirm('refund')}>Rimborsa vendita</Button
									>
								{/if}
							</div>
						</div>
					</CardHeader>
					<CardContent>
						{#if selectedSale}
							<div class="space-y-4">
								<div class="grid gap-3 sm:grid-cols-2">
									<div class="rounded-[18px] border border-border/70 p-4">
										<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Totale</p>
										<p class="mt-2 text-lg font-semibold">
											{money.format(selectedSale.totalAmount)}
										</p>
										<p class="mt-1 text-sm text-muted-foreground">
											Sconto {money.format(selectedSale.discountAmount)}
										</p>
									</div>
									<div class="rounded-[18px] border border-border/70 p-4">
										<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
											Incassato
										</p>
										<p class="mt-2 text-lg font-semibold">
											{money.format(selectedSale.paidAmount)}
										</p>
										<p class="mt-1 text-sm text-muted-foreground">
											Residuo {money.format(selectedSale.remainingAmount)}
										</p>
									</div>
								</div>
								<div class="grid gap-3 sm:grid-cols-2">
									<div class="rounded-[18px] border border-border/70 p-4">
										<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
											Stato vendita
										</p>
										<div class="mt-2">
											<Badge variant={statusMeta(saleDisplayStatus(selectedSale)).variant}
												>{statusMeta(saleDisplayStatus(selectedSale)).label}</Badge
											>
										</div>
										<p class="mt-2 text-sm text-muted-foreground">
											Movimenti registrati {selectedSale.payments.length}
										</p>
									</div>
									<div class="rounded-[18px] border border-border/70 p-4">
										<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
											Azione rapida
										</p>
										<p class="mt-2 text-sm text-muted-foreground">
											{selectedSale.status === 'Cancelled'
												? 'Vendita chiusa come annullata.'
												: selectedSale.status === 'Refunded'
													? 'Vendita chiusa come rimborsata.'
													: selectedSale.remainingAmount > 0
														? 'Usa il pulsante in alto per registrare il saldo o pianificare una rata futura.'
														: 'Vendita completamente saldata.'}
										</p>
									</div>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
										Righe vendita
									</p>
									<div class="mt-3 space-y-2">
										{#each selectedSale.lines as line (line.id)}<div
												class="flex items-start justify-between gap-3 rounded-[14px] border border-border/60 bg-secondary/15 p-3 text-sm"
											>
												<div>
													<p class="font-medium">{line.name}</p>
													<p class="text-muted-foreground">
														{line.quantity} x {money.format(line.unitPriceAmount)}
													</p>
													{#if line.servicePeriodStartUtc || line.servicePeriodEndUtc}
														<p class="text-xs text-muted-foreground">
															Servizio
															{line.servicePeriodStartUtc
																? ` dal ${date.format(line.servicePeriodStartUtc)}`
																: ''}
															{line.servicePeriodEndUtc
																? ` al ${date.format(line.servicePeriodEndUtc)}`
																: ''}
														</p>
													{/if}
													{#if line.creditsGranted !== null}
														<p class="text-xs text-muted-foreground">
															Crediti concessi: {line.creditsGranted}
														</p>
													{/if}
													{#if line.notes}
														<p class="text-xs text-muted-foreground">{line.notes}</p>
													{/if}
												</div>
												<p class="font-medium">{money.format(line.lineTotalAmount)}</p>
											</div>{/each}
									</div>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Pagamenti</p>
									<div class="mt-3 space-y-3">
										{#each selectedSale.payments as payment (payment.id)}<div
												class="flex items-start justify-between gap-3 text-sm"
											>
												<div>
													<p class="font-medium">
														{paymentMethods.find((option) => option.value === payment.method)
															?.label ?? payment.method}
													</p>
													<p class="text-muted-foreground">{money.format(payment.amount)}</p>
													{#if payment.status === 'Refunded'}<p
															class="text-xs text-muted-foreground"
														>
															Rimborsato {dateTime.format(payment.createdAtUtc)}
														</p>{:else if payment.paidAtUtc}<p
															class="text-xs text-muted-foreground"
														>
															Incassato {dateTime.format(payment.paidAtUtc)}
														</p>{:else if payment.dueAtUtc}<p class="text-xs text-muted-foreground">
															Scadenza {dateTime.format(payment.dueAtUtc)}
														</p>{/if}{#if payment.receiptCode}<p
															class="text-xs text-muted-foreground"
														>
															Ricevuta {payment.receiptCode}
														</p>{/if}{#if payment.notes}<p class="text-xs text-muted-foreground">
															{payment.notes}
														</p>{/if}
												</div>
												<div class="flex shrink-0 flex-col items-end gap-2">
													<Badge variant={statusMeta(payment.status).variant}
														>{statusMeta(payment.status).label}</Badge
													>
													{#if (payment.status === 'Pending' || payment.status === 'Failed') && selectedSale.status !== 'Cancelled' && selectedSale.status !== 'Refunded'}
														<Button
															variant="outline"
															size="sm"
															onclick={() => openCollectPaymentForm(payment.id)}
															><CreditCardIcon class="size-4" />Incassa ora</Button
														>
													{/if}
													{#if payment.status !== 'Refunded' && selectedSale.status !== 'Cancelled' && selectedSale.status !== 'Refunded'}
														<Button
															variant="outline"
															size="sm"
															onclick={() => openEditPaymentForm(payment.id)}
															><PencilIcon class="size-4" />Modifica</Button
														>
													{/if}
													{#if payment.status === 'Paid' && payment.paidAtUtc}
														<Button
															variant="outline"
															size="sm"
															onclick={() => printPaymentReceipt(payment.id)}
															><DownloadIcon class="size-4" />Scarica ricevuta</Button
														>
													{/if}
												</div>
											</div>{/each}
									</div>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
										Storico operativo
									</p>
									<div class="mt-3 space-y-3">
										{#each selectedSaleActivities as activity (activity.id)}<div
												class="flex items-start justify-between gap-3 rounded-[14px] border border-border/60 bg-secondary/15 p-3 text-sm"
											>
												<div>
													<p class="font-medium">{activity.title}</p>
													<p class="mt-1 text-muted-foreground">{activity.description}</p>
												</div>
												<div class="flex shrink-0 flex-col items-end gap-2">
													<Badge variant={activity.variant}>{activity.title}</Badge><span
														class="text-xs text-muted-foreground"
														>{dateTime.format(activity.happenedAt)}</span
													>
												</div>
											</div>{/each}
									</div>
								</div>
								<div class="rounded-[18px] border border-border/70 p-4">
									<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Note</p>
									<p class="mt-2 text-sm">{selectedSale.notes || 'Nessuna nota operativa.'}</p>
								</div>
							</div>
						{:else}
							<div
								class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
							>
								<p class="font-semibold">Nessuna vendita selezionata</p>
							</div>
						{/if}
					</CardContent>
				</Card>
			</section>
		{/if}
	{/if}
</main>

<AlertDialog.Root bind:open={saleConfirmOpen}>
	<AlertDialog.Content>
		<AlertDialog.Header>
			<AlertDialog.Title>{saleConfirmTitle(pendingSaleAction)}</AlertDialog.Title>
			<AlertDialog.Description>
				{saleConfirmDescription(pendingSaleAction, selectedSale)}
			</AlertDialog.Description>
		</AlertDialog.Header>

		{#if selectedSale}
			<div class="rounded-[18px] border border-border/70 bg-secondary/35 p-4">
				<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Vendita selezionata</p>
				<p class="mt-2 text-sm font-semibold">{selectedSale.referenceCode}</p>
				<p class="mt-1 text-sm text-muted-foreground">
					{selectedSale.memberName} - {selectedSale.locationName}
				</p>
				<p class="mt-2 text-sm text-foreground/80">
					Totale {money.format(selectedSale.totalAmount)} · Incassato {money.format(
						selectedSale.paidAmount
					)}
				</p>
			</div>
		{/if}

		<AlertDialog.Footer>
			<Button
				type="button"
				variant="outline"
				onclick={closeSaleConfirm}
				disabled={saleActionSubmitting}>Torna indietro</Button
			>
			<Button
				type="button"
				variant="destructive"
				onclick={handleConfirmSaleAction}
				disabled={saleActionSubmitting || !pendingSaleAction}
			>
				{#if saleActionSubmitting}
					<RefreshCwIcon class="size-4 animate-spin" />
					Salvataggio...
				{:else}
					{pendingSaleAction === 'cancel' ? 'Conferma annullo' : 'Conferma rimborso'}
				{/if}
			</Button>
		</AlertDialog.Footer>
	</AlertDialog.Content>
</AlertDialog.Root>

<Sheet.Root bind:open={catalogOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-2xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">
				{editingCatalogItemId ? 'Modifica elemento listino' : 'Nuovo elemento listino'}
			</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				Configura una riga predefinita riusabile nelle vendite del desk per la sede selezionata.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleSaveCatalogItem}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Sede</span>
						<Select.Root type="single" bind:value={catalogFormState.locationId}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{center.locations.find((location) => location.id === catalogFormState.locationId)
										?.name || 'Seleziona una sede'}
								</span>
							</Select.Trigger>
							<Select.Content>
								{#each center.locations as location (location.id)}
									<Select.Item value={location.id} label={location.name}>
										{location.name}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Tipo</span>
						<Select.Root type="single" bind:value={catalogFormState.itemType}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{saleItemTypeLabel(catalogFormState.itemType)}</span>
							</Select.Trigger>
							<Select.Content>
								{#each itemTypes as option (option.value)}
									<Select.Item value={option.value} label={option.label}>
										{option.label}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>
					<label class="space-y-2 md:col-span-2">
						<span class="text-sm font-medium">Nome elemento</span>
						<Input
							bind:value={catalogFormState.name}
							placeholder="Es. Mensile open gym, Pacchetto 5 PT, Lucchetto"
						/>
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Prezzo unitario</span>
						<Input bind:value={catalogFormState.unitPriceAmount} />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Quantita predefinita</span>
						<Input type="number" min="1" bind:value={catalogFormState.defaultQuantity} />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Sconto predefinito</span>
						<Input bind:value={catalogFormState.defaultDiscountAmount} />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Crediti predefiniti</span>
						<Input bind:value={catalogFormState.defaultCreditsGranted} placeholder="10" />
					</label>
					<label class="space-y-2">
						<span class="text-sm font-medium">Giorni servizio</span>
						<Input bind:value={catalogFormState.servicePeriodDays} placeholder="30" />
					</label>
					<label class="space-y-2 md:col-span-2">
						<span class="text-sm font-medium">Note predefinite</span>
						<Textarea
							bind:value={catalogFormState.notes}
							rows={4}
							placeholder="Dettagli che vuoi riportare automaticamente nella riga vendita..."
						/>
					</label>
				</div>

				<label class="flex items-center gap-3 rounded-[16px] border border-border/70 p-4">
					<Checkbox bind:checked={catalogFormState.isActive} />
					<div>
						<p class="font-medium">Elemento attivo</p>
						<p class="text-sm text-muted-foreground">
							Se disattivato, resta storico nel listino ma non compare nell aggiunta rapida.
						</p>
					</div>
				</label>

				{#if catalogError}
					<div
						class="rounded-[18px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]"
					>
						{catalogError}
					</div>
				{/if}
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button
					type="button"
					variant="outline"
					onclick={() => {
						catalogOpen = false;
						catalogError = '';
					}}
				>
					Annulla
				</Button>
				<Button type="submit" disabled={catalogSubmitting}>
					{#if catalogSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<PlusCircleIcon class="size-4" />
						Salva elemento
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={createOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-2xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl">Nuova vendita</Sheet.Title>
			<Sheet.Description class="text-sm text-muted-foreground">
				Registra una vendita tenant-scoped con righe e pagamenti reali.
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleCreateSale}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Cliente</span>
						<Select.Root type="single" bind:value={formState.membershipId}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{membershipOptions.find((membership) => membership.id === formState.membershipId)
										?.label || 'Seleziona un cliente'}
								</span>
							</Select.Trigger>
							<Select.Content>
								{#each membershipOptions as membership (membership.id)}
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
						<Select.Root type="single" bind:value={formState.locationId}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{filteredLocations.find((location) => location.id === formState.locationId)
										?.name || 'Seleziona una sede'}
								</span>
							</Select.Trigger>
							<Select.Content>
								{#each filteredLocations as location (location.id)}
									<Select.Item value={location.id} label={location.name}>
										{location.name}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Data vendita</span>
						<input
							class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
							type="datetime-local"
							bind:value={formState.soldAtLocal}
						/>
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Note</span>
						<Input
							bind:value={formState.notes}
							placeholder="Es. rinnovo desk, promo attiva, follow-up amministrativo"
						/>
					</label>
				</div>

				<div class="space-y-3 rounded-[18px] border border-border/70 p-4">
					<div class="flex items-center justify-between gap-3">
						<div>
							<p class="font-semibold">Aggiunta rapida da listino</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Usa elementi preconfigurati della sede selezionata per compilare le righe piu in
								fretta.
							</p>
						</div>
						<Button type="button" variant="outline" size="sm" href="/sales/catalog">
							Gestisci listino
						</Button>
					</div>

					{#if catalogItemsForCurrentSaleLocation.length === 0}
						<div
							class="rounded-[16px] border border-dashed border-border bg-secondary/20 px-4 py-5"
						>
							<p class="text-sm font-medium">Nessun elemento attivo per questa sede</p>
							<p class="mt-1 text-sm text-muted-foreground">
								Crea o riattiva elementi nel listino per velocizzare le vendite del desk.
							</p>
						</div>
					{:else}
						<div class="grid gap-3 md:grid-cols-2">
							{#each catalogItemsForCurrentSaleLocation.slice(0, 6) as item (item.catalogItemId)}
								<button
									type="button"
									class="rounded-[16px] border border-border/70 bg-secondary/15 p-4 text-left transition-colors hover:bg-secondary/30"
									onclick={() => addCatalogItemToSale(item)}
								>
									<div class="flex items-start justify-between gap-3">
										<div>
											<p class="font-medium">{item.name}</p>
											<p class="mt-1 text-sm text-muted-foreground">
												{itemTypes.find((option) => option.value === item.itemType)?.label ??
													item.itemType}
											</p>
										</div>
										<p class="font-medium">{money.format(item.unitPriceAmount)}</p>
									</div>
									<div class="mt-3 flex flex-wrap gap-2 text-xs text-muted-foreground">
										<span>Qta {item.defaultQuantity}</span>
										{#if item.defaultDiscountAmount > 0}
											<span>Sconto {money.format(item.defaultDiscountAmount)}</span>
										{/if}
										{#if item.servicePeriodDays !== null}
											<span>{item.servicePeriodDays} giorni</span>
										{/if}
										{#if item.defaultCreditsGranted !== null}
											<span>{item.defaultCreditsGranted} crediti</span>
										{/if}
									</div>
								</button>
							{/each}
						</div>
					{/if}
				</div>

				<div class="space-y-3 rounded-[18px] border border-border/70 p-4">
					<div class="flex items-center justify-between gap-3">
						<p class="font-semibold">Righe vendita</p>
						<Button
							type="button"
							variant="outline"
							size="sm"
							onclick={() => (formState = { ...formState, lines: [...formState.lines, newLine()] })}
							>Aggiungi riga</Button
						>
					</div>
					{#each formState.lines as line, index (index)}
						<div
							class="grid gap-3 rounded-[16px] border border-border/70 bg-secondary/20 p-4 md:grid-cols-2"
						>
							<label class="space-y-2">
								<span class="text-sm font-medium">Tipo</span>
								<Select.Root
									type="single"
									value={line.itemType}
									onValueChange={(value) => (line.itemType = value as SaleItemType)}
								>
									<Select.Trigger class="w-full">
										<span data-slot="select-value">{saleItemTypeLabel(line.itemType)}</span>
									</Select.Trigger>
									<Select.Content>
										{#each itemTypes as option (option.value)}
											<Select.Item value={option.value} label={option.label}>
												{option.label}
											</Select.Item>
										{/each}
									</Select.Content>
								</Select.Root>
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Nome riga</span>
								<Input bind:value={line.name} placeholder="Bimestrale, PT 10 sedute, prodotto..." />
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Quantita</span>
								<Input type="number" min="1" bind:value={line.quantity} />
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Prezzo unitario</span>
								<Input bind:value={line.unitPriceAmount} />
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Sconto riga</span>
								<Input bind:value={line.discountAmount} />
							</label>
							{#if lineNeedsServicePeriod(line.itemType)}
								<label class="space-y-2">
									<span class="text-sm font-medium">Inizio servizio</span>
									<input
										class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
										type="datetime-local"
										bind:value={line.servicePeriodStartLocal}
									/>
								</label>
								<label class="space-y-2">
									<span class="text-sm font-medium">Fine servizio</span>
									<input
										class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
										type="datetime-local"
										bind:value={line.servicePeriodEndLocal}
									/>
								</label>
							{/if}
							{#if lineSupportsCredits(line.itemType)}
								<label class="space-y-2">
									<span class="text-sm font-medium">Crediti concessi</span>
									<Input bind:value={line.creditsGranted} placeholder="10" />
								</label>
							{/if}
							<label
								class={`space-y-2 ${
									lineNeedsServicePeriod(line.itemType) || lineSupportsCredits(line.itemType)
										? 'md:col-span-2'
										: ''
								}`}
							>
								<span class="text-sm font-medium">Note riga</span>
								<Textarea
									bind:value={line.notes}
									rows={3}
									placeholder="Periodo coperto, dettagli pacchetto, note operative..."
								/>
							</label>
							<div class="flex items-end justify-end">
								<Button
									type="button"
									variant="outline"
									size="sm"
									disabled={formState.lines.length === 1}
									onclick={() =>
										(formState = {
											...formState,
											lines: formState.lines.filter((_, currentIndex) => currentIndex !== index)
										})}>Rimuovi</Button
								>
							</div>
						</div>
					{/each}
				</div>

				<div class="space-y-3 rounded-[18px] border border-border/70 p-4">
					<div class="flex items-center justify-between gap-3">
						<p class="font-semibold">Pagamenti</p>
						<Button
							type="button"
							variant="outline"
							size="sm"
							onclick={() =>
								(formState = { ...formState, payments: [...formState.payments, newPayment()] })}
							>Aggiungi pagamento</Button
						>
					</div>
					{#each formState.payments as payment, index (index)}
						<div
							class="grid gap-3 rounded-[16px] border border-border/70 bg-secondary/20 p-4 md:grid-cols-2"
						>
							<label class="space-y-2">
								<span class="text-sm font-medium">Importo</span>
								<Input bind:value={payment.amount} />
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Metodo</span>
								<Select.Root
									type="single"
									value={payment.method}
									onValueChange={(value) => (payment.method = value as SalePaymentMethod)}
								>
									<Select.Trigger class="w-full">
										<span data-slot="select-value">{paymentMethodLabel(payment.method)}</span>
									</Select.Trigger>
									<Select.Content>
										{#each paymentMethods as option (option.value)}
											<Select.Item value={option.value} label={option.label}>
												{option.label}
											</Select.Item>
										{/each}
									</Select.Content>
								</Select.Root>
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium">Stato</span>
								<Select.Root
									type="single"
									value={payment.status}
									onValueChange={(value) => (payment.status = value as PaymentEntryStatus)}
								>
									<Select.Trigger class="w-full">
										<span data-slot="select-value">
											{paymentEntryStatusLabel(payment.status)}
										</span>
									</Select.Trigger>
									<Select.Content>
										{#each paymentEntryStatuses as option (option.value)}
											<Select.Item value={option.value} label={option.label}>
												{option.label}
											</Select.Item>
										{/each}
									</Select.Content>
								</Select.Root>
							</label>
							<label class="space-y-2">
								<span class="text-sm font-medium"
									>{payment.status === 'Paid' ? 'Data incasso' : 'Scadenza'}</span
								>
								{#if payment.status === 'Paid'}
									<input
										class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
										type="datetime-local"
										bind:value={payment.paidAtLocal}
									/>
								{:else}
									<input
										class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
										type="datetime-local"
										bind:value={payment.dueAtLocal}
									/>
								{/if}
							</label>
							<label class="space-y-2 md:col-span-2">
								<span class="text-sm font-medium">Note pagamento</span>
								<Textarea
									bind:value={payment.notes}
									rows={3}
									placeholder="Es. rata 1/3, bonifico in verifica, scadenza concordata..."
								/>
							</label>
							<div class="flex items-end justify-end">
								<Button
									type="button"
									variant="outline"
									size="sm"
									disabled={formState.payments.length === 1}
									onclick={() =>
										(formState = {
											...formState,
											payments: formState.payments.filter(
												(_, currentIndex) => currentIndex !== index
											)
										})}>Rimuovi</Button
								>
							</div>
						</div>
					{/each}
				</div>

				<div class="grid gap-3 md:grid-cols-3">
					<div class="rounded-[16px] border border-border/70 p-4">
						<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Totale</p>
						<p class="mt-2 text-lg font-semibold">{money.format(totalPreview)}</p>
					</div>
					<div class="rounded-[16px] border border-border/70 p-4">
						<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">
							Incassato subito
						</p>
						<p class="mt-2 text-lg font-semibold">{money.format(paidPreview)}</p>
					</div>
					<div class="rounded-[16px] border border-border/70 p-4">
						<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Residuo</p>
						<p class="mt-2 text-lg font-semibold">
							{money.format(Math.max(0, totalPreview - paidPreview))}
						</p>
					</div>
				</div>

				{#if createError}
					<div
						class="rounded-[18px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]"
					>
						{createError}
					</div>
				{/if}
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button
					type="button"
					variant="outline"
					onclick={() => {
						createOpen = false;
						createError = '';
					}}>Annulla</Button
				>
				<Button type="submit" disabled={createSubmitting}>
					{#if createSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<CreditCardIcon class="size-4" />
						Registra vendita
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>

<Sheet.Root bind:open={paymentOpen}>
	<Sheet.Content side="right" class="w-full sm:max-w-xl">
		<Sheet.Header class="border-b border-border/70 pb-4">
			<Sheet.Title class="text-xl"
				>{editingPayment ? 'Modifica movimento' : 'Registra incasso'}</Sheet.Title
			>
			<Sheet.Description class="text-sm text-muted-foreground">
				{#if selectedSale}
					{selectedSale.referenceCode} - {editingPayment
						? 'aggiorna il movimento selezionato'
						: `residuo ${money.format(selectedSale.remainingAmount)}`}
				{:else}
					Seleziona una vendita dal registro.
				{/if}
			</Sheet.Description>
		</Sheet.Header>

		<form class="flex min-h-0 flex-1 flex-col" onsubmit={handleAddPayment}>
			<div class="flex-1 space-y-5 overflow-y-auto p-4">
				{#if selectedSale}
					<div class="grid gap-3 md:grid-cols-3">
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Totale</p>
							<p class="mt-2 text-lg font-semibold">{money.format(selectedSale.totalAmount)}</p>
						</div>
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Incassato</p>
							<p class="mt-2 text-lg font-semibold">{money.format(selectedSale.paidAmount)}</p>
						</div>
						<div class="rounded-[16px] border border-border/70 p-4">
							<p class="text-xs tracking-[0.18em] text-muted-foreground uppercase">Residuo</p>
							<p class="mt-2 text-lg font-semibold">{money.format(selectedSale.remainingAmount)}</p>
						</div>
					</div>
				{/if}

				<div class="grid gap-4 md:grid-cols-2">
					<label class="space-y-2">
						<span class="text-sm font-medium">Importo</span>
						<Input bind:value={paymentFormState.amount} />
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Metodo</span>
						<Select.Root type="single" bind:value={paymentFormState.method}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">{paymentMethodLabel(paymentFormState.method)}</span>
							</Select.Trigger>
							<Select.Content>
								{#each paymentMethods as option (option.value)}
									<Select.Item value={option.value} label={option.label}>
										{option.label}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium">Stato</span>
						<Select.Root type="single" bind:value={paymentFormState.status}>
							<Select.Trigger class="w-full">
								<span data-slot="select-value">
									{paymentEntryStatusLabel(paymentFormState.status)}
								</span>
							</Select.Trigger>
							<Select.Content>
								{#each paymentEntryStatuses as option (option.value)}
									<Select.Item value={option.value} label={option.label}>
										{option.label}
									</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					</label>

					<label class="space-y-2">
						<span class="text-sm font-medium"
							>{paymentFormState.status === 'Paid' ? 'Data incasso' : 'Scadenza'}</span
						>
						{#if paymentFormState.status === 'Paid'}
							<input
								class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
								type="datetime-local"
								bind:value={paymentFormState.paidAtLocal}
							/>
							{#if paymentTimingLabel(paymentFormState.status, paymentFormState.paidAtLocal)}
								<p class="text-xs text-muted-foreground">
									{paymentTimingLabel(paymentFormState.status, paymentFormState.paidAtLocal)}
								</p>
							{/if}
						{:else}
							<input
								class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
								type="datetime-local"
								bind:value={paymentFormState.dueAtLocal}
							/>
							{#if paymentTimingLabel(paymentFormState.status, paymentFormState.dueAtLocal)}
								<p class="text-xs text-muted-foreground">
									{paymentTimingLabel(paymentFormState.status, paymentFormState.dueAtLocal)}
								</p>
							{/if}
						{/if}
					</label>
				</div>

				<label class="space-y-2">
					<span class="text-sm font-medium">Note operative</span>
					<Textarea
						bind:value={paymentFormState.notes}
						rows={4}
						placeholder="Es. saldo quota aprile, seconda rata, tentativo POS non riuscito..."
					/>
				</label>

				{#if paymentError}
					<div
						class="rounded-[18px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]"
					>
						{paymentError}
					</div>
				{/if}
			</div>

			<Sheet.Footer class="border-t border-border/70 bg-background/95 sm:flex-row sm:justify-end">
				<Button
					type="button"
					variant="outline"
					onclick={() => {
						paymentOpen = false;
						paymentError = '';
						editingPaymentId = null;
					}}>Annulla</Button
				>
				<Button type="submit" disabled={paymentSubmitting}>
					{#if paymentSubmitting}
						<RefreshCwIcon class="size-4 animate-spin" />
						Salvataggio...
					{:else}
						<CreditCardIcon class="size-4" />
						{editingPayment ? 'Aggiorna movimento' : 'Salva movimento'}
					{/if}
				</Button>
			</Sheet.Footer>
		</form>
	</Sheet.Content>
</Sheet.Root>
