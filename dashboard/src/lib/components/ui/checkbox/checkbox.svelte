<script lang="ts">
	import CheckIcon from '@lucide/svelte/icons/check';
	import MinusIcon from '@lucide/svelte/icons/minus';
	import { Checkbox as CheckboxPrimitive } from 'bits-ui';
	import { cn, type WithoutChild } from '$lib/utils.js';

	let {
		ref = $bindable(null),
		class: className,
		checked = $bindable(false),
		indeterminate = $bindable(false),
		children,
		...restProps
	}: WithoutChild<CheckboxPrimitive.RootProps> = $props();
</script>

<CheckboxPrimitive.Root
	bind:ref
	bind:checked
	bind:indeterminate
	data-slot="checkbox"
	class={cn(
		'peer inline-flex size-4 shrink-0 items-center justify-center rounded-[5px] border border-input bg-background text-primary shadow-[0_1px_2px_rgba(12,20,36,0.06)] outline-none transition-[background-color,border-color,box-shadow,color] focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/20 disabled:cursor-not-allowed disabled:opacity-50 data-[state=checked]:border-primary data-[state=checked]:bg-primary data-[state=checked]:text-primary-foreground data-[state=indeterminate]:border-primary data-[state=indeterminate]:bg-primary data-[state=indeterminate]:text-primary-foreground',
		className
	)}
	{...restProps}
>
	{#snippet child({ checked: isChecked, indeterminate: isIndeterminate })}
		{#if isIndeterminate}
			<MinusIcon class="size-3.5" />
		{:else if isChecked}
			<CheckIcon class="size-3.5" />
		{/if}
	{/snippet}
</CheckboxPrimitive.Root>
