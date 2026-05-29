import { ResponseError, type ApiError } from '$lib/api';

export type ParsedApiError = {
	status: number | null;
	code: string | null;
	message: string;
	details: ApiError['details'] | null;
	retryAfterSeconds: number | null;
};

export function getErrorDetail(
	details: ApiError['details'] | null | undefined,
	key: string
): string | null {
	const value = details?.[key];
	if (!value || value.length === 0) {
		return null;
	}

	return value[0] ?? null;
}

export async function parseApiError(
	error: unknown,
	fallbackMessage: string
): Promise<ParsedApiError> {
	if (error instanceof ResponseError) {
		const retryAfterHeader = error.response.headers.get('Retry-After');

		try {
			const body = await error.response.clone().json();
			const retryAfterFromBody = Number.parseInt(
				getErrorDetail(body?.error?.details, 'retryAfterSeconds') ?? '',
				10
			);

			return {
				status: error.response.status,
				code: body?.error?.code ?? null,
				message: body?.error?.message ?? body?.message ?? fallbackMessage,
				details: body?.error?.details ?? null,
				retryAfterSeconds: Number.isFinite(retryAfterFromBody)
					? retryAfterFromBody
					: Number.parseInt(retryAfterHeader ?? '', 10) || null
			};
		} catch {
			return {
				status: error.response.status,
				code: null,
				message: fallbackMessage,
				details: null,
				retryAfterSeconds: Number.parseInt(retryAfterHeader ?? '', 10) || null
			};
		}
	}

	if (typeof error === 'object' && error !== null && 'response' in error) {
		const response = (error as { response?: Response }).response;
		if (response) {
			return parseApiError(new ResponseError(response), fallbackMessage);
		}
	}

	return {
		status: null,
		code: null,
		message: fallbackMessage,
		details: null,
		retryAfterSeconds: null
	};
}
