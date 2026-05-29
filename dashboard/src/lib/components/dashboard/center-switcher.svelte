<script lang="ts">
	import Building2Icon from '@lucide/svelte/icons/building-2';
	import CheckIcon from '@lucide/svelte/icons/check';
	import CirclePlusIcon from '@lucide/svelte/icons/circle-plus';
	import ChevronsUpDownIcon from '@lucide/svelte/icons/chevrons-up-down';
	import MapPinIcon from '@lucide/svelte/icons/map-pin';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu/index.js';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import SetupOnboardingSheet from '$lib/components/dashboard/setup-onboarding-sheet.svelte';
	import * as Sidebar from '$lib/components/ui/sidebar/index.js';
	import { useSidebar } from '$lib/components/ui/sidebar/index.js';
	import { Skeleton } from '$lib/components/ui/skeleton/index.js';
	import { cn } from '$lib/utils.js';
	import * as m from '$lib/paraglide/messages.js';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';
	import type { SetupIntent } from '$lib/utils/dashboard-onboarding-validation.js';

	const center = useCenterSelectionStore();
	const sidebar = useSidebar();
	let open = $state(false);
	let onboardingOpen = $state(false);
	let onboardingIntent = $state<SetupIntent | null>(null);
	const hasNoTenants = $derived(
		!center.isLoadingGyms && !center.gymsError && center.gyms.length === 0
	);

	const selectedLabel = $derived(
		center.selectedGym?.name ??
			(hasNoTenants ? m.center_switcher_no_tenants_short() : m.center_switcher_loading_tenants())
	);
	const selectedScopeLabel = $derived(
		center.selectedLocation?.name ??
			(hasNoTenants ? m.center_switcher_pending_scope() : m.center_switcher_all_locations())
	);

	function selectLocation(locationId: string | null) {
		center.selectLocation(locationId);
		open = false;
	}

	function locationMeta() {
		if (!center.selectedGym) {
			return '';
		}

		return m.center_switcher_locations_count({ count: center.locations.length });
	}

	function openOnboarding(intent: SetupIntent) {
		onboardingIntent = intent;
		onboardingOpen = true;
		open = false;
	}
</script>

