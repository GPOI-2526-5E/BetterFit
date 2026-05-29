<script lang="ts">
	import { page } from '$app/state';
	import HomeHeader from '$lib/components/dashboard/home/home-header.svelte';
	import AccessWorkspace from '$lib/components/dashboard/access/access-workspace.svelte';

	const presetMembershipId = $derived(page.url.searchParams.get('membershipId'));
</script>

<HomeHeader />
<AccessWorkspace mode="checkin" presetMembershipId={presetMembershipId} />
