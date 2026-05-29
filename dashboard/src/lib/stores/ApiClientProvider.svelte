<script lang="ts" module>
	import * as api from '$lib/api';
	import { getAuthenticatedSession } from '$lib/utils/auth-session-storage.js';
	import { getContext } from 'svelte';

	export const useApiClient = () => {
		return getContext('betterfitApiClient') as {
			client: api.BetterfitApi;
		};
	};
</script>

<script lang="ts">
	import { onDestroy, setContext, type Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();

	let client: api.BetterfitApi = new api.BetterfitApi(
		new api.Configuration({
			basePath: 'http://localhost:5299',
			accessToken: () => getAuthenticatedSession()?.token ?? ''
		})
	);

	setContext('betterfitApiClient', {
		get client() {
			return client;
		}
	});

	onDestroy(() => {});
</script>

{@render children()}
