import { getAuthenticatedSession } from '$lib/utils/auth-session-storage';

const API_BASE_URL = 'http://localhost:5299';

type ApiEnvelope<T> = {
	success: boolean;
	data: T | null;
	message?: string | null;
	error?: {
		code?: string | null;
		message?: string | null;
		details?: Record<string, string[]>;
	} | null;
};

export type LeadStage = 'New' | 'Contacted' | 'TrialBooked' | 'Negotiation' | 'Won' | 'Lost';
export type LeadSource = 'WalkIn' | 'Website' | 'MetaAds' | 'Referral' | 'WhatsApp' | 'Corporate' | 'Other';
export type LeadTaskStatus = 'Open' | 'Completed' | 'Cancelled';
export type CampaignChannel = 'Email' | 'Sms' | 'WhatsApp';
export type CampaignAudienceType = 'ActiveMembers' | 'ExpiringMemberships' | 'LeadsDueFollowUp' | 'LeadsInStage';
export type CampaignStatus = 'Draft' | 'Scheduled' | 'Sent' | 'Archived';
export type AutomationScheduleType = 'Daily' | 'Weekly';
export type AutomationStatus = 'Active' | 'Paused';

export type GymLeadTask = {
	taskId: string;
	leadId: string;
	gymId: string;
	assignedAssignmentId: string | null;
	assignedStaffName: string | null;
	title: string;
	notes: string | null;
	status: LeadTaskStatus;
	dueAtUtc: Date | null;
	completedAtUtc: Date | null;
	createdAtUtc: Date;
	updatedAtUtc: Date;
};

export type GymCampaign = {
	campaignId: string;
	gymId: string;
	locationId: string;
	locationName: string;
	ownerAssignmentId: string | null;
	ownerName: string | null;
	createdByUserId: string;
	createdByUserName: string;
	name: string;
	channel: CampaignChannel;
	audienceType: CampaignAudienceType;
	targetLeadStage: LeadStage | null;
	status: CampaignStatus;
	subject: string;
	message: string;
	notes: string | null;
	scheduledAtUtc: Date | null;
	sentAtUtc: Date | null;
	estimatedAudienceCount: number;
	lastAudienceCount: number | null;
	createdAtUtc: Date;
	updatedAtUtc: Date;
};

export type GymAutomationRule = {
	automationRuleId: string;
	gymId: string;
	locationId: string;
	locationName: string;
	ownerAssignmentId: string | null;
	ownerName: string | null;
	createdByUserId: string;
	createdByUserName: string;
	name: string;
	channel: CampaignChannel;
	audienceType: CampaignAudienceType;
	targetLeadStage: LeadStage | null;
	scheduleType: AutomationScheduleType;
	status: AutomationStatus;
	subjectTemplate: string;
	messageTemplate: string;
	notes: string | null;
	nextRunAtUtc: Date;
	lastRunAtUtc: Date | null;
	estimatedAudienceCount: number;
	lastAudienceCount: number | null;
	createdAtUtc: Date;
	updatedAtUtc: Date;
};

export type GymLead = {
	leadId: string;
	gymId: string;
	locationId: string;
	locationName: string;
	ownerAssignmentId: string | null;
	ownerName: string | null;
	convertedMembershipId: string | null;
	fullName: string;
	email: string | null;
	phoneNumber: string | null;
	source: LeadSource;
	stage: LeadStage;
	interest: string | null;
	notes: string | null;
	lastContactedAtUtc: Date | null;
	nextFollowUpAtUtc: Date | null;
	createdAtUtc: Date;
	updatedAtUtc: Date;
	tasks: GymLeadTask[];
};

export type GymLeadStageSummary = {
	stage: LeadStage;
	leadsCount: number;
};

export type GymCrmOverview = {
	totalLeads: number;
	leadsNeedingFollowUpCount: number;
	leadsWonThisMonthCount: number;
	openTasksCount: number;
	pipeline: GymLeadStageSummary[];
	recentLeads: GymLead[];
	generatedAtUtc: Date;
};

