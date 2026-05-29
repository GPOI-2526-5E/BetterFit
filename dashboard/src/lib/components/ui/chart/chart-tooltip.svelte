<script lang="ts">
	import { cn, type WithElementRef } from '$lib/utils.js';
	import type { HTMLAttributes } from 'svelte/elements';
	import type { Snippet } from 'svelte';

	let {
		ref = $bindable(null),
		class: className,
		children,
		...restProps
	}: WithElementRef<HTMLAttributes<HTMLDivElement>> & {
		children?: Snippet;
	} = $props();
</script>

<div
	bind:this={ref}
	class={cn('rounded-lg border border-border/50 bg-background px-3 py-2 text-xs shadow-xl', className)}
	{...restProps}
>
	{@render children?.()}
</div>
