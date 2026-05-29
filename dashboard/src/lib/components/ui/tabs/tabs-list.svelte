<script lang="ts">
	import { Tabs as TabsPrimitive } from 'bits-ui';
	import { cn } from '$lib/utils.js';

	let { ref = $bindable(null), class: className, ...restProps }: TabsPrimitive.ListProps = $props();
</script>

<TabsPrimitive.List
	bind:ref
	data-slot="tabs-list"
	class={cn(
		'inline-flex h-11 w-fit items-center justify-center rounded-[12px] border border-border/70 bg-background/70 p-1 text-muted-foreground shadow-[inset_0_1px_0_rgba(255,255,255,0.7)] backdrop-blur-[2px]',
		className
	)}
	{...restProps}
/>
