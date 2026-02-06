<script lang="ts">
	import BarChart3Icon from '@lucide/svelte/icons/bar-chart-3';
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
	import CenterSwitcher from './center-switcher.svelte';
	import NavMain from './nav-main.svelte';
	import NavUser from './nav-user.svelte';

	const data = $derived({
		user: {
			name: 'Antonio Petricciuoli',
			email: 'antonio@betterfit.app',
			avatar: 'https://api.dicebear.com/9.x/initials/svg?seed=Antonio%20Petricciuoli'
		},
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
					{ title: m.nav_user_search(), url: '/users', available: false },
					{ title: m.nav_new_user(), url: '/users/new', available: false },
					{ title: m.nav_memberships_expiring(), url: '/users/expiring', available: false }
				]
			},
			{
				title: m.nav_access(),
				url: '/access',
				icon: DoorOpenIcon,
				items: [
					{ title: m.nav_access_live_monitor(), url: '/access/live', available: true },
					{ title: m.nav_access_history(), url: '/access/history', available: false },
					{ title: m.nav_quick_checkin(), url: '/access/checkin', available: false }
				]
			},
			{
				title: m.nav_sales(),
				url: '/sales',
				icon: CreditCardIcon,
				items: [
					{ title: m.nav_new_sale(), url: '/sales/new', available: false },
					{ title: m.nav_payments_receipts(), url: '/sales/payments', available: false },
					{ title: m.nav_renewals(), url: '/sales/renewals', available: false }
				]
			},
			{
				title: m.nav_training(),
				url: '/training',
				icon: DumbbellIcon,
				items: [
					{ title: m.nav_training_templates(), url: '/training/templates', available: false },
					{ title: m.nav_training_builder(), url: '/training/builder', available: false },
					{ title: m.nav_training_measurements(), url: '/training/measurements', available: false }
				]
			},
			{
				title: m.nav_crm(),
				url: '/crm',
				icon: MegaphoneIcon,
				items: [
					{ title: m.nav_crm_lead_pipeline(), url: '/crm/pipeline', available: false },
					{ title: m.nav_crm_campaigns(), url: '/crm/campaigns', available: false },
					{ title: m.nav_crm_automations(), url: '/crm/automations', available: false }
				]
			},
			{
				title: m.nav_reports(),
				url: '/analytics',
				icon: BarChart3Icon,
				items: [
					{ title: m.nav_reports_kpi_dashboard(), url: '/analytics/kpi', available: false },
					{ title: m.nav_reports_retention_churn(), url: '/analytics/churn', available: false },
					{ title: m.nav_reports_export(), url: '/analytics/export', available: false }
				]
			},
			{
				title: m.nav_settings(),
				url: '/settings',
				icon: SettingsIcon,
				items: [
					{ title: m.nav_settings_team(), url: '/settings/team', available: false },
					{ title: m.nav_settings_permissions(), url: '/settings/roles', available: false },
					{ title: m.nav_settings_integrations(), url: '/settings/integrations', available: false }
				]
			}
		]
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
			groupLabel=""
			comingSoonSuffix={m.common_coming_soon()}
			currentPath={page.url.pathname}
			currentHash={page.url.hash}
		/>

		<Sidebar.Group class="md:hidden">
			<Sidebar.GroupLabel>{m.sidebar_quick_actions()}</Sidebar.GroupLabel>
			<Sidebar.GroupContent>
				<Sidebar.Menu>
					<Sidebar.MenuItem>
						<Sidebar.MenuButton tooltipContent={m.quick_action_new_sale()} isActive={false}>
							<PlusCircleIcon />
							<span>{m.quick_action_new_sale()}</span>
						</Sidebar.MenuButton>
						<SidebarMenuBadge>{m.common_desk()}</SidebarMenuBadge>
					</Sidebar.MenuItem>
					<Sidebar.MenuItem>
						<Sidebar.MenuButton tooltipContent={m.quick_action_unlock_turnstile()} isActive={false}>
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
