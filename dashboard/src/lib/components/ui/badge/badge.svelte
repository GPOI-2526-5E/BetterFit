<script lang="ts" module>
	import { type VariantProps, tv } from 'tailwind-variants';

	export const badgeVariants = tv({
		base: 'inline-flex w-fit shrink-0 items-center justify-center gap-1 overflow-hidden rounded-full border px-2.5 py-1 text-[11px] leading-none font-semibold whitespace-nowrap transition-[color,box-shadow,background-color] focus-visible:ring-[3px] focus-visible:ring-ring/25 [&>svg]:pointer-events-none [&>svg]:size-3',
		variants: {
			variant: {
				default:
					'border-transparent bg-primary [background-image:var(--bf-sprint-gradient)] text-primary-foreground shadow-[0_8px_18px_rgba(23,105,255,0.18)] [a&]:hover:brightness-[1.03]',
				secondary:
					'border-transparent bg-secondary text-secondary-foreground [a&]:hover:bg-primary/12',
				destructive: 'border-transparent bg-[#fee2e2] text-[#991b1b] [a&]:hover:bg-[#fecaca]',
				outline: 'border-border bg-background/85 text-foreground [a&]:hover:bg-secondary/70',
				success: 'border-transparent bg-[#dcfce7] text-[#166534] [a&]:hover:bg-[#bbf7d0]',
				warning: 'border-transparent bg-[#fef3c7] text-[#92400e] [a&]:hover:bg-[#fde68a]'
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
