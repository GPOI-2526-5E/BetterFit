<script lang="ts">
	import AlertTriangleIcon from '@lucide/svelte/icons/alert-triangle';
	import * as m from '$lib/paraglide/messages.js';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import type { AccessDeniedAttempt } from './types';

	let { deniedAttempts }: { deniedAttempts: AccessDeniedAttempt[] } = $props();
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>{m.access_live_denials_title()}</CardTitle>
		<CardDescription>{m.access_live_denials_desc()}</CardDescription>
	</CardHeader>
	<CardContent class="space-y-3 pt-4">
		{#each deniedAttempts as denied}
			<div class="rounded-[10px] border border-border bg-background p-3">
				<div class="flex items-start justify-between gap-3">
					<div class="min-w-0">
						<p class="truncate text-sm font-semibold">{denied.member}</p>
						<p class="mt-1 flex items-center gap-1.5 text-xs text-muted-foreground">
							<AlertTriangleIcon class="size-3.5 text-warning-600" />
							{denied.reason}
						</p>
					</div>
					<Badge variant="warning">{m.access_live_attempts_count({ count: denied.attempts })}</Badge
					>
				</div>
				<p class="mt-2 text-xs text-muted-foreground">
					{m.access_live_denial_last_attempt({ time: denied.lastAttempt })}
				</p>
			</div>
		{/each}
	</CardContent>
</Card>
