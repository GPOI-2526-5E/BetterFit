<script lang="ts">
	import { goto } from '$app/navigation';
	import * as Avatar from '$lib/components/ui/avatar/index.js';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu/index.js';
	import { useUserAuthenticationStore } from '$lib/stores/AuthenticationStoreProvider.svelte';
	import BadgeCheckIcon from '@lucide/svelte/icons/badge-check';
	import BellIcon from '@lucide/svelte/icons/bell';
	import CreditCardIcon from '@lucide/svelte/icons/credit-card';
	import LogOutIcon from '@lucide/svelte/icons/log-out';
	import SparklesIcon from '@lucide/svelte/icons/sparkles';
	import * as m from '$lib/paraglide/messages.js';

	let {
		user
	}: {
		user: { name: string; email: string; avatar: string };
	} = $props();

	const auth = useUserAuthenticationStore();
	const initials = $derived(
		user.name
			.split(' ')
			.map((namePart) => namePart[0])
			.join('')
			.slice(0, 2)
			.toUpperCase()
	);

	async function handleLogout() {
		auth.logout();
		await goto('/login', { replaceState: true });
	}
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger>
		{#snippet child({ props })}
			<button
				type="button"
				class="inline-flex size-8 items-center justify-center rounded-full transition-colors outline-none hover:bg-secondary/70 focus-visible:ring-[3px] focus-visible:ring-ring/35 focus-visible:ring-offset-1"
				aria-label={m.user_account()}
				{...props}
			>
				<Avatar.Root class="size-8">
					<Avatar.Image src={user.avatar} alt={user.name} />
					<Avatar.Fallback>{initials}</Avatar.Fallback>
				</Avatar.Root>
			</button>
		{/snippet}
	</DropdownMenu.Trigger>
	<DropdownMenu.Content class="min-w-56 rounded-[12px]" align="end" sideOffset={8}>
		<DropdownMenu.Label class="p-0 font-normal">
			<div class="flex items-center gap-2 px-1 py-1.5 text-start text-sm">
				<Avatar.Root class="size-8">
					<Avatar.Image src={user.avatar} alt={user.name} />
					<Avatar.Fallback>{initials}</Avatar.Fallback>
				</Avatar.Root>
				<div class="grid flex-1 text-start text-sm leading-tight">
					<span class="truncate font-medium">{user.name}</span>
					<span class="truncate text-xs text-muted-foreground">{user.email}</span>
				</div>
			</div>
		</DropdownMenu.Label>
		<DropdownMenu.Separator />
		<DropdownMenu.Group>
			<DropdownMenu.Item>
				<SparklesIcon />
				{m.user_upgrade_pro()}
			</DropdownMenu.Item>
		</DropdownMenu.Group>
		<DropdownMenu.Separator />
		<DropdownMenu.Group>
			<DropdownMenu.Item>
				<BadgeCheckIcon />
				{m.user_account()}
			</DropdownMenu.Item>
			<DropdownMenu.Item>
				<CreditCardIcon />
				{m.user_billing()}
			</DropdownMenu.Item>
			<DropdownMenu.Item>
				<BellIcon />
				{m.user_notifications()}
			</DropdownMenu.Item>
		</DropdownMenu.Group>
		<DropdownMenu.Separator />
		<DropdownMenu.Item onclick={handleLogout}>
			<LogOutIcon />
			{m.user_logout()}
		</DropdownMenu.Item>
	</DropdownMenu.Content>
</DropdownMenu.Root>
