<!--
Sync Impact Report
- Version change: scaffold to 1.0.0
- Modified principles: none; all five principles are newly adopted
- Added sections: Additional Constraints; Development Workflow
- Removed sections: none
- Follow-up TODOs: ratification date is unknown and remains explicitly marked below
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Scope
ContosoDashboard MUST remain suitable for offline training and demonstration. Features MUST
respect the repository's documented non-production status, avoid unapproved external service
dependencies, and keep production migration concerns isolated behind configuration or service
abstractions. This preserves repeatable exercises and prevents training code from being
mistaken for production guidance.

### II. Secure by Boundary
Authentication and authorization MUST be enforced at the request, page, and service boundaries
that control protected data. Services MUST verify the current user's access to requested
resources, including ownership or membership checks, to prevent IDOR-style access. New security
behavior MUST include a focused verification scenario, and mock authentication MUST remain clearly
identified as training-only.

### III. Layered Design
UI components, application services, data access, and infrastructure MUST retain clear
responsibilities. Business rules MUST live in services or domain models rather than being
duplicated in Razor markup. Infrastructure integrations MUST use interfaces or configuration
boundaries when a local implementation may later be replaced by an approved cloud equivalent.

### IV. Verifiable Change
Every feature or bug fix MUST have a proportionate verification plan before completion. Changes
to services, authorization, persistence, or shared models MUST receive focused automated tests
when the project supports them; otherwise, the change MUST document and execute reproducible
build or scenario checks. A change MUST NOT be considered complete while its primary failure mode
remains untested or unchecked.

### V. Simple, Observable Evolution
The smallest design that satisfies the stated training scenario MUST be preferred. New complexity,
dependencies, or abstractions MUST have a documented reason. Failures at application boundaries
MUST be observable through appropriate logs or user-visible error handling without exposing
sensitive data. Breaking changes to documented behavior MUST be called out in the change record.

## Additional Constraints

The application MUST remain compatible with the repository's supported .NET target and Blazor
Server architecture unless an approved amendment changes that direction. Local development MUST
work without cloud credentials or network-only services. SQLite and mock authentication are
training implementations; production claims MUST NOT be inferred from them. Security-sensitive
documentation MUST identify the production gaps, including the need for a real identity provider,
password protection, MFA, TLS, audit logging, and applicable accessibility or compliance review.

## Development Workflow

Each change MUST identify the user-visible behavior or technical contract it affects, the
boundary that owns that behavior, and the verification used to validate it. Before review,
contributors MUST run the narrowest relevant checks and a project build when feasible. Reviewers
MUST check authorization paths, data isolation, error handling, and consistency with the training
scope for changes that touch protected data or infrastructure. Documentation MUST be updated when
setup, behavior, security assumptions, or migration guidance changes.

## Governance

This constitution governs implementation and review decisions for ContosoDashboard. An amendment
MUST state the affected principles or sections, explain the rationale, identify migration impact,
and update the Sync Impact Report. A maintainer MUST review the amendment before it is committed.
Every feature or bug-fix review MUST check compliance with the principles relevant to its scope;
any justified exception MUST be recorded with an owner and a follow-up condition.

The constitution uses semantic versioning. MAJOR increments represent incompatible governance
changes or removed principles. MINOR increments represent new principles or materially expanded
requirements. PATCH increments represent clarifications, wording, or non-semantic refinements.
The constitution MUST be reviewed whenever the architecture, authentication model, supported
runtime, or training purpose materially changes.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): confirm original adoption date | **Last Amended**: 2026-09-14
