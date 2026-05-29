<script lang="ts">
	import * as m from '$lib/paraglide/messages.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';

	let {
		onRetry,
		message = null
	}: {
		onRetry: () => void;
		message?: string | null;
	} = $props();
</script>

<Card class="border-destructive/30">
	<CardHeader>
		<CardTitle>{m.home_error_title()}</CardTitle>
		<CardDescription>{m.home_error_desc()}</CardDescription>
	</CardHeader>
	<CardContent>
		{#if message}
			<p class="mb-4 text-sm text-muted-foreground">{message}</p>
		{/if}
		<Button onclick={onRetry}>{m.common_retry()}</Button>
	</CardContent>
</Card>
