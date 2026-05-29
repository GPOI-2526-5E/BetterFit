<script lang="ts">
	import { page } from '$app/state';
	import AuthRouteRedirect from '$lib/components/auth/auth-route-redirect.svelte';
	import { useUserAuthenticationStore } from '$lib/stores/AuthenticationStoreProvider.svelte';
	import { getActivePreAuthRoute } from '$lib/utils/auth-session-storage.js';
	import favicon from '$lib/assets/favicon.svg';

	let { children } = $props();

	const auth = useUserAuthenticationStore();
	const authShellState = $derived.by(() => {
		if (!auth.ready) {
			return {
				showUI: false,
				redirect: null as { href: string; replaceState?: boolean } | null
			};
		}

		if (auth.user) {
			return {
				showUI: false,
				redirect: { href: '/' }
			};
		}

		const activePreAuthRoute = getActivePreAuthRoute();
		if (activePreAuthRoute && page.url.pathname !== activePreAuthRoute) {
			return {
				showUI: false,
				redirect: { href: activePreAuthRoute, replaceState: true }
			};
		}

		return {
			showUI: true,
			redirect: null
		};
	});
</script>

{#if authShellState.redirect}
	{#key `${authShellState.redirect.href}:${authShellState.redirect.replaceState ? 'replace' : 'push'}`}
		<AuthRouteRedirect
			href={authShellState.redirect.href}
			replaceState={authShellState.redirect.replaceState ?? false}
		/>
	{/key}
{/if}

{#if authShellState.showUI}
	<div
		class="auth-login-stage relative flex min-h-svh items-center justify-center overflow-hidden bg-background px-4 py-12"
	>
		<div aria-hidden="true" class="pointer-events-none absolute inset-0 overflow-hidden">
			<div class="auth-login-atmosphere"></div>
			<div class="auth-login-veil"></div>
		</div>

		<div class="auth-login-content relative z-10 w-full max-w-[520px]">
			<div class="mb-8 flex flex-col items-center gap-3">
				<div class="flex h-12 w-12 items-center justify-center rounded-xl bg-primary shadow-lg">
					<img src={favicon} alt="Betterfit" class="h-7 w-7" />
				</div>
				<span class="text-xl font-semibold tracking-tight text-foreground">Betterfit</span>
			</div>
			{@render children()}
		</div>
	</div>
{/if}

<style>
	.auth-login-stage {
		background-image:
			linear-gradient(180deg, rgba(215, 229, 255, 0.98) 0%, rgba(235, 243, 255, 1) 100%),
			radial-gradient(circle at 18% 14%, rgba(23, 105, 255, 0.2) 0%, rgba(23, 105, 255, 0) 34%),
			radial-gradient(circle at 82% 18%, rgba(49, 184, 255, 0.16) 0%, rgba(49, 184, 255, 0) 30%),
			radial-gradient(circle at 50% 100%, rgba(184, 242, 29, 0.1) 0%, rgba(244, 247, 252, 0) 48%);
	}

	.auth-login-content {
		filter: drop-shadow(0 20px 34px rgba(12, 20, 36, 0.08));
	}

	.auth-login-atmosphere,
	.auth-login-veil {
		position: absolute;
		left: 50%;
		top: 50%;
		border-radius: 9999px;
		transform: translate(-50%, -50%);
	}

	.auth-login-atmosphere {
		width: max(138vw, 96rem);
		aspect-ratio: 1 / 1;
		background: radial-gradient(
			circle,
			rgba(23, 105, 255, 0.42) 0%,
			rgba(49, 184, 255, 0.28) 20%,
			rgba(23, 105, 255, 0.18) 40%,
			rgba(184, 242, 29, 0.1) 56%,
			rgba(244, 247, 252, 0) 78%
		);
		filter: blur(22px);
		opacity: 1;
		transform: translate(-50%, -50%) scale(0.9);
	}

	.auth-login-veil {
		width: max(180vw, 126rem);
		aspect-ratio: 1 / 1;
		background: radial-gradient(
			circle,
			rgba(255, 255, 255, 1) 0%,
			rgba(255, 255, 255, 1) 30%,
			rgba(255, 255, 255, 0.96) 46%,
			rgba(255, 255, 255, 0.74) 62%,
			rgba(255, 255, 255, 0.18) 82%,
			rgba(255, 255, 255, 0) 100%
		);
		filter: blur(12px);
		opacity: 0.92;
		transform: translate(-50%, -50%) scale(0.34);
	}

	@media (max-width: 640px) {
		.auth-login-atmosphere,
		.auth-login-veil {
			top: 46%;
		}

		.auth-login-atmosphere {
			width: 182vw;
		}

		.auth-login-veil {
			width: 240vw;
		}
	}
</style>
