# ADR-0001: Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-09-06
- **Deciders:** repository owner

## Context

This repository is measured against a constitution it does not contain
([00-REFERENCE-ARCHITECTURE](https://github.com/konradcinkusz/architecture-standards/blob/main/docs/architecture/00-REFERENCE-ARCHITECTURE.md)).
Anything it does differently, and anything it chose where the constitution left a
choice open, is invisible without a record — and an unrecorded departure reads as
carelessness to the next person, whether or not it was.

P14 puts it directly: documentation records reasoning, not just steps, and a
document saying "we considered X and rejected it because Y" is worth more than one
listing commands.

## Decision

Architecture decisions are recorded here as numbered Markdown files, in the form
of [ADR-0000](0000-template.md). One decision per file. Numbers are never reused
and files are never deleted — a decision that stops applying is marked Superseded
with a pointer to the ADR that replaced it.

The bar for writing one: **would a competent reader be surprised, or assume this
was inevitable when it was not?** That covers the choices the initializer asked
about explicitly (region, registry, whether the system has users) and the ones
taken quietly while building.

## Consequences

- Structural review starts from the ADRs, not from the diff. A finding that
  contradicts an accepted ADR is a request to revisit that ADR, and should say so.
- ADRs and the deviation register in
  [`docs/architecture/00-ARCHITECTURE.md`](../architecture/00-ARCHITECTURE.md) are
  different things and must not be merged. An ADR records a decision; the register
  records a **departure from the constitution**, with a date, so it can be paid off
  or accepted deliberately.
- There is a per-decision cost, and it is real. The mitigation is that these are
  short: most of the ADRs here are under a page.

## Alternatives considered

- **A single DECISIONS.md.** One file is easier to skim and impossible to review:
  every decision touches the same file, so the diff never shows which decision
  changed. Rejected.
- **Commit messages as the record.** They already carry much of the reasoning here.
  But `git log` is searched by people who already know what they are looking for,
  and the audience for a decision record is the person who does not. Rejected as
  the *primary* record; commit messages still carry reasoning, and should.
- **Nothing, at this size.** The repository has one service and one screen, so the
  argument is that decisions are self-evident. They are not: the region, the
  registry and the absence of accounts are all invisible in the code and all
  expensive to reverse. Rejected.
