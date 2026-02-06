<script lang="ts">
	import CircleAlertIcon from '@lucide/svelte/icons/circle-alert';
	import CircleCheckIcon from '@lucide/svelte/icons/circle-check';
	import CircleDashedIcon from '@lucide/svelte/icons/circle-dashed';
	import * as m from '$lib/paraglide/messages.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import { Tabs, TabsContent, TabsList, TabsTrigger } from '$lib/components/ui/tabs/index.js';
	import type { HomeTimelineEvent } from './types';

	let { timeline }: { timeline: HomeTimelineEvent[] } = $props();
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>{m.home_user_feed_title()}</CardTitle>
		<CardDescription>{m.home_user_feed_desc()}</CardDescription>
	</CardHeader>
	<CardContent class="pt-4">
		<Tabs value="timeline" class="w-full">
			<TabsList class="mb-4 w-full">
				<TabsTrigger value="timeline">{m.tab_timeline()}</TabsTrigger>
				<TabsTrigger value="health">{m.tab_health_check()}</TabsTrigger>
			</TabsList>
			<TabsContent value="timeline" class="space-y-2">
				{#each timeline as event}
					<div class="flex items-start gap-2 rounded-[10px] border border-border bg-background p-3">
						<CircleCheckIcon class="mt-0.5 size-4 shrink-0 text-chart-2" />
						<div class="min-w-0">
							<p class="text-sm font-medium">{event.text}</p>
							<p class="mt-1 text-xs text-muted-foreground">{event.time}</p>
						</div>
					</div>
				{/each}
			</TabsContent>
			<TabsContent value="health" class="space-y-2">
				<div class="flex items-start gap-2 rounded-[10px] border border-border bg-background p-3">
					<CircleAlertIcon class="mt-0.5 size-4 shrink-0 text-warning-600" />
					<div>
						<p class="text-sm font-medium">{m.health_medical_expiring_title()}</p>
						<p class="mt-1 text-xs text-muted-foreground">{m.health_medical_expiring_desc()}</p>
					</div>
				</div>
				<div class="flex items-start gap-2 rounded-[10px] border border-border bg-background p-3">
					<CircleDashedIcon class="mt-0.5 size-4 shrink-0 text-muted-foreground" />
					<div>
						<p class="text-sm font-medium">{m.health_anamnesis_update_title()}</p>
						<p class="mt-1 text-xs text-muted-foreground">{m.health_anamnesis_update_desc()}</p>
					</div>
				</div>
			</TabsContent>
		</Tabs>
	</CardContent>
</Card>
