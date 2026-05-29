<script lang="ts">
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';

	let {
		href,
		replaceState = false
	}: {
		href: string;
		replaceState?: boolean;
	} = $props();

	onMount(() => {
		void goto(href, replaceState ? { replaceState: true } : undefined);
	});
</script>
