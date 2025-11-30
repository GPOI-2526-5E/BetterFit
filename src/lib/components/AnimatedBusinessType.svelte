<script lang="ts">
	import { onMount } from 'svelte';
	import { m } from '$lib/paraglide/messages';

	const businessTypes = [
		m.landing_heading_2_gym,
		m.landing_heading_2_personal_training,
		m.landing_heading_2_pool,
		m.landing_heading_2_yoga,
		m.landing_heading_2_dance,
		m.landing_heading_2_martial_arts
	];

	let currentIndex = $state(0);
	let nextIndex = $state(1);
	let isAnimating = $state(false);
    
	onMount(() => {
		const interval = setInterval(() => {
			nextIndex = (currentIndex + 1) % businessTypes.length;
			isAnimating = true;
			setTimeout(() => {
				currentIndex = nextIndex;
				isAnimating = false;
			}, 400);
		}, 3000);

		return () => clearInterval(interval);
	});
</script>

<span class="inline-grid relative min-h-[1.2em] overflow-hidden">
	<!-- Invisible spacer to maintain width -->
	<span class="invisible whitespace-nowrap col-start-1 row-start-1">
		{businessTypes.reduce((longest, type) => {
			const text = type();
			return text.length > longest.length ? text : longest;
		}, '')}
	</span>
	
	<!-- Current text sliding out -->
	<span
		class="inline-block bg-gradient-to-r from-secondary to-accent bg-clip-text text-transparent transition-all duration-400 ease-out whitespace-nowrap col-start-1 row-start-1"
		class:opacity-0={isAnimating}
		class:opacity-100={!isAnimating}
		class:translate-y-8={isAnimating}
		class:translate-y-0={!isAnimating}
	>
		{businessTypes[currentIndex]()}
	</span>
	
	<!-- Next text sliding in -->
	{#if isAnimating}
		<span
			class="inline-block bg-gradient-to-r from-secondary to-accent bg-clip-text text-transparent whitespace-nowrap col-start-1 row-start-1 opacity-0 -translate-y-8"
			style="animation: slideDown 400ms ease-out forwards;"
		>
			{businessTypes[nextIndex]()}
		</span>
	{/if}
</span>

<style>
	@keyframes slideDown {
		from {
			opacity: 0;
			transform: translateY(-2rem);
		}
		to {
			opacity: 1;
			transform: translateY(0);
		}
	}
</style>
