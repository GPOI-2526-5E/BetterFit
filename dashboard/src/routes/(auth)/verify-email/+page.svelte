<script lang="ts">
	import { goto } from '$app/navigation';
	import * as m from '$lib/paraglide/messages.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Card from '$lib/components/ui/card/index.js';
	import * as Form from '$lib/components/ui/form/index.js';
	import * as InputOTP from '$lib/components/ui/input-otp/index.js';
	import type { EmailVerificationChallengeResponse } from '$lib/api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useUserAuthenticationStore } from '$lib/stores/AuthenticationStoreProvider.svelte';
	import { processAuthFlowResponse } from '$lib/utils/auth-flow.js';
	import { hasFormErrors, validateCodeFormData } from '$lib/utils/auth-form-validation.js';
	import {
		clearPreAuthState,
		getVerificationSession,
		saveVerificationSession,
		type VerificationSession
	} from '$lib/utils/auth-session-storage.js';
	import { getErrorDetail, parseApiError } from '$lib/utils/auth-api.js';
	import { LoaderCircle, MailCheck } from '@lucide/svelte';
	import { onDestroy, onMount } from 'svelte';
	import { superForm } from 'sveltekit-superforms';
	import { get } from 'svelte/store';

	const { client } = useApiClient();
	const auth = useUserAuthenticationStore();

	let verificationSession = $state<VerificationSession | null>(null);
	let loading = $state(false);
	let initializing = $state(true);
	let resending = $state(false);
	let resendSuccess = $state(false);
	let error = $state('');
	let now = $state(Date.now());
	let expiredSessionCleared = $state(false);

	let clockTimer: number | null = null;
	let resendSuccessTimer: ReturnType<typeof setTimeout> | null = null;

	const verificationForm = superForm({
		code: ''
	});

	const { form: verificationFormData, errors: verificationFormErrors } = verificationForm;

	let codeLength = $derived(verificationSession?.codeLength ?? 6);
	let sessionExpired = $derived(
		verificationSession ? verificationSession.sessionExpiresAt.getTime() <= now : true
	);
	let codeExpired = $derived(
		verificationSession ? verificationSession.codeExpiresAt.getTime() <= now : false
	);
	let resendCooldownSeconds = $derived(
		verificationSession
			? Math.max(0, Math.ceil((verificationSession.resendAvailableAt.getTime() - now) / 1000))
			: 0
	);
	let canVerify = $derived(
		Boolean(verificationSession) &&
			!initializing &&
			!loading &&
			!sessionExpired &&
			!codeExpired &&
			$verificationFormData.code.length >= codeLength
	);
	let canResend = $derived(
		Boolean(verificationSession) &&
			!initializing &&
			!resending &&
			!sessionExpired &&
			resendCooldownSeconds === 0
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

	function clearResendSuccessTimeout() {
		if (!resendSuccessTimer) {
			return;
		}

		clearTimeout(resendSuccessTimer);
		resendSuccessTimer = null;
	}

	function showResendSuccessState() {
		clearResendSuccessTimeout();
		resendSuccess = true;
		resendSuccessTimer = setTimeout(() => {
			resendSuccess = false;
		}, 3000);
	}

	function mapVerificationSession(
		response?: EmailVerificationChallengeResponse
	): VerificationSession | null {
		if (
			!response?.verificationToken ||
			!response.sessionExpiresAtUtc ||
			!response.codeExpiresAtUtc ||
			!response.resendAvailableAtUtc
		) {
			return null;
		}

		return {
			verificationToken: response.verificationToken,
			codeLength: response.codeLength ?? 6,
			sessionExpiresAt: response.sessionExpiresAtUtc,
			codeExpiresAt: response.codeExpiresAtUtc,
			resendAvailableAt: response.resendAvailableAtUtc
		};
	}

	function syncVerificationSession(nextSession: VerificationSession) {
		verificationSession = nextSession;
		expiredSessionCleared = false;
		saveVerificationSession(nextSession);
	}

	function markCodeExpired() {
		if (!verificationSession) {
			return;
		}

		syncVerificationSession({
			...verificationSession,
			codeExpiresAt: new Date()
		});
	}

	function applyCooldownFromError(
		retryAfterSeconds: number | null,
		retryAvailableAtUtc: string | null
	) {
		if (!verificationSession) {
			return;
		}

		const retryAvailableAt = retryAvailableAtUtc
			? new Date(retryAvailableAtUtc)
			: new Date(Date.now() + (retryAfterSeconds ?? 0) * 1000);

		if (Number.isNaN(retryAvailableAt.getTime())) {
			return;
		}

		syncVerificationSession({
			...verificationSession,
			resendAvailableAt: retryAvailableAt
		});
	}

	async function redirectToLogin() {
		clearPreAuthState();
		await goto('/login', { replaceState: true });
	}

	async function hydrateVerificationSession() {
		const storedSession = getVerificationSession();
		if (!storedSession) {
			await redirectToLogin();
			return;
		}

		syncVerificationSession(storedSession);

		if (storedSession.sessionExpiresAt <= new Date()) {
			clearPreAuthState();
			expiredSessionCleared = true;
			error = m.auth_session_expired();
			initializing = false;
			return;
		}

		try {
			const response = await client.apiAuthVerificationSessionStatusPost({
				getEmailVerificationSessionStatusRequest: {
					verificationToken: storedSession.verificationToken
				}
			});

			const nextSession = mapVerificationSession(response.data);
			if (!response.success || !nextSession) {
				error = response.error?.message ?? response.message ?? m.auth_error_generic();
				initializing = false;
				return;
			}

			syncVerificationSession(nextSession);
		} catch (caughtError) {
			const parsedError = await parseApiError(caughtError, m.auth_error_generic());
			if (parsedError.status === 404 || parsedError.status === 409) {
				await redirectToLogin();
				return;
			}

			error = parsedError.message;
		} finally {
			initializing = false;
		}
	}

	async function handleVerify(event: Event) {
		event.preventDefault();
		const values = get(verificationFormData);
		const validationErrors = validateCodeFormData(values, codeLength);
		verificationFormErrors.set(validationErrors, { force: true });

		if (hasFormErrors(validationErrors) || !verificationSession || !canVerify) {
			return;
		}

		error = '';
		loading = true;

		try {
			const response = await client.apiAuthVerifyEmailCodePost({
				verifyEmailCodeRequest: {
					verificationToken: verificationSession.verificationToken,
					code: values.code.trim()
				}
			});

			const result = processAuthFlowResponse(response, auth);
			switch (result.kind) {
				case 'authenticated':
					await goto('/');
					break;
				case 'two-factor':
					await goto('/two-factor');
					break;
				case 'error':
					error = result.message;
					break;
				default:
					error = m.auth_error_generic();
			}
		} catch (caughtError) {
			const parsedError = await parseApiError(caughtError, m.auth_error_generic());

			if (parsedError.code === 'verification_code_expired') {
				markCodeExpired();
				error = '';
				verificationFormData.set({ code: '' }, { taint: 'untaint' });
				verificationFormErrors.set({ code: [m.auth_code_expired()] }, { force: true });
				return;
			}

			if (parsedError.status === 404 || parsedError.status === 409) {
				await redirectToLogin();
				return;
			}

			if (parsedError.status === 400) {
				error = '';
				verificationFormErrors.set({ code: [parsedError.message] }, { force: true });
				return;
			}

			error = parsedError.message;
		} finally {
			loading = false;
		}
	}

	async function handleResend() {
		if (!verificationSession || !canResend) {
			return;
		}

		error = '';
		resending = true;
		resendSuccess = false;

		try {
			const response = await client.apiAuthResendEmailCodePost({
				resendEmailVerificationCodeRequest: {
					verificationToken: verificationSession.verificationToken
				}
			});

			const nextSession = mapVerificationSession(response.data);
			if (!response.success || !nextSession) {
				error = response.error?.message ?? response.message ?? m.auth_error_generic();
				return;
			}

			syncVerificationSession(nextSession);
			verificationFormData.set({ code: '' }, { taint: 'untaint' });
			verificationFormErrors.clear();
			showResendSuccessState();
		} catch (caughtError) {
			const parsedError = await parseApiError(caughtError, m.auth_error_generic());

			if (parsedError.code === 'verification_code_recently_sent') {
				applyCooldownFromError(
					parsedError.retryAfterSeconds,
					getErrorDetail(parsedError.details, 'retryAvailableAtUtc')
				);
				error = parsedError.message;
				return;
			}

			if (parsedError.status === 404 || parsedError.status === 409) {
				await redirectToLogin();
				return;
			}

			error = parsedError.message;
		} finally {
			resending = false;
		}
	}

	function handleBackToLogin(event: Event) {
		event.preventDefault();
		void redirectToLogin();
	}

	function handleOtpComplete(value: string) {
		error = '';
		verificationFormData.set({ code: value });

		if (get(verificationFormErrors).code) {
			verificationFormErrors.clear();
		}
	}

	$effect(() => {
		if (!verificationSession || !sessionExpired || expiredSessionCleared) {
			return;
		}

		clearPreAuthState();
		expiredSessionCleared = true;

		if (!error) {
			error = m.auth_session_expired();
		}
	});

	onMount(() => {
		startClock();
		void hydrateVerificationSession();

		return () => {
			stopClock();
			clearResendSuccessTimeout();
		};
	});

	onDestroy(() => {
		stopClock();
		clearResendSuccessTimeout();
	});
</script>

<Card.Root class="border-0 shadow-xl shadow-primary/5">
	<Card.Header class="items-center space-y-3 pb-4">
		<div class="flex h-14 w-14 items-center justify-center rounded-full bg-primary/10">
			<MailCheck class="h-7 w-7 text-primary" />
		</div>
		<Card.Title class="text-center text-2xl font-bold tracking-tight">
			{m.auth_verify_email_title()}
		</Card.Title>
		<Card.Description class="text-center text-muted-foreground">
			{m.auth_verify_email_desc()}
		</Card.Description>
	</Card.Header>

	<Card.Content class="space-y-6">
		<form onsubmit={handleVerify} class="flex flex-col items-center gap-6">
			<Form.Field form={verificationForm} name="code" class="w-full">
				<Form.Control>
					{#snippet children({ props })}
						<Form.Label class="sr-only">{m.auth_verify_email_title()}</Form.Label>
						<div class="flex justify-center">
							<InputOTP.Root
								{...props}
								maxlength={codeLength}
								onValueChange={handleOtpComplete}
								value={$verificationFormData.code}
							>
								{#snippet children({ cells })}
									<InputOTP.Group>
										{#each cells as cell, index (index)}
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

			{#if initializing}
				<div
					class="flex w-full items-center justify-center rounded-[18px] border border-border/80 bg-muted/60 px-4 py-3 text-sm text-muted-foreground"
				>
					<LoaderCircle class="mr-2 h-4 w-4 animate-spin" />
					{m.auth_loading_session()}
				</div>
			{:else if error}
				<div
					class="w-full rounded-[18px] border border-destructive/20 bg-destructive/10 px-4 py-3 text-sm text-destructive"
				>
					{error}
				</div>
			{:else if resendSuccess}
				<div
					class="w-full rounded-[18px] border border-success-600/20 bg-[#eefbf2] px-4 py-3 text-sm text-[#166534]"
				>
					{m.auth_resend_code_sent()}
				</div>
			{:else if codeExpired}
				<div
					class="w-full rounded-[18px] border border-warning-600/20 bg-[#fff8eb] px-4 py-3 text-sm text-[#9a5b00]"
				>
					{m.auth_code_expired()}
				</div>
			{/if}

			<Form.Button class="w-full" disabled={!canVerify}>
				{#if loading}
					<LoaderCircle class="mr-2 h-4 w-4 animate-spin" />
				{/if}
				{m.auth_verify_button()}
			</Form.Button>
		</form>
	</Card.Content>

	<Card.Footer class="flex-col gap-3">
		<Button
			variant="ghost"
			class="text-sm text-muted-foreground"
			disabled={!canResend}
			onclick={handleResend}
		>
			{#if resending}
				<LoaderCircle class="mr-2 h-4 w-4 animate-spin" />
			{/if}
			{#if resendSuccess}
				{m.auth_resend_code_sent()}
			{:else if resendCooldownSeconds > 0}
				{m.auth_resend_cooldown({ seconds: resendCooldownSeconds })}
			{:else}
				{m.auth_resend_code()}
			{/if}
		</Button>

		<a
			href="/login"
			class="text-sm font-medium text-primary underline-offset-4 hover:underline"
			onclick={handleBackToLogin}
		>
			{m.auth_back_to_login()}
		</a>
	</Card.Footer>
</Card.Root>
