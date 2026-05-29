<script lang="ts" module>
	import type { CurrentAccountResponse as CurrentAccount } from '$lib/api';
	import type { AuthenticatedSession } from '$lib/utils/auth-session-storage.js';
	export type User = {
		token: AuthenticatedSession['token'];
		expiresAt: AuthenticatedSession['expiresAt'];
		user: AuthenticatedSession['user'];
		account?: CurrentAccount | null;
	};

	import { getContext } from 'svelte';

	export const useUserAuthenticationStore = () => {
		return getContext('betterfitUserAuthentication') as {
			user: User | null;
			expired: boolean;
			ready: boolean;
			showUI: boolean;
			refreshCurrentUser: (force?: boolean) => Promise<CurrentAccount | null>;
			logout: () => void;
		};
	};
</script>

<script lang="ts">
	import type { CurrentAccountResponse } from '$lib/api';
	import {
		clearPreAuthState,
		clearAuthenticatedSession,
		getAuthenticatedSession,
		saveAuthenticatedSession
	} from '$lib/utils/auth-session-storage.js';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { onDestroy, onMount, setContext, type Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();
	const { client } = useApiClient();

	let showUI = $state(false);

	let user: User | null = $state(null);

	let expired = $state(false);

	let ready = $state(false);

	let expiredTimer: ReturnType<typeof setTimeout> | null = $state(null);
	let refreshCurrentUserPromise: Promise<CurrentAccountResponse | null> | null = null;

	function clearExpiredTimer() {
		if (!expiredTimer) {
			return;
		}

		clearTimeout(expiredTimer);
		expiredTimer = null;
	}

	function checkExpired() {
		if (!user) {
			expired = false;
			return;
		}

		if (user.expiresAt <= new Date()) {
			clearExpiredTimer();
			user = null;
			expired = true;
			clearAuthenticatedSession();
			return;
		}

		expired = false;
	}

	function setUser(value: User | null) {
		clearExpiredTimer();
		user = value;
		expired = false;

		if (!user) {
			clearAuthenticatedSession();
			return;
		}

		saveAuthenticatedSession(user);
		checkExpired();

		if (expired) {
			return;
		}

		expiredTimer = setTimeout(checkExpired, Math.max(0, user.expiresAt.getTime() - Date.now()));
	}

	async function refreshCurrentUser(force = false): Promise<CurrentAccountResponse | null> {
		if (!user?.token) {
			return null;
		}

		if (!force && user.account) {
			return user.account;
		}

		if (refreshCurrentUserPromise) {
			return refreshCurrentUserPromise;
		}

		refreshCurrentUserPromise = (async () => {
			try {
				const response = await client.apiAuthMeGet();
				if (!response.success || !response.data || !user) {
					return user?.account ?? null;
				}

				user = {
					...user,
					user: response.data.user?.email ?? user.user,
					account: response.data
				};

				return response.data;
			} catch (error: any) {
				const status = error?.response?.status;
				if (status === 401 || status === 403) {
					setUser(null);
					expired = true;
					return null;
				}

				return user?.account ?? null;
			} finally {
				refreshCurrentUserPromise = null;
			}
		})();

		return refreshCurrentUserPromise;
	}

	function logout() {
		clearPreAuthState();
		setUser(null);
	}

	setContext('betterfitUserAuthentication', {
		get ready() {
			return ready;
		},
		get showUI() {
			return showUI;
		},
		set showUI(value: boolean) {
			showUI = value;
		},
		get user() {
			return user;
		},
		set user(value: User | null) {
			setUser(value);
		},

		get expired() {
			return expired;
		},
		refreshCurrentUser,
		logout
	});

	onMount(() => {
		const storedSession = getAuthenticatedSession();
		if (storedSession) {
			setUser(storedSession);
			void refreshCurrentUser();
		}

		ready = true;
	});

	onDestroy(() => {
		clearExpiredTimer();
	});
</script>

{@render children()}
