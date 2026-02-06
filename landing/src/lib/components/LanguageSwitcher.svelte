<script lang="ts">
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import { getLocale, setLocale, locales } from '$lib/paraglide/runtime';

	const languages = {
		en: { name: 'English', flag: '🇬🇧' },
		it: { name: 'Italiano', flag: '🇮🇹' }
	};

	let currentLang = $derived(getLocale());
	let currentLanguage = $derived(languages[currentLang as keyof typeof languages]);
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger
		class="inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground h-8 px-3 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
	>
		<span class="text-base">{currentLanguage.flag}</span>
		<span class="hidden sm:inline">{currentLanguage.name}</span>
		<svg
			xmlns="http://www.w3.org/2000/svg"
			width="16"
			height="16"
			viewBox="0 0 24 24"
			fill="none"
			stroke="currentColor"
			stroke-width="2"
			stroke-linecap="round"
			stroke-linejoin="round"
			class="opacity-50"
		>
			<path d="m6 9 6 6 6-6" />
		</svg>
	</DropdownMenu.Trigger>
	<DropdownMenu.Content align="end">
		{#each locales as lang (lang)}
			<DropdownMenu.Item
				onclick={() => setLocale(lang)}
				class="gap-2"
				disabled={lang === currentLang}
			>
				<span class="text-base">{languages[lang as keyof typeof languages].flag}</span>
				<span>{languages[lang as keyof typeof languages].name}</span>
				{#if lang === currentLang}
					<svg
						xmlns="http://www.w3.org/2000/svg"
						width="16"
						height="16"
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						stroke-linecap="round"
						stroke-linejoin="round"
						class="ml-auto"
					>
						<path d="M20 6 9 17l-5-5" />
					</svg>
				{/if}
			</DropdownMenu.Item>
		{/each}
	</DropdownMenu.Content>
</DropdownMenu.Root>
