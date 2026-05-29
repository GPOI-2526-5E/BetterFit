<script lang="ts">
	import { page } from '$app/state';
	import HomeHeader from '$lib/components/dashboard/home/home-header.svelte';
	import SalesWorkspace from '$lib/components/dashboard/sales/sales-workspace.svelte';

	const presetMembershipId = $derived(page.url.searchParams.get('membershipId'));
</script>

<HomeHeader />
<SalesWorkspace mode="new" autoOpenCreateSheet presetMembershipId={presetMembershipId} />
