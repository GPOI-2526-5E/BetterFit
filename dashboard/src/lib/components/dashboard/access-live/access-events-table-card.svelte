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
	import {
		Table,
		TableBody,
		TableCell,
		TableHead,
		TableHeader,
		TableRow
	} from '$lib/components/ui/table/index.js';
	import type { AccessLiveEvent, AccessLiveOrigin, AccessLiveResult } from './types';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';

	let { events }: { events: AccessLiveEvent[] } = $props();

	const eventBadge = (result: AccessLiveResult): BadgeVariant => {
		if (result === 'denied') return 'destructive';
		if (result === 'manual_override') return 'warning';
		return 'success';
	};

	const eventLabel = (result: AccessLiveResult): string => {
		if (result === 'denied') return m.access_live_result_denied();
		if (result === 'manual_override') return m.access_live_result_manual_override();
		return m.access_live_result_granted();
	};

	const originLabel = (origin: AccessLiveOrigin): string => {
		if (origin === 'badge') return m.access_live_origin_badge();
		if (origin === 'app_qr') return m.access_live_origin_app_qr();
		if (origin === 'desk') return m.access_live_origin_desk();
		return m.access_live_origin_unknown();
	};
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>{m.access_live_events_title()}</CardTitle>
		<CardDescription>{m.access_live_events_desc()}</CardDescription>
	</CardHeader>
	<CardContent class="pt-4">
		<Table>
			<TableHeader>
				<TableRow>
					<TableHead>{m.access_live_col_time()}</TableHead>
					<TableHead>{m.access_live_col_member()}</TableHead>
					<TableHead>{m.access_live_col_gate()}</TableHead>
					<TableHead>{m.access_live_col_result()}</TableHead>
					<TableHead>{m.access_live_col_origin()}</TableHead>
				</TableRow>
			</TableHeader>
			<TableBody>
				{#each events as event}
					<TableRow>
						<TableCell class="font-medium">{event.time}</TableCell>
						<TableCell>{event.member}</TableCell>
						<TableCell>{event.gate}</TableCell>
						<TableCell>
							<Badge variant={eventBadge(event.result)}>{eventLabel(event.result)}</Badge>
						</TableCell>
						<TableCell class="text-muted-foreground">{originLabel(event.origin)}</TableCell>
					</TableRow>
				{/each}
			</TableBody>
		</Table>
	</CardContent>
</Card>
