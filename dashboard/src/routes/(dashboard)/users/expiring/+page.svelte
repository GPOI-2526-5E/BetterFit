<script lang="ts">
	import ClientsWorkspace from '$lib/components/dashboard/clients/clients-workspace.svelte';
	import HomeHeader from '$lib/components/dashboard/home/home-header.svelte';
</script>

<HomeHeader />
<ClientsWorkspace mode="expiring" />
