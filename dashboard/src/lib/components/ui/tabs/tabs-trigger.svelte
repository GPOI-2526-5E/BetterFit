<script lang="ts">
	import { Tabs as TabsPrimitive } from 'bits-ui';
	import { cn } from '$lib/utils.js';

	let {
		ref = $bindable(null),
		class: className,
		...restProps
	}: TabsPrimitive.TriggerProps = $props();
</script>

<TabsPrimitive.Trigger
	bind:ref
	data-slot="tabs-trigger"
	class={cn(
		'inline-flex h-full flex-1 items-center justify-center gap-1.5 rounded-[10px] border border-transparent px-3 py-1.5 text-sm font-semibold whitespace-nowrap text-foreground transition-[color,box-shadow,background-color] focus-visible:ring-[3px] focus-visible:ring-ring/20 disabled:pointer-events-none disabled:opacity-50 data-[state=active]:border-primary/12 data-[state=active]:bg-primary data-[state=active]:[background-image:var(--bf-sprint-gradient)] data-[state=active]:text-primary-foreground data-[state=active]:shadow-[0_8px_18px_rgba(23,105,255,0.18)] [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*=size-])]:size-4',
		className
	)}
	{...restProps}
/>
