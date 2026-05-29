# Betterfit Authentication Guide

This document explains how authentication, pre-auth sessions, and client-side session handling should work across Betterfit.

Use it as the source of truth when changing:
- `ApplicationUser` authentication behavior
- email verification
- 2FA / MFA flows
- client-side auth persistence
- tenant authentication policy

## Goals

- Keep authentication global to the Betterfit account.
- Keep tenant policy separate from account state.
- Never issue the real API JWT before all required auth steps are complete.
- Make browser and mobile flows resumable without turning pre-auth tokens into full login tokens.

## Core Concepts

### 1. Access token

The normal bearer JWT used to call protected API endpoints.

- issued only after:
  - email is verified
  - required 2FA is completed
- sent as `Authorization: Bearer <token>`
- returned by the final authenticated step only

### 2. Verification session

The opaque token used during email verification.

- returned by register and unverified login
- body-bound, never used as bearer auth
- longer-lived than the email code
- intended to survive page refresh and short interruptions
- valid only for:
  - `POST /api/auth/verification-session/status`
  - `POST /api/auth/resend-email-code`
  - `POST /api/auth/verify-email-code`

### 3. Verification code

The numeric code sent by email.

- shorter-lived than the verification session
- rotated on resend
- only valid inside the current verification session

### 4. 2FA challenge token

The opaque token used during TOTP setup or TOTP login verification.

- body-bound, never used as bearer auth
- shorter-lived than the final JWT
- valid only for the relevant 2FA endpoints

### 5. Tenant authentication policy

Tenant policy decides whether 2FA is required for access in that tenant.

- `RequireTwoFactorForStaff`
- `RequireTwoFactorForMembers`

These settings do not replace account-level auth state.

- account state:
  - `EmailConfirmed`
  - `TwoFactorEnabled`
- tenant state:
  - whether 2FA is required for the roles/relationships the account has there

## Backend Model

Authentication is based on:

- `ApplicationUser`
- `AuthenticationChallenge`
- `GymAuthenticationPolicy`

`AuthenticationChallenge` has two important flavors:

- email verification session
- 2FA challenge

For email verification, the challenge must track:

- `SessionExpiresAtUtc`
- `CodeExpiresAtUtc`
- `LastSentAtUtc`
- `AttemptCount`
- `MaxAttempts`

The session and the code are intentionally separate. Do not collapse them into one expiry again.

## Register And Login Flow

### Register

1. `POST /api/auth/register`
2. Backend creates the account.
3. Backend creates an email verification session.
4. Backend sends the verification code.
5. Backend returns:
   - `step = EmailVerificationRequired`
   - `verificationToken`
   - `sessionExpiresAtUtc`
   - `codeExpiresAtUtc`
   - `resendAvailableAtUtc`

### Login

1. `POST /api/auth/login`
2. If email is already confirmed, continue toward 2FA or full auth.
3. If email is not confirmed:
   - create a new verification session
   - send a fresh code
   - return the same email-verification payload as register

## Email Verification Flow

### Resume Session

Use:

- `POST /api/auth/verification-session/status`

Input:

- `verificationToken`

Return 200 when the session is still active and the email is still pending.

Return:

- `verificationToken`
- `sessionExpiresAtUtc`
- `codeExpiresAtUtc`
- `codeLength`
- `resendAvailableAtUtc`

Return 404 when the session is gone or expired.

Return 409 when the email is already verified.

### Resend Code

Use:

- `POST /api/auth/resend-email-code`

This keeps the same verification session alive and rotates only the email code.

Success returns updated:

- `codeExpiresAtUtc`
- `resendAvailableAtUtc`

If resend is too early, return:

- error code `verification_code_recently_sent`
- `error.details.retryAfterSeconds`
- `error.details.retryAvailableAtUtc`
- HTTP header `Retry-After`

### Verify Code

Use:

- `POST /api/auth/verify-email-code`

If the code is valid and the email becomes confirmed:

- if 2FA is not required, return the final authenticated response with JWT
- if 2FA is required, return the next auth step instead of the JWT

If the code is expired but the session is still active:

- return `verification_code_expired`
- do not treat it as a full session failure
- the client should offer resend

## 2FA Flow

After the primary factor is complete, the backend evaluates:

- account-level `TwoFactorEnabled`
- tenant authentication policy requirements

Possible next steps:

- `TwoFactorRequired`
  - account already has 2FA enabled
- `TwoFactorSetupRequired`
  - tenant policy requires 2FA, but the account has not configured it yet
- `Authenticated`
  - no further factor is required

Endpoints:

- `POST /api/auth/2fa/setup`
- `POST /api/auth/2fa/enable`
- `POST /api/auth/2fa/verify`
- `POST /api/auth/2fa/recovery-code`

## Browser Client Guidance

### Recommended storage

For browser apps:

- store `verificationToken` and 2FA challenge tokens in `sessionStorage`
- store the matching expiry metadata alongside them
- clear them as soon as the flow finishes or becomes invalid

Why:

- `sessionStorage` survives refresh
- it is tab-scoped
- it is less sticky than `localStorage`

If you want the flow to survive full browser close/reopen, `localStorage` can be used, but it has a larger persistence and XSS exposure footprint.

### Resume after refresh

If the page refreshes during email verification:

1. read the saved `verificationToken`
2. call `POST /api/auth/verification-session/status`
3. if 200:
   - rebuild the verify-email UI
   - use returned timestamps to drive timers and resend state
4. if 404 or 409:
   - clear stored pre-auth state
   - route back to login/register

### Final JWT storage

Betterfit currently uses bearer JWTs, not server cookies and not refresh tokens.

For web clients:

- `sessionStorage` is safer if you can accept sign-out on full browser close
- `localStorage` gives better persistence but increases XSS impact
- in-memory state is best for exposure, but the user signs out on refresh

Choose explicitly. Do not let this happen accidentally.

## Native Client Guidance

For native apps:

- store final JWTs in platform secure storage
- store pre-auth verification and 2FA tokens in secure storage only if the flow truly needs to survive app restart
- otherwise keep pre-auth tokens in memory

## Security Rules

- Never send pre-auth tokens in query strings.
- Never use verification or 2FA challenge tokens as bearer tokens.
- Never unlock app features before the final authenticated step.
- `GET /api/auth/me` must stay lightweight and must not be used as a data dump endpoint.
- Do not make email code lifetime and verification session lifetime equal unless product requirements explicitly change.

## Suggested Defaults

Current good defaults:

- verification session: 24 hours
- verification code: 15 minutes
- resend cooldown: 30 seconds
- 2FA challenge: 10 minutes

These values are configurable, but the relationship matters more than the exact numbers:

- session lifetime > code lifetime
- resend cooldown << code lifetime

## Integration Checklist

When changing auth behavior, verify all of this:

1. Register returns `EmailVerificationRequired` for new accounts.
2. Unverified login also returns `EmailVerificationRequired`.
3. `verification-session/status` can rehydrate a pending flow.
4. Resend rotates the code without creating a brand-new account session.
5. Expired code does not automatically mean expired verification session.
6. Verified email still does not mint a JWT when tenant policy requires 2FA.
7. The JWT is returned only from the final authenticated step.
8. `GET /api/auth/me` remains minimal.
