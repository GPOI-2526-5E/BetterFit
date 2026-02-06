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
		'flex flex-col gap-4 rounded-[14px] border border-border bg-card py-5 text-card-foreground shadow-[var(--bf-shadow-card)]',
		className
	)}
	{...restProps}
>
	{@render children?.()}
</div>
