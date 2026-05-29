<script lang="ts">
	import ActivityIcon from '@lucide/svelte/icons/activity';
	import CalendarClockIcon from '@lucide/svelte/icons/calendar-clock';
	import IdCardIcon from '@lucide/svelte/icons/id-card';
	import TriangleAlertIcon from '@lucide/svelte/icons/triangle-alert';
	import UserMinusIcon from '@lucide/svelte/icons/user-minus';
	import UsersIcon from '@lucide/svelte/icons/users';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import type { HomeStatusCard } from './types';

	let { cards }: { cards: HomeStatusCard[] } = $props();

	const iconFor = (id: HomeStatusCard['id']) => {
		if (id === 'profiles') return IdCardIcon;
		if (id === 'activeUsers') return UsersIcon;
		if (id === 'lowAccessUsers') return UserMinusIcon;
		if (id === 'noAccessUsers') return TriangleAlertIcon;
		if (id === 'expiringToday') return CalendarClockIcon;
		return ActivityIcon;
	};

	const toneClasses = (tone: HomeStatusCard['tone']) => {
		if (tone === 'danger') return 'border-error-600/30 bg-[#fff4f4]';
		if (tone === 'warning') return 'border-warning-600/30 bg-[#fff9ef]';
		if (tone === 'success') return 'border-success-600/30 bg-[#f4fff6]';
		return 'border-border bg-card';
	};

	const iconToneClasses = (tone: HomeStatusCard['tone']) => {
		if (tone === 'danger') return 'bg-error-600 text-white';
		if (tone === 'warning') return 'bg-warning-600 text-white';
		if (tone === 'success') return 'bg-success-600 text-white';
		return 'bg-secondary text-secondary-foreground';
	};
</script>

<section class="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
	{#each cards as card}
		{@const CardIcon = iconFor(card.id)}
		<Card class={toneClasses(card.tone)}>
			<CardHeader class="pb-2">
				<div class="flex items-center justify-between gap-3">
					<div
						class={`grid size-9 place-items-center rounded-[10px] ${iconToneClasses(card.tone)}`}
					>
						<CardIcon class="size-4" />
					</div>
				</div>
				<CardDescription class="pt-2 text-sm text-foreground/75">{card.label}</CardDescription>
			</CardHeader>
			<CardContent>
				<CardTitle class="text-4xl leading-none">{card.value}</CardTitle>
				{#if card.description}
					<p class="pt-2 text-xs text-muted-foreground">{card.description}</p>
				{/if}
			</CardContent>
		</Card>
	{/each}
</section>
