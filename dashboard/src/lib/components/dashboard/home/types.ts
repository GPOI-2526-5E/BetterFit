export type TaskStatus = 'urgent' | 'pending' | 'ok';
export type AlertStatus = 'critical' | 'warning' | 'ok';
export type DeviceStatus = 'online' | 'offline' | 'maintenance';

export type HomeKpi = {
	label: string;
	value: string;
	trend: string;
	positive: boolean;
	disabled?: boolean;
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

export type HomeStatusTone = 'neutral' | 'success' | 'warning' | 'danger';

export type HomeStatusCard = {
	id:
		| 'profiles'
		| 'activeUsers'
		| 'lowAccessUsers'
		| 'noAccessUsers'
		| 'expiringToday'
		| 'activity';
	label: string;
	value: string;
	description?: string | null;
	tone: HomeStatusTone;
};

export type HomeTrendPoint = {
	label: string;
	value: number;
};

export type HomeTrendCard = {
	id: string;
	title: string;
	color: string;
	maxValue: string;
	maxDate: string;
	updatedAt: string;
	format: 'number' | 'currency';
	points: HomeTrendPoint[];
};

export type HomeTimelineEvent = {
	time: string;
	text: string;
};

export type HomeLocationSnapshot = {
	locationId: string;
	locationName: string;
	accessesTodayCount: number;
	revenueTodayLabel: string;
	expiringMembershipsCount: number;
	pendingActivationsCount: number;
};

export type HomeRecentCollection = {
	paymentId: string;
	paidAt: string;
	referenceCode: string;
	receiptCode: string;
	memberName: string;
	locationName: string;
	amountLabel: string;
	method: string;
};

export type HomeData = {
	kpis: HomeKpi[];
	statusCards: HomeStatusCard[];
	tasks: HomeTask[];
	alerts: HomeAlert[];
	devices: HomeDevice[];
	timeline: HomeTimelineEvent[];
	locations: HomeLocationSnapshot[];
	recentCollections: HomeRecentCollection[];
	operationalMessage: string | null;
	coverageNotice: string | null;
};
