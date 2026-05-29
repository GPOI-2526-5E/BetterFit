<script lang="ts">
	import Building2Icon from '@lucide/svelte/icons/building-2';
	import CirclePlusIcon from '@lucide/svelte/icons/circle-plus';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Empty from '$lib/components/ui/empty/index.js';
	import SetupOnboardingSheet from '$lib/components/dashboard/setup-onboarding-sheet.svelte';
	import * as m from '$lib/paraglide/messages.js';

	let {
		mode = 'empty',
		message = null,
		onRetry
	}: {
		mode?: 'empty' | 'error';
		message?: string | null;
		onRetry: () => void;
	} = $props();

	const isError = $derived(mode === 'error');
	let onboardingOpen = $state(false);
</script>

<section class="min-h-[calc(100svh-9rem)] p-4 md:p-6">
	{#if isError}
		<div class="grid h-full place-items-center">
			<Empty.Root
				class="w-full max-w-2xl rounded-[24px] border-border/80 bg-[linear-gradient(180deg,rgba(233,241,255,0.62),rgba(255,255,255,0.98))] p-8 shadow-[var(--bf-shadow-card)] md:p-10"
			>
				<Empty.Header class="max-w-xl">
					<Empty.Media
						variant="icon"
						class="mb-1 size-16 rounded-[22px] bg-primary/10 text-primary shadow-[0_10px_30px_rgba(23,105,255,0.12)] [&_svg]:size-7"
					>
						<Building2Icon class="size-7" />
					</Empty.Media>
					<Badge variant="destructive">{m.dashboard_tenants_error_badge()}</Badge>
					<Empty.Title class="text-2xl font-semibold tracking-tight">
						{m.dashboard_tenants_error_title()}
					</Empty.Title>
					<Empty.Description class="max-w-xl text-sm leading-relaxed">
						{m.dashboard_tenants_error_desc()}
					</Empty.Description>
				</Empty.Header>

				<Empty.Content class="max-w-xl gap-5">
					<div class="rounded-[16px] border border-border/70 bg-background/80 px-4 py-4">
						<p class="text-sm leading-relaxed text-muted-foreground">
							{message ?? m.dashboard_tenants_error_fallback()}
						</p>
					</div>

					<div class="flex flex-wrap justify-center gap-3">
						<Button onclick={onRetry}>
							<RefreshCwIcon class="size-4" />
							{m.common_retry()}
						</Button>
					</div>
				</Empty.Content>
			</Empty.Root>
		</div>
	{:else}
		<div class="grid h-full place-items-center">
			<div
				class="relative w-full max-w-4xl overflow-hidden rounded-[32px] border border-border/70 bg-[linear-gradient(140deg,rgba(232,240,255,0.88),rgba(255,255,255,0.96),rgba(242,251,210,0.45))] p-4 shadow-[var(--bf-shadow-card)] md:p-6"
			>
				<div
					class="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(23,105,255,0.14),transparent_32%),radial-gradient(circle_at_bottom_right,rgba(184,242,29,0.16),transparent_32%)]"
				></div>
				<Empty.Root
					class="relative rounded-[28px] border border-white/70 bg-background/86 p-6 shadow-[0_18px_48px_rgba(12,20,36,0.08)] md:p-8"
				>
					<Empty.Header class="max-w-2xl">
						<Empty.Media
							variant="icon"
							class="size-16 rounded-[22px] bg-primary/10 text-primary shadow-[0_10px_30px_rgba(23,105,255,0.12)] [&_svg]:size-7"
						>
							<Building2Icon class="size-7" />
						</Empty.Media>
						<Badge variant="secondary" class="rounded-full px-3 py-1">
							{m.dashboard_no_tenants_badge()}
						</Badge>
						<Empty.Title class="max-w-xl text-3xl font-semibold tracking-tight">
							{m.dashboard_no_tenants_title()}
						</Empty.Title>
						<Empty.Description class="max-w-2xl text-sm leading-relaxed">
							{m.dashboard_no_tenants_desc()}
						</Empty.Description>
					</Empty.Header>

					<Empty.Content class="max-w-2xl gap-5">
						<div class="grid w-full gap-3 text-left md:grid-cols-2">
							<div
								class="rounded-[20px] border border-border/70 bg-[linear-gradient(160deg,rgba(23,105,255,0.08),rgba(255,255,255,0.9))] px-4 py-4"
							>
								<p class="text-sm font-semibold">{m.dashboard_no_tenants_focus_title()}</p>
								<p class="mt-2 text-sm leading-relaxed text-muted-foreground">
									{m.dashboard_no_tenants_focus_desc()}
								</p>
							</div>

							<div class="rounded-[20px] border border-border/70 bg-background/78 px-4 py-4">
								<p class="text-sm leading-relaxed text-muted-foreground">
									{m.dashboard_no_tenants_help()}
								</p>
							</div>
						</div>

						<div class="flex flex-wrap justify-center gap-3">
							<Button onclick={() => (onboardingOpen = true)}>
								<CirclePlusIcon class="size-4" />
								{m.dashboard_no_tenants_action()}
							</Button>
							<Button variant="outline" onclick={onRetry}>
								<RefreshCwIcon class="size-4" />
								{m.dashboard_no_tenants_refresh()}
							</Button>
						</div>
					</Empty.Content>
				</Empty.Root>
			</div>

			<SetupOnboardingSheet bind:open={onboardingOpen} />
		</div>
	{/if}
</section>
