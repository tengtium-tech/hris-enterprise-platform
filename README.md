# HRIS Enterprise Platform

Enterprise-grade, Philippines-first HRIS platform built as a modular monolith on .NET 9, PostgreSQL, and EF Core, using Domain-Driven Design.

## About this project

This is a personal project built end-to-end: product concept, market research, business requirements, system design, and implementation. The code in this repository is the implementation side of that work.

The product thinking behind it — the market research, the features pulled from existing HR/payroll platforms and why, the domain modeling decisions, and the system design rationale — is written up separately as a blog series rather than kept in this repository. This repo is the codebase; the write-ups are the story of how it got designed and built.

## Tech stack

- .NET 9 / C#
- PostgreSQL
- Entity Framework Core
- Domain-Driven Design (aggregates, value objects, domain events)
- Modular monolith architecture

## Architecture

The backend is organized as a modular monolith:

- **SharedKernel** — common building blocks used across every module: the `Result` pattern for error handling, strongly-typed IDs, and base `AggregateRoot`/`Entity`/`ValueObject` types.
- **Core Kernel frameworks** — nine foundation frameworks every business module depends on: Configuration, Logging, Identity, Events, Authorization, Audit, Rules Engine, Validation, and Localization.
- **Business modules** (not yet built) — Employee, Payroll, Leave, Attendance, Recruitment, and the rest of the HR domain, layered on top of the Core Kernel once it's complete.

## Status

Actively in progress. The Core Kernel foundation frameworks are implemented at the domain layer; the first of the nine (Configuration) also has a working application and persistence layer end to end, along with the shared CQRS/EF Core infrastructure the remaining eight will reuse. Business modules come next, in dependency order, per the project's implementation roadmap. A suite of Critical Test Requirements (59 in total, covering authorization, tenant isolation, audit, payroll correctness, and more) is tracked as stub tests and gets filled in as each requirement becomes implementable.

## Getting started

See [`backend/README.md`](backend/README.md) for build instructions and the current development conventions.