export type CreateGymLeadRequest = {
	locationId: string;
	ownerAssignmentId?: string | null;
	fullName: string;
	email?: string | null;
	phoneNumber?: string | null;
	source: LeadSource;
	interest?: string | null;
	notes?: string | null;
	nextFollowUpAtUtc?: string | null;
};

export type UpdateGymLeadStageRequest = {
	stage: LeadStage;
	convertedMembershipId?: string | null;
	lastContactedAtUtc?: string | null;
	nextFollowUpAtUtc?: string | null;
	notes?: string | null;
};

export type CreateGymLeadTaskRequest = {
	assignedAssignmentId?: string | null;
	title: string;
	notes?: string | null;
	dueAtUtc?: string | null;
};

export type UpdateGymLeadTaskStatusRequest = {
	status: LeadTaskStatus;
	completedAtUtc?: string | null;
	notes?: string | null;
};

export type CreateGymCampaignRequest = {
	locationId: string;
	ownerAssignmentId?: string | null;
	name: string;
	channel: CampaignChannel;
	audienceType: CampaignAudienceType;
	targetLeadStage?: LeadStage | null;
	subject: string;
	message: string;
	notes?: string | null;
	scheduledAtUtc?: string | null;
};

export type ScheduleGymCampaignRequest = {
	scheduledAtUtc: string;
};

export type CreateGymAutomationRuleRequest = {
	locationId: string;
	ownerAssignmentId?: string | null;
	name: string;
	channel: CampaignChannel;
	audienceType: CampaignAudienceType;
	targetLeadStage?: LeadStage | null;
	scheduleType: AutomationScheduleType;
	nextRunAtUtc: string;
	subjectTemplate: string;
	messageTemplate: string;
	notes?: string | null;
};

type LeadFilters = {
	locationId?: string | null;
	ownerAssignmentId?: string | null;
	membershipId?: string | null;
	stage?: LeadStage | 'all';
	dueOnly?: boolean;
	search?: string | null;
};

type CampaignFilters = {
	locationId?: string | null;
	status?: CampaignStatus | 'all';
	channel?: CampaignChannel | 'all';
};

type AutomationFilters = {
	locationId?: string | null;
	status?: AutomationStatus | 'all';
	channel?: CampaignChannel | 'all';
};

function getAccessToken() {
	return getAuthenticatedSession()?.token ?? '';
}

function parseDate(value: string | null | undefined) {
	if (!value) {
		return null;
	}

	const parsed = new Date(value);
	return Number.isNaN(parsed.getTime()) ? null : parsed;
}

async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
	const response = await fetch(`${API_BASE_URL}${path}`, {
		...init,
		headers: {
			'Content-Type': 'application/json',
			Authorization: `Bearer ${getAccessToken()}`,
			...(init?.headers ?? {})
		}
	});

	const payload = (await response.json()) as ApiEnvelope<T>;
	if (!response.ok || !payload.success || payload.data === null || payload.data === undefined) {
		throw new Error(payload.error?.message ?? payload.message ?? 'Richiesta API non riuscita.');
	}

	return payload.data;
}

function buildLeadFiltersQuery(filters: LeadFilters = {}) {
	const params = new URLSearchParams();
	if (filters.locationId) params.set('locationId', filters.locationId);
	if (filters.ownerAssignmentId) params.set('ownerAssignmentId', filters.ownerAssignmentId);
	if (filters.membershipId) params.set('membershipId', filters.membershipId);
	if (filters.stage && filters.stage !== 'all') params.set('stage', filters.stage);
	if (filters.dueOnly) params.set('dueOnly', 'true');
	if (filters.search?.trim()) params.set('search', filters.search.trim());
	const query = params.toString();
	return query ? `?${query}` : '';
}

