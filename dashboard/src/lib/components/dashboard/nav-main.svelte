<script lang="ts">
	import * as Collapsible from '$lib/components/ui/collapsible/index.js';
	import * as Sidebar from '$lib/components/ui/sidebar/index.js';
	import ChevronRightIcon from '@lucide/svelte/icons/chevron-right';

	let {
		items,
		currentPath,
		currentHash,
		groupLabel,
		comingSoonSuffix,
		disabled = false,
		disabledSuffix
	}: {
		items: {
			title: string;
			url: string;
			// This should be `Component` after @lucide/svelte updates types
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			icon?: any;
			items?: {
				title: string;
				url: string;
				available?: boolean;
			}[];
		}[];
		currentPath: string;
		currentHash: string;
		groupLabel: string;
		comingSoonSuffix: string;
		disabled?: boolean;
		disabledSuffix?: string;
	} = $props();

	const isTopLevelActive = (itemUrl: string): boolean => {
		if (itemUrl === '/') return currentPath === '/';
		return currentPath.startsWith(itemUrl);
	};

	const isSubItemActive = (subItemUrl: string): boolean => {
		if (!subItemUrl.includes('#')) return currentPath === subItemUrl;
		const [path, hash] = subItemUrl.split('#');
		return currentPath === path && currentHash === `#${hash}`;
	};

	const isGroupOpen = (item: {
		title: string;
		url: string;
		items?: { title: string; url: string; available?: boolean }[];
	}) => {
		if (disabled) return false;
		if (isTopLevelActive(item.url)) return true;
		return Boolean(item.items?.some((subItem) => isSubItemActive(subItem.url)));
	};
</script>

<Sidebar.Group>
	<!--<Sidebar.GroupLabel>{groupLabel}</Sidebar.GroupLabel>-->
	<Sidebar.Menu>
		{#each items as item (item.title)}
			{#if item.items && item.items.length > 0}
				<Collapsible.Root open={isGroupOpen(item)} class="group/collapsible">
					{#snippet child({ props })}
						<Sidebar.MenuItem {...props}>
							<Sidebar.MenuButton
								tooltipContent={item.items?.every((subItem) => subItem.available === false)
									? `${item.title} (${comingSoonSuffix})`
									: disabled && disabledSuffix
										? `${item.title} (${disabledSuffix})`
										: item.title}
								isActive={!disabled && isTopLevelActive(item.url)}
								aria-disabled={disabled || undefined}
							>
								{#snippet child({ props: buttonProps })}
									{#if disabled}
										<span {...buttonProps}>
											{#if item.icon}
												<item.icon />
											{/if}
											<span>{item.title}</span>
										</span>
									{:else}
										<a href={item.url} {...buttonProps}>
											{#if item.icon}
												<item.icon />
											{/if}
											<span>{item.title}</span>
										</a>
									{/if}
								{/snippet}
							</Sidebar.MenuButton>
							<Collapsible.Trigger>
								{#snippet child({ props: triggerProps })}
									<Sidebar.MenuAction
										{...triggerProps}
										aria-label={`Apri ${item.title}`}
										aria-disabled={disabled || undefined}
										class="transition-transform duration-200 group-data-[state=open]/collapsible:rotate-90"
									>
										<ChevronRightIcon />
									</Sidebar.MenuAction>
								{/snippet}
							</Collapsible.Trigger>
							<Collapsible.Content class="sidebar-submenu-motion">
								<Sidebar.MenuSub>
									{#each item.items ?? [] as subItem (subItem.title)}
										<Sidebar.MenuSubItem>
											<Sidebar.MenuSubButton isActive={!disabled && isSubItemActive(subItem.url)}>
												{#snippet child({ props: subProps })}
													{#if disabled || subItem.available === false}
														<span class="opacity-55" {...subProps}>
															<span>{subItem.title}</span>
														</span>
													{:else}
														<a href={subItem.url} {...subProps}>
															<span>{subItem.title}</span>
														</a>
													{/if}
												{/snippet}
											</Sidebar.MenuSubButton>
										</Sidebar.MenuSubItem>
									{/each}
								</Sidebar.MenuSub>
							</Collapsible.Content>
						</Sidebar.MenuItem>
					{/snippet}
				</Collapsible.Root>
			{:else}
				<Sidebar.MenuItem>
					<Sidebar.MenuButton
						tooltipContent={disabled && disabledSuffix
							? `${item.title} (${disabledSuffix})`
							: item.title}
						isActive={!disabled && isTopLevelActive(item.url)}
						aria-disabled={disabled || undefined}
					>
						{#snippet child({ props })}
							{#if disabled}
								<span {...props}>
									{#if item.icon}
										<item.icon />
									{/if}
									<span>{item.title}</span>
								</span>
							{:else}
								<a href={item.url} {...props}>
									{#if item.icon}
										<item.icon />
									{/if}
									<span>{item.title}</span>
								</a>
							{/if}
						{/snippet}
					</Sidebar.MenuButton>
				</Sidebar.MenuItem>
			{/if}
		{/each}
	</Sidebar.Menu>
</Sidebar.Group>
