<script lang="ts">
	import { page } from '$app/state';
	import HomeHeader from '$lib/components/dashboard/home/home-header.svelte';
	import SalesWorkspace from '$lib/components/dashboard/sales/sales-workspace.svelte';

	const presetSaleId = $derived(page.url.searchParams.get('saleId'));
</script>

<HomeHeader />
<SalesWorkspace mode="all" {presetSaleId} />
