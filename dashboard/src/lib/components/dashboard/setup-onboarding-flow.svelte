<script lang="ts">
	import ArrowLeftIcon from '@lucide/svelte/icons/arrow-left';
	import ArrowRightIcon from '@lucide/svelte/icons/arrow-right';
	import Building2Icon from '@lucide/svelte/icons/building-2';
	import CheckIcon from '@lucide/svelte/icons/check';
	import CirclePlusIcon from '@lucide/svelte/icons/circle-plus';
	import LoaderCircleIcon from '@lucide/svelte/icons/loader-circle';
	import MapPinnedIcon from '@lucide/svelte/icons/map-pinned';
	import SparklesIcon from '@lucide/svelte/icons/sparkles';
	import { onDestroy, tick, untrack } from 'svelte';
	import { Tween, prefersReducedMotion } from 'svelte/motion';
	import { fly } from 'svelte/transition';
	import { cubicOut, sineOut } from 'svelte/easing';
	import { get } from 'svelte/store';
	import { superForm } from 'sveltekit-superforms';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Form from '$lib/components/ui/form/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import * as m from '$lib/paraglide/messages.js';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import {
		useCenterSelectionStore,
		type TenantGym
	} from '$lib/stores/CenterSelectionStoreProvider.svelte';
	import {
		createInitialOnboardingFormData,
		extractOnboardingErrorMessage,
		hasOnboardingErrors,
		toLocationPayload,
		validateOnboardingDetailsStep,
		validateOnboardingIntentStep,
		validateOnboardingLocationStep,
		type SetupIntent
	} from '$lib/utils/dashboard-onboarding-validation.js';
	import { cn } from '$lib/utils.js';

	type SetupCompletion = {
		gymId: string;
		locationId: string | null;
	};

	let {
		surface = 'inline',
		lockedIntent = null,
		showDismissAction = false,
		onClose,
		onComplete
	}: {
		surface?: 'inline' | 'sheet';
		lockedIntent?: SetupIntent | null;
		showDismissAction?: boolean;
		onClose?: () => void;
		onComplete?: (completion: SetupCompletion) => void | Promise<void>;
	} = $props();

	const api = useApiClient();
	const center = useCenterSelectionStore();
	const initialLockedIntent = untrack(() => lockedIntent);

	const initialTargetGymId = center.selectedGymId ?? center.gyms[0]?.id ?? null;
	const onboardingForm = superForm(
		createInitialOnboardingFormData(initialLockedIntent, initialTargetGymId)
	);

	const { form: onboardingFormData, errors: onboardingFormErrors } = onboardingForm;

	let step = $state(1);
	let direction = $state<'forward' | 'backward'>('forward');
	let creationState = $state<'idle' | 'loading' | 'success' | 'error'>('idle');
	let creationMessage = $state('');
	let progress = $state(12);
	let createdGymId = $state<string | null>(null);
	let createdLocationId = $state<string | null>(null);
	let createdGymName = $state('');
	let createdLocationName = $state('');
	let stepViewport: HTMLDivElement | null = null;
	let activeStepObserver: ResizeObserver | null = null;
	let stepSyncVersion = 0;
	let hasMeasuredViewport = $state(false);

	const defaultTransitionDuration = 280;
	const viewportHeightTween = new Tween(0, {
		duration: defaultTransitionDuration,
		easing: sineOut
	});

	const detailsStepNumber = initialLockedIntent ? 1 : 2;
	const setupStepNumber = initialLockedIntent ? 2 : 3;
	const createStepNumber = initialLockedIntent ? 3 : 4;

	const availableTenants = $derived(center.gyms);
	const effectiveIntent = $derived(
		(initialLockedIntent ?? $onboardingFormData.intent) as SetupIntent | ''
	);
	const requiresLocationStep = $derived(
		effectiveIntent === 'location' ||
			(effectiveIntent === 'tenant' && $onboardingFormData.includeFirstLocation)
	);
	const totalSteps = $derived(initialLockedIntent ? 3 : 4);
	const canCreateLocation = $derived(availableTenants.length > 0);
	const selectedTargetTenant = $derived(
		availableTenants.find((tenant) => tenant.id === $onboardingFormData.targetGymId) ?? null
	);
	const progressSteps = $derived.by(() => {
		const steps = [];
		if (!initialLockedIntent) {
			steps.push(m.setup_flow_progress_type());
		}

		steps.push(
			effectiveIntent === 'location'
				? m.setup_flow_progress_tenant_pick()
				: m.setup_flow_progress_tenant_details()
		);
		steps.push(
			requiresLocationStep
				? m.setup_flow_progress_location_details()
				: m.setup_flow_progress_review()
		);
		steps.push(m.setup_flow_progress_finish());

		return steps;
	});
	const stepTitle = $derived.by(() => {
		if (!initialLockedIntent && step === 1) {
			return m.setup_flow_step_type_title();
		}

		if (step === detailsStepNumber) {
			return effectiveIntent === 'location'
				? m.setup_flow_step_target_tenant_title()
				: m.setup_flow_step_tenant_title();
		}

		if (step === setupStepNumber) {
			return requiresLocationStep
				? m.setup_flow_step_location_title()
				: m.setup_flow_step_review_title();
		}

		return creationState === 'success'
			? m.setup_flow_success_title()
			: creationState === 'error'
				? m.setup_flow_error_title()
				: m.setup_flow_creating_title();
	});
	const stepDescription = $derived.by(() => {
		if (!initialLockedIntent && step === 1) {
			return m.setup_flow_step_type_desc();
		}

		if (step === detailsStepNumber) {
			return effectiveIntent === 'location'
				? m.setup_flow_step_target_tenant_desc()
				: m.setup_flow_step_tenant_desc();
		}

		if (step === setupStepNumber) {
			return requiresLocationStep
				? effectiveIntent === 'tenant'
					? m.setup_flow_step_first_location_desc()
					: m.setup_flow_step_location_desc()
				: m.setup_flow_step_review_desc();
		}

		return creationState === 'success'
			? m.setup_flow_success_desc()
			: creationState === 'error'
				? creationMessage || m.setup_flow_error_generic()
				: m.setup_flow_creating_desc();
	});
	const primaryButtonLabel = $derived.by(() => {
		if (step === createStepNumber) {
			if (creationState === 'success') {
				return m.setup_flow_action_finish();
			}
			if (creationState === 'error') {
				return m.common_retry();
			}
			return m.setup_flow_action_creating();
		}

		if (step === setupStepNumber) {
			if (effectiveIntent === 'location') {
				return m.setup_flow_action_create_location();
			}

			return requiresLocationStep
				? m.setup_flow_action_create_tenant_and_location()
				: m.setup_flow_action_create_tenant();
		}

		return m.setup_flow_action_continue();
	});
	const isBusy = $derived(creationState === 'loading');
	const renderedViewportHeight = $derived(
		hasMeasuredViewport ? `${Math.max(Math.round(viewportHeightTween.current), 0)}px` : 'auto'
	);

	function clearErrors(fields: string[]) {
		onboardingFormErrors.update((current) => {
			const next = { ...current };
			for (const field of fields) {
				delete next[field as keyof typeof next];
			}
			return next;
		});
	}

	function nextStep() {
		moveToStep(step + 1, 'forward');
	}

	function previousStep() {
		moveToStep(step - 1, 'backward');
	}

	function selectIntent(intent: SetupIntent) {
		onboardingFormData.update((data) => ({
			...data,
			intent,
			targetGymId:
				intent === 'location'
					? data.targetGymId || center.selectedGymId || availableTenants[0]?.id || ''
					: data.targetGymId
		}));
		clearErrors(['intent']);
	}

	function selectTargetTenant(tenant: TenantGym) {
		onboardingFormData.update((data) => ({
			...data,
			targetGymId: tenant.id
		}));
		clearErrors(['targetGymId']);
	}

	function setIncludeFirstLocation(includeFirstLocation: boolean) {
		onboardingFormData.update((data) => ({
			...data,
			includeFirstLocation
		}));
	}

	function resetCreationState() {
		creationState = 'idle';
		creationMessage = '';
		progress = 12;
		createdGymId = null;
		createdLocationId = null;
		createdGymName = '';
		createdLocationName = '';
	}

	function getTransitionParams(inOrOut: 'in' | 'out', currentDirection: 'forward' | 'backward') {
		const x =
			inOrOut === 'in'
				? currentDirection === 'forward'
					? 24
					: -24
				: currentDirection === 'forward'
					? -24
					: 24;

		return {
			x,
			duration: defaultTransitionDuration,
			easing: inOrOut === 'in' ? cubicOut : sineOut
		};
	}

	function disconnectActiveStepObserver() {
		activeStepObserver?.disconnect();
		activeStepObserver = null;
	}

	function setViewportHeight(nextHeight: number, instant = false) {
		hasMeasuredViewport = true;
		void viewportHeightTween.set(Math.max(Math.round(nextHeight), 0), {
			duration: instant || prefersReducedMotion.current ? 0 : defaultTransitionDuration,
			easing: sineOut
		});
	}

	function observeActiveStep(targetStep: number, instant = false) {
		const syncVersion = ++stepSyncVersion;
		disconnectActiveStepObserver();

		void tick().then(() => {
			requestAnimationFrame(() => {
				if (syncVersion !== stepSyncVersion) {
					return;
				}

				const activeStep = stepViewport?.querySelector<HTMLElement>(
					`[data-step-panel="${targetStep}"]`
				);

				if (!activeStep) {
					return;
				}

				setViewportHeight(activeStep.scrollHeight, instant || !hasMeasuredViewport);

				if (typeof ResizeObserver === 'undefined') {
					return;
				}

				activeStepObserver = new ResizeObserver(() => {
					if (syncVersion !== stepSyncVersion) {
						return;
					}

					setViewportHeight(activeStep.scrollHeight);
				});

				activeStepObserver.observe(activeStep);
			});
		});
	}

	function moveToStep(targetStep: number, nextDirection: 'forward' | 'backward') {
		direction = nextDirection;
		step = targetStep;
	}

	function goBackOrClose() {
		if (isBusy) {
			return;
		}

		if (step === 1 && showDismissAction) {
			onClose?.();
			return;
		}

		if (step > 1) {
			if (step === createStepNumber) {
				resetCreationState();
			}
			previousStep();
		}
	}

	function validateCurrentStep() {
		const values = get(onboardingFormData);
		const errors =
			!initialLockedIntent && step === 1
				? validateOnboardingIntentStep(values, availableTenants.length)
				: step === detailsStepNumber
					? validateOnboardingDetailsStep({
							...values,
							intent: effectiveIntent
						})
					: step === setupStepNumber
						? validateOnboardingLocationStep({
								...values,
								intent: effectiveIntent
							})
						: {};

		onboardingFormErrors.set(errors, { force: true });
		return !hasOnboardingErrors(errors);
	}

	async function finishSetup() {
		if (!createdGymId) {
			return;
		}

		await center.refetchGyms();
		center.selectGym(createdGymId);
		await center.refetchLocations();

		if (createdLocationId) {
			center.selectLocation(createdLocationId);
		} else {
			center.selectLocation(null);
		}

		await onComplete?.({
			gymId: createdGymId,
			locationId: createdLocationId
		});
		onClose?.();
	}

	async function handlePrimaryAction() {
		if (step === createStepNumber) {
			if (creationState === 'success') {
				await finishSetup();
				return;
			}

			if (creationState === 'error') {
				resetCreationState();
				await runCreation();
			}

			return;
		}

		if (!validateCurrentStep()) {
			return;
		}

		if (step === setupStepNumber) {
			nextStep();
			await tick();
			await runCreation();
			return;
		}

		nextStep();
	}

	async function runCreation() {
		const values = get(onboardingFormData);
		const tenantName = values.tenantName.trim();
		const fallbackErrorMessage = m.setup_flow_error_generic();
		let progressInterval: ReturnType<typeof setInterval> | null = null;

		creationState = 'loading';
		creationMessage = '';
		progress = 14;
		progressInterval = setInterval(() => {
			if (progress < 92) {
				progress = Math.min(progress + Math.floor(Math.random() * 11) + 4, 92);
			}
		}, 420);

		try {
			if (effectiveIntent === 'tenant') {
				const tenantResponse = await api.client.apiGymsPost({
					createGymRequest: {
						name: tenantName
					}
				});

				if (!tenantResponse.success || !tenantResponse.data?.id) {
					throw new Error(tenantResponse.message ?? fallbackErrorMessage);
				}

				createdGymId = tenantResponse.data.id;
				createdGymName = tenantResponse.data.name ?? tenantName;

				if (values.includeFirstLocation) {
					const locationResponse = await api.client.apiGymsGymIdLocationsPost({
						gymId: tenantResponse.data.id,
						createGymLocationRequest: toLocationPayload(values)
					});

					if (!locationResponse.success || !locationResponse.data?.id) {
						throw new Error(locationResponse.message ?? fallbackErrorMessage);
					}

					createdLocationId = locationResponse.data.id;
					createdLocationName = locationResponse.data.name ?? values.locationName.trim();
				}
			} else {
				const targetGymId = values.targetGymId;
				const locationResponse = await api.client.apiGymsGymIdLocationsPost({
					gymId: targetGymId,
					createGymLocationRequest: toLocationPayload(values)
				});

				if (!locationResponse.success || !locationResponse.data?.id) {
					throw new Error(locationResponse.message ?? fallbackErrorMessage);
				}

				createdGymId = targetGymId;
				createdLocationId = locationResponse.data.id;
				createdGymName = selectedTargetTenant?.name ?? '';
				createdLocationName = locationResponse.data.name ?? values.locationName.trim();
			}

			progress = 100;
			creationState = 'success';
		} catch (error) {
			creationMessage = await extractOnboardingErrorMessage(error, fallbackErrorMessage);
			creationState = 'error';
		} finally {
			if (progressInterval) {
				clearInterval(progressInterval);
			}
		}
	}

	$effect(() => {
		step;
		observeActiveStep(
			step,
			untrack(() => !hasMeasuredViewport)
		);
	});

	onDestroy(() => {
		stepSyncVersion += 1;
		disconnectActiveStepObserver();
	});
