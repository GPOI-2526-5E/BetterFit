<script lang="ts">
	import * as m from '$lib/paraglide/messages.js';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import type { AlertStatus, HomeAlert } from './types';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';

	let { alerts }: { alerts: HomeAlert[] } = $props();

	const alertBadge = (status: AlertStatus): BadgeVariant => {
		if (status === 'critical') return 'destructive';
		if (status === 'warning') return 'warning';
		return 'success';
	};

	const alertLabel = (status: AlertStatus): string => {
		if (status === 'critical') return m.alert_status_critical();
		if (status === 'warning') return m.alert_status_warning();
		return m.alert_status_ok();
	};
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>{m.home_alerts_title()}</CardTitle>
		<CardDescription>{m.home_alerts_desc()}</CardDescription>
	</CardHeader>
	<CardContent class="space-y-3 pt-4">
		{#each alerts as alert}
			<div
				class="flex items-start justify-between gap-3 rounded-[10px] border border-border bg-background p-3"
			>
				<div class="min-w-0 flex-1">
					<p class="truncate text-sm font-semibold">{alert.title}</p>
					<p class="mt-1 text-xs text-muted-foreground">{alert.description}</p>
				</div>
				<Badge variant={alertBadge(alert.status)}>{alertLabel(alert.status)}</Badge>
			</div>
		{/each}
	</CardContent>
</Card>
