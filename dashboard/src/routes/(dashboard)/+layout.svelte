<script lang="ts">
	import '@fontsource-variable/sora';
	import AuthRouteRedirect from '$lib/components/auth/auth-route-redirect.svelte';
	import AppSidebar from '$lib/components/dashboard/app-sidebar.svelte';
	import DashboardContentGate from '$lib/components/dashboard/dashboard-content-gate.svelte';
	import { SidebarInset, SidebarProvider } from '$lib/components/ui/sidebar/index.js';
	import CenterSelectionStoreProvider from '$lib/stores/CenterSelectionStoreProvider.svelte';
	import { useUserAuthenticationStore } from '$lib/stores/AuthenticationStoreProvider.svelte';

	let { children } = $props();

	const auth = useUserAuthenticationStore();
	const redirectHref = $derived(auth.ready && !auth.user ? '/login' : null);
</script>

{#if redirectHref}
	{#key redirectHref}
		<AuthRouteRedirect href={redirectHref} />
	{/key}
{/if}

<CenterSelectionStoreProvider>
	<SidebarProvider>
		<AppSidebar />
		<SidebarInset class="md:rounded-[20px]">
			<DashboardContentGate>
				{@render children()}
			</DashboardContentGate>
		</SidebarInset>
	</SidebarProvider>
</CenterSelectionStoreProvider>
