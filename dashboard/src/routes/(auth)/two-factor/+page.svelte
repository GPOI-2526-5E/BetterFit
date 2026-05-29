<script lang="ts">
	import { goto } from '$app/navigation';
	import * as m from '$lib/paraglide/messages.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Card from '$lib/components/ui/card/index.js';
	import * as Form from '$lib/components/ui/form/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import * as InputOTP from '$lib/components/ui/input-otp/index.js';
	import * as Tabs from '$lib/components/ui/tabs/index.js';
	import type { TwoFactorSetupResponse } from '$lib/api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useUserAuthenticationStore } from '$lib/stores/AuthenticationStoreProvider.svelte';
	import { toAuthenticatedUser } from '$lib/utils/auth-flow.js';
	import { parseApiError } from '$lib/utils/auth-api.js';
	import {
		hasFormErrors,
		validateCodeFormData,
		validateRecoveryCodeFormData
	} from '$lib/utils/auth-form-validation.js';
	import {
		clearPreAuthState,
		getTwoFactorChallenge,
		saveTwoFactorChallenge,
		type TwoFactorChallengeSession
	} from '$lib/utils/auth-session-storage.js';
	import { LoaderCircle, ShieldCheck } from '@lucide/svelte';
	import { onDestroy, onMount } from 'svelte';
	import { superForm } from 'sveltekit-superforms';
	import { get } from 'svelte/store';

	const { client } = useApiClient();
	const auth = useUserAuthenticationStore();

	let challenge = $state<TwoFactorChallengeSession | null>(null);
	let setupState = $state<TwoFactorSetupResponse | null>(null);
	let completedAuthUser = $state<ReturnType<typeof toAuthenticatedUser> | null>(null);
	let recoveryCodes = $state<string[] | null>(null);
	let loading = $state(false);
	let initializing = $state(true);
	let error = $state('');
	let activeTab = $state('authenticator');
	let now = $state(Date.now());
	let expiredChallengeCleared = $state(false);

	let clockTimer: number | null = null;

	const otpForm = superForm({
		code: ''
	});

	const recoveryForm = superForm({
		recoveryCode: ''
	});

	const { form: otpFormData, errors: otpFormErrors } = otpForm;
	const { form: recoveryFormData, errors: recoveryFormErrors } = recoveryForm;

	let isSetupMode = $derived(challenge?.mode === 'setup');
	let isCompletionState = $derived(Boolean(completedAuthUser));
	let challengeExpired = $derived(
		challenge?.expiresAt ? challenge.expiresAt.getTime() <= now : false
	);
	let canVerifyOtp = $derived(
		Boolean(challenge) &&
			!loading &&
			!initializing &&
			!challengeExpired &&
			$otpFormData.code.length >= 6 &&
			!isCompletionState
	);
	let canSubmitRecoveryCode = $derived(
		Boolean(challenge) &&
			!loading &&
			!initializing &&
			!challengeExpired &&
			$recoveryFormData.recoveryCode.trim().length > 0 &&
			!isCompletionState
	);

	function startClock() {
		now = Date.now();
		clockTimer = window.setInterval(() => {
			now = Date.now();
		}, 1000);
	}

	function stopClock() {
		if (!clockTimer) {
			return;
		}

		window.clearInterval(clockTimer);
		clockTimer = null;
	}

	function syncChallenge(nextChallenge: TwoFactorChallengeSession) {
		challenge = nextChallenge;
		expiredChallengeCleared = false;
		saveTwoFactorChallenge(nextChallenge);
	}

	async function redirectToLogin() {
		clearPreAuthState();
		await goto('/login', { replaceState: true });
	}

	async function completeAuthentication(authPayload: Parameters<typeof toAuthenticatedUser>[0]) {
		clearPreAuthState();
		auth.user = toAuthenticatedUser(authPayload);
		await goto('/');
	}

	async function loadTwoFactorSetup() {
		if (!challenge) {
			return;
		}

		try {
			const response = await client.apiAuth2faSetupPost({
				beginTwoFactorSetupRequest: {
					challengeToken: challenge.challengeToken
				}
			});

			if (!response.success || !response.data) {
				error = response.error?.message ?? response.message ?? m.auth_error_generic();
				return;
			}

			setupState = response.data;
			syncChallenge({
				challengeToken: response.data.challengeToken ?? challenge.challengeToken,
				mode: 'setup',
				expiresAt: response.data.expiresAtUtc ?? challenge.expiresAt ?? null
			});
		} catch (caughtError) {
			const parsedError = await parseApiError(caughtError, m.auth_error_generic());
			if (parsedError.status === 404 || parsedError.status === 409) {
				await redirectToLogin();
				return;
			}

			error = parsedError.message;
		}
	}

	async function hydrateChallenge() {
		const storedChallenge = getTwoFactorChallenge();
		if (!storedChallenge) {
			await redirectToLogin();
			return;
		}

		syncChallenge(storedChallenge);

		if (storedChallenge.expiresAt && storedChallenge.expiresAt <= new Date()) {
			clearPreAuthState();
			expiredChallengeCleared = true;
			error = m.auth_two_factor_session_expired();
			initializing = false;
			return;
		}

		if (storedChallenge.mode === 'setup') {
			await loadTwoFactorSetup();
		}

		initializing = false;
	}

	async function handleVerify(event: Event) {
		event.preventDefault();
		const values = get(otpFormData);
		const validationErrors = validateCodeFormData(values, 6);
		otpFormErrors.set(validationErrors, { force: true });

		if (hasFormErrors(validationErrors) || !challenge || !canVerifyOtp) {
			return;
		}

		error = '';
		loading = true;

		try {
			if (challenge.mode === 'setup') {
				const response = await client.apiAuth2faEnablePost({
					enableTwoFactorRequest: {
						challengeToken: challenge.challengeToken,
						code: values.code.trim()
					}
				});

				if (!response.success || !response.data?.auth) {
					error = response.error?.message ?? response.message ?? m.auth_error_generic();
					return;
				}

				recoveryCodes = response.data.recoveryCodes ?? [];
				completedAuthUser = toAuthenticatedUser(response.data.auth);
				otpFormData.set({ code: '' }, { taint: 'untaint' });
				otpFormErrors.clear();
				return;
			}

			const response = await client.apiAuth2faVerifyPost({
				verifyTwoFactorCodeRequest: {
					challengeToken: challenge.challengeToken,
					code: values.code.trim()
				}
			});

			if (!response.success || !response.data) {
				error = response.error?.message ?? response.message ?? m.auth_error_generic();
				return;
			}

			await completeAuthentication(response.data);
		} catch (caughtError) {
			const parsedError = await parseApiError(caughtError, m.auth_error_generic());
			if (parsedError.status === 404 || parsedError.status === 409) {
				await redirectToLogin();
				return;
			}

			if (parsedError.status === 400) {
				error = '';
				otpFormErrors.set({ code: [parsedError.message] }, { force: true });
				return;
			}

			error = parsedError.message;
		} finally {
			loading = false;
		}
	}

	async function handleRecoveryCode(event: Event) {
		event.preventDefault();
		const values = get(recoveryFormData);
		const validationErrors = validateRecoveryCodeFormData(values);
		recoveryFormErrors.set(validationErrors, { force: true });

		if (
			hasFormErrors(validationErrors) ||
			!challenge ||
			!canSubmitRecoveryCode ||
			challenge.mode !== 'verify'
		) {
			return;
		}

		error = '';
		loading = true;

		try {
			const response = await client.apiAuth2faRecoveryCodePost({
				useRecoveryCodeRequest: {
					challengeToken: challenge.challengeToken,
					recoveryCode: values.recoveryCode.trim()
				}
			});

			if (!response.success || !response.data) {
				error = response.error?.message ?? response.message ?? m.auth_error_generic();
				return;
			}

			await completeAuthentication(response.data);
		} catch (caughtError) {
			const parsedError = await parseApiError(caughtError, m.auth_error_generic());
			if (parsedError.status === 404 || parsedError.status === 409) {
				await redirectToLogin();
				return;
			}

			if (parsedError.status === 400) {
				error = '';
				recoveryFormErrors.set({ recoveryCode: [parsedError.message] }, { force: true });
				return;
			}

			error = parsedError.message;
		} finally {
			loading = false;
		}
	}

	async function handleContinueToDashboard() {
		if (!completedAuthUser) {
			return;
		}

		clearPreAuthState();
		auth.user = completedAuthUser;
		await goto('/');
	}

	function handleBackToLogin(event: Event) {
		event.preventDefault();
		void redirectToLogin();
	}

	function handleOtpComplete(value: string) {
		error = '';
		otpFormData.set({ code: value });

		if (get(otpFormErrors).code) {
			otpFormErrors.clear();
		}
	}

	function handleRecoveryCodeInput() {
		error = '';

		if (get(recoveryFormErrors).recoveryCode) {
			recoveryFormErrors.clear();
		}
	}

	$effect(() => {
		if (!challenge || !challengeExpired || expiredChallengeCleared) {
			return;
		}

		clearPreAuthState();
		expiredChallengeCleared = true;

		if (!error) {
			error = m.auth_two_factor_session_expired();
		}
	});

	onMount(() => {
		startClock();
		void hydrateChallenge();

		return () => {
			stopClock();
		};
	});

	onDestroy(() => {
		stopClock();
	});
