<script lang="ts">
	import BellIcon from '@lucide/svelte/icons/bell';
	import SearchIcon from '@lucide/svelte/icons/search';
	import LanguageSwitcher from '$lib/components/dashboard/language-switcher.svelte';
	import TopUserMenu from '$lib/components/dashboard/top-user-menu.svelte';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { SidebarTrigger } from '$lib/components/ui/sidebar/index.js';
	import * as m from '$lib/paraglide/messages.js';
	import { center } from '$lib/stores/center-selection.svelte';

	const user = {
		name: 'Antonio Petricciuoli',
		email: 'antonio@betterfit.app',
		avatar: 'https://api.dicebear.com/9.x/initials/svg?seed=Antonio%20Petricciuoli'
	};
</script>

<header
	class="sticky top-0 z-20 border-b border-border/80 bg-background/80 px-4 py-3 backdrop-blur md:px-6"
>
	<div class="flex flex-wrap items-center justify-between gap-3">
		<div class="flex min-w-[280px] flex-1 items-center gap-3">
			<SidebarTrigger class="md:hidden" />
			<h1 class="text-lg font-semibold tracking-tight">BetterFit</h1>
			<!--<Badge variant="secondary" class="hidden lg:inline-flex">{center.selected.name}</Badge>
		<div class="relative hidden w-full max-w-xl md:block">
				<SearchIcon
					class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
				/>
				<Input placeholder={m.search_global_placeholder()} class="pl-9" />
			</div> KEEP COMMENTED FOR FUTURE USE -->
		</div>
		<div class="flex items-center gap-2">
			<Button variant="outline" size="sm" class="hidden md:inline-flex"
				>{m.button_unlock_turnstile()}</Button
			>
			<Button size="sm" class="hidden md:inline-flex">{m.button_new_sale()}</Button>
			<LanguageSwitcher />
			<Button
				variant="ghost"
				size="icon-sm"
				aria-label={m.common_notifications()}
				class="hidden md:inline-flex"
			>
				<BellIcon class="size-4" />
			</Button>
			<div class="hidden md:block">
				<TopUserMenu {user} />
			</div>
		</div>
	</div>
</header>
