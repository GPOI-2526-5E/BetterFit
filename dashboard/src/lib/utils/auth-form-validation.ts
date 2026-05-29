import * as m from '$lib/paraglide/messages.js';

export type LoginFormData = {
	email: string;
	password: string;
};

export type SignupFormData = {
	fullName: string;
	email: string;
	password: string;
};

export type CodeFormData = {
	code: string;
};

export type RecoveryCodeFormData = {
	recoveryCode: string;
};

export type FormErrors<T extends Record<string, unknown>> = Partial<
	Record<Extract<keyof T, string>, string[]>
>;

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const PASSWORD_POLICY_REGEX = /^(?=.*[a-z])(?=.*\d).{8,}$/;
const MAX_FULL_NAME_LENGTH = 120;

export function hasFormErrors<T extends Record<string, unknown>>(errors: FormErrors<T>) {
	return Object.keys(errors).length > 0;
}

export function validateLoginFormData(data: LoginFormData): FormErrors<LoginFormData> {
	const errors: FormErrors<LoginFormData> = {};
	const email = data.email.trim();
	const password = data.password.trim();

	if (!email) {
		errors.email = [m.auth_validation_email_required()];
	} else if (!EMAIL_REGEX.test(email)) {
		errors.email = [m.auth_validation_email_invalid()];
	}

	if (!password) {
		errors.password = [m.auth_validation_password_required()];
	}

	return errors;
}

export function validateSignupFormData(data: SignupFormData): FormErrors<SignupFormData> {
	const errors: FormErrors<SignupFormData> = {};
	const fullName = data.fullName.trim();
	const email = data.email.trim();
	const password = data.password.trim();

	if (fullName.length > MAX_FULL_NAME_LENGTH) {
		errors.fullName = [m.auth_validation_fullname_max({ count: MAX_FULL_NAME_LENGTH })];
	}

	if (!email) {
		errors.email = [m.auth_validation_email_required()];
	} else if (!EMAIL_REGEX.test(email)) {
		errors.email = [m.auth_validation_email_invalid()];
	}

	if (!password) {
		errors.password = [m.auth_validation_password_required()];
	} else if (!PASSWORD_POLICY_REGEX.test(password)) {
		errors.password = [m.auth_validation_password_policy()];
	}

	return errors;
}

export function validateCodeFormData(
	data: CodeFormData,
	codeLength: number
): FormErrors<CodeFormData> {
	const errors: FormErrors<CodeFormData> = {};
	const normalizedCode = data.code.trim();

	if (normalizedCode.length !== codeLength) {
		errors.code = [m.auth_validation_code_length({ count: codeLength })];
	}

	return errors;
}

export function validateRecoveryCodeFormData(
	data: RecoveryCodeFormData
): FormErrors<RecoveryCodeFormData> {
	const errors: FormErrors<RecoveryCodeFormData> = {};

	if (!data.recoveryCode.trim()) {
		errors.recoveryCode = [m.auth_validation_recovery_code_required()];
	}

	return errors;
}
