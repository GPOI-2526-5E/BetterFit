<script lang="ts">
	import { cn, type WithElementRef } from '$lib/utils.js';
	import type { HTMLAttributes } from 'svelte/elements';

	let {
		ref = $bindable(null),
		class: className,
		children,
		...restProps
	}: WithElementRef<HTMLAttributes<HTMLTableRowElement>> = $props();
</script>

<tr
	bind:this={ref}
	data-slot="table-row"
	class={cn(
		'border-b border-border/80 transition-colors data-[state=selected]:bg-primary/10 hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-secondary/70',
		className
	)}
	{...restProps}
>
	{@render children?.()}
</tr>
