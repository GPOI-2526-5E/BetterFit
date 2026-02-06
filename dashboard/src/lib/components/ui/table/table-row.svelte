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
		'border-b border-border transition-colors data-[state=selected]:bg-secondary/60 hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-[#f2f7ff]',
		className
	)}
	{...restProps}
>
	{@render children?.()}
</tr>
