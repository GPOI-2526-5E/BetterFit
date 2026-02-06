<script lang="ts">
	import BoltIcon from '@lucide/svelte/icons/bolt';
	import KeyRoundIcon from '@lucide/svelte/icons/key-round';
	import WrenchIcon from '@lucide/svelte/icons/wrench';
	import * as m from '$lib/paraglide/messages.js';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import type { AccessController, AccessControllerStatus } from './types';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';

	let { controllers }: { controllers: AccessController[] } = $props();

	const statusBadge = (status: AccessControllerStatus): BadgeVariant => {
		if (status === 'offline') return 'destructive';
		if (status === 'degraded') return 'warning';
		return 'success';
	};

	const statusLabel = (status: AccessControllerStatus): string => {
		if (status === 'offline') return m.access_live_status_offline();
		if (status === 'degraded') return m.access_live_status_degraded();
		return m.access_live_status_online();
	};
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>{m.access_live_controllers_title()}</CardTitle>
		<CardDescription>{m.access_live_controllers_desc()}</CardDescription>
	</CardHeader>
	<CardContent class="space-y-3 pt-4">
		{#each controllers as controller}
			<div class="space-y-3 rounded-[10px] border border-border bg-background p-3">
				<div class="flex items-start justify-between gap-3">
					<div class="min-w-0">
						<p class="truncate text-sm font-semibold">{controller.name}</p>
						<p class="mt-1 text-xs text-muted-foreground">
							{m.access_live_last_seen({ time: controller.lastSeen })} ·
							{m.access_live_latency_ms({ ms: controller.latencyMs })}
						</p>
					</div>
					<Badge variant={statusBadge(controller.status)}>{statusLabel(controller.status)}</Badge>
				</div>

				<div class="flex flex-wrap gap-2">
					<Button size="sm" variant="outline" class="h-8">
						<KeyRoundIcon class="size-3.5" />
						{m.access_live_action_unlock()}
					</Button>
					<Button size="sm" variant="outline" class="h-8">
						<BoltIcon class="size-3.5" />
						{m.access_live_action_power_cycle()}
					</Button>
					<Button size="sm" class="h-8">
						<WrenchIcon class="size-3.5" />
						{m.access_live_action_tech_ticket()}
					</Button>
				</div>
			</div>
		{/each}
	</CardContent>
</Card>
