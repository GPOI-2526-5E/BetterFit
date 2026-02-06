<script lang="ts" module>
	import { type VariantProps, tv } from 'tailwind-variants';

	export const badgeVariants = tv({
		base: 'focus-visible:ring-ring/25 aria-invalid:ring-destructive/20 aria-invalid:border-destructive inline-flex w-fit shrink-0 items-center justify-center gap-1 overflow-hidden rounded-full border px-2.5 py-1 text-[11px] leading-none font-semibold whitespace-nowrap transition-[color,box-shadow] focus-visible:ring-[3px] [&>svg]:pointer-events-none [&>svg]:size-3',
		variants: {
			variant: {
				default: 'bg-primary text-primary-foreground border-transparent [a&]:hover:bg-[#0a4fd4]',
				secondary:
					'bg-secondary text-secondary-foreground border-transparent [a&]:hover:bg-[#dce8ff]',
				destructive: 'bg-[#fee2e2] text-[#991b1b] border-transparent [a&]:hover:bg-[#fecaca]',
				outline: 'text-foreground bg-background border-border [a&]:hover:bg-secondary/60',
				success: 'bg-[#dcfce7] text-[#166534] border-transparent [a&]:hover:bg-[#bbf7d0]',
				warning: 'bg-[#fef3c7] text-[#92400e] border-transparent [a&]:hover:bg-[#fde68a]'
			}
		},
		defaultVariants: {
			variant: 'default'
		}
	});

	export type BadgeVariant = VariantProps<typeof badgeVariants>['variant'];
</script>

<script lang="ts">
	import type { HTMLAnchorAttributes } from 'svelte/elements';
	import { cn, type WithElementRef } from '$lib/utils.js';

	let {
		ref = $bindable(null),
		href,
		class: className,
		variant = 'default',
		children,
		...restProps
	}: WithElementRef<HTMLAnchorAttributes> & {
		variant?: BadgeVariant;
	} = $props();
</script>

<svelte:element
	this={href ? 'a' : 'span'}
	bind:this={ref}
	data-slot="badge"
	{href}
	class={cn(badgeVariants({ variant }), className)}
	{...restProps}
>
	{@render children?.()}
</svelte:element>
