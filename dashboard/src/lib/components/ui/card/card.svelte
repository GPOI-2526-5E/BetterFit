<script lang="ts">
	import type { HTMLAttributes } from 'svelte/elements';
	import { cn, type WithElementRef } from '$lib/utils.js';

	let {
		ref = $bindable(null),
		class: className,
		children,
		...restProps
	}: WithElementRef<HTMLAttributes<HTMLDivElement>> = $props();
</script>

<div
	bind:this={ref}
	data-slot="card"
	class={cn(
		'flex flex-col gap-4 overflow-hidden rounded-[14px] border border-white/70 bg-[linear-gradient(180deg,color-mix(in_srgb,var(--card)_90%,white_10%)_0%,var(--card)_100%)] py-5 text-card-foreground shadow-[var(--bf-shadow-card)] backdrop-blur-[2px]',
		className
	)}
	{...restProps}
>
	{@render children?.()}
</div>
