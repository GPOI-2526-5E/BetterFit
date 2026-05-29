import type { GymMembershipResponse, GymMembershipSource, GymMembershipStatus } from '$lib/api';

export type MembershipBadgeVariant =
	| 'default'
	| 'secondary'
	| 'destructive'
	| 'outline'
	| 'success'
	| 'warning';

export function formatMembershipDate(
	value: Date | null | undefined,
	formatter: Intl.DateTimeFormat,
	fallback = 'Non disponibile'
) {
	return value ? formatter.format(value) : fallback;
}

export function formatMembershipDateTime(
	value: Date | null | undefined,
	formatter: Intl.DateTimeFormat,
	fallback = 'Non disponibile'
) {
	return value ? formatter.format(value) : fallback;
}

export function formatMembershipCustomFieldValue(
	value: string | null | undefined,
	valueType: string | null | undefined,
	dateFormatter: (value: Date | null | undefined, fallback?: string) => string
) {
	if (!value?.trim()) {
		return 'Non compilato';
	}

	if (valueType === 'Boolean') {
		return value === 'true' ? 'Si' : 'No';
	}

	if (valueType === 'Date') {
		const parsed = new Date(value);
		return Number.isNaN(parsed.getTime()) ? value : dateFormatter(parsed, value);
	}

	return value;
}

export function membershipDisplayName(record: GymMembershipResponse) {
	const firstName = record.memberProfile?.firstName?.trim();
	const lastName = record.memberProfile?.lastName?.trim();
	const fullName = [firstName, lastName].filter(Boolean).join(' ').trim();
	return fullName || record.userEmail?.trim() || record.invitationEmail?.trim() || 'Cliente senza nome';
}

export function membershipDisplayEmail(record: GymMembershipResponse) {
	return record.userEmail?.trim() || record.invitationEmail?.trim() || 'Email non disponibile';
}

export function membershipSourceLabel(source: GymMembershipSource | undefined) {
	if (source === 'StaffInvite') {
		return 'Invito desk';
	}

	if (source === 'SelfSignup') {
		return 'Registrazione autonoma';
	}

	if (source === 'Import') {
		return 'Importato';
	}

	if (source === 'Migration') {
		return 'Migrazione';
	}

	return 'Origine non disponibile';
}

export function membershipStatusMeta(status: GymMembershipStatus | undefined): {
	label: string;
	variant: MembershipBadgeVariant;
} {
	if (status === 'Active') {
		return { label: 'Attivo', variant: 'success' };
	}

	if (status === 'PendingClaim') {
		return { label: 'Da attivare', variant: 'warning' };
	}

	if (status === 'Suspended') {
		return { label: 'Sospeso', variant: 'destructive' };
	}

	return { label: 'Archiviato', variant: 'outline' };
}

export function membershipInitials(name: string) {
	return name
		.split(' ')
		.map((part) => part[0])
		.join('')
		.slice(0, 2)
		.toUpperCase();
}
