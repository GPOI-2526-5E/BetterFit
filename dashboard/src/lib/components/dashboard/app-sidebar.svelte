<script lang="ts">
	import BarChart3Icon from '@lucide/svelte/icons/bar-chart-3';
	import CalendarDaysIcon from '@lucide/svelte/icons/calendar-days';
	import CreditCardIcon from '@lucide/svelte/icons/credit-card';
	import DoorOpenIcon from '@lucide/svelte/icons/door-open';
	import DumbbellIcon from '@lucide/svelte/icons/dumbbell';
	import HomeIcon from '@lucide/svelte/icons/home';
	import MegaphoneIcon from '@lucide/svelte/icons/megaphone';
	import PlusCircleIcon from '@lucide/svelte/icons/plus-circle';
	import SettingsIcon from '@lucide/svelte/icons/settings';
	import ShieldIcon from '@lucide/svelte/icons/shield';
	import UsersIcon from '@lucide/svelte/icons/users';
	import { page } from '$app/state';
	import * as m from '$lib/paraglide/messages.js';
	import { SidebarMenuBadge } from '$lib/components/ui/sidebar/index.js';
	import * as Sidebar from '$lib/components/ui/sidebar/index.js';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';
	import { useUserAuthenticationStore } from '$lib/stores/AuthenticationStoreProvider.svelte';
	import { onMount } from 'svelte';
	import CenterSwitcher from './center-switcher.svelte';
	import NavMain from './nav-main.svelte';
	import NavUser from './nav-user.svelte';

	const center = useCenterSelectionStore();
	const auth = useUserAuthenticationStore();
	const navigationDisabled = $derived(
		!center.isLoadingGyms && !center.gymsError && center.gyms.length === 0
	);
	const sidebarUser = $derived.by(() => {
		const profile = auth.user?.account?.user;
		const email = profile?.email?.trim() || auth.user?.user || 'account@betterfit.local';
		const fallbackName =
			email
				.split('@')[0]
				?.replace(/[._-]+/g, ' ')
				.trim() || 'Betterfit account';
		const fullName = profile?.fullName?.trim() || fallbackName;

		return {
			name: fullName,
			email,
			avatar: `https://api.dicebear.com/9.x/initials/svg?seed=${encodeURIComponent(fullName)}`
		};
	});

	const data = $derived({
		user: sidebarUser,
		navMain: [
			{
				title: m.nav_home(),
				url: '/',
				icon: HomeIcon
			},
			{
				title: m.nav_users(),
				url: '/users',
				icon: UsersIcon,
				items: [
					{ title: m.nav_new_user(), url: '/users/new', available: true },
					{ title: m.nav_memberships_expiring(), url: '/users/expiring', available: true }
				]
			},
			{
				title: m.nav_access(),
				url: '/access',
				icon: DoorOpenIcon,
				items: [
					{ title: m.nav_access_history(), url: '/access/history', available: true },
					{ title: m.nav_quick_checkin(), url: '/access/checkin', available: true }
				]
			},
			{
				title: m.nav_sales(),
				url: '/sales',
				icon: CreditCardIcon,
				items: [
					{ title: m.nav_new_sale(), url: '/sales/new', available: true },
					{ title: m.nav_payments_receipts(), url: '/sales/payments', available: true },
					{ title: m.nav_renewals(), url: '/sales/renewals', available: true },
					{ title: 'Listino vendite', url: '/sales/catalog', available: true }
				]
			},
			{
				title: 'Attivita',
				url: '/activities',
				icon: CalendarDaysIcon,
				items: [
					{ title: 'Desk operativo', url: '/activities#desk', available: true },
					{ title: 'Catalogo attivita', url: '/activities#templates', available: true },
					{ title: 'Agenda imminente', url: '/activities#sessions', available: true },
					{ title: 'Registro sessioni', url: '/activities#registry', available: true },
					{ title: 'Prenotazioni e presenze', url: '/activities#bookings', available: true }
				]
			},
			{
				title: m.nav_training(),
				url: '/training',
				icon: DumbbellIcon,
				items: [
					{ title: 'Piani assegnati', url: '/training#assigned', available: true },
					{ title: m.nav_training_templates(), url: '/training#templates', available: true },
					{ title: m.nav_training_builder(), url: '/training#builder', available: true },
					{ title: m.nav_training_measurements(), url: '/training#measures', available: true }
				]
			},
			{
				title: m.nav_crm(),
				url: '/crm',
				icon: MegaphoneIcon,
				items: [
					{ title: 'Pipeline lead', url: '/crm#pipeline-board', available: true },
					{ title: 'Dettaglio e task', url: '/crm#pipeline-detail', available: true },
					{ title: m.nav_crm_campaigns(), url: '/crm/campaigns', available: true },
					{ title: m.nav_crm_automations(), url: '/crm/automations', available: true }
				]
			},
			{
				title: m.nav_reports(),
				url: '/analytics',
				icon: BarChart3Icon,
				items: [
					{ title: 'Dashboard KPI', url: '/analytics#kpi-summary', available: true },
					{ title: 'Pipeline lead', url: '/analytics#lead-pipeline', available: true },
					{ title: 'Attivita in arrivo', url: '/analytics#upcoming-activities', available: true },
					{ title: 'Operativita sedi', url: '/analytics#location-operations', available: true },
					{ title: m.nav_reports_retention_churn(), url: '/analytics/churn', available: true },
					{ title: m.nav_reports_export(), url: '/analytics/export', available: true }
				]
			},
			{
				title: m.nav_settings(),
				url: '/settings',
				icon: SettingsIcon,
				items: [
					{ title: 'Sedi e orari', url: '/settings#locations', available: true },
					{ title: m.nav_settings_team(), url: '/settings#team', available: true },
					{ title: m.nav_settings_permissions(), url: '/settings#roles', available: true },
					{ title: 'Security policy', url: '/settings#security', available: true },
					{ title: m.nav_settings_integrations(), url: '/settings/integrations', available: true }
				]
			}
		]
	});

	onMount(() => {
		if (auth.user && !auth.user.account?.user) {
			void auth.refreshCurrentUser();
		}
	});
