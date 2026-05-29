import type { AuthResponse, AuthenticationFlowResponseApiResponse } from '$lib/api';
import { AuthenticationStep } from '$lib/api';
import type { User } from '$lib/stores/AuthenticationStoreProvider.svelte';
import {
	clearPreAuthState,
	saveTwoFactorChallenge,
	saveVerificationSession,
	type TwoFactorChallengeMode
} from './auth-session-storage.js';

export type AuthFlowResult =
	| { kind: 'authenticated' }
	| { kind: 'email-verification' }
	| { kind: 'two-factor'; mode: TwoFactorChallengeMode }
	| { kind: 'error'; message: string };

export function toAuthenticatedUser(authResponse?: AuthResponse | null): User {
	return {
		token: authResponse?.token ?? '',
		expiresAt: authResponse?.expiresAtUtc ?? new Date(Date.now() + 3600_000),
		user: authResponse?.account?.user?.email ?? '',
		account: authResponse?.account ?? null
	};
}

/**
 * Process an AuthenticationFlowResponseApiResponse and return a structured result.
 * Optionally sets the user on the auth store when authenticated.
 */
export function processAuthFlowResponse(
	apiResponse: AuthenticationFlowResponseApiResponse,
	auth?: { user: User | null }
): AuthFlowResult {
	if (!apiResponse.success || !apiResponse.data) {
		return {
			kind: 'error',
			message: apiResponse.error?.message ?? apiResponse.message ?? 'Authentication failed'
		};
	}

	const flow = apiResponse.data;

	switch (flow.step) {
		case AuthenticationStep.Authenticated: {
			if (auth && flow.auth) {
				auth.user = toAuthenticatedUser(flow.auth);
			}
			clearPreAuthState();
			return { kind: 'authenticated' };
		}

		case AuthenticationStep.EmailVerificationRequired: {
			if (
				!flow.emailVerification?.verificationToken ||
				!flow.emailVerification.sessionExpiresAtUtc ||
				!flow.emailVerification.codeExpiresAtUtc ||
				!flow.emailVerification.resendAvailableAtUtc
			) {
				return { kind: 'error', message: 'Email verification session is incomplete.' };
			}

			clearPreAuthState();
			saveVerificationSession({
				verificationToken: flow.emailVerification.verificationToken,
				codeLength: flow.emailVerification.codeLength ?? 6,
				sessionExpiresAt: flow.emailVerification.sessionExpiresAtUtc,
				codeExpiresAt: flow.emailVerification.codeExpiresAtUtc,
				resendAvailableAt: flow.emailVerification.resendAvailableAtUtc
			});

			return {
				kind: 'email-verification'
			};
		}

		case AuthenticationStep.TwoFactorSetupRequired:
		case AuthenticationStep.TwoFactorRequired: {
			if (!flow.twoFactor?.challengeToken) {
				return { kind: 'error', message: 'Two-factor challenge is missing.' };
			}

			const mode: TwoFactorChallengeMode =
				flow.step === AuthenticationStep.TwoFactorSetupRequired ? 'setup' : 'verify';

			clearPreAuthState();
			saveTwoFactorChallenge({
				challengeToken: flow.twoFactor.challengeToken,
				mode,
				expiresAt: flow.twoFactor.expiresAtUtc ?? null
			});

			return {
				kind: 'two-factor',
				mode
			};
		}

		default:
			return { kind: 'error', message: 'Unknown authentication step' };
	}
}
