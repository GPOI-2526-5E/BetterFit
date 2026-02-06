<script lang="ts">
	import { onMount } from 'svelte';
	import { Button } from '$lib/components/ui/button';
	import LanguageSwitcher from './LanguageSwitcher.svelte';
	import * as m from '$lib/paraglide/messages.js';

	let { currentPath = $bindable('/') } = $props();

	let mobileMenuOpen = $state(false);
	let theme = $state<'light' | 'dark'>('light');

	function toggleMobileMenu() {
		mobileMenuOpen = !mobileMenuOpen;
	}

	function applyTheme(nextTheme: 'light' | 'dark') {
		theme = nextTheme;
		if (typeof document !== 'undefined') {
			document.documentElement.classList.toggle('dark', nextTheme === 'dark');
		}
		try {
			localStorage.setItem('theme', nextTheme);
		} catch {
			// Ignore storage errors (private mode, blocked storage, etc.)
		}
	}

	function toggleTheme() {
		applyTheme(theme === 'dark' ? 'light' : 'dark');
	}

	onMount(() => {
		let initialTheme: 'light' | 'dark' = 'light';
		try {
			const savedTheme = localStorage.getItem('theme');
			if (savedTheme === 'light' || savedTheme === 'dark') {
				initialTheme = savedTheme;
			} else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
				initialTheme = 'dark';
			}
		} catch {
			// Ignore storage errors
		}
		applyTheme(initialTheme);
	});

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
			<Button
				variant="ghost"
				size="sm"
				onclick={toggleTheme}
				aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
			>
				{#if theme === 'dark'}
					<svg
						xmlns="http://www.w3.org/2000/svg"
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						stroke-linecap="round"
						stroke-linejoin="round"
						class="w-4 h-4"
					>
						<circle cx="12" cy="12" r="4" />
						<path d="M12 2v2" />
						<path d="M12 20v2" />
						<path d="M4.93 4.93l1.41 1.41" />
						<path d="M17.66 17.66l1.41 1.41" />
						<path d="M2 12h2" />
						<path d="M20 12h2" />
						<path d="M6.34 17.66l-1.41 1.41" />
						<path d="M19.07 4.93l-1.41 1.41" />
					</svg>
				{:else}
					<svg
						xmlns="http://www.w3.org/2000/svg"
						viewBox="0 0 24 24"
						fill="none"
						stroke="currentColor"
						stroke-width="2"
						stroke-linecap="round"
						stroke-linejoin="round"
						class="w-4 h-4"
					>
						<path d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z" />
					</svg>
				{/if}
			</Button>
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
					<div class="px-3 pb-2 flex items-center justify-between">
						<LanguageSwitcher />
						<button
							onclick={toggleTheme}
							class="flex items-center gap-2 text-sm text-foreground/80 hover:text-foreground transition-colors"
							aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
						>
							{#if theme === 'dark'}
								<svg
									xmlns="http://www.w3.org/2000/svg"
									viewBox="0 0 24 24"
									fill="none"
									stroke="currentColor"
									stroke-width="2"
									stroke-linecap="round"
									stroke-linejoin="round"
									class="w-4 h-4"
								>
									<circle cx="12" cy="12" r="4" />
									<path d="M12 2v2" />
									<path d="M12 20v2" />
									<path d="M4.93 4.93l1.41 1.41" />
									<path d="M17.66 17.66l1.41 1.41" />
									<path d="M2 12h2" />
									<path d="M20 12h2" />
									<path d="M6.34 17.66l-1.41 1.41" />
									<path d="M19.07 4.93l-1.41 1.41" />
								</svg>
								<span>Light</span>
							{:else}
								<svg
									xmlns="http://www.w3.org/2000/svg"
									viewBox="0 0 24 24"
									fill="none"
									stroke="currentColor"
									stroke-width="2"
									stroke-linecap="round"
									stroke-linejoin="round"
									class="w-4 h-4"
								>
									<path d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z" />
								</svg>
								<span>Dark</span>
							{/if}
						</button>
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
