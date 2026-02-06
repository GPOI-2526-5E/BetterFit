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

	let { onRetry }: { onRetry: () => void } = $props();
</script>

<Card class="border-destructive/30">
	<CardHeader>
		<CardTitle>{m.home_error_title()}</CardTitle>
		<CardDescription>{m.home_error_desc()}</CardDescription>
	</CardHeader>
	<CardContent>
		<Button onclick={onRetry}>{m.common_retry()}</Button>
	</CardContent>
</Card>
