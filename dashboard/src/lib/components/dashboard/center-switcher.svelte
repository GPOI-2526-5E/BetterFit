<script lang="ts">
	import Building2Icon from '@lucide/svelte/icons/building-2';
	import CheckIcon from '@lucide/svelte/icons/check';
	import ChevronsUpDownIcon from '@lucide/svelte/icons/chevrons-up-down';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu/index.js';
	import * as Sidebar from '$lib/components/ui/sidebar/index.js';
	import { useSidebar } from '$lib/components/ui/sidebar/index.js';
	import * as m from '$lib/paraglide/messages.js';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	const center = useCenterSelectionStore();

	const sidebar = useSidebar();

	const centerStatusLabel = (status: 'operational' | 'reduced') => {
		if (status === 'operational') return m.center_status_operational();
		return m.center_status_reduced();
	};
</script>

<Sidebar.Menu>
	<Sidebar.MenuItem>
		<DropdownMenu.Root>
			<DropdownMenu.Trigger>
				{#snippet child({ props })}
					<Sidebar.MenuButton
						{...props}
						size="lg"
						class="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
					>
						<div
							class="grid size-8 place-items-center rounded-[10px] bg-sidebar-primary text-sidebar-primary-foreground"
						>
							<Building2Icon class="size-4" />
						</div>
						<div class="grid flex-1 text-start text-sm leading-tight">
							<span class="truncate font-semibold">{center.selected?.name}</span>
							<span class="truncate text-xs text-sidebar-foreground/70">{center.selected?.city}</span
							>
						</div>
						<ChevronsUpDownIcon class="ms-auto size-4" />
					</Sidebar.MenuButton>
				{/snippet}
			</DropdownMenu.Trigger>
			<DropdownMenu.Content
				class="w-(--bits-dropdown-menu-anchor-width) min-w-58 rounded-[12px]"
				align="start"
				side={sidebar.isMobile ? 'bottom' : 'right'}
				sideOffset={6}
			>
				<DropdownMenu.Label class="text-xs text-muted-foreground"
					>{m.center_switcher_title()}</DropdownMenu.Label
				>
				{#each center.centers as acenter, index (acenter.id)}
					<DropdownMenu.Item
						onclick={() => (center.selectedId = acenter.id)}
						class="flex items-center justify-between gap-3 p-2"
					>
						<div class="min-w-0 flex-1">
							<p class="truncate text-sm font-medium">{acenter.name}</p>
							<p class="truncate text-xs text-muted-foreground">
								{acenter.city} · {centerStatusLabel(acenter.status)}
							</p>
						</div>
						<div class="flex items-center gap-2">
							<DropdownMenu.Shortcut>⌘{index + 1}</DropdownMenu.Shortcut>
							{#if center.selectedId === acenter.id}
								<CheckIcon class="size-4 text-primary" />
							{/if}
						</div>
					</DropdownMenu.Item>
				{/each}
				<DropdownMenu.Separator />
				<DropdownMenu.Item class="gap-2 p-2 opacity-65" disabled>
					<div class="flex size-6 items-center justify-center rounded-md border border-border">
						<PlusIcon class="size-4" />
					</div>
					<div class="text-sm font-medium">{m.center_add()}</div>
				</DropdownMenu.Item>
			</DropdownMenu.Content>
		</DropdownMenu.Root>
	</Sidebar.MenuItem>
</Sidebar.Menu>
