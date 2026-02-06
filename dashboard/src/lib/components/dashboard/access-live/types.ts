export type AccessLiveResult = 'granted' | 'denied' | 'manual_override';
export type AccessLiveOrigin = 'badge' | 'app_qr' | 'desk' | 'unknown';
export type AccessControllerStatus = 'online' | 'offline' | 'degraded';

export type AccessLiveKpi = {
	label: string;
	value: string;
	trend: string;
	positive: boolean;
};

export type AccessLiveEvent = {
	time: string;
	member: string;
	gate: string;
	result: AccessLiveResult;
	origin: AccessLiveOrigin;
};

export type AccessController = {
	name: string;
	status: AccessControllerStatus;
	latencyMs: number;
	lastSeen: string;
};

export type AccessDeniedAttempt = {
	member: string;
	reason: string;
	attempts: number;
	lastAttempt: string;
};

export type AccessLiveData = {
	kpis: AccessLiveKpi[];
	events: AccessLiveEvent[];
	controllers: AccessController[];
	deniedAttempts: AccessDeniedAttempt[];
	lastSync: string;
};
