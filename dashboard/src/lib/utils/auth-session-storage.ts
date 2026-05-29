export type AuthenticatedSession = {
	token: string;
	expiresAt: Date;
	user: string;
};

export type VerificationSession = {
	verificationToken: string;
	codeLength: number;
	sessionExpiresAt: Date;
	codeExpiresAt: Date;
	resendAvailableAt: Date;
};

export type TwoFactorChallengeMode = 'verify' | 'setup';

export type TwoFactorChallengeSession = {
	challengeToken: string;
	mode: TwoFactorChallengeMode;
	expiresAt: Date | null;
};

type StoredAuthenticatedSession = {
	token: string;
	expiresAt: string;
	user: string;
};

type StoredVerificationSession = {
	verificationToken: string;
	codeLength: number;
	sessionExpiresAt: string;
	codeExpiresAt: string;
	resendAvailableAt: string;
};

type StoredTwoFactorChallengeSession = {
	challengeToken: string;
	mode: TwoFactorChallengeMode;
	expiresAt: string | null;
};

const AUTHENTICATED_SESSION_KEY = 'betterfit.auth.session';
const VERIFICATION_SESSION_KEY = 'betterfit.auth.verification-session';
const TWO_FACTOR_CHALLENGE_KEY = 'betterfit.auth.two-factor-challenge';

function canUseSessionStorage() {
	return typeof window !== 'undefined' && typeof window.sessionStorage !== 'undefined';
}

function safeParse<T>(rawValue: string | null): T | null {
	if (!rawValue) {
		return null;
	}

	try {
		return JSON.parse(rawValue) as T;
	} catch {
		return null;
	}
}

function parseDate(value: string | null | undefined): Date | null {
	if (!value) {
		return null;
	}

	const date = new Date(value);
	return Number.isNaN(date.getTime()) ? null : date;
}

function readSessionStorage<T>(key: string): T | null {
	if (!canUseSessionStorage()) {
		return null;
	}

	return safeParse<T>(window.sessionStorage.getItem(key));
}

function writeSessionStorage<T>(key: string, value: T) {
	if (!canUseSessionStorage()) {
		return;
	}

	window.sessionStorage.setItem(key, JSON.stringify(value));
}

function removeSessionStorage(key: string) {
	if (!canUseSessionStorage()) {
		return;
	}

	window.sessionStorage.removeItem(key);
}

export function saveAuthenticatedSession(session: AuthenticatedSession) {
	writeSessionStorage<StoredAuthenticatedSession>(AUTHENTICATED_SESSION_KEY, {
		token: session.token,
		expiresAt: session.expiresAt.toISOString(),
		user: session.user
	});
}

export function getAuthenticatedSession(): AuthenticatedSession | null {
	const stored = readSessionStorage<StoredAuthenticatedSession>(AUTHENTICATED_SESSION_KEY);
	if (!stored) {
		return null;
	}

	const expiresAt = parseDate(stored.expiresAt);
	if (!expiresAt || !stored.token || !stored.user) {
		clearAuthenticatedSession();
		return null;
	}

	return {
		token: stored.token,
		expiresAt,
		user: stored.user
	};
}

export function clearAuthenticatedSession() {
	removeSessionStorage(AUTHENTICATED_SESSION_KEY);
}

export function saveVerificationSession(session: VerificationSession) {
	writeSessionStorage<StoredVerificationSession>(VERIFICATION_SESSION_KEY, {
		verificationToken: session.verificationToken,
		codeLength: session.codeLength,
		sessionExpiresAt: session.sessionExpiresAt.toISOString(),
		codeExpiresAt: session.codeExpiresAt.toISOString(),
		resendAvailableAt: session.resendAvailableAt.toISOString()
	});
}

export function getVerificationSession(): VerificationSession | null {
	const stored = readSessionStorage<StoredVerificationSession>(VERIFICATION_SESSION_KEY);
	if (!stored) {
		return null;
	}

	const sessionExpiresAt = parseDate(stored.sessionExpiresAt);
	const codeExpiresAt = parseDate(stored.codeExpiresAt);
	const resendAvailableAt = parseDate(stored.resendAvailableAt);

	if (
		!stored.verificationToken ||
		!sessionExpiresAt ||
		!codeExpiresAt ||
		!resendAvailableAt ||
		!Number.isFinite(stored.codeLength)
	) {
		clearVerificationSession();
		return null;
	}

	return {
		verificationToken: stored.verificationToken,
		codeLength: stored.codeLength,
		sessionExpiresAt,
		codeExpiresAt,
		resendAvailableAt
	};
}

export function clearVerificationSession() {
	removeSessionStorage(VERIFICATION_SESSION_KEY);
}

export function saveTwoFactorChallenge(session: TwoFactorChallengeSession) {
	writeSessionStorage<StoredTwoFactorChallengeSession>(TWO_FACTOR_CHALLENGE_KEY, {
		challengeToken: session.challengeToken,
		mode: session.mode,
		expiresAt: session.expiresAt?.toISOString() ?? null
	});
}

export function getTwoFactorChallenge(): TwoFactorChallengeSession | null {
	const stored = readSessionStorage<StoredTwoFactorChallengeSession>(TWO_FACTOR_CHALLENGE_KEY);
	if (!stored) {
		return null;
	}

	if (!stored.challengeToken || (stored.mode !== 'verify' && stored.mode !== 'setup')) {
		clearTwoFactorChallenge();
		return null;
	}

	const expiresAt = stored.expiresAt ? parseDate(stored.expiresAt) : null;
	if (stored.expiresAt && !expiresAt) {
		clearTwoFactorChallenge();
		return null;
	}

	return {
		challengeToken: stored.challengeToken,
		mode: stored.mode,
		expiresAt
	};
}

export function clearTwoFactorChallenge() {
	removeSessionStorage(TWO_FACTOR_CHALLENGE_KEY);
}

export function getActivePreAuthRoute(
	now: Date = new Date()
): '/verify-email' | '/two-factor' | null {
	const verificationSession = getVerificationSession();
	if (verificationSession) {
		if (verificationSession.sessionExpiresAt > now) {
			return '/verify-email';
		}

		clearVerificationSession();
	}

	const twoFactorChallenge = getTwoFactorChallenge();
	if (twoFactorChallenge) {
		if (!twoFactorChallenge.expiresAt || twoFactorChallenge.expiresAt > now) {
			return '/two-factor';
		}

		clearTwoFactorChallenge();
	}

	return null;
}

export function clearPreAuthState() {
	clearVerificationSession();
	clearTwoFactorChallenge();
}