<Sidebar.Menu>
	<Sidebar.MenuItem>
		<DropdownMenu.Root bind:open>
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
							<span class="truncate font-semibold">{selectedLabel}</span>
							<span class="truncate text-xs text-sidebar-foreground/70">{selectedScopeLabel}</span>
						</div>
						<ChevronsUpDownIcon class="ms-auto size-4" />
					</Sidebar.MenuButton>
				{/snippet}
			</DropdownMenu.Trigger>
			<DropdownMenu.Content
				class="w-[min(42rem,calc(100vw-1.5rem))] rounded-[16px] border-border/70 p-0 shadow-[var(--bf-shadow-card)]"
				align="start"
				side={sidebar.isMobile ? 'bottom' : 'right'}
				sideOffset={6}
			>
				<div class="border-b border-border/70 px-4 py-4">
					<div class="flex items-start justify-between gap-3">
						<div class="space-y-1">
							<p class="text-sm font-semibold">{m.center_switcher_title()}</p>
							<p class="text-xs leading-relaxed text-muted-foreground">
								{m.center_switcher_desc()}
							</p>
						</div>
						<Badge variant="secondary" class="shrink-0">
							{center.selectedLocation
								? m.center_switcher_scope_location()
								: m.center_switcher_scope_tenant()}
						</Badge>
					</div>
				</div>

				<div class="grid gap-0 md:grid-cols-[minmax(0,1fr)_minmax(0,1.12fr)]">
					<section class="space-y-3 p-4 md:border-r md:border-border/70">
						<div class="space-y-1">
							<div class="flex items-center gap-2">
								<Building2Icon class="size-4 text-primary" />
								<p class="text-sm font-semibold">{m.center_switcher_tenants_label()}</p>
							</div>
							<p class="text-xs leading-relaxed text-muted-foreground">
								{m.center_switcher_tenants_desc()}
							</p>
						</div>

						{#if center.isLoadingGyms}
							<div class="space-y-2">
								<Skeleton class="h-16 w-full rounded-[14px]" />
								<Skeleton class="h-16 w-full rounded-[14px]" />
							</div>
						{:else if center.gymsError}
							<div class="rounded-[14px] border border-dashed border-border/80 bg-muted/35 p-4">
								<p class="text-sm font-medium">{m.center_switcher_error_tenants()}</p>
								<p class="mt-1 text-xs text-muted-foreground">{center.gymsError}</p>
								<Button
									variant="outline"
									size="sm"
									class="mt-3"
									onclick={() => center.refetchGyms()}
								>
									{m.common_retry()}
								</Button>
							</div>
						{:else if center.gyms.length === 0}
							<div class="rounded-[14px] border border-dashed border-border/80 bg-muted/35 p-4">
								<p class="text-sm font-medium">{m.center_switcher_no_tenants()}</p>
								<p class="mt-1 text-xs text-muted-foreground">
									{m.center_switcher_no_tenants_desc()}
								</p>
								<Button
									variant="outline"
									size="sm"
									class="mt-3"
									onclick={() => center.refetchGyms()}
								>
									{m.dashboard_no_tenants_refresh()}
								</Button>
							</div>
						{:else}
							<div class="space-y-2">
								{#each center.gyms as gym (gym.id)}
									<button
										type="button"
										onclick={() => center.selectGym(gym.id)}
										class={cn(
											'w-full rounded-[14px] border px-3 py-3 text-left transition-colors',
											center.selectedGymId === gym.id
												? 'border-primary/35 bg-primary/8 shadow-[0_0_0_1px_rgba(14,99,255,0.08)]'
												: 'border-border/70 bg-background hover:border-primary/25 hover:bg-secondary/35'
										)}
									>
										<div class="flex items-start justify-between gap-3">
											<div class="min-w-0">
												<p class="truncate text-sm font-semibold">{gym.name}</p>
												<p class="mt-1 text-xs text-muted-foreground">
													{m.center_switcher_tenant_hint()}
												</p>
											</div>
											{#if center.selectedGymId === gym.id}
												<CheckIcon class="mt-0.5 size-4 shrink-0 text-primary" />
											{/if}
										</div>
									</button>
								{/each}
							</div>
						{/if}

						<div class="rounded-[16px] border border-dashed border-border/70 bg-background/80 p-3">
							<p class="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
								{m.center_switcher_setup_group()}
							</p>
							<p class="mt-2 text-xs leading-relaxed text-muted-foreground">
								{m.center_switcher_setup_tenant_desc()}
							</p>
							<Button
								variant="outline"
								size="sm"
								class="mt-3 w-full justify-center"
								onclick={() => openOnboarding('tenant')}
							>
								<CirclePlusIcon class="size-4" />
								{m.center_switcher_setup_tenant_action()}
							</Button>
						</div>
					</section>

					<section class="space-y-3 p-4">
						<div class="space-y-1">
							<div class="flex items-center gap-2">
								<MapPinIcon class="size-4 text-primary" />
								<p class="text-sm font-semibold">{m.center_switcher_locations_label()}</p>
							</div>
							<p class="text-xs leading-relaxed text-muted-foreground">
								{#if center.selectedGym}
									{m.center_switcher_locations_desc({ gym: center.selectedGym.name })}
								{:else}
									{m.center_switcher_locations_waiting()}
								{/if}
							</p>
						</div>

						{#if center.selectedGym}
							<div
								class="flex items-center justify-between gap-3 rounded-[14px] bg-secondary/40 px-3 py-2"
							>
								<div class="min-w-0">
									<p class="truncate text-sm font-semibold">{center.selectedGym.name}</p>
									<p class="text-xs text-muted-foreground">{locationMeta()}</p>
								</div>
								<Badge variant="outline">{m.center_switcher_scope_tenant()}</Badge>
							</div>
						{/if}

						{#if center.isLoadingLocations}
							<div class="space-y-2">
								<Skeleton class="h-14 w-full rounded-[14px]" />
								<Skeleton class="h-14 w-full rounded-[14px]" />
							</div>
						{:else if center.locationsError}
							<div class="rounded-[14px] border border-dashed border-border/80 bg-muted/35 p-4">
								<p class="text-sm font-medium">{m.center_switcher_error_locations()}</p>
								<p class="mt-1 text-xs text-muted-foreground">{center.locationsError}</p>
								<Button
									variant="outline"
									size="sm"
									class="mt-3"
									onclick={() => center.refetchLocations()}
								>
									{m.common_retry()}
								</Button>
							</div>
						{:else if !center.selectedGym}
							<div class="rounded-[14px] border border-dashed border-border/80 bg-muted/35 p-4">
								<p class="text-sm font-medium">{m.center_switcher_locations_waiting()}</p>
							</div>
						{:else}
							<div class="space-y-2">
								<button
									type="button"
									onclick={() => selectLocation(null)}
									class={cn(
										'w-full rounded-[14px] border px-3 py-3 text-left transition-colors',
										center.selectedLocationId === null
											? 'border-primary/35 bg-primary/8 shadow-[0_0_0_1px_rgba(14,99,255,0.08)]'
											: 'border-border/70 bg-background hover:border-primary/25 hover:bg-secondary/35'
									)}
								>
									<div class="flex items-start justify-between gap-3">
										<div class="min-w-0">
											<p class="truncate text-sm font-semibold">
												{m.center_switcher_all_locations()}
											</p>
											<p class="mt-1 text-xs text-muted-foreground">
												{m.center_switcher_all_locations_desc()}
											</p>
										</div>
										{#if center.selectedLocationId === null}
											<CheckIcon class="mt-0.5 size-4 shrink-0 text-primary" />
										{/if}
									</div>
								</button>

								{#if center.locations.length === 0}
									<div class="rounded-[14px] border border-dashed border-border/80 bg-muted/35 p-4">
										<p class="text-sm font-medium">{m.center_switcher_no_locations()}</p>
									</div>
								{:else}
									{#each center.locations as location (location.id)}
										<button
											type="button"
											onclick={() => selectLocation(location.id)}
											class={cn(
												'w-full rounded-[14px] border px-3 py-3 text-left transition-colors',
												center.selectedLocationId === location.id
													? 'border-primary/35 bg-primary/8 shadow-[0_0_0_1px_rgba(14,99,255,0.08)]'
													: 'border-border/70 bg-background hover:border-primary/25 hover:bg-secondary/35'
											)}
										>
											<div class="flex items-start justify-between gap-3">
												<div class="min-w-0">
													<div class="flex flex-wrap items-center gap-2">
														<p class="truncate text-sm font-semibold">{location.name}</p>
														{#if !location.isActive}
															<Badge variant="outline"
																>{m.center_switcher_location_inactive()}</Badge
															>
														{/if}
													</div>
													<p class="mt-1 truncate text-xs text-muted-foreground">
														{[location.city, location.addressLine1, location.code]
															.filter(Boolean)
															.join(' · ')}
													</p>
												</div>
												{#if center.selectedLocationId === location.id}
													<CheckIcon class="mt-0.5 size-4 shrink-0 text-primary" />
												{/if}
											</div>
										</button>
									{/each}
								{/if}
							</div>
						{/if}

						<div class="rounded-[16px] border border-dashed border-border/70 bg-background/80 p-3">
							<p class="text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
								{m.center_switcher_setup_group()}
							</p>
							<p class="mt-2 text-xs leading-relaxed text-muted-foreground">
								{#if center.selectedGym}
									{m.center_switcher_setup_location_desc({ gym: center.selectedGym.name })}
								{:else}
									{m.center_switcher_setup_location_waiting()}
								{/if}
							</p>
							<Button
								variant="outline"
								size="sm"
								class="mt-3 w-full justify-center"
								onclick={() => openOnboarding('location')}
								disabled={!center.selectedGym}
							>
								<CirclePlusIcon class="size-4" />
								{m.center_switcher_setup_location_action()}
							</Button>
						</div>
					</section>
				</div>
			</DropdownMenu.Content>
		</DropdownMenu.Root>
	</Sidebar.MenuItem>
</Sidebar.Menu>

<SetupOnboardingSheet bind:open={onboardingOpen} lockedIntent={onboardingIntent} />
