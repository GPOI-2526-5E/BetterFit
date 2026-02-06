<script lang="ts">
	import { Skeleton } from '$lib/components/ui/skeleton/index.js';
</script>

<section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
	{#each Array(4) as _}
		<Skeleton class="h-30" />
	{/each}
</section>
<section class="grid gap-4 xl:grid-cols-[1.4fr_1fr]">
	<Skeleton class="h-90" />
	<Skeleton class="h-90" />
</section>
