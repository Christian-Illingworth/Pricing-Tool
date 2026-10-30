# Specification Quality Checklist: CU Pricing Tool

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-10-30
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- Iteration 1: two markers remained, for the product owner:
  - FR-027: whether consumption exactly at the microbusiness threshold counts as a microbusiness.
  - FR-066: whether an uplift above a supplier's maximum can be approved by a manager.
- Iteration 2: both resolved. FR-027: at or below the threshold counts (compliance to confirm,
  recorded in Assumptions). FR-066: a supplier's maximum is a hard limit that nobody can approve.
  All items pass.
- Named external services (POWWR, Outlook, Power BI) and file formats (PDF, CSV, Excel) are business
  requirements from the request, not implementation choices.
- Fixed in iteration 1: company registration number, phone, postcode and MPRN rules made specific;
  lettered requirement numbers replaced with a clean FR-001 to FR-092 sequence.
- POWWR's open questions (commission meaning, plan types, price validity, green products, inclusive
  price, DUoS and MHHS sources, large gas thresholds) are written so the spec does not depend on the
  answers; `/speckit-clarify` covers them next.
- Items marked incomplete require spec updates before `/speckit-clarify` or `/speckit-plan`
