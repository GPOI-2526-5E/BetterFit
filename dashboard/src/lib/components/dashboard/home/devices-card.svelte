<script lang="ts">
	import { Badge } from '$lib/components/ui/badge/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import type { DeviceStatus, HomeDevice } from './types';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';

	let { devices }: { devices: HomeDevice[] } = $props();

	const deviceBadge = (status: DeviceStatus): BadgeVariant => {
		if (status === 'offline') return 'destructive';
		if (status === 'maintenance') return 'warning';
		return 'success';
	};

	const deviceLabel = (status: DeviceStatus): string => {
		if (status === 'offline') return 'Offline';
		if (status === 'maintenance') return 'Attenzione';
		return 'Online';
	};
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>Sistemi e integrazioni</CardTitle>
		<CardDescription>
			Stato operativo di accessi, messaggistica ed export amministrativi del tenant.
		</CardDescription>
	</CardHeader>
	<CardContent class="space-y-3 pt-4">
		{#each devices as device}
			<div
				class="flex items-center justify-between rounded-[10px] border border-border bg-background p-3"
			>
				<div>
					<p class="text-sm font-semibold">{device.name}</p>
					<p class="mt-1 text-xs text-muted-foreground">{device.lastEvent}</p>
				</div>
				<Badge variant={deviceBadge(device.status)}>{deviceLabel(device.status)}</Badge>
			</div>
		{/each}
	</CardContent>
</Card>
