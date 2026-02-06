<script lang="ts">
	import * as m from '$lib/paraglide/messages.js';
	import { Badge } from '$lib/components/ui/badge/index.js';
	import {
		Card,
		CardContent,
		CardDescription,
		CardHeader,
		CardTitle
	} from '$lib/components/ui/card/index.js';
	import {
		Table,
		TableBody,
		TableCell,
		TableHead,
		TableHeader,
		TableRow
	} from '$lib/components/ui/table/index.js';
	import type { HomeTask, TaskStatus } from './types';

	type BadgeVariant = 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning';

	let { tasks }: { tasks: HomeTask[] } = $props();

	const taskBadge = (status: TaskStatus): BadgeVariant => {
		if (status === 'urgent') return 'destructive';
		if (status === 'pending') return 'warning';
		return 'success';
	};

	const taskLabel = (status: TaskStatus): string => {
		if (status === 'urgent') return m.task_status_urgent();
		if (status === 'pending') return m.task_status_pending();
		return m.task_status_ok();
	};
</script>

<Card>
	<CardHeader class="border-b border-border/70">
		<CardTitle>{m.home_priority_queue_title()}</CardTitle>
		<CardDescription>{m.home_priority_queue_desc()}</CardDescription>
	</CardHeader>
	<CardContent class="pt-4">
		<Table>
			<TableHeader>
				<TableRow>
					<TableHead>{m.table_task()}</TableHead>
					<TableHead>{m.table_user()}</TableHead>
					<TableHead>{m.table_due()}</TableHead>
					<TableHead>{m.table_status()}</TableHead>
				</TableRow>
			</TableHeader>
			<TableBody>
				{#each tasks as task}
					<TableRow>
						<TableCell class="max-w-[280px] truncate font-medium">{task.task}</TableCell>
						<TableCell>{task.member}</TableCell>
						<TableCell>{task.due}</TableCell>
						<TableCell>
							<Badge variant={taskBadge(task.status)}>{taskLabel(task.status)}</Badge>
						</TableCell>
					</TableRow>
				{/each}
			</TableBody>
		</Table>
	</CardContent>
</Card>
