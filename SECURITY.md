# Security policy

## Reporting a vulnerability

Please don't open a public issue for security problems. Use GitHub's
[private vulnerability reporting](../../security/advisories/new) for this repository instead.
I'll acknowledge reports within a week.

## Scope

This is a portfolio project that ships with synthetic data only. Reports about the code itself are welcome:
for example header or CSP gaps, input handling in the API, or the way database connections and secrets are handled.

## Handling secrets

Connection strings must never be committed. Use `dotnet user-secrets` in development and environment variables
or a secret store in deployment (see "Connecting to Oracle" in the README).
