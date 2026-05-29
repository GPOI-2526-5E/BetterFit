import { getContext, setContext, type Component, type ComponentProps, type Snippet } from 'svelte';

export const THEMES = { light: '', dark: '.dark' } as const;

export type ChartConfig = {
	[k in string]: {
		label?: string;
		icon?: Component;
	} & (
		| { color?: string; theme?: never }
		| { color?: never; theme: Record<keyof typeof THEMES, string> }
	);
};

export type ExtractSnippetParams<T> = T extends Snippet<[infer P]> ? P : never;

export type TooltipPayload = Record<string, any>;

// Helper to extract item config from a payload.
export function getPayloadConfigFromPayload(
	config: ChartConfig,
	payload: TooltipPayload,
	key: string
) {
	if (typeof payload !== 'object' || payload === null) return undefined;

	const payloadRecord = payload as Record<string, any>;
	const payloadPayload =
		typeof payloadRecord.payload === 'object' && payloadRecord.payload !== null
			? (payloadRecord.payload as Record<string, any>)
			: undefined;

	let configLabelKey: string = key;

	if (payloadRecord.key === key) {
		configLabelKey = payloadRecord.key;
	} else if (payloadRecord.name === key) {
		configLabelKey = payloadRecord.name;
	} else if (key in payloadRecord && typeof payloadRecord[key] === 'string') {
		configLabelKey = payloadRecord[key];
	} else if (
		payloadPayload !== undefined &&
		key in payloadPayload &&
		typeof payloadPayload[key] === 'string'
	) {
		configLabelKey = payloadPayload[key];
	}

	return configLabelKey in config ? config[configLabelKey] : config[key as keyof typeof config];
}

type ChartContextValue = {
	config: ChartConfig;
};

const chartContextKey = Symbol('chart-context');

export function setChartContext(value: ChartContextValue) {
	return setContext(chartContextKey, value);
}

export function useChart() {
	return getContext<ChartContextValue>(chartContextKey);
}
