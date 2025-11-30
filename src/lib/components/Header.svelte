<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import LanguageSwitcher from './LanguageSwitcher.svelte';
	import * as m from '$lib/paraglide/messages.js';

	let { currentPath = $bindable('/') } = $props();

	let mobileMenuOpen = $state(false);

	function toggleMobileMenu() {
		mobileMenuOpen = !mobileMenuOpen;
	}

	const navItems = [
		{ id: 'features', label: 'Features', href: '#features' },
		{ id: 'waitlist', label: 'Waitlist', href: '#waitlist' },
		{ id: 'about', label: 'About', href: '#about' }
	];
</script>

<header class="sticky top-0 z-50 w-full border-b border-border/40 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
	<nav class="max-w-7xl mx-auto flex h-16 items-center justify-between px-4 md:px-6">
		<!-- Logo -->
		<a href="/" data-sveltekit-reload class="flex items-center gap-2 hover:opacity-80 transition-opacity">
			<div class="flex items-center justify-center w-9 h-9 rounded-lg bg-gradient-to-br from-primary to-secondary">
				<svg
					xmlns="http://www.w3.org/2000/svg"
					viewBox="0 0 24 24"
					fill="none"
					stroke="currentColor"
					stroke-width="2"
					stroke-linecap="round"
					stroke-linejoin="round"
					class="w-5 h-5 text-white"
				>
					<path d="M14 14.5c0 1.4-1.1 2.5-2.5 2.5S9 15.9 9 14.5s1.1-2.5 2.5-2.5 2.5 1.1 2.5 2.5z"/>
					<path d="M8.5 8.5c0 1.4-1.1 2.5-2.5 2.5S3.5 9.9 3.5 8.5 4.6 6 6 6s2.5 1.1 2.5 2.5z"/>
					<path d="M20 8.5c0 1.4-1.1 2.5-2.5 2.5S15 9.9 15 8.5 16.1 6 17.5 6s2.5 1.1 2.5 2.5z"/>
					<line x1="6" y1="11" x2="9" y2="12"/>
					<line x1="15" y1="12" x2="18" y2="11"/>
					<line x1="12" y1="17" x2="12" y2="21"/>
				</svg>
			</div>
			<span class="font-bold text-xl text-primary">
				BetterFit
			</span>
		</a>

		<!-- Desktop Navigation -->
		<div class="hidden md:flex items-center gap-8">
			{#each navItems as item (item.id)}
				<a 
					href={item.href}
					class="text-sm font-medium text-foreground/60 transition-colors hover:text-foreground"
				>
					{item.label}
				</a>
			{/each}
		</div>

		<div class="hidden md:flex items-center gap-3">
			<LanguageSwitcher />
            		<!-- Desktop CTA Buttons 

			<Button variant="ghost" size="sm">
				Sign In
			</Button>
			<Button size="sm">
				Start Free Trial
			</Button>
            		<!-- Mobile Menu Button -->

		</div>

		<button 
			onclick={toggleMobileMenu}
			class="md:hidden flex items-center justify-center w-10 h-10 rounded-md hover:bg-accent transition-colors"
			aria-label="Toggle menu"
		>
			{#if !mobileMenuOpen}
				<svg
					xmlns="http://www.w3.org/2000/svg"
					width="24"
					height="24"
					viewBox="0 0 24 24"
					fill="none"
					stroke="currentColor"
					stroke-width="2"
					stroke-linecap="round"
					stroke-linejoin="round"
				>
					<line x1="4" x2="20" y1="6" y2="6" />
					<line x1="4" x2="20" y1="12" y2="12" />
					<line x1="4" x2="20" y1="18" y2="18" />
				</svg>
			{:else}
				<svg
					xmlns="http://www.w3.org/2000/svg"
					width="24"
					height="24"
					viewBox="0 0 24 24"
					fill="none"
					stroke="currentColor"
					stroke-width="2"
					stroke-linecap="round"
					stroke-linejoin="round"
				>
					<path d="M18 6 6 18" />
					<path d="m6 6 12 12" />
				</svg>
			{/if}
		</button>
	</nav>

	<!-- Mobile Menu -->
	{#if mobileMenuOpen}
		<div class="md:hidden border-t border-border/40 bg-background/95 backdrop-blur">
			<div class="max-w-7xl mx-auto px-4 py-4 space-y-3">
				{#each navItems as item (item.id)}
					<a 
						href={item.href}
						onclick={() => mobileMenuOpen = false}
						class="block px-3 py-2 text-sm font-medium text-foreground/80 rounded-md hover:bg-accent hover:text-foreground transition-colors"
					>
						{item.label}
					</a>
				{/each}
				<div class="pt-3 space-y-2 border-t border-border/40">
					<div class="px-3 pb-2">
						<LanguageSwitcher />
					</div>
                    <!--
					<Button variant="ghost" size="sm" class="w-full justify-start">
						Sign In
					</Button>
					<Button size="sm" class="w-full shadow-md">
						Start Free Trial
					</Button>--->
				</div>
			</div>
		</div>
	{/if}
</header>
