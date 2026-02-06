import type { AccessLiveData } from '$lib/components/dashboard/access-live/types';
import * as m from '$lib/paraglide/messages.js';
import { getLocale } from '$lib/paraglide/runtime';

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

const centerMultiplier: Record<string, number> = {
	'caserta-centro': 1,
	'caserta-nord': 0.86,
	'napoli-vomero': 1.22
};

const shiftMinutes = (minutes: number) => new Date(Date.now() + minutes * 60_000);

const formatClock = (locale: string, date: Date): string =>
	new Intl.DateTimeFormat(locale, {
		hour: '2-digit',
		minute: '2-digit'
	}).format(date);

export async function fetchAccessLivePlaceholder(centerId: string): Promise<AccessLiveData> {
	await sleep(280);

	const locale = getLocale();
	const multiplier = centerMultiplier[centerId] ?? 1;

	const peopleInside = Math.round(121 * multiplier);
	const checkins30m = Math.max(8, Math.round(27 * multiplier));
	const deniedToday = Math.max(2, Math.round(11 * multiplier));
	const manualUnlocks = Math.max(1, Math.round(4 * multiplier));

	return {
		kpis: [
			{
				label: m.access_live_kpi_people_inside(),
				value: peopleInside.toString(),
				trend: m.access_live_kpi_people_inside_trend(),
				positive: true
			},
			{
				label: m.access_live_kpi_checkins_30m(),
				value: checkins30m.toString(),
				trend: m.access_live_kpi_checkins_30m_trend(),
				positive: true
			},
			{
				label: m.access_live_kpi_denied_today(),
				value: deniedToday.toString(),
				trend: m.access_live_kpi_denied_today_trend(),
				positive: false
			},
			{
				label: m.access_live_kpi_manual_unlocks(),
				value: manualUnlocks.toString(),
				trend: m.access_live_kpi_manual_unlocks_trend(),
				positive: false
			}
		],
		events: [
			{
				time: formatClock(locale, shiftMinutes(-2)),
				member: 'Luca Ferrante',
				gate: m.device_name_turnstile_1(),
				result: 'granted',
				origin: 'app_qr'
			},
			{
				time: formatClock(locale, shiftMinutes(-4)),
				member: 'Marta Russo',
				gate: m.device_name_turnstile_2(),
				result: 'denied',
				origin: 'badge'
			},
			{
				time: formatClock(locale, shiftMinutes(-6)),
				member: 'Desk Action',
				gate: m.device_name_turnstile_2(),
				result: 'manual_override',
				origin: 'desk'
			},
			{
				time: formatClock(locale, shiftMinutes(-9)),
				member: 'Silvia Greco',
				gate: m.device_name_turnstile_1(),
				result: 'granted',
				origin: 'badge'
			},
			{
				time: formatClock(locale, shiftMinutes(-12)),
				member: 'Marco Santoro',
				gate: m.device_name_turnstile_1(),
				result: 'denied',
				origin: 'unknown'
			}
		],
		controllers: [
			{
				name: m.device_name_turnstile_1(),
				status: 'online',
				latencyMs: 114,
				lastSeen: formatClock(locale, shiftMinutes(-1))
			},
			{
				name: m.device_name_turnstile_2(),
				status: 'degraded',
				latencyMs: 392,
				lastSeen: formatClock(locale, shiftMinutes(-2))
			},
			{
				name: m.device_name_womens_showers(),
				status: 'offline',
				latencyMs: 0,
				lastSeen: formatClock(locale, shiftMinutes(-19))
			}
		],
		deniedAttempts: [
			{
				member: 'Marta Russo',
				reason: m.access_live_denial_reason_medical_expired(),
				attempts: 2,
				lastAttempt: formatClock(locale, shiftMinutes(-4))
			},
			{
				member: 'Marco Santoro',
				reason: m.access_live_denial_reason_membership_expired(),
				attempts: 3,
				lastAttempt: formatClock(locale, shiftMinutes(-12))
			},
			{
				member: 'Giada Conte',
				reason: m.access_live_denial_reason_no_booking(),
				attempts: 1,
				lastAttempt: formatClock(locale, shiftMinutes(-21))
			},
			{
				member: 'Alessio Romano',
				reason: m.access_live_denial_reason_profile_blocked(),
				attempts: 1,
				lastAttempt: formatClock(locale, shiftMinutes(-28))
			}
		],
		lastSync: formatClock(locale, new Date())
	};
}
