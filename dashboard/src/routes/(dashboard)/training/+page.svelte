<script lang="ts">
	import { page } from '$app/state';
	import HomeHeader from '$lib/components/dashboard/home/home-header.svelte';
	import TrainingWorkspace from '$lib/components/dashboard/training/training-workspace.svelte';

	const presetMembershipId = $derived(page.url.searchParams.get('membershipId'));
</script>

<HomeHeader />
<TrainingWorkspace presetMembershipId={presetMembershipId} />