</script>

<div
	class={cn(
		'relative overflow-hidden rounded-[28px] border border-border/70 bg-[linear-gradient(180deg,rgba(255,255,255,0.98),rgba(242,247,255,0.96))] shadow-[var(--bf-shadow-card)]',
		surface === 'inline' ? 'min-h-[34rem]' : 'min-h-[32rem]'
	)}
>
	<div
		class="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(23,105,255,0.18),transparent_42%),radial-gradient(circle_at_bottom_right,rgba(184,242,29,0.14),transparent_38%)]"
	></div>

	<div class="relative flex h-full flex-col p-5 md:p-7">
		<div class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
			<div class="min-w-0 space-y-2 md:flex-1 md:pr-6">
				<Badge variant="secondary" class="w-fit gap-1 rounded-full px-3 py-1">
					<SparklesIcon class="size-3.5" />
					{m.setup_flow_badge()}
				</Badge>
				<div class="space-y-1">
					<h3 class="text-xl font-semibold tracking-tight">{stepTitle}</h3>
					<p class="max-w-xl text-sm leading-relaxed text-muted-foreground">{stepDescription}</p>
				</div>
			</div>

			<div
				class="rounded-full border border-border/70 bg-background/80 px-3 py-1 text-xs font-medium text-muted-foreground"
			>
				{m.setup_flow_progress_counter({ current: step, total: totalSteps })}
			</div>
		</div>

		<div class="mt-5 flex flex-wrap items-center gap-2">
			{#each progressSteps as progressStep, index (progressStep)}
				<div class="flex items-center gap-2">
					<div
						class={cn(
							'grid h-8 min-w-8 place-items-center rounded-full border px-2 text-xs font-semibold transition-colors',
							step > index + 1
								? 'border-primary/20 bg-primary text-primary-foreground'
								: step === index + 1
									? 'border-primary/35 bg-primary/12 text-primary'
									: 'border-border/70 bg-background/70 text-muted-foreground'
						)}
						aria-current={step === index + 1 ? 'step' : undefined}
					>
						{#if step > index + 1}
							<CheckIcon class="size-3.5" />
						{:else}
							{index + 1}
						{/if}
					</div>
					<span class="hidden text-xs text-muted-foreground md:inline">{progressStep}</span>
				</div>
			{/each}
		</div>

		{#snippet stepActions()}
			<div
				class="mt-6 flex flex-wrap items-center justify-between gap-3 border-t border-border/70 pt-5"
			>
				<div class="text-xs leading-relaxed text-muted-foreground">
					{m.setup_flow_footer_note()}
				</div>

				<div class="flex flex-wrap items-center gap-2">
					<Button
						type="button"
						variant="outline"
						onclick={goBackOrClose}
						disabled={isBusy || (!showDismissAction && step === 1)}
					>
						<ArrowLeftIcon class="size-4" />
						{step === 1 && showDismissAction
							? m.setup_flow_action_cancel()
							: m.setup_flow_action_back()}
					</Button>
					<Button type="button" onclick={handlePrimaryAction} disabled={isBusy}>
						{#if isBusy}
							<LoaderCircleIcon class="size-4 animate-spin" />
						{:else if creationState === 'success'}
							<CheckIcon class="size-4" />
						{:else}
							<CirclePlusIcon class="size-4" />
						{/if}
						{primaryButtonLabel}
						{#if step !== createStepNumber}
							<ArrowRightIcon class="size-4" />
						{/if}
					</Button>
				</div>
			</div>
		{/snippet}

		<div class="relative mt-6 flex-1 overflow-hidden">
			<div bind:this={stepViewport} class="step-stage" style={`height: ${renderedViewportHeight};`}>
				{#if !initialLockedIntent && step === 1}
					<div
						in:fly|local={getTransitionParams('in', direction)}
						out:fly|local={getTransitionParams('out', direction)}
						class="step-panel space-y-6"
						data-step-panel="1"
					>
						<div class="grid gap-3 md:grid-cols-2">
							<button
								type="button"
								class={cn(
									'rounded-[24px] border p-5 text-left transition-all',
									effectiveIntent === 'tenant'
										? 'border-primary/35 bg-primary/8 shadow-[0_0_0_1px_rgba(23,105,255,0.08)]'
										: 'border-border/70 bg-background/80 hover:border-primary/20 hover:bg-primary/6'
								)}
								onclick={() => selectIntent('tenant')}
							>
								<div class="flex items-start justify-between gap-3">
									<div
										class="grid size-11 place-items-center rounded-[16px] bg-primary/12 text-primary"
									>
										<Building2Icon class="size-5" />
									</div>
									{#if effectiveIntent === 'tenant'}
										<div
											class="grid size-7 place-items-center rounded-full bg-primary text-primary-foreground"
										>
											<CheckIcon class="size-4" />
										</div>
									{/if}
								</div>
								<div class="mt-5 space-y-2">
									<p class="text-base font-semibold">{m.setup_flow_type_tenant_title()}</p>
									<p class="text-sm leading-relaxed text-muted-foreground">
										{m.setup_flow_type_tenant_desc()}
									</p>
									<p class="text-xs text-muted-foreground">
										{m.setup_flow_type_tenant_help()}
									</p>
								</div>
							</button>

							<button
								type="button"
								class={cn(
									'rounded-[24px] border p-5 text-left transition-all',
									!canCreateLocation && 'cursor-not-allowed opacity-60',
									effectiveIntent === 'location'
										? 'border-primary/35 bg-primary/8 shadow-[0_0_0_1px_rgba(23,105,255,0.08)]'
										: 'border-border/70 bg-background/80 hover:border-primary/20 hover:bg-primary/6'
								)}
								onclick={() => canCreateLocation && selectIntent('location')}
								disabled={!canCreateLocation}
							>
								<div class="flex items-start justify-between gap-3">
									<div
										class="grid size-11 place-items-center rounded-[16px] bg-accent/25 text-foreground"
									>
										<MapPinnedIcon class="size-5" />
									</div>
									{#if !canCreateLocation}
										<Badge variant="outline" class="rounded-full">
											{m.setup_flow_type_location_disabled()}
										</Badge>
									{:else if effectiveIntent === 'location'}
										<div
											class="grid size-7 place-items-center rounded-full bg-primary text-primary-foreground"
										>
											<CheckIcon class="size-4" />
										</div>
									{/if}
								</div>
								<div class="mt-5 space-y-2">
									<p class="text-base font-semibold">{m.setup_flow_type_location_title()}</p>
									<p class="text-sm leading-relaxed text-muted-foreground">
										{m.setup_flow_type_location_desc()}
									</p>
									<p class="text-xs text-muted-foreground">
										{m.setup_flow_type_location_help()}
									</p>
								</div>
							</button>
						</div>

						{#if $onboardingFormErrors.intent}
							<p class="text-sm font-medium text-destructive">
								{$onboardingFormErrors.intent[0]}
							</p>
						{/if}

						{@render stepActions()}
					</div>
				{/if}

				{#if step === detailsStepNumber}
					<div
						in:fly|local={getTransitionParams('in', direction)}
						out:fly|local={getTransitionParams('out', direction)}
						class="step-panel space-y-6"
						data-step-panel={detailsStepNumber}
					>
						{#if effectiveIntent === 'tenant'}
							<div class="grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,0.88fr)]">
								<div class="space-y-4">
									<Form.Field form={onboardingForm} name="tenantName">
										<Form.Control>
											{#snippet children({ props })}
												<Form.Label>{m.setup_flow_tenant_name_label()}</Form.Label>
												<Input
													{...props}
													bind:value={$onboardingFormData.tenantName}
													oninput={() => clearErrors(['tenantName'])}
													placeholder={m.setup_flow_tenant_name_placeholder()}
													disabled={isBusy}
												/>
											{/snippet}
										</Form.Control>
										<Form.FieldErrors />
									</Form.Field>

									<div class="rounded-[22px] border border-border/70 bg-background/78 p-4">
										<p class="text-sm font-semibold">{m.setup_flow_tenant_note_title()}</p>
										<p class="mt-2 text-sm leading-relaxed text-muted-foreground">
											{m.setup_flow_tenant_note_desc()}
										</p>
									</div>
								</div>

								<div class="space-y-3">
									<p class="text-sm font-semibold">{m.setup_flow_scope_title()}</p>
									<div class="space-y-3">
										<button
											type="button"
											class={cn(
												'w-full rounded-[22px] border p-4 text-left transition-all',
												!$onboardingFormData.includeFirstLocation
													? 'border-primary/35 bg-primary/8'
													: 'border-border/70 bg-background/80 hover:border-primary/20 hover:bg-primary/6'
											)}
											onclick={() => setIncludeFirstLocation(false)}
										>
											<p class="text-sm font-semibold">{m.setup_flow_scope_tenant_only_title()}</p>
											<p class="mt-1 text-xs leading-relaxed text-muted-foreground">
												{m.setup_flow_scope_tenant_only_desc()}
											</p>
										</button>

										<button
											type="button"
											class={cn(
												'w-full rounded-[22px] border p-4 text-left transition-all',
												$onboardingFormData.includeFirstLocation
													? 'border-primary/35 bg-primary/8'
													: 'border-border/70 bg-background/80 hover:border-primary/20 hover:bg-primary/6'
											)}
											onclick={() => setIncludeFirstLocation(true)}
										>
											<p class="text-sm font-semibold">
												{m.setup_flow_scope_tenant_and_location_title()}
											</p>
											<p class="mt-1 text-xs leading-relaxed text-muted-foreground">
												{m.setup_flow_scope_tenant_and_location_desc()}
											</p>
										</button>
									</div>
								</div>
							</div>
						{:else}
							<div class="space-y-4">
								<div class="rounded-[22px] border border-border/70 bg-background/78 p-4">
									<p class="text-sm font-semibold">{m.setup_flow_target_tenant_hint_title()}</p>
									<p class="mt-2 text-sm leading-relaxed text-muted-foreground">
										{m.setup_flow_target_tenant_hint_desc()}
									</p>
								</div>

								<div class="grid gap-3 md:grid-cols-2">
									{#each availableTenants as tenant (tenant.id)}
										<button
											type="button"
											class={cn(
												'rounded-[22px] border p-4 text-left transition-all',
												$onboardingFormData.targetGymId === tenant.id
													? 'border-primary/35 bg-primary/8 shadow-[0_0_0_1px_rgba(23,105,255,0.08)]'
													: 'border-border/70 bg-background/80 hover:border-primary/20 hover:bg-primary/6'
											)}
											onclick={() => selectTargetTenant(tenant)}
										>
											<div class="flex items-start justify-between gap-3">
												<div class="min-w-0">
													<p class="truncate text-sm font-semibold">{tenant.name}</p>
													<p class="mt-1 text-xs text-muted-foreground">
														{m.center_switcher_tenant_hint()}
													</p>
												</div>
												{#if $onboardingFormData.targetGymId === tenant.id}
													<CheckIcon class="size-4 shrink-0 text-primary" />
												{/if}
											</div>
										</button>
									{/each}
								</div>

								{#if $onboardingFormErrors.targetGymId}
									<p class="text-sm font-medium text-destructive">
										{$onboardingFormErrors.targetGymId[0]}
									</p>
								{/if}
							</div>
						{/if}

						{@render stepActions()}
					</div>
				{/if}

				{#if step === setupStepNumber}
					<div
						in:fly|local={getTransitionParams('in', direction)}
						out:fly|local={getTransitionParams('out', direction)}
						class="step-panel space-y-6"
						data-step-panel={setupStepNumber}
					>
						{#if requiresLocationStep}
							<div class="grid gap-4 md:grid-cols-2">
								<Form.Field form={onboardingForm} name="locationName">
									<Form.Control>
										{#snippet children({ props })}
											<Form.Label>{m.setup_flow_location_name_label()}</Form.Label>
											<Input
												{...props}
												bind:value={$onboardingFormData.locationName}
												oninput={() => clearErrors(['locationName'])}
												placeholder={m.setup_flow_location_name_placeholder()}
												disabled={isBusy}
											/>
										{/snippet}
									</Form.Control>
									<Form.FieldErrors />
								</Form.Field>

								<Form.Field form={onboardingForm} name="locationCode">
									<Form.Control>
										{#snippet children({ props })}
											<Form.Label>{m.setup_flow_location_code_label()}</Form.Label>
											<Input
												{...props}
												bind:value={$onboardingFormData.locationCode}
												oninput={() => clearErrors(['locationCode'])}
												placeholder={m.setup_flow_location_code_placeholder()}
												disabled={isBusy}
											/>
										{/snippet}
									</Form.Control>
									<Form.FieldErrors />
								</Form.Field>

								<Form.Field form={onboardingForm} name="city">
									<Form.Control>
										{#snippet children({ props })}
											<Form.Label>{m.setup_flow_location_city_label()}</Form.Label>
											<Input
												{...props}
												bind:value={$onboardingFormData.city}
												oninput={() => clearErrors(['city'])}
												placeholder={m.setup_flow_location_city_placeholder()}
												disabled={isBusy}
											/>
										{/snippet}
									</Form.Control>
									<Form.FieldErrors />
								</Form.Field>

								<Form.Field form={onboardingForm} name="countryCode">
									<Form.Control>
										{#snippet children({ props })}
											<Form.Label>{m.setup_flow_location_country_label()}</Form.Label>
											<Input
												{...props}
												bind:value={$onboardingFormData.countryCode}
												oninput={() => clearErrors(['countryCode'])}
												placeholder={m.setup_flow_location_country_placeholder()}
												disabled={isBusy}
												maxlength={2}
											/>
										{/snippet}
									</Form.Control>
									<Form.FieldErrors />
								</Form.Field>

								<div class="md:col-span-2">
									<Form.Field form={onboardingForm} name="addressLine1">
										<Form.Control>
											{#snippet children({ props })}
												<Form.Label>{m.setup_flow_location_address_label()}</Form.Label>
												<Input
													{...props}
													bind:value={$onboardingFormData.addressLine1}
													oninput={() => clearErrors(['addressLine1'])}
													placeholder={m.setup_flow_location_address_placeholder()}
													disabled={isBusy}
												/>
											{/snippet}
										</Form.Control>
										<Form.FieldErrors />
									</Form.Field>
								</div>
							</div>
						{:else}
							<div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,0.88fr)]">
								<div class="rounded-[24px] border border-border/70 bg-background/82 p-5">
									<div class="flex items-center gap-2">
										<Building2Icon class="size-4 text-primary" />
										<p class="text-sm font-semibold">{m.setup_flow_review_tenant_label()}</p>
									</div>
									<p class="mt-4 text-2xl font-semibold tracking-tight">
										{$onboardingFormData.tenantName || m.setup_flow_review_placeholder()}
									</p>
									<p class="mt-2 text-sm leading-relaxed text-muted-foreground">
										{m.setup_flow_review_tenant_desc()}
									</p>
								</div>

								<div
									class="rounded-[24px] border border-border/70 bg-[linear-gradient(160deg,rgba(23,105,255,0.12),rgba(255,255,255,0.92))] p-5"
								>
									<Badge variant="outline" class="rounded-full">
										{m.setup_flow_scope_tenant_only_title()}
									</Badge>
									<p class="mt-4 text-base font-semibold">
										{m.setup_flow_review_next_steps_title()}
									</p>
									<p class="mt-2 text-sm leading-relaxed text-muted-foreground">
										{m.setup_flow_review_next_steps_desc()}
									</p>
								</div>
							</div>
						{/if}

						{@render stepActions()}
					</div>
				{/if}

				{#if step === createStepNumber}
					<div
						in:fly|local={getTransitionParams('in', direction)}
						out:fly|local={getTransitionParams('out', direction)}
						class="step-panel space-y-6"
						data-step-panel={createStepNumber}
					>
						{#if creationState === 'loading'}
							<div class="space-y-5 py-6">
								<div class="flex flex-col items-center justify-center text-center">
									<div
										class="grid size-18 place-items-center rounded-full bg-primary/10 text-primary"
									>
										<LoaderCircleIcon class="size-8 animate-spin" />
									</div>
									<p class="mt-5 text-lg font-semibold">{m.setup_flow_creating_title()}</p>
									<p class="mt-2 max-w-md text-sm leading-relaxed text-muted-foreground">
										{m.setup_flow_creating_desc()}
									</p>
								</div>

								<div class="space-y-2">
									<div class="h-2 overflow-hidden rounded-full bg-secondary/75">
										<div
											class="h-full rounded-full bg-primary transition-[width] duration-500"
											style={`width: ${progress}%`}
										></div>
									</div>
									<div class="flex items-center justify-between text-xs text-muted-foreground">
										<span>{m.setup_flow_progress_active()}</span>
										<span>{progress}%</span>
									</div>
								</div>
							</div>
						{:else if creationState === 'success'}
							<div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,0.92fr)]">
								<div class="rounded-[24px] border border-success-600/20 bg-success-600/6 p-5">
									<div
										class="grid size-14 place-items-center rounded-[18px] bg-success-600/12 text-success-600"
									>
										<CheckIcon class="size-7" />
									</div>
									<p class="mt-5 text-xl font-semibold">
										{m.setup_flow_success_title()}
									</p>
									<p class="mt-2 text-sm leading-relaxed text-muted-foreground">
										{effectiveIntent === 'tenant' && createdLocationId
											? m.setup_flow_success_tenant_and_location()
											: effectiveIntent === 'tenant'
												? m.setup_flow_success_tenant_only()
												: m.setup_flow_success_location_only()}
									</p>
								</div>

								<div class="rounded-[24px] border border-border/70 bg-background/82 p-5">
									<div class="space-y-4">
										<div>
											<p
												class="text-xs font-medium tracking-[0.22em] text-muted-foreground uppercase"
											>
												{m.setup_flow_success_tenant_label()}
											</p>
											<p class="mt-2 text-lg font-semibold">{createdGymName}</p>
										</div>

										{#if createdLocationName}
											<div>
												<p
													class="text-xs font-medium tracking-[0.22em] text-muted-foreground uppercase"
												>
													{m.setup_flow_success_location_label()}
												</p>
												<p class="mt-2 text-lg font-semibold">{createdLocationName}</p>
											</div>
										{/if}
									</div>
								</div>
							</div>
						{:else if creationState === 'error'}
							<div class="rounded-[24px] border border-destructive/25 bg-destructive/7 p-5">
								<p class="text-lg font-semibold text-destructive">{m.setup_flow_error_title()}</p>
								<p class="mt-3 text-sm leading-relaxed text-muted-foreground">
									{creationMessage || m.setup_flow_error_generic()}
								</p>
							</div>
						{/if}

						{@render stepActions()}
					</div>
				{/if}
			</div>
		</div>
	</div>
</div>

<style>
	.step-stage {
		position: relative;
		contain: layout paint;
		will-change: height;
	}

	.step-panel {
		position: absolute;
		top: 0;
		left: 0;
		width: 100%;
		overflow: hidden;
		backface-visibility: hidden;
		transform: translateZ(0);
		will-change: transform, opacity;
	}

	.step-panel:last-child {
		position: relative;
	}
</style>