function mapLeadTask(task: { [id: string]: unknown }) {
	return {
		taskId: String(task.taskId),
		leadId: String(task.leadId),
		gymId: String(task.gymId),
		assignedAssignmentId: (task.assignedAssignmentId as string | null | undefined) ?? null,
		assignedStaffName: (task.assignedStaffName as string | null | undefined) ?? null,
		title: String(task.title),
		notes: (task.notes as string | null | undefined) ?? null,
		status: String(task.status) as LeadTaskStatus,
		dueAtUtc: parseDate(task.dueAtUtc as string | null | undefined),
		completedAtUtc: parseDate(task.completedAtUtc as string | null | undefined),
		createdAtUtc: parseDate(task.createdAtUtc as string | null | undefined) ?? new Date(),
		updatedAtUtc: parseDate(task.updatedAtUtc as string | null | undefined) ?? new Date()
	} satisfies GymLeadTask;
}

function mapLead(lead: { [id: string]: unknown }) {
	return {
		leadId: String(lead.leadId),
		gymId: String(lead.gymId),
		locationId: String(lead.locationId),
		locationName: String(lead.locationName ?? ''),
		ownerAssignmentId: (lead.ownerAssignmentId as string | null | undefined) ?? null,
		ownerName: (lead.ownerName as string | null | undefined) ?? null,
		convertedMembershipId: (lead.convertedMembershipId as string | null | undefined) ?? null,
		fullName: String(lead.fullName),
		email: (lead.email as string | null | undefined) ?? null,
		phoneNumber: (lead.phoneNumber as string | null | undefined) ?? null,
		source: String(lead.source) as LeadSource,
		stage: String(lead.stage) as LeadStage,
		interest: (lead.interest as string | null | undefined) ?? null,
		notes: (lead.notes as string | null | undefined) ?? null,
		lastContactedAtUtc: parseDate(lead.lastContactedAtUtc as string | null | undefined),
		nextFollowUpAtUtc: parseDate(lead.nextFollowUpAtUtc as string | null | undefined),
		createdAtUtc: parseDate(lead.createdAtUtc as string | null | undefined) ?? new Date(),
		updatedAtUtc: parseDate(lead.updatedAtUtc as string | null | undefined) ?? new Date(),
		tasks: Array.isArray(lead.tasks) ? lead.tasks.map((task) => mapLeadTask(task as { [id: string]: unknown })) : []
	} satisfies GymLead;
}

function mapOverview(overview: { [id: string]: unknown }) {
	return {
		totalLeads: Number(overview.totalLeads ?? 0),
		leadsNeedingFollowUpCount: Number(overview.leadsNeedingFollowUpCount ?? 0),
		leadsWonThisMonthCount: Number(overview.leadsWonThisMonthCount ?? 0),
		openTasksCount: Number(overview.openTasksCount ?? 0),
		pipeline: Array.isArray(overview.pipeline)
			? overview.pipeline.map((item) => ({
					stage: String((item as { [id: string]: unknown }).stage) as LeadStage,
					leadsCount: Number((item as { [id: string]: unknown }).leadsCount ?? 0)
				}))
			: [],
		recentLeads: Array.isArray(overview.recentLeads)
			? overview.recentLeads.map((lead) => mapLead(lead as { [id: string]: unknown }))
			: [],
		generatedAtUtc: parseDate(overview.generatedAtUtc as string | null | undefined) ?? new Date()
	} satisfies GymCrmOverview;
}

function buildCampaignFiltersQuery(filters: CampaignFilters = {}) {
	const params = new URLSearchParams();
	if (filters.locationId) params.set('locationId', filters.locationId);
	if (filters.status && filters.status !== 'all') params.set('status', filters.status);
	if (filters.channel && filters.channel !== 'all') params.set('channel', filters.channel);
	const query = params.toString();
	return query ? `?${query}` : '';
}

