<script lang="ts" module>
	import { cn, type WithElementRef } from '$lib/utils.js';
	import type { HTMLAnchorAttributes, HTMLButtonAttributes } from 'svelte/elements';
	import { type VariantProps, tv } from 'tailwind-variants';

	export const buttonVariants = tv({
		base: 'inline-flex shrink-0 items-center justify-center gap-2 rounded-[10px] text-sm font-semibold whitespace-nowrap outline-none transition-all duration-150 focus-visible:ring-[3px] focus-visible:ring-ring/30 focus-visible:ring-offset-0 disabled:pointer-events-none disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*=size-])]:size-4',
		variants: {
			variant: {
				default:
					'bg-primary [background-image:linear-gradient(180deg,rgba(255,255,255,0.08),rgba(255,255,255,0)_42%),linear-gradient(135deg,#1769ff_0%,#1460eb_100%)] text-primary-foreground shadow-[0_10px_22px_rgba(23,105,255,0.18)] hover:bg-[#1460eb] hover:shadow-[0_12px_24px_rgba(23,105,255,0.2)] active:bg-[#0f56dd]',
				destructive:
					'bg-destructive text-white shadow-[0_12px_26px_rgba(220,38,38,0.18)] hover:-translate-y-[1px] hover:bg-[#b91c1c] active:translate-y-0',
				outline:
					'border border-border/80 bg-background/90 text-foreground shadow-[0_1px_2px_rgba(12,20,36,0.04)] hover:border-primary/25 hover:bg-secondary/80 hover:text-primary',
				secondary:
					'bg-secondary text-secondary-foreground shadow-[0_1px_2px_rgba(12,20,36,0.04)] hover:bg-primary/10 hover:text-primary',
				ghost: 'text-foreground hover:bg-secondary/80 hover:text-primary',
				link: 'text-primary underline-offset-4 hover:underline'
			},
			size: {
				default: 'h-10 px-4 py-2 has-[>svg]:px-3',
				sm: 'h-9 gap-1.5 rounded-[10px] px-3 has-[>svg]:px-2.5',
				lg: 'h-11 rounded-[10px] px-6 has-[>svg]:px-4',
				icon: 'size-10',
				'icon-sm': 'size-8',
				'icon-lg': 'size-11'
			}
		},
		defaultVariants: {
			variant: 'default',
			size: 'default'
		}
	});

	export type ButtonVariant = VariantProps<typeof buttonVariants>['variant'];
	export type ButtonSize = VariantProps<typeof buttonVariants>['size'];

	export type ButtonProps = WithElementRef<HTMLButtonAttributes> &
		WithElementRef<HTMLAnchorAttributes> & {
			variant?: ButtonVariant;
			size?: ButtonSize;
		};
</script>

<script lang="ts">
	let {
		class: className,
		variant = 'default',
		size = 'default',
		ref = $bindable(null),
		href = undefined,
		type = 'button',
		disabled,
		children,
		...restProps
	}: ButtonProps = $props();
</script>

{#if href}
	<a
		bind:this={ref}
		data-slot="button"
		class={cn(buttonVariants({ variant, size }), className)}
		href={disabled ? undefined : href}
		aria-disabled={disabled}
		role={disabled ? 'link' : undefined}
		tabindex={disabled ? -1 : undefined}
		{...restProps}
	>
		{@render children?.()}
	</a>
{:else}
	<button
		bind:this={ref}
		data-slot="button"
		class={cn(buttonVariants({ variant, size }), className)}
		{type}
		{disabled}
		{...restProps}
	>
		{@render children?.()}
	</button>
{/if}
