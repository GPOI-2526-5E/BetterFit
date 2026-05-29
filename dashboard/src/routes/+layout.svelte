<script lang="ts">
	import '@fontsource-variable/sora';
	import { page } from '$app/state';
	import AppSidebar from '$lib/components/dashboard/app-sidebar.svelte';
	import { SidebarInset, SidebarProvider } from '$lib/components/ui/sidebar/index.js';
	import { locales, localizeHref } from '$lib/paraglide/runtime';
	import { QueryClient, QueryClientProvider } from '@tanstack/svelte-query';
	import './layout.css';
	import favicon from '$lib/assets/favicon.svg';
	import CenterSelectionStoreProvider from '$lib/stores/CenterSelectionStoreProvider.svelte';
	import AuthenticationStoreProvider from '$lib/stores/AuthenticationStoreProvider.svelte';
	import ApiClientProvider from '$lib/stores/ApiClientProvider.svelte';

	const queryClient = new QueryClient({
		defaultOptions: {
			queries: {
				staleTime: 60_000,
				refetchOnWindowFocus: false,
				retry: 1
			}
		}
	});

	let { children } = $props();
</script>

<svelte:head><link rel="icon" href={favicon} /></svelte:head>
<ApiClientProvider>
	<AuthenticationStoreProvider>
		<QueryClientProvider client={queryClient}>
			{@render children()}
		</QueryClientProvider>
	</AuthenticationStoreProvider>
</ApiClientProvider>
<div style="display:none">
	{#each locales as locale}
		<a href={localizeHref(page.url.pathname, { locale })}>
			{locale}
		</a>
	{/each}
</div>
