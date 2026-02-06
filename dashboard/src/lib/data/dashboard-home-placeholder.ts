import type { HomeData } from '$lib/components/dashboard/home/types';
import * as m from '$lib/paraglide/messages.js';
import { getLocale } from '$lib/paraglide/runtime';

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

const centerMultiplier: Record<string, number> = {
	'caserta-centro': 1,
	'caserta-nord': 0.82,
	'napoli-vomero': 1.24
};

const centerName: Record<string, string> = {
	'caserta-centro': 'Caserta Centro',
	'caserta-nord': 'Caserta Nord',
	'napoli-vomero': 'Napoli Vomero'
};

export async function fetchDashboardHomePlaceholder(centerId: string): Promise<HomeData> {
	await sleep(320);
	const multiplier = centerMultiplier[centerId] ?? 1;
	const locale = getLocale();

	const accesses = Math.round(428 * multiplier);
	const reservations = Math.round(36 * multiplier);
	const revenue = Math.round(4920 * multiplier);
	const renewals = Math.max(6, Math.round(19 * multiplier));

	return {
		kpis: [
			{
				label: m.kpi_accesses_today_label(),
				value: accesses.toString(),
				trend: m.kpi_accesses_today_trend(),
				positive: true
			},
			{
				label: m.kpi_daily_revenue_label(),
				value: new Intl.NumberFormat(locale, {
					style: 'currency',
					currency: 'EUR',
					maximumFractionDigits: 0
				}).format(revenue),
				trend: m.kpi_daily_revenue_trend(),
				positive: true
			},
			{
				label: m.kpi_renewals_expiring_label(),
				value: renewals.toString(),
				trend: m.kpi_renewals_expiring_trend({ count: Math.max(3, Math.round(renewals * 0.35)) }),
				positive: false
			},
			{
				label: m.kpi_next2h_bookings_label(),
				value: reservations.toString(),
				trend: m.kpi_next2h_bookings_trend(),
				positive: true
			}
		],
		tasks: [
			{
				task: m.task_renewal_quarterly(),
				member: 'Antonio Petricciuoli',
				due: m.due_today_at({ time: '19:00' }),
				status: 'pending'
			},
			{
				task: m.task_missing_medical_document(),
				member: 'Giulia Ferraro',
				due: m.due_tomorrow(),
				status: 'urgent'
			},
			{
				task: m.task_failed_rid_payment_center({ center: centerName[centerId] ?? 'center' }),
				member: 'Mario Rossi',
				due: m.due_plus_hours({ hours: 48 }),
				status: 'urgent'
			},
			{
				task: m.task_cancellation_request_review(),
				member: 'Francesca Viola',
				due: m.due_plus_days({ days: 6 }),
				status: 'ok'
			}
		],
		alerts: [
			{
				title: m.alert_showers_controller_offline_title(),
				description: m.alert_showers_controller_offline_desc(),
				status: 'critical'
			},
			{
				title: m.alert_access_peak_title(),
				description: m.alert_access_peak_desc(),
				status: 'warning'
			},
			{
				title: m.alert_renewal_campaign_sent_title(),
				description: m.alert_renewal_campaign_sent_desc({ count: 84 }),
				status: 'ok'
			}
		],
		devices: [
			{
				name: m.device_name_turnstile_1(),
				status: 'online',
				lastEvent: m.device_event_valid_checkin_time({ time: '08:21' })
			},
			{
				name: m.device_name_turnstile_2(),
				status: 'online',
				lastEvent: m.device_event_valid_checkin_time({ time: '08:27' })
			},
			{
				name: m.device_name_mens_showers(),
				status: 'online',
				lastEvent: m.device_event_service_session_time({ time: '08:35' })
			},
			{
				name: m.device_name_womens_showers(),
				status: 'offline',
				lastEvent: m.device_event_controller_unreachable()
			},
			{
				name: m.device_name_mens_hairdryer(),
				status: 'maintenance',
				lastEvent: m.device_event_command_timeout_time({ time: '07:52' })
			}
		],
		timeline: [
			{ time: '08:35', text: m.timeline_event_showers_enabled_credit() },
			{ time: '08:27', text: m.timeline_event_qr_access_turnstile_2() },
			{ time: '08:21', text: m.timeline_event_qr_access_turnstile_1() },
			{ time: m.timeline_time_yesterday(), text: m.timeline_event_functional_booking_confirmed() }
		]
	};
}