</script>

<Sidebar.Root collapsible="icon">
	<Sidebar.Header
		class="border-b border-sidebar-border/70 px-3 py-4 group-data-[collapsible=icon]:px-2"
	>
		<CenterSwitcher />
	</Sidebar.Header>

	<Sidebar.Content class="px-2 py-2 group-data-[collapsible=icon]:px-0">
		<NavMain
			items={data.navMain}
			groupLabel="dashboard"
			comingSoonSuffix={m.common_coming_soon()}
			currentPath={page.url.pathname}
			currentHash={page.url.hash}
			disabled={navigationDisabled}
			disabledSuffix={m.dashboard_navigation_disabled_suffix()}
		/>

		<Sidebar.Group class="md:hidden">
			<Sidebar.GroupLabel>{m.sidebar_quick_actions()}</Sidebar.GroupLabel>
			<Sidebar.GroupContent>
				<Sidebar.Menu>
					<Sidebar.MenuItem>
						<Sidebar.MenuButton
							tooltipContent={m.quick_action_new_sale()}
							isActive={false}
							aria-disabled={navigationDisabled || undefined}
						>
							<PlusCircleIcon />
							<span>{m.quick_action_new_sale()}</span>
						</Sidebar.MenuButton>
						<SidebarMenuBadge>{m.common_desk()}</SidebarMenuBadge>
					</Sidebar.MenuItem>
					<Sidebar.MenuItem>
						<Sidebar.MenuButton
							tooltipContent={m.quick_action_unlock_turnstile()}
							isActive={false}
							aria-disabled={navigationDisabled || undefined}
						>
							<ShieldIcon />
							<span>{m.quick_action_unlock_turnstile()}</span>
						</Sidebar.MenuButton>
					</Sidebar.MenuItem>
				</Sidebar.Menu>
			</Sidebar.GroupContent>
		</Sidebar.Group>
	</Sidebar.Content>

	<Sidebar.Separator class="md:hidden" />
	<Sidebar.Footer class="px-2 pb-3 md:hidden">
		<NavUser user={data.user} />
	</Sidebar.Footer>
	<Sidebar.Rail />
</Sidebar.Root>
