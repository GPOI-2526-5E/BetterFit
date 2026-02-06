<script lang="ts">
	import DoorOpenIcon from '@lucide/svelte/icons/door-open';
	import ShowerHeadIcon from '@lucide/svelte/icons/shower-head';
	import * as m from '$lib/paraglide/messages.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>{m.access_live_desk_actions_title()}</CardTitle>
		<CardDescription>{m.access_live_desk_actions_desc()}</CardDescription>
	</CardHeader>
	<CardContent class="space-y-2 pt-4">
		<Button variant="outline" class="w-full justify-start">
			<DoorOpenIcon class="size-4" />
			{m.access_live_desk_action_open_turnstile_1()}
		</Button>
		<Button variant="outline" class="w-full justify-start">
			<DoorOpenIcon class="size-4" />
			{m.access_live_desk_action_open_turnstile_2()}
		</Button>
		<Button variant="outline" class="w-full justify-start">
			<ShowerHeadIcon class="size-4" />
			{m.access_live_desk_action_enable_women_showers()}
		</Button>
		<Button variant="outline" class="w-full justify-start">
			<ShowerHeadIcon class="size-4" />
			{m.access_live_desk_action_enable_men_showers()}
		</Button>
		<p class="pt-2 text-xs text-muted-foreground">{m.access_live_desk_action_note()}</p>
	</CardContent>
</Card>
