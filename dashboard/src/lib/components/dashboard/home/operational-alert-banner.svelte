<script lang="ts">
	import AlertCircleIcon from '@lucide/svelte/icons/alert-circle';

	let { message }: { message: string } = $props();
</script>

<div
	class="flex items-center gap-2 rounded-[10px] border border-error-600/35 bg-error-600 px-4 py-3 text-sm font-semibold text-white shadow-[var(--bf-shadow-card)]"
>
	<AlertCircleIcon class="size-4 shrink-0" />
	<span>{message}</span>
</div>
