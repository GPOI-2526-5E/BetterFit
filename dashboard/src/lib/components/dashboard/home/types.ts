export type TaskStatus = 'urgent' | 'pending' | 'ok';
export type AlertStatus = 'critical' | 'warning' | 'ok';
export type DeviceStatus = 'online' | 'offline' | 'maintenance';

export type HomeKpi = {
	label: string;
	value: string;
	trend: string;
	positive: boolean;
};

export type HomeTask = {
	task: string;
	member: string;
	due: string;
	status: TaskStatus;
};

export type HomeAlert = {
	title: string;
	description: string;
	status: AlertStatus;
};

export type HomeDevice = {
	name: string;
	status: DeviceStatus;
	lastEvent: string;
};

export type HomeTimelineEvent = {
	time: string;
	text: string;
};

export type HomeData = {
	kpis: HomeKpi[];
	tasks: HomeTask[];
	alerts: HomeAlert[];
	devices: HomeDevice[];
	timeline: HomeTimelineEvent[];
};