function mapCampaign(campaign: { [id: string]: unknown }) {
	return {
		campaignId: String(campaign.campaignId),
		gymId: String(campaign.gymId),
		locationId: String(campaign.locationId),
		locationName: String(campaign.locationName ?? ''),
		ownerAssignmentId: (campaign.ownerAssignmentId as string | null | undefined) ?? null,
		ownerName: (campaign.ownerName as string | null | undefined) ?? null,
		createdByUserId: String(campaign.createdByUserId),
		createdByUserName: String(campaign.createdByUserName ?? ''),
		name: String(campaign.name),
		channel: String(campaign.channel) as CampaignChannel,
		audienceType: String(campaign.audienceType) as CampaignAudienceType,
		targetLeadStage: (campaign.targetLeadStage as LeadStage | null | undefined) ?? null,
		status: String(campaign.status) as CampaignStatus,
		subject: String(campaign.subject),
		message: String(campaign.message),
		notes: (campaign.notes as string | null | undefined) ?? null,
		scheduledAtUtc: parseDate(campaign.scheduledAtUtc as string | null | undefined),
		sentAtUtc: parseDate(campaign.sentAtUtc as string | null | undefined),
		estimatedAudienceCount: Number(campaign.estimatedAudienceCount ?? 0),
		lastAudienceCount:
			campaign.lastAudienceCount === null || campaign.lastAudienceCount === undefined
				? null
				: Number(campaign.lastAudienceCount),
		createdAtUtc: parseDate(campaign.createdAtUtc as string | null | undefined) ?? new Date(),
		updatedAtUtc: parseDate(campaign.updatedAtUtc as string | null | undefined) ?? new Date()
	} satisfies GymCampaign;
}

function buildAutomationFiltersQuery(filters: AutomationFilters = {}) {
	const params = new URLSearchParams();
	if (filters.locationId) params.set('locationId', filters.locationId);
	if (filters.status && filters.status !== 'all') params.set('status', filters.status);
	if (filters.channel && filters.channel !== 'all') params.set('channel', filters.channel);
	const query = params.toString();
	return query ? `?${query}` : '';
}

function mapAutomationRule(rule: { [id: string]: unknown }) {
	return {
		automationRuleId: String(rule.automationRuleId),
		gymId: String(rule.gymId),
		locationId: String(rule.locationId),
		locationName: String(rule.locationName ?? ''),
		ownerAssignmentId: (rule.ownerAssignmentId as string | null | undefined) ?? null,
		ownerName: (rule.ownerName as string | null | undefined) ?? null,
		createdByUserId: String(rule.createdByUserId),
		createdByUserName: String(rule.createdByUserName ?? ''),
		name: String(rule.name),
		channel: String(rule.channel) as CampaignChannel,
		audienceType: String(rule.audienceType) as CampaignAudienceType,
		targetLeadStage: (rule.targetLeadStage as LeadStage | null | undefined) ?? null,
		scheduleType: String(rule.scheduleType) as AutomationScheduleType,
		status: String(rule.status) as AutomationStatus,
		subjectTemplate: String(rule.subjectTemplate ?? ''),
		messageTemplate: String(rule.messageTemplate ?? ''),
		notes: (rule.notes as string | null | undefined) ?? null,
		nextRunAtUtc: parseDate(rule.nextRunAtUtc as string | null | undefined) ?? new Date(),
		lastRunAtUtc: parseDate(rule.lastRunAtUtc as string | null | undefined),
		estimatedAudienceCount: Number(rule.estimatedAudienceCount ?? 0),
		lastAudienceCount:
			rule.lastAudienceCount === null || rule.lastAudienceCount === undefined
				? null
				: Number(rule.lastAudienceCount),
		createdAtUtc: parseDate(rule.createdAtUtc as string | null | undefined) ?? new Date(),
		updatedAtUtc: parseDate(rule.updatedAtUtc as string | null | undefined) ?? new Date()
	} satisfies GymAutomationRule;
}

export async function fetchGymCrmOverview(gymId: string, locationId?: string | null) {
	const query = locationId ? `?locationId=${encodeURIComponent(locationId)}` : '';
	const overview = await apiRequest<{ [id: string]: unknown }>(`/api/gyms/${gymId}/crm/overview${query}`);
	return mapOverview(overview);
}

export async function fetchGymLeads(gymId: string, filters: LeadFilters = {}) {
	const leads = await apiRequest<Array<{ [id: string]: unknown }>>(
		`/api/gyms/${gymId}/crm/leads${buildLeadFiltersQuery(filters)}`
	);
	return leads.map(mapLead);
}

