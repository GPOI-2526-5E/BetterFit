<script lang="ts">
	import * as Sheet from '$lib/components/ui/sheet/index.js';
	import * as m from '$lib/paraglide/messages.js';
	import SetupOnboardingFlow from './setup-onboarding-flow.svelte';
	import type { SetupIntent } from '$lib/utils/dashboard-onboarding-validation.js';

	let {
		open = $bindable(false),
		lockedIntent = null
	}: {
		open?: boolean;
		lockedIntent?: SetupIntent | null;
	} = $props();
</script>

<Sheet.Root bind:open>
	<Sheet.Content
		side="right"
		class="w-full border-border/70 bg-[linear-gradient(180deg,rgba(247,250,255,0.98),rgba(255,255,255,0.98))] p-0 sm:max-w-3xl"
	>
		<Sheet.Header class="sr-only">
			<Sheet.Title>{m.setup_flow_sheet_title()}</Sheet.Title>
			<Sheet.Description>{m.setup_flow_sheet_desc()}</Sheet.Description>
		</Sheet.Header>

		<div class="h-full overflow-y-auto p-4 md:p-6">
			{#if open}
				<SetupOnboardingFlow
					surface="sheet"
					{lockedIntent}
					showDismissAction={true}
					onClose={() => {
						open = false;
					}}
				/>
			{/if}
		</div>
	</Sheet.Content>
</Sheet.Root>
