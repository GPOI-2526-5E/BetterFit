<script lang="ts">
	import CreditCardIcon from '@lucide/svelte/icons/credit-card';
	import ReceiptTextIcon from '@lucide/svelte/icons/receipt-text';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import type { HomeRecentCollection } from './types';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';

	let { recentCollections }: { recentCollections: HomeRecentCollection[] } = $props();

	const methodLabel = (method: string) => {
		if (method === 'Cash') return 'Contanti';
		if (method === 'Card') return 'Carta';
		if (method === 'BankTransfer') return 'Bonifico';
		if (method === 'DirectDebit') return 'RID';
		if (method === 'DigitalWallet') return 'Wallet';
		return 'Altro';
	};

	const methodBadge = (method: string): BadgeVariant => {
		if (method === 'Cash') return 'secondary';
		if (method === 'Card') return 'success';
		if (method === 'BankTransfer') return 'outline';
		if (method === 'DigitalWallet') return 'warning';
		return 'default';
	};
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>Ultimi incassi</CardTitle>
		<CardDescription>
			Movimenti gia saldati e pronti da ricontrollare dal desk con riferimento ricevuta.
		</CardDescription>
	</CardHeader>
	<CardContent class="space-y-3 pt-4">
		{#if recentCollections.length === 0}
			<div
				class="rounded-[14px] border border-dashed border-border bg-secondary/20 px-4 py-8 text-center"
			>
				<ReceiptTextIcon class="mx-auto size-5 text-muted-foreground" />
				<p class="mt-3 text-sm font-semibold">Nessun incasso recente</p>
				<p class="mt-2 text-sm text-muted-foreground">
					Appena vengono chiusi pagamenti nel tenant li vedrai comparire qui.
				</p>
			</div>
		{:else}
			{#each recentCollections as collection}
				<div class="rounded-[14px] border border-border bg-background p-3">
					<div class="flex items-start justify-between gap-3">
						<div class="min-w-0">
							<div class="flex flex-wrap items-center gap-2">
								<p class="text-sm font-semibold">{collection.referenceCode}</p>
								<Badge variant="outline">{collection.receiptCode}</Badge>
							</div>
							<p class="mt-1 text-sm text-muted-foreground">{collection.memberName}</p>
							<p class="mt-1 text-xs text-muted-foreground">
								{collection.locationName} - {collection.paidAt}
							</p>
						</div>
						<div class="shrink-0 text-right">
							<div class="inline-flex items-center gap-2 text-sm font-semibold">
								<CreditCardIcon class="size-4 text-chart-2" />
								{collection.amountLabel}
							</div>
							<div class="mt-2">
								<Badge variant={methodBadge(collection.method)}
									>{methodLabel(collection.method)}</Badge
								>
							</div>
						</div>
					</div>
				</div>
			{/each}
		{/if}
	</CardContent>
</Card>
