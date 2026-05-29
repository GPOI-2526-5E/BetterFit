import type { ResponseError } from '$lib/api';
import * as m from '$lib/paraglide/messages.js';

export type SetupIntent = 'tenant' | 'location';

export type SetupOnboardingFormData = {
	intent: SetupIntent | '';
	tenantName: string;
	includeFirstLocation: boolean;
	targetGymId: string;
	locationName: string;
	locationCode: string;
	addressLine1: string;
	city: string;
	countryCode: string;
};

export type SetupOnboardingFormErrors = Partial<
	Record<Extract<keyof SetupOnboardingFormData, string>, string[]>
>;

const MAX_TENANT_NAME_LENGTH = 120;
const MAX_LOCATION_NAME_LENGTH = 120;
const MAX_LOCATION_CODE_LENGTH = 24;
const MAX_CITY_LENGTH = 80;
const MAX_ADDRESS_LENGTH = 160;
const COUNTRY_CODE_REGEX = /^[A-Za-z]{2}$/;

export function hasOnboardingErrors(errors: SetupOnboardingFormErrors) {
	return Object.keys(errors).length > 0;
}

export function createInitialOnboardingFormData(
	intent: SetupIntent | null,
	defaultGymId: string | null
): SetupOnboardingFormData {
	return {
		intent: intent ?? '',
		tenantName: '',
		includeFirstLocation: true,
		targetGymId: defaultGymId ?? '',
		locationName: '',
		locationCode: '',
		addressLine1: '',
		city: '',
		countryCode: ''
	};
}

export function validateOnboardingIntentStep(
	data: SetupOnboardingFormData,
	availableTenantsCount: number
): SetupOnboardingFormErrors {
	const errors: SetupOnboardingFormErrors = {};

	if (!data.intent) {
		errors.intent = [m.setup_flow_validation_intent_required()];
	} else if (data.intent === 'location' && availableTenantsCount === 0) {
		errors.intent = [m.setup_flow_validation_location_requires_tenant()];
	}

	return errors;
}

export function validateOnboardingDetailsStep(
	data: SetupOnboardingFormData
): SetupOnboardingFormErrors {
	const errors: SetupOnboardingFormErrors = {};

	if (data.intent === 'tenant') {
		const tenantName = data.tenantName.trim();
		if (!tenantName) {
			errors.tenantName = [m.setup_flow_validation_tenant_required()];
		} else if (tenantName.length > MAX_TENANT_NAME_LENGTH) {
			errors.tenantName = [m.setup_flow_validation_tenant_max({ count: MAX_TENANT_NAME_LENGTH })];
		}
	}

	if (data.intent === 'location' && !data.targetGymId) {
		errors.targetGymId = [m.setup_flow_validation_target_tenant_required()];
	}

	return errors;
}

export function validateOnboardingLocationStep(
	data: SetupOnboardingFormData
): SetupOnboardingFormErrors {
	const errors: SetupOnboardingFormErrors = {};
	const requiresLocation =
		data.intent === 'location' || (data.intent === 'tenant' && data.includeFirstLocation);

	if (!requiresLocation) {
		return errors;
	}

	const locationName = data.locationName.trim();
	const locationCode = data.locationCode.trim();
	const city = data.city.trim();
	const addressLine1 = data.addressLine1.trim();
	const countryCode = data.countryCode.trim();

	if (!locationName) {
		errors.locationName = [m.setup_flow_validation_location_required()];
	} else if (locationName.length > MAX_LOCATION_NAME_LENGTH) {
		errors.locationName = [
			m.setup_flow_validation_location_max({ count: MAX_LOCATION_NAME_LENGTH })
		];
	}

	if (locationCode.length > MAX_LOCATION_CODE_LENGTH) {
		errors.locationCode = [
			m.setup_flow_validation_location_code_max({ count: MAX_LOCATION_CODE_LENGTH })
		];
	}

	if (city.length > MAX_CITY_LENGTH) {
		errors.city = [m.setup_flow_validation_city_max({ count: MAX_CITY_LENGTH })];
	}

	if (addressLine1.length > MAX_ADDRESS_LENGTH) {
		errors.addressLine1 = [m.setup_flow_validation_address_max({ count: MAX_ADDRESS_LENGTH })];
	}

	if (countryCode && !COUNTRY_CODE_REGEX.test(countryCode)) {
		errors.countryCode = [m.setup_flow_validation_country_code_invalid()];
	}

	return errors;
}

export function toLocationPayload(data: SetupOnboardingFormData) {
	return {
		name: data.locationName.trim(),
		code: data.locationCode.trim() || undefined,
		addressLine1: data.addressLine1.trim() || undefined,
		city: data.city.trim() || undefined,
		countryCode: data.countryCode.trim().toUpperCase() || undefined
	};
}

export async function extractOnboardingErrorMessage(error: unknown, fallback: string) {
	if (error && typeof error === 'object' && 'response' in error) {
		const responseError = error as ResponseError;

		try {
			const body = await responseError.response.json();
			return body?.error?.message ?? body?.message ?? fallback;
		} catch {
			return fallback;
		}
	}

	if (error instanceof Error && error.message) {
		return error.message;
	}

	return fallback;
}
