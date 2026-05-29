export type DashboardScopeSelection = {
	gymId: string;
	locationId: string | null;
};

const DASHBOARD_SCOPE_SELECTION_KEY = 'betterfit.dashboard.scope';

function canUseSessionStorage() {
	return typeof window !== 'undefined' && typeof window.sessionStorage !== 'undefined';
}

function safeParse<T>(rawValue: string | null): T | null {
	if (!rawValue) {
		return null;
	}

	try {
		return JSON.parse(rawValue) as T;
	} catch {
		return null;
	}
}

export function getDashboardScopeSelection(): DashboardScopeSelection | null {
	if (!canUseSessionStorage()) {
		return null;
	}

	const storedSelection = safeParse<DashboardScopeSelection>(
		window.sessionStorage.getItem(DASHBOARD_SCOPE_SELECTION_KEY)
	);

	if (!storedSelection?.gymId) {
		clearDashboardScopeSelection();
		return null;
	}

	if (storedSelection.locationId !== null && typeof storedSelection.locationId !== 'string') {
		clearDashboardScopeSelection();
		return null;
	}

	return storedSelection;
}

export function saveDashboardScopeSelection(selection: DashboardScopeSelection) {
	if (!canUseSessionStorage()) {
		return;
	}

	window.sessionStorage.setItem(DASHBOARD_SCOPE_SELECTION_KEY, JSON.stringify(selection));
}

export function clearDashboardScopeSelection() {
	if (!canUseSessionStorage()) {
		return;
	}

	window.sessionStorage.removeItem(DASHBOARD_SCOPE_SELECTION_KEY);
}