</script>

<Card.Root class="border-0 shadow-xl shadow-primary/5">
	<Card.Header class="items-center space-y-3 pb-4">
		<div class="flex h-14 w-14 items-center justify-center rounded-full bg-primary/10">
			<ShieldCheck class="h-7 w-7 text-primary" />
		</div>
		<Card.Title class="text-center text-2xl font-bold tracking-tight">
			{isSetupMode ? m.auth_2fa_setup_title() : m.auth_2fa_title()}
		</Card.Title>
		<Card.Description class="text-center text-muted-foreground">
			{isSetupMode ? m.auth_2fa_setup_desc() : m.auth_2fa_desc()}
		</Card.Description>
	</Card.Header>

	<Card.Content class="space-y-6">
		{#if initializing}
			<div
				class="flex items-center justify-center rounded-[18px] border border-border/80 bg-muted/60 px-4 py-3 text-sm text-muted-foreground"
			>
				<LoaderCircle class="mr-2 h-4 w-4 animate-spin" />
				{m.auth_loading_two_factor()}
			</div>
		{:else if isCompletionState}
			<div class="space-y-4">
				<div
					class="rounded-[18px] border border-success-600/20 bg-[#eefbf2] px-4 py-3 text-sm text-[#166534]"
				>
					{m.auth_2fa_recovery_codes_desc()}
				</div>

				<div class="rounded-[18px] border border-border/80 bg-muted/60 p-4">
					<p class="mb-3 text-sm font-semibold text-foreground">
						{m.auth_2fa_recovery_codes_title()}
					</p>
					<div class="grid gap-2 sm:grid-cols-2">
						{#each recoveryCodes ?? [] as item (item)}
							<div
								class="rounded-[14px] border border-border/80 bg-background px-3 py-2 font-mono text-sm text-foreground"
							>
								{item}
							</div>
						{/each}
					</div>
				</div>

				<Button class="w-full" onclick={handleContinueToDashboard}>
					{m.auth_continue_to_dashboard()}
				</Button>
			</div>
		{:else if isSetupMode}
			<div class="space-y-5">
				<div class="rounded-[18px] border border-border/80 bg-muted/60 p-4">
					<p class="mb-2 text-sm font-semibold text-foreground">
						{m.auth_2fa_setup_secret_key()}
					</p>
					<div
						class="rounded-[14px] border border-border/80 bg-background px-3 py-3 font-mono text-sm tracking-[0.18em] break-all text-foreground"
					>
						{setupState?.formattedSharedKey ?? '...'}
					</div>
					<p class="mt-3 text-sm text-muted-foreground">
						{m.auth_2fa_setup_instructions()}
					</p>
				</div>

				<form onsubmit={handleVerify} class="flex flex-col items-center gap-6">
					<Form.Field form={otpForm} name="code" class="w-full">
						<Form.Control>
							{#snippet children({ props })}
								<Form.Label class="sr-only">{m.auth_2fa_setup_title()}</Form.Label>
								<div class="flex justify-center">
									<InputOTP.Root
										{...props}
										maxlength={6}
										onValueChange={handleOtpComplete}
										value={$otpFormData.code}
									>
										{#snippet children({ cells })}
											<InputOTP.Group>
												{#each cells.slice(0, 3) as cell, index (index)}
													<InputOTP.Slot {cell} />
												{/each}
											</InputOTP.Group>
											<InputOTP.Separator />
											<InputOTP.Group>
												{#each cells.slice(3, 6) as cell, index (index + 3)}
													<InputOTP.Slot {cell} />
												{/each}
											</InputOTP.Group>
										{/snippet}
									</InputOTP.Root>
								</div>
							{/snippet}
						</Form.Control>
						<Form.FieldErrors class="text-center" />
					</Form.Field>

					{#if error}
						<div
							class="w-full rounded-[18px] border border-destructive/20 bg-destructive/10 px-4 py-3 text-sm text-destructive"
						>
							{error}
						</div>
					{/if}

					<Form.Button class="w-full" disabled={!canVerifyOtp}>
						{#if loading}
							<LoaderCircle class="mr-2 h-4 w-4 animate-spin" />
						{/if}
						{m.auth_verify_button()}
					</Form.Button>
				</form>
			</div>
		{:else}
			<Tabs.Root bind:value={activeTab} class="space-y-4">
				<Tabs.List class="grid w-full grid-cols-2">
					<Tabs.Trigger value="authenticator">
						{m.auth_2fa_tab_authenticator()}
					</Tabs.Trigger>
					<Tabs.Trigger value="recovery">{m.auth_2fa_tab_recovery()}</Tabs.Trigger>
				</Tabs.List>

				<Tabs.Content value="authenticator">
					<form onsubmit={handleVerify} class="flex flex-col items-center gap-6">
						<Form.Field form={otpForm} name="code" class="w-full">
							<Form.Control>
								{#snippet children({ props })}
									<Form.Label class="sr-only">{m.auth_2fa_title()}</Form.Label>
									<div class="flex justify-center">
										<InputOTP.Root
											{...props}
											maxlength={6}
											onValueChange={handleOtpComplete}
											value={$otpFormData.code}
										>
											{#snippet children({ cells })}
												<InputOTP.Group>
													{#each cells.slice(0, 3) as cell, index (index)}
														<InputOTP.Slot {cell} />
													{/each}
												</InputOTP.Group>
												<InputOTP.Separator />
												<InputOTP.Group>
													{#each cells.slice(3, 6) as cell, index (index + 3)}
														<InputOTP.Slot {cell} />
													{/each}
												</InputOTP.Group>
											{/snippet}
										</InputOTP.Root>
									</div>
								{/snippet}
							</Form.Control>
							<Form.FieldErrors class="text-center" />
						</Form.Field>

						{#if error && activeTab === 'authenticator'}
							<div
								class="w-full rounded-[18px] border border-destructive/20 bg-destructive/10 px-4 py-3 text-sm text-destructive"
							>
								{error}
							</div>
						{/if}

						<Form.Button class="w-full" disabled={!canVerifyOtp}>
							{#if loading}
								<LoaderCircle class="mr-2 h-4 w-4 animate-spin" />
							{/if}
							{m.auth_verify_button()}
						</Form.Button>
					</form>
				</Tabs.Content>

				<Tabs.Content value="recovery">
					<form onsubmit={handleRecoveryCode} class="space-y-4">
						<Form.Field form={recoveryForm} name="recoveryCode">
							<Form.Control>
								{#snippet children({ props })}
									<Form.Label>{m.auth_recovery_code_label()}</Form.Label>
									<Input
										{...props}
										type="text"
										bind:value={$recoveryFormData.recoveryCode}
										placeholder={m.auth_recovery_code_placeholder()}
										disabled={loading}
										autocomplete="one-time-code"
										oninput={handleRecoveryCodeInput}
									/>
								{/snippet}
							</Form.Control>
							<Form.FieldErrors />
						</Form.Field>

						{#if error && activeTab === 'recovery'}
							<div
								class="w-full rounded-[18px] border border-destructive/20 bg-destructive/10 px-4 py-3 text-sm text-destructive"
							>
								{error}
							</div>
						{/if}

						<Form.Button class="w-full" disabled={!canSubmitRecoveryCode}>
							{#if loading}
								<LoaderCircle class="mr-2 h-4 w-4 animate-spin" />
							{/if}
							{m.auth_recovery_code_button()}
						</Form.Button>
					</form>
				</Tabs.Content>
			</Tabs.Root>
		{/if}
	</Card.Content>

	<Card.Footer class="flex-col gap-2">
		<a
			href="/login"
			class="text-sm font-medium text-primary underline-offset-4 hover:underline"
			onclick={handleBackToLogin}
		>
			{m.auth_back_to_login()}
		</a>
	</Card.Footer>
</Card.Root>
