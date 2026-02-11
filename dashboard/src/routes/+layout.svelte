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

<QueryClientProvider client={queryClient}>
	<CenterSelectionStoreProvider>
		<SidebarProvider>
			<AppSidebar />
			<SidebarInset class="md:rounded-[20px]">
				{@render children()}
			</SidebarInset>
		</SidebarProvider>
	</CenterSelectionStoreProvider>
</QueryClientProvider>
<div style="display:none">
	{#each locales as locale}
		<a href={localizeHref(page.url.pathname, { locale })}>
			{locale}
		</a>
	{/each}
</div>
