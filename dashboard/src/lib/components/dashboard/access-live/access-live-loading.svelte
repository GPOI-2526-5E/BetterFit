<script lang="ts">
	import { Skeleton } from '$lib/components/ui/skeleton/index.js';
</script>

<section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
	{#each Array(4) as _}
		<Skeleton class="h-30" />
	{/each}
</section>
<section class="grid gap-4 xl:grid-cols-[1.6fr_1fr]">
	<Skeleton class="h-96" />
	<Skeleton class="h-96" />
</section>
<section class="grid gap-4 xl:grid-cols-2">
	<Skeleton class="h-78" />
	<Skeleton class="h-78" />
</section>
