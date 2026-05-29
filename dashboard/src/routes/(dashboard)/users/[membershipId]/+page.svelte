<script lang="ts">
	import { page } from '$app/state';
	import ClientProfileWorkspace from '$lib/components/dashboard/clients/client-profile-workspace.svelte';
	import HomeHeader from '$lib/components/dashboard/home/home-header.svelte';

	const membershipId = $derived(page.params.membershipId ?? '');
</script>

<HomeHeader />
<ClientProfileWorkspace membershipId={membershipId} />