export async function createGymLead(gymId: string, request: CreateGymLeadRequest) {
	const lead = await apiRequest<{ [id: string]: unknown }>(`/api/gyms/${gymId}/crm/leads`, {
		method: 'POST',
		body: JSON.stringify(request)
	});
	return mapLead(lead);
}

export async function updateGymLeadStage(gymId: string, leadId: string, request: UpdateGymLeadStageRequest) {
	const lead = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/crm/leads/${leadId}/stage`,
		{
			method: 'PATCH',
			body: JSON.stringify(request)
		}
	);
	return mapLead(lead);
}

export async function createGymLeadTask(gymId: string, leadId: string, request: CreateGymLeadTaskRequest) {
	const lead = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/crm/leads/${leadId}/tasks`,
		{
			method: 'POST',
			body: JSON.stringify(request)
		}
	);
	return mapLead(lead);
}

export async function updateGymLeadTaskStatus(
	gymId: string,
	leadId: string,
	taskId: string,
	request: UpdateGymLeadTaskStatusRequest
) {
	const lead = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/crm/leads/${leadId}/tasks/${taskId}/status`,
		{
			method: 'PATCH',
			body: JSON.stringify(request)
		}
	);
	return mapLead(lead);
}

export async function fetchGymCampaigns(gymId: string, filters: CampaignFilters = {}) {
	const campaigns = await apiRequest<Array<{ [id: string]: unknown }>>(
		`/api/gyms/${gymId}/crm/campaigns${buildCampaignFiltersQuery(filters)}`
	);
	return campaigns.map(mapCampaign);
}

export async function createGymCampaign(gymId: string, request: CreateGymCampaignRequest) {
	const campaign = await apiRequest<{ [id: string]: unknown }>(`/api/gyms/${gymId}/crm/campaigns`, {
		method: 'POST',
		body: JSON.stringify(request)
	});
	return mapCampaign(campaign);
}

export async function scheduleGymCampaign(
	gymId: string,
	campaignId: string,
	request: ScheduleGymCampaignRequest
) {
	const campaign = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/crm/campaigns/${campaignId}/schedule`,
		{
			method: 'POST',
			body: JSON.stringify(request)
		}
	);
	return mapCampaign(campaign);
}

export async function sendGymCampaign(gymId: string, campaignId: string) {
	const campaign = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/crm/campaigns/${campaignId}/send`,
		{
			method: 'POST'
		}
	);
	return mapCampaign(campaign);
}

export async function archiveGymCampaign(gymId: string, campaignId: string) {
	const campaign = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/crm/campaigns/${campaignId}/archive`,
		{
			method: 'POST'
		}
	);
	return mapCampaign(campaign);
}

export async function fetchGymAutomationRules(gymId: string, filters: AutomationFilters = {}) {
	const rules = await apiRequest<Array<{ [id: string]: unknown }>>(
		`/api/gyms/${gymId}/crm/automations${buildAutomationFiltersQuery(filters)}`
	);
	return rules.map(mapAutomationRule);
}

export async function createGymAutomationRule(gymId: string, request: CreateGymAutomationRuleRequest) {
	const rule = await apiRequest<{ [id: string]: unknown }>(`/api/gyms/${gymId}/crm/automations`, {
		method: 'POST',
		body: JSON.stringify(request)
	});
	return mapAutomationRule(rule);
}

export async function pauseGymAutomationRule(gymId: string, automationRuleId: string) {
	const rule = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/crm/automations/${automationRuleId}/pause`,
		{
			method: 'POST'
		}
	);
	return mapAutomationRule(rule);
}

export async function resumeGymAutomationRule(gymId: string, automationRuleId: string) {
	const rule = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/crm/automations/${automationRuleId}/resume`,
		{
			method: 'POST'
		}
	);
	return mapAutomationRule(rule);
}

export async function runGymAutomationRule(gymId: string, automationRuleId: string) {
	const rule = await apiRequest<{ [id: string]: unknown }>(
		`/api/gyms/${gymId}/crm/automations/${automationRuleId}/run`,
		{
			method: 'POST'
		}
	);
	return mapAutomationRule(rule);
}
