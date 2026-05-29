import type { PermissionCatalogItemResponse, PermissionResponse } from '$lib/api';
import { getLocale } from '$lib/paraglide/runtime';

type Locale = 'en' | 'it';

type PermissionTranslation = {
	resourceLabel: string;
	actionLabel: string;
	title: string;
	description: string;
};

const fallbackByLocale: Record<Locale, PermissionTranslation> = {
	it: {
		resourceLabel: 'Permessi',
		actionLabel: 'Azione',
		title: 'Permesso operativo',
		description: 'Consente l accesso operativo alla funzione selezionata.'
	},
	en: {
		resourceLabel: 'Permissions',
		actionLabel: 'Action',
		title: 'Operational permission',
		description: 'Allows operational access to the selected feature.'
	}
};

const translationsIt: Record<string, PermissionTranslation> = {
	READ_GYMS: {
		resourceLabel: 'Tenant e sedi',
		actionLabel: 'Consulta',
		title: 'Consulta tenant e sedi',
		description: 'Visualizza dati base della palestra, sedi e configurazioni generali.'
	},
	WRITE_GYMS: {
		resourceLabel: 'Tenant e sedi',
		actionLabel: 'Modifica',
		title: 'Gestisci tenant e sedi',
		description: 'Aggiorna sedi, dati organizzativi e configurazioni del tenant.'
	},
	READ_MEMBERS: {
		resourceLabel: 'Clienti',
		actionLabel: 'Consulta',
		title: 'Consulta clienti e membership',
		description: 'Accede ad anagrafica cliente, stato membership, sedi abilitate e note operative.'
	},
	WRITE_MEMBERS: {
		resourceLabel: 'Clienti',
		actionLabel: 'Modifica',
		title: 'Gestisci clienti e membership',
		description: 'Crea o aggiorna clienti, membership e campi personalizzati del tenant.'
	},
	READ_CRM: {
		resourceLabel: 'CRM',
		actionLabel: 'Consulta',
		title: 'Consulta CRM',
		description: 'Visualizza lead, pipeline, task commerciali e storico contatti.'
	},
	WRITE_CRM: {
		resourceLabel: 'CRM',
		actionLabel: 'Modifica',
		title: 'Gestisci CRM',
		description: 'Aggiorna lead, task e stato commerciale del funnel.'
	},
	READ_STAFF_ASSIGNMENTS: {
		resourceLabel: 'Team',
		actionLabel: 'Consulta',
		title: 'Consulta team e assegnazioni',
		description: 'Visualizza ruoli, scope, stato e profili operativi dello staff.'
	},
	WRITE_STAFF_ASSIGNMENTS: {
		resourceLabel: 'Team',
		actionLabel: 'Modifica',
		title: 'Gestisci team e assegnazioni',
		description: 'Assegna staff al tenant e aggiorna ruolo, scope e stato operativo.'
	},
	READ_ROLES: {
		resourceLabel: 'Ruoli e permessi',
		actionLabel: 'Consulta',
		title: 'Consulta ruoli e permessi',
		description: 'Visualizza ruoli disponibili e catalogo permessi del tenant.'
	},
	WRITE_ROLES: {
		resourceLabel: 'Ruoli e permessi',
		actionLabel: 'Modifica',
		title: 'Gestisci ruoli e permessi',
		description: 'Crea o aggiorna ruoli personalizzati scegliendo i permessi necessari.'
	},
	READ_BILLING: {
		resourceLabel: 'Vendite e incassi',
		actionLabel: 'Consulta',
		title: 'Consulta vendite e incassi',
		description: 'Visualizza registro vendite, stato pagamenti, rinnovi e listino.'
	},
	WRITE_BILLING: {
		resourceLabel: 'Vendite e incassi',
		actionLabel: 'Modifica',
		title: 'Gestisci vendite e incassi',
		description: 'Registra vendite, aggiorna incassi, rimborsi e listino della palestra.'
	},
	APPROVE_PLANS: {
		resourceLabel: 'Piani',
		actionLabel: 'Approva',
		title: 'Approva piani',
		description: 'Conferma piani o passaggi che richiedono una validazione operativa.'
	},
	READ_CLASSES: {
		resourceLabel: 'Attivita',
		actionLabel: 'Consulta',
		title: 'Consulta attivita e prenotazioni',
		description: 'Visualizza corsi, sessioni, prenotazioni e capienze.'
	},
	WRITE_CLASSES: {
		resourceLabel: 'Attivita',
		actionLabel: 'Modifica',
		title: 'Gestisci attivita e prenotazioni',
		description: 'Crea o aggiorna corsi, sessioni e disponibilita operative.'
	},
	READ_REPORTS: {
		resourceLabel: 'Report',
		actionLabel: 'Consulta',
		title: 'Consulta report',
		description: 'Accede ai report operativi del tenant.'
	},
	EXPORT_REPORTS: {
		resourceLabel: 'Report',
		actionLabel: 'Esporta',
		title: 'Esporta report',
		description: 'Permette di esportare dati e report fuori dal gestionale.'
	},
	READ_WORKOUTS: {
		resourceLabel: 'Allenamenti',
		actionLabel: 'Consulta',
		title: 'Consulta schede e valutazioni',
		description: 'Visualizza schede, assessment e progressi dei clienti.'
	},
	WRITE_WORKOUTS: {
		resourceLabel: 'Allenamenti',
		actionLabel: 'Modifica',
		title: 'Gestisci schede e valutazioni',
		description: 'Crea o aggiorna programmi di allenamento e valutazioni.'
	},
	READ_PROFILE: {
		resourceLabel: 'Profili',
		actionLabel: 'Consulta',
		title: 'Consulta profili',
		description: 'Visualizza dati di profilo rilevanti per l operativita.'
	},
	WRITE_PROFILE: {
		resourceLabel: 'Profili',
		actionLabel: 'Modifica',
		title: 'Gestisci profili',
		description: 'Aggiorna dati di profilo collegati a clienti o staff.'
	},
	APPROVE_CHECKINS: {
		resourceLabel: 'Accessi',
		actionLabel: 'Approva',
		title: 'Gestisci accessi',
		description: 'Interviene su accessi, check-in e anomalie all ingresso.'
	},
	READ_LOCATIONS: {
		resourceLabel: 'Sedi',
		actionLabel: 'Consulta',
		title: 'Consulta sedi',
		description: 'Visualizza elenco sedi e dati principali di ciascuna struttura.'
	},
	WRITE_LOCATIONS: {
		resourceLabel: 'Sedi',
		actionLabel: 'Modifica',
		title: 'Gestisci sedi',
		description: 'Crea o aggiorna le sedi operative del tenant.'
	},
	READ_SECURITY_POLICY: {
		resourceLabel: 'Sicurezza',
		actionLabel: 'Consulta',
		title: 'Consulta policy di sicurezza',
		description: 'Visualizza le regole di autenticazione attive per il tenant.'
	},
	WRITE_SECURITY_POLICY: {
		resourceLabel: 'Sicurezza',
		actionLabel: 'Modifica',
		title: 'Gestisci policy di sicurezza',
		description: 'Aggiorna requisiti 2FA e impostazioni di sicurezza del tenant.'
	},
	READ_INTEGRATIONS: {
		resourceLabel: 'Integrazioni',
		actionLabel: 'Consulta',
		title: 'Consulta integrazioni',
		description: 'Visualizza provider collegati e stato sincronizzazioni.'
	},
	WRITE_INTEGRATIONS: {
		resourceLabel: 'Integrazioni',
		actionLabel: 'Modifica',
		title: 'Gestisci integrazioni',
		description: 'Aggiorna configurazioni e stato operativo delle integrazioni.'
	}
};

const translations: Record<Locale, Record<string, PermissionTranslation>> = {
	it: translationsIt,
	en: Object.fromEntries(Object.entries(translationsIt))
};

function resolveLocale(): Locale {
	return getLocale() === 'it' ? 'it' : 'en';
}

function stablePermissionKey(permission: PermissionCatalogItemResponse | PermissionResponse) {
	return (
		permission.descriptionKey?.trim() ||
		`${permission.action ?? ''}_${permission.resource ?? ''}`.toUpperCase()
	);
}

export function getPermissionTranslation(permission: PermissionCatalogItemResponse | PermissionResponse) {
	const locale = resolveLocale();
	const key = stablePermissionKey(permission);
	return translations[locale][key] ?? fallbackByLocale[locale];
}

export function getPermissionGroupLabel(group: Array<PermissionCatalogItemResponse | PermissionResponse>) {
	if (group.length === 0) {
		return fallbackByLocale[resolveLocale()].resourceLabel;
	}

	return getPermissionTranslation(group[0]).resourceLabel;
}
