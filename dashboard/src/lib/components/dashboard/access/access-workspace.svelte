<script lang="ts">
	import { createQuery } from '@tanstack/svelte-query';
	import DoorOpenIcon from '@lucide/svelte/icons/door-open';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import SearchIcon from '@lucide/svelte/icons/search';
	import type { GymMembershipResponse } from '$lib/api';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import { Button } from '$lib/components/ui/button/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import * as Select from '$lib/components/ui/select/index.js';
	import {
		Table,
		TableBody,
		TableCell,
		TableHead,
		TableHeader,
		TableRow
	} from '$lib/components/ui/table/index.js';
	import {
		type AccessEventResult,
		type RecordGymCheckinRequest,
		fetchGymAccessEvents,
		fetchGymAccessOverview,
		recordGymCheckin
	} from '$lib/data/access-api';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useCenterSelectionStore } from '$lib/stores/CenterSelectionStoreProvider.svelte';

	type Mode = 'live' | 'history' | 'checkin';
	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';
	const ACCESS_ALL_MEMBERS_SELECT_VALUE = '__all_members__';

	let {
		mode = 'live',
		presetMembershipId = null
	}: {
		mode?: Mode;
		presetMembershipId?: string | null;
	} = $props();

	const api = useApiClient();
	const center = useCenterSelectionStore();
	const dateTime = new Intl.DateTimeFormat('it-IT', {
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	});

	const eventResults: Array<{ value: AccessEventResult | 'all'; label: string }> = [
		{ value: 'all', label: 'Tutti i risultati' },
		{ value: 'Granted', label: 'Consentiti' },
		{ value: 'Denied', label: 'Negati' },
		{ value: 'ManualOverride', label: 'Override manuali' }
	];

	const membershipName = (membership: GymMembershipResponse) =>
		[membership.memberProfile?.firstName, membership.memberProfile?.lastName]
			.filter(Boolean)
			.join(' ')
			.trim() ||
		membership.userEmail ||
		membership.invitationEmail ||
		'Cliente senza nome';

	const eventMeta = (result: AccessEventResult): { label: string; variant: BadgeVariant } => {
		if (result === 'Denied') return { label: 'Negato', variant: 'destructive' };
		if (result === 'ManualOverride') return { label: 'Override', variant: 'warning' };
		return { label: 'Consentito', variant: 'success' };
	};

	const originLabel = (origin: string) => {
		if (origin === 'Desk') return 'Desk';
		if (origin === 'Badge') return 'Badge';
		if (origin === 'AppQr') return 'App QR';
		return 'Altro';
	};

	let searchTerm = $state('');
	let resultFilter = $state<AccessEventResult | 'all'>('all');
	let membershipFilter = $state('');
	let submitMessage = $state('');
	let submitError = $state('');
	let submitPending = $state(false);
	let checkinForm = $state({
		membershipId: '',
		locationId: center.selectedLocationId ?? '',
		gateName: 'Desk check-in'
	});

	const membershipsQuery = createQuery(() => ({
		queryKey: ['access-memberships', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: async () => {
			const response = await api.client.apiGymsGymIdMembershipsGet({
				gymId: center.selectedGymId!
			});
			if (!response.success)
				throw new Error(
					response.error?.message ?? response.message ?? 'Impossibile caricare le membership.'
				);
			return response.data ?? [];
		}
	}));

	const overviewQuery = createQuery(() => ({
		queryKey: ['access-overview', center.selectedGymId],
		enabled: !!center.selectedGymId,
		queryFn: () => fetchGymAccessOverview(center.selectedGymId!)
	}));

	const eventsQuery = createQuery(() => ({
		queryKey: [
			'access-events',
			center.selectedGymId,
			mode,
			resultFilter,
			searchTerm,
			membershipFilter
		],
		enabled: !!center.selectedGymId,
		queryFn: () =>
			fetchGymAccessEvents(center.selectedGymId!, {
				result: resultFilter,
				search: searchTerm,
				membershipId: membershipFilter || null
			})
	}));

	const memberships = $derived(membershipsQuery.data ?? []);
	const membershipOptions = $derived(
		memberships
			.map((membership) => ({
				id: membership.membershipId ?? '',
				label: membershipName(membership),
				email: membership.userEmail ?? membership.invitationEmail ?? '',
				primaryLocationId: membership.primaryLocationId ?? '',
				locations: membership.locations ?? [],
				status: membership.status ?? 'PendingClaim'
			}))
			.filter((membership) => membership.id)
	);
	const filteredLocations = $derived.by(() => {
		const current = membershipOptions.find(
			(membership) => membership.id === checkinForm.membershipId
		);
		if (!current) return center.locations;
		const ids = new Set(current.locations.map((location) => location.id));
		return center.locations.filter((location) => ids.has(location.id));
	});
	const displayedEvents = $derived(
		mode === 'live' ? (overviewQuery.data?.recentEvents ?? []) : (eventsQuery.data ?? [])
	);
	const pageTitle = $derived(
		mode === 'history'
			? 'Storico accessi'
			: mode === 'checkin'
				? 'Check-in desk'
				: 'Monitor accessi'
	);
	const pageDescription = $derived(
		mode === 'history'
			? 'Storico reale degli accessi registrati per sede.'
			: mode === 'checkin'
				? 'Registra un ingresso desk con regole membership e log accessi.'
				: 'Monitor live di ingressi consentiti, negati e attivita desk.'
	);
	const selectedResultFilterLabel = $derived(
		eventResults.find((option) => option.value === resultFilter)?.label ?? 'Tutti i risultati'
	);
	const selectedMembershipFilterLabel = $derived(
		membershipFilter
			? (membershipOptions.find((membership) => membership.id === membershipFilter)?.label ??
					'Membro selezionato')
			: 'Tutti i membri'
	);
	const selectedCheckinMembershipLabel = $derived(
		membershipOptions.find((membership) => membership.id === checkinForm.membershipId)?.label ??
			'Seleziona membro'
	);
	const selectedCheckinLocationLabel = $derived(
		checkinForm.locationId
			? (filteredLocations.find((location) => location.id === checkinForm.locationId)?.name ??
					'Sede selezionata')
			: 'Seleziona sede'
	);
	const membershipsQueryError = $derived(
		membershipsQuery.error instanceof Error
			? membershipsQuery.error.message
			: membershipsQuery.error
				? 'Impossibile caricare le membership.'
				: null
	);
	const overviewQueryError = $derived(
		overviewQuery.error instanceof Error
			? overviewQuery.error.message
			: overviewQuery.error
				? 'Impossibile caricare l overview accessi.'
				: null
	);
	const eventsQueryError = $derived(
		eventsQuery.error instanceof Error
			? eventsQuery.error.message
			: eventsQuery.error
				? 'Impossibile caricare gli eventi accesso.'
				: null
	);
	const hasSelectedGym = $derived(!!center.selectedGymId);
	const workspaceLoading = $derived(
		center.isLoadingGyms
			|| (!!center.selectedGymId
				&& ((!membershipsQuery.data && membershipsQuery.isPending)
					|| (!overviewQuery.data && overviewQuery.isPending)
					|| (!eventsQuery.data && eventsQuery.isPending)))
	);
	const workspaceError = $derived(
		center.gymsError ?? membershipsQueryError ?? overviewQueryError ?? eventsQueryError ?? null
	);

	$effect(() => {
		if (!checkinForm.membershipId && membershipOptions.length > 0) {
			const first = membershipOptions[0];
			checkinForm = {
				...checkinForm,
				membershipId: first.id,
				locationId:
					center.selectedLocationId || first.primaryLocationId || first.locations[0]?.id || ''
			};
		}
	});
	$effect(() => {
		if (!presetMembershipId) {
			return;
		}

		const preset = membershipOptions.find((membership) => membership.id === presetMembershipId);
		if (!preset) {
			return;
		}

		if (membershipFilter !== preset.id) {
			membershipFilter = preset.id;
		}

		if (checkinForm.membershipId === preset.id) {
			return;
		}

		checkinForm = {
			...checkinForm,
			membershipId: preset.id,
			locationId:
				center.selectedLocationId || preset.primaryLocationId || preset.locations[0]?.id || ''
		};
	});

	async function refreshAll() {
		if (!center.selectedGymId) {
			return;
		}

		await Promise.all([overviewQuery.refetch(), eventsQuery.refetch(), membershipsQuery.refetch()]);
	}

	async function handleCheckin(event: Event) {
		event.preventDefault();
		submitMessage = '';
		submitError = '';

		if (!center.selectedGymId || !checkinForm.membershipId) {
			submitError = 'Seleziona tenant e membro prima di registrare un ingresso.';
			return;
		}

		submitPending = true;
		try {
			const result = await recordGymCheckin(center.selectedGymId, {
				membershipId: checkinForm.membershipId,
				locationId: checkinForm.locationId || null,
				gateName: checkinForm.gateName.trim() || null
			} satisfies RecordGymCheckinRequest);

			await Promise.all([overviewQuery.refetch(), eventsQuery.refetch()]);

			if (result.result === 'Granted') {
				submitMessage = `Ingresso consentito per ${result.memberName} su ${result.locationName}.`;
			} else {
				submitError = result.reason ?? `Ingresso negato per ${result.memberName}.`;
			}
		} catch (error: unknown) {
			submitError = error instanceof Error ? error.message : 'Impossibile registrare il check-in.';
		} finally {
			submitPending = false;
		}
	}
</script>

<main class="grid gap-4 p-4 md:gap-6 md:p-6">
	<section class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
		<div class="space-y-2">
			<div class="flex flex-wrap items-center gap-2">
				<Badge variant="secondary" class="rounded-full px-3 py-1">Accessi</Badge>
				{#if center.selectedGym}<Badge variant="outline" class="rounded-full px-3 py-1"
						>{center.selectedGym.name}</Badge
					>{/if}
				{#if center.selectedLocation}<Badge variant="outline" class="rounded-full px-3 py-1"
						>{center.selectedLocation.name}</Badge
					>{/if}
			</div>
			<div>
				<h2 class="text-2xl font-semibold tracking-tight">{pageTitle}</h2>
				<p class="text-sm text-muted-foreground">{pageDescription}</p>
			</div>
		</div>
		<div class="flex flex-wrap items-center gap-2">
			{#if overviewQuery.data}
				<p class="text-xs text-muted-foreground">
					Ultimo sync {dateTime.format(overviewQuery.data.lastSyncUtc)}
				</p>
			{/if}
			<Button variant="outline" size="sm" onclick={refreshAll} disabled={!hasSelectedGym || workspaceLoading}
				><RefreshCwIcon class="size-4" />Aggiorna</Button
			>
		</div>
	</section>

	{#if submitMessage}<section
			class="rounded-[20px] border border-[#bbf7d0] bg-[#f0fdf4] px-4 py-3 text-sm text-[#166534]"
		>
			{submitMessage}
		</section>{/if}
	{#if submitError}<section
			class="rounded-[20px] border border-[#fecaca] bg-[#fff7f7] px-4 py-3 text-sm text-[#991b1b]"
		>
			{submitError}
		</section>{/if}

	{#if workspaceLoading}
		<Card class="border-dashed border-border/70 bg-muted/25">
			<CardHeader>
				<CardTitle>Caricamento area accessi</CardTitle>
				<CardDescription>
					Sto recuperando membership, overview e log accessi prima di mostrarti il monitor reale del tenant.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else if workspaceError}
		<Card class="border-dashed border-[#fecaca] bg-[#fff7f7]">
			<CardHeader>
				<CardTitle>Impossibile caricare l area accessi</CardTitle>
				<CardDescription>{workspaceError}</CardDescription>
			</CardHeader>
			<CardContent>
				<Button variant="outline" onclick={refreshAll} disabled={!hasSelectedGym}>
					<RefreshCwIcon class="mr-2 size-4" />
					Riprova
				</Button>
			</CardContent>
		</Card>
	{:else if !hasSelectedGym}
		<Card class="border-dashed border-border/70 bg-muted/25">
			<CardHeader>
				<CardTitle>Seleziona un tenant</CardTitle>
				<CardDescription>
					Scegli prima la palestra dal selettore in alto a sinistra per vedere monitor, storico e desk check-in del tenant corretto.
				</CardDescription>
			</CardHeader>
		</Card>
	{:else}
	{#if overviewQuery.data}
		<section class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
			<Card
				><CardHeader class="pb-2"
					><CardDescription>Presenti oggi</CardDescription><CardTitle class="text-3xl"
						>{overviewQuery.data.peoplePresentTodayCount}</CardTitle
					></CardHeader
				><CardContent class="text-sm text-muted-foreground"
					>Membri con accesso consentito oggi</CardContent
				></Card
			>
			<Card
				><CardHeader class="pb-2"
					><CardDescription>Check-in ultimi 30m</CardDescription><CardTitle class="text-3xl"
						>{overviewQuery.data.checkinsLast30MinutesCount}</CardTitle
					></CardHeader
				><CardContent class="text-sm text-muted-foreground">Ingressi consentiti recenti</CardContent
				></Card
			>
			<Card
				><CardHeader class="pb-2"
					><CardDescription>Negati oggi</CardDescription><CardTitle class="text-3xl"
						>{overviewQuery.data.deniedTodayCount}</CardTitle
					></CardHeader
				><CardContent class="text-sm text-muted-foreground"
					>Tentativi bloccati dalle regole</CardContent
				></Card
			>
			<Card
				><CardHeader class="pb-2"
					><CardDescription>Desk approvati</CardDescription><CardTitle class="text-3xl"
						>{overviewQuery.data.deskApprovalsTodayCount}</CardTitle
					></CardHeader
				><CardContent class="text-sm text-muted-foreground"
					>Check-in registrati manualmente</CardContent
				></Card
			>
		</section>
	{/if}

	<section class="grid gap-4 xl:grid-cols-[1.5fr_1fr]">
		<Card>
			<CardHeader class="gap-4">
				<div class="flex items-center justify-between gap-3">
					<div>
						<CardTitle>{mode === 'history' ? 'Storico accessi' : 'Eventi accesso'}</CardTitle
						><CardDescription
							>{mode === 'history'
								? 'Storico filtrabile per ricerca e risultato.'
								: 'Ultimi accessi registrati nel sistema.'}</CardDescription
						>
					</div>
					<Badge variant="outline">{displayedEvents.length} eventi</Badge>
				</div>
				<div class="grid gap-3 md:grid-cols-3">
					<div class="relative">
						<SearchIcon
							class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
						/><Input
							class="pl-9"
							placeholder="Cerca per membro, gate o motivo"
							bind:value={searchTerm}
						/>
					</div>
					<Select.Root type="single" bind:value={resultFilter}>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{selectedResultFilterLabel}</span>
						</Select.Trigger>
						<Select.Content>
							{#each eventResults as option (option.value)}
								<Select.Item value={option.value} label={option.label}>
									{option.label}
								</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
					<Select.Root
						type="single"
						value={membershipFilter || ACCESS_ALL_MEMBERS_SELECT_VALUE}
						onValueChange={(value) =>
							(membershipFilter = value === ACCESS_ALL_MEMBERS_SELECT_VALUE ? '' : value)}
					>
						<Select.Trigger class="w-full">
							<span data-slot="select-value">{selectedMembershipFilterLabel}</span>
						</Select.Trigger>
						<Select.Content>
							<Select.Item value={ACCESS_ALL_MEMBERS_SELECT_VALUE} label="Tutti i membri">
								Tutti i membri
							</Select.Item>
							{#each membershipOptions as membership (membership.id)}
								<Select.Item value={membership.id} label={membership.label}>
									{membership.label}
								</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
				</div>
			</CardHeader>
			<CardContent>
				{#if (mode === 'history' && eventsQuery.isPending) || (mode === 'live' && overviewQuery.isPending)}
					<div
						class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
					>
						<p class="font-semibold">Carico gli accessi...</p>
					</div>
				{:else if displayedEvents.length === 0}
					<div
						class="rounded-[20px] border border-dashed border-border bg-secondary/20 px-5 py-10 text-center"
					>
						<p class="font-semibold">Nessun accesso registrato</p>
						<p class="mt-2 text-sm text-muted-foreground">
							Usa il desk check-in per creare i primi eventi reali.
						</p>
					</div>
				{:else}
					<div class="overflow-hidden rounded-[20px] border border-border/70">
						<Table>
							<TableHeader
								><TableRow class="bg-secondary/30 hover:bg-secondary/30"
									><TableHead>Ora</TableHead><TableHead>Membro</TableHead><TableHead
										>Sede / Gate</TableHead
									><TableHead>Esito</TableHead><TableHead>Origine</TableHead></TableRow
								></TableHeader
							>
							<TableBody>
								{#each displayedEvents as event (event.eventId)}
									<TableRow>
										<TableCell
											><div>
												<p class="font-medium">{dateTime.format(event.occurredAtUtc)}</p>
												{#if event.reason}<p class="text-xs text-muted-foreground">
														{event.reason}
													</p>{/if}
											</div></TableCell
										>
										<TableCell
											><div>
												<p class="font-medium">{event.memberName}</p>
												<p class="text-sm text-muted-foreground">{event.memberEmail}</p>
											</div></TableCell
										>
										<TableCell
											><div>
												<p class="font-medium">{event.locationName}</p>
												<p class="text-sm text-muted-foreground">{event.gateName}</p>
											</div></TableCell
										>
										<TableCell
											><Badge variant={eventMeta(event.result).variant}
												>{eventMeta(event.result).label}</Badge
											></TableCell
										>
										<TableCell class="text-muted-foreground">{originLabel(event.origin)}</TableCell>
									</TableRow>
								{/each}
							</TableBody>
						</Table>
					</div>
				{/if}
			</CardContent>
		</Card>

		<div class="space-y-4">
			<Card>
				<CardHeader
					><CardTitle>Check-in desk</CardTitle><CardDescription
						>Registra un ingresso reale su membro e sede.</CardDescription
					></CardHeader
				>
				<CardContent>
					<form class="space-y-4" onsubmit={handleCheckin}>
						<label class="space-y-2">
							<span class="text-sm font-medium">Membro</span>
							<Select.Root type="single" bind:value={checkinForm.membershipId}>
								<Select.Trigger class="w-full">
									<span data-slot="select-value">{selectedCheckinMembershipLabel}</span>
								</Select.Trigger>
								<Select.Content>
									{#each membershipOptions as membership (membership.id)}
										<Select.Item
											value={membership.id}
											label={`${membership.label} - ${membership.email}`}
										>
											{membership.label} - {membership.email}
										</Select.Item>
									{/each}
								</Select.Content>
							</Select.Root>
						</label>
						<label class="space-y-2">
							<span class="text-sm font-medium">Sede</span>
							<Select.Root type="single" bind:value={checkinForm.locationId}>
								<Select.Trigger class="w-full">
									<span data-slot="select-value">{selectedCheckinLocationLabel}</span>
								</Select.Trigger>
								<Select.Content>
									{#each filteredLocations as location (location.id)}
										<Select.Item value={location.id} label={location.name}>
											{location.name}
										</Select.Item>
									{/each}
								</Select.Content>
							</Select.Root>
						</label>
						<label class="space-y-2">
							<span class="text-sm font-medium">Gate / canale</span>
							<Input bind:value={checkinForm.gateName} placeholder="Desk check-in" />
						</label>
						<Button type="submit" class="w-full" disabled={submitPending}>
							{#if submitPending}
								<RefreshCwIcon class="size-4 animate-spin" />
								Registrazione...
							{:else}
								<DoorOpenIcon class="size-4" />
								Registra ingresso
							{/if}
						</Button>
					</form>
				</CardContent>
			</Card>

			<Card>
				<CardHeader
					><CardTitle>Negati recenti</CardTitle><CardDescription
						>Raggruppamento dei tentativi bloccati oggi.</CardDescription
					></CardHeader
				>
				<CardContent class="space-y-3">
					{#if overviewQuery.data?.deniedAttempts.length}
						{#each overviewQuery.data.deniedAttempts as denied (denied.memberName + denied.reason)}
							<div class="rounded-[14px] border border-border/70 bg-secondary/15 p-3">
								<div class="flex items-start justify-between gap-3">
									<div>
										<p class="font-medium">{denied.memberName}</p>
										<p class="mt-1 text-sm text-muted-foreground">{denied.reason}</p>
									</div>
									<Badge variant="warning">{denied.attempts} tentativi</Badge>
								</div>
								<p class="mt-2 text-xs text-muted-foreground">
									Ultimo tentativo {dateTime.format(denied.lastAttemptAtUtc)}
								</p>
							</div>
						{/each}
					{:else}
						<div
							class="rounded-[14px] border border-dashed border-border bg-secondary/15 px-4 py-6 text-center text-sm text-muted-foreground"
						>
							Nessun negato recente nella giornata corrente.
						</div>
					{/if}
				</CardContent>
			</Card>
		</div>
	</section>
	{/if}
</main>
