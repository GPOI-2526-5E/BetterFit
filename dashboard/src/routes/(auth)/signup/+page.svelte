<script lang="ts">
	import * as m from '$lib/paraglide/messages.js';
	import * as Form from '$lib/components/ui/form/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import * as Card from '$lib/components/ui/card/index.js';
	import { useApiClient } from '$lib/stores/ApiClientProvider.svelte';
	import { useUserAuthenticationStore } from '$lib/stores/AuthenticationStoreProvider.svelte';
	import { processAuthFlowResponse } from '$lib/utils/auth-flow.js';
	import { hasFormErrors, validateSignupFormData } from '$lib/utils/auth-form-validation.js';
	import { goto } from '$app/navigation';
	import { LoaderCircle } from '@lucide/svelte';
	import { superForm } from 'sveltekit-superforms';
	import { get } from 'svelte/store';

	const { client } = useApiClient();
	const auth = useUserAuthenticationStore();

	let loading = $state(false);
	let error = $state('');

	const signupForm = superForm({
		fullName: '',
		email: '',
		password: ''
	});

	const { form: signupFormData, errors: signupFormErrors } = signupForm;

	function handleSignupInput() {
		error = '';

		if (Object.keys(get(signupFormErrors)).length > 0) {
			signupFormErrors.clear();
		}
	}

	async function handleSignup(e: Event) {
		e.preventDefault();
		error = '';
		const values = get(signupFormData);
		const validationErrors = validateSignupFormData(values);
		signupFormErrors.set(validationErrors, { force: true });

		if (hasFormErrors(validationErrors)) {
			return;
		}

		loading = true;

		try {
			const response = await client.apiAuthRegisterPost({
				registerRequest: {
					email: values.email.trim(),
					password: values.password,
					fullName: values.fullName.trim() || undefined
				}
			});

			const result = processAuthFlowResponse(response, auth);

			switch (result.kind) {
				case 'authenticated':
					goto('/');
					break;
				case 'email-verification':
					goto('/verify-email');
					break;
				case 'two-factor':
					goto('/two-factor');
					break;
				case 'error':
					error = result.message;
					break;
			}
		} catch (err: any) {
			if (err?.response) {
				try {
					const body = await err.response.json();
					error = body?.error?.message ?? body?.message ?? m.auth_error_generic();
				} catch {
					error = m.auth_error_generic();
				}
			} else {
				error = m.auth_error_generic();
			}
		} finally {
			loading = false;
		}
	}
</script>

<Card.Root class="border-0 shadow-xl shadow-primary/5">
	<Card.Header class="space-y-1 pb-4">
		<Card.Title class="text-2xl font-bold tracking-tight">{m.auth_signup_title()}</Card.Title>
		<Card.Description class="text-muted-foreground">{m.auth_signup_desc()}</Card.Description>
	</Card.Header>
	<Card.Content>
		<form onsubmit={handleSignup} class="space-y-4">
			<Form.Field form={signupForm} name="fullName">
				<Form.Control>
					{#snippet children({ props })}
						<Form.Label>{m.auth_fullname_label()}</Form.Label>
						<Input
							{...props}
							type="text"
							placeholder={m.auth_fullname_placeholder()}
							bind:value={$signupFormData.fullName}
							disabled={loading}
							autocomplete="name"
							oninput={handleSignupInput}
						/>
					{/snippet}
				</Form.Control>
				<Form.FieldErrors />
			</Form.Field>
			<Form.Field form={signupForm} name="email">
				<Form.Control>
					{#snippet children({ props })}
						<Form.Label>{m.auth_email_label()}</Form.Label>
						<Input
							{...props}
							type="email"
							placeholder={m.auth_email_placeholder()}
							bind:value={$signupFormData.email}
							disabled={loading}
							autocomplete="email"
							oninput={handleSignupInput}
						/>
					{/snippet}
				</Form.Control>
				<Form.FieldErrors />
			</Form.Field>
			<Form.Field form={signupForm} name="password">
				<Form.Control>
					{#snippet children({ props })}
						<Form.Label>{m.auth_password_label()}</Form.Label>
						<Input
							{...props}
							type="password"
							placeholder={m.auth_password_placeholder()}
							bind:value={$signupFormData.password}
							disabled={loading}
							autocomplete="new-password"
							oninput={handleSignupInput}
						/>
					{/snippet}
				</Form.Control>
				<Form.FieldErrors />
			</Form.Field>

			{#if error}
				<div
					class="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive"
				>
					{error}
				</div>
			{/if}

			<Form.Button class="w-full" disabled={loading}>
				{#if loading}
					<LoaderCircle class="mr-2 h-4 w-4 animate-spin" />
				{/if}
				{m.auth_signup_button()}
			</Form.Button>
		</form>
	</Card.Content>
	<Card.Footer class="flex-col gap-2">
		<div class="text-center text-sm text-muted-foreground">
			{m.auth_have_account()}
			<a href="/login" class="font-medium text-primary underline-offset-4 hover:underline">
				{m.auth_login_link()}
			</a>
		</div>
	</Card.Footer>
</Card.Root>
