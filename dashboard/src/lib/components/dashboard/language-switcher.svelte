<script lang="ts">
	import CheckIcon from '@lucide/svelte/icons/check';
	import ChevronsUpDownIcon from '@lucide/svelte/icons/chevrons-up-down';
	import GlobeIcon from '@lucide/svelte/icons/globe';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu/index.js';
	import * as m from '$lib/paraglide/messages.js';
	import { getLocale, setLocale } from '$lib/paraglide/runtime';

	const locale = $derived(getLocale());
	const localeCode = $derived(locale.toUpperCase());

	function switchLocale(nextLocale: 'en' | 'it') {
		if (locale === nextLocale) return;
		setLocale(nextLocale);
	}
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger>
		{#snippet child({ props })}
			<button
				type="button"
				class="inline-flex h-9 items-center gap-1.5 rounded-[10px] border border-border bg-background px-3 text-sm font-semibold whitespace-nowrap text-foreground transition-colors outline-none hover:bg-secondary/70 focus-visible:ring-[3px] focus-visible:ring-ring/35 focus-visible:ring-offset-1"
				aria-label={m.user_language()}
				{...props}
			>
				<GlobeIcon class="size-4" />
				<span class="text-xs uppercase">{localeCode}</span>
				<ChevronsUpDownIcon class="size-3.5 opacity-70" />
			</button>
		{/snippet}
	</DropdownMenu.Trigger>
	<DropdownMenu.Content class="min-w-48 rounded-[12px]" align="end" sideOffset={8}>
		<DropdownMenu.Label class="text-xs text-muted-foreground"
			>{m.user_language()}</DropdownMenu.Label
		>
		<DropdownMenu.Item class="justify-between" onclick={() => switchLocale('en')}>
			<span>{m.user_language_en()}</span>
			{#if locale === 'en'}
				<CheckIcon class="size-4 text-primary" />
			{/if}
		</DropdownMenu.Item>
		<DropdownMenu.Item class="justify-between" onclick={() => switchLocale('it')}>
			<span>{m.user_language_it()}</span>
			{#if locale === 'it'}
				<CheckIcon class="size-4 text-primary" />
			{/if}
		</DropdownMenu.Item>
	</DropdownMenu.Content>
</DropdownMenu.Root>
