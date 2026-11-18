---
name: architect-review
description: >-
  Read-only pre-merge correctness review of a pull request diff. Uses the pull
  request description and the linked work item's story and acceptance criteria,
  applies a .NET and Sonar baseline unless the repo's own rules cover that
  topic, traces failure paths and breaking changes, then self-verifies every
  finding. Use when the user asks for /architect-review or an architect review
  of a pull request.
disable-model-invocation: true
---

# architect-review

Read-only for code: never edit source, tests, or config, and never push or approve. Post the review as one pull-request comment. Do not write `review.md`. Use this in addition to `/review` (style) and `/review-security` (exploit-focused), not instead of them.

This is pull-request review only. Do not implement `/develop-story`. Do not read or search for `plan.md`, `story.md`, `loop.md`, story decisions, deviations, or any other develop-story artifact, even when those files exist.

## Inputs (gather before judging anything)
1. **Diff** against the real merge base: `git diff $(git merge-base origin/<base> HEAD)`. A naive two-branch diff overstates scope.
2. **Intent**, and no local story files:
   - The pull request description.
   - The user story description and acceptance criteria on the linked work item, when one is linked. On GitHub that is a linked issue. On Azure DevOps that is the linked work item. Read it through the host.
   - If no item is linked, or the link cannot be opened, say the linked story was not found and review the diff against the code using the pull request description.
   - If the pull request description is also missing, say "no spec; reviewed against code only".
3. **Repo rules.** Always apply the baseline below to changed C#. When `.cursor/rules` or `.cursor/docs/review-checklist.md` exists, those files replace the baseline for every topic they cover. Topics they do not mention still use the baseline. `CONTRIBUTING.md` and the pull-request template add process; they do not remove the baseline. A missing checklist is not a finding and is not a reason to skip the baseline. Also read `AGENTS.md` when it exists.
4. **Indirect files:** consumers of changed shapes, switches keyed on changed enums or tokens, validators enforcing the same rule. Use the repo docs index when it exists, then search. Do not require graphify.
5. **Prior decisions**, only when present: `agent-knowledge/` if that folder exists, otherwise ADRs or another docs index the repo actually has. If none exist, Checklist D's knowledge row is "not checked". This is repository knowledge, not a story folder.

Where a check says "the plan", "the ACs", or "Decisions", use the pull request description and, when opened, the linked story and its acceptance criteria.

## .NET and Sonar baseline
Apply this to changed C# only. Do not copy repo-specific hotspots into it. Do not fix pre-existing violations in untouched code, and do not make them worse.

- No new `#pragma warning` or `[SuppressMessage]`. An analyzer change belongs in `.editorconfig` with a justification. Generated code (`**/Migrations/**`, `*.Designer.cs`) is out of scope.
- Methods at most 4 parameters. `CancellationToken` does not count. Defaults and `params` do. DTO and record properties are not method parameters.
- Injected constructors at most 6 dependencies, counting `ILogger<T>`. A type whose every public member is a single delegating call, with no branching or business logic, may hold 10.
- Per method: cognitive complexity at most 15, cyclomatic at most 10, 30 lines, nesting at most 3. Per class: 500 lines.
- Do not grow a method, constructor, or class that is already over a limit.
- No unused or unreachable code, no empty `catch`, no `.Result` or `.Wait()`, no `async void` except an event handler.
- `using` for disposables, null checks before dereference, `StringComparison` on string compares, no hardcoded secrets, parameterized queries only.

## Hard rules
- **Only real findings.** Every finding needs a traced path in the code. "Could be a problem" without a trace is not a finding. Zero findings is a valid result. Never pad to look thorough, and never invent.
- Severity reflects what is **reachable today**. Latent issues are Low, with a trigger noted.
- Data loss, authorization bypass and routing bypass that are reachable today are always Blocker. A deadline never downgrades a finding.
- A break counts as reachable today if existing data, callers or deployed services hit it once this ships. An undeclared or unaccepted break is at least High. If it is deliberate, also put it under Needs a decision. A break the pull request description or the linked acceptance criteria record as accepted is not a finding: list it with who accepted it.
- A test counts as coverage only if its fixture goes through the real production builder, template or mapper. For import/export and payload work, insist on a real-artifact test.
- Every deferred item needs an owner and a trigger. "Out of scope" is not allowed to drop it silently.
- State plainly what you did not check, and whether builds and tests were run or not.

## Checklist A — shape and structure
1. Every new or changed DTO/JSON/proto shape crossing a boundary: diff producer vs every consumer, comparing exact keys, nesting and casing.
2. Credit a test only if it uses a real-path fixture (see Hard rules).
3. Every new enum member or discriminator value: check every switch or dispatch site handles it explicitly.
4. Shared code outside the feature: list every other caller and say whether it is safe for each.
5. A bypassed orchestration path (facade called directly instead of the bus, and similar): list the side effects that path normally produces (events, audit, publish) and say whether each still happens.
6. Hot or polling paths: repeated external round-trips.
7. Dead or contradictory writes, and silent out-of-domain defaults.
8. The baseline above, plus repo rules for topics those files cover.

## Checklist B — failure paths and boundaries
9. Every external call touched: what its failure branch does, and whether that output reaches a write or a successful result.
10. A failure fallback that reaches a write or a successful result must fail closed, unless the pull request description or the linked acceptance criteria explicitly say otherwise.
11. Parallel flows that share the same read (preview vs confirm, save vs update) behave identically on failure.
12. Advertised contract vs validator: hand-trace one worked example per distinct shape.
13. Every request field that selects a path or write target: enumerate its values, including invalid ones, and follow each through every consumer. Audit each gating field on its own.
14. **Category exhaustion.** When one instance of a problem class is found, check the whole category: every validator, every endpoint, every flow of that kind.
15. Every finding names the regression test that would catch it. A missing test is part of the finding, not a separate one.

## Checklist C — cohesion and scope
16. **One decision point per rule.** The same rule or check implemented in more than one place, or the same predicate under two names: name the place that should own it, and the existing helper it should have reused.
17. **Orphan states.** Enum members, parse states or branches that nothing produces any more, and names that stopped being true (for example a "ValidInteger" state that now accepts decimals).
18. **Scope.** Every changed file and hunk traces to the pull request description or, when opened, a linked acceptance criterion. Unplanned behaviour changes are findings, even inside a planned file: message or event names, status codes, public signatures, unrelated refactors, environment workarounds. Fix: revert, or move to its own work item.
19. **Path parity.** The same input is parsed and validated the same way on every path that accepts it: read vs ingest, import vs edit API, preview vs confirm. Extends item 11 from failure behaviour to acceptance rules.
20. **Text and number parsing.** Culture-sensitive parsing; precision checked on raw text vs on the value (formula results carry float noise); formats accepted before the change that are now rejected (thousands separators, exponents, leading zeros), or the reverse.
21. **Server-side enforcement.** A rule the acceptance criteria require that only the client enforces.

## Checklist D — two-way audit (breaking changes and regression risk)
Compare behaviour at the merge base with behaviour after the change, for what already exists. Each item gets a row in the output table.
- **Forward (new code vs base behaviour):** items 24–26 and 29–32. Same input, same output, unless the pull request description or a linked acceptance criterion says otherwise.
- **Backward (existing consumers vs new code):** items 22, 23, 27 and 28. Start from who uses the changed thing and confirm each still works.

Apply a row only when that kind of artifact is in the diff or the repo checklist names it. Stored queries, seed flags, and messages are examples, not a requirement that the repo has them.

22. **Contracts.** REST routes, verbs, status codes and error shape; gRPC/proto fields and numbers; topics, event names and payloads; public signatures in shared packages. Look for anything removed, renamed, retyped, newly required, or with a changed meaning. Trace every consumer, including other repos listed under the repo checklist's "Contract map" and the `consumes` / `provides` lines in `agent-knowledge/contracts/` when that folder exists. A consumer you cannot open goes under Not checked, never "safe".
23. **Version skew.** Old producer with new consumer and the reverse during a rolling deploy, messages already queued, and services released separately. State any required deploy order.
24. **Existing data.** Rows, nodes, documents and seeds written by the old code must still load, validate and update: new required properties, tightened rules, changed enum values or casing. Name the backfill or migration if one is needed, and check it is idempotent.
25. **Seeding and startup writes.** Does a re-run overwrite values customers changed? Check seed flags and safe-to-update effects, and whether existing environments get the new rows.
26. **Changed defaults and silent behaviour.** Same input, different output, with no contract change: defaults, fallbacks, ordering, paging, filters, time zone, culture. Input accepted before and rejected now, or the reverse (see item 20).
27. **Access.** New or changed policies on existing endpoints: which existing roles lose or gain access, and whether the permission rows reach existing environments.
28. **Config and dependencies.** New, renamed or removed config keys, env vars and secrets: what happens when one is missing in a deployed environment, and which keys DevOps must add. Shared package bumps: behaviour changes that come in with the new version.
29. **Removed behaviour.** Deleted code paths, events, side effects, jobs and endpoints: who relied on them.
30. **Changed or deleted tests.** A test edited or removed means behaviour changed. Each one must trace to the pull request description or a linked acceptance criterion; otherwise the test was bent to pass, which is a finding.
31. **Load on existing paths.** New round-trips, queries without an index, larger payloads, or a removed cache on a path existing traffic already uses (extends item 6).
32. **Rollback.** After this has run and written data, can the previous version still read it? Flag irreversible migrations and data written in a new shape.
33. **Declared vs actual (parity ledger).** Reconcile with the pull request description and the linked acceptance criteria, in both directions: a break found but not declared is a finding, and a declared item that is not in the diff gets corrected. An item still open for sign-off goes under Needs a decision.
34. **Knowledge check.** When `agent-knowledge/INDEX.md` or another decision index exists, match the changed paths against it. For every active entry that matches: a decision record the diff contradicts is a finding unless the pull request supersedes it; a contract line it changes needs item 22's consumer trace; a lesson's check is applied, and a gap is a finding. Name the entry id in the row. When no index exists, the verdict is "not checked".

## Self-verification pass (mandatory before returning)
Re-open the code for every finding and assign a verdict:
- `Confirmed`
- `Narrower than first written` (fix the text)
- `Latent` (downgrade, add trigger)
- `Disproved` (delete it)

Only `Confirmed` and `Narrower` findings appear under Findings. Keep the verdict table.

## Output (one pull-request comment)
```
# <target> — Architect Review
Status: <pass or fail> · Base/merge-base: <sha> · Builds/tests: <result or not run>
Scope: <repos, files> · Not checked: <list or none>
Intent: <pull request description, linked story, or no spec; reviewed against code only>

## Findings   (or "none")
| # | Sev | Reachable today? | Evidence (file:line, trace) | Impact if wrong | Fix | Regression test |

## Breaking changes and regression risk   (two-way audit: one row per item 22–34)
| Item | Area | Verdict: none / safe / breaking / risk / not checked | Evidence or affected party | Status: accepted by whom / open / finding # |

## Needs a decision   (or none)
| # | Question | Options | Recommendation |

## Self-verification
| Draft finding | Verdict | Note |

## Verified OK   (short; only checks that would otherwise be re-done)
```
The breaking-changes rows record coverage and are not findings, so they are not padding; `none` with a short reason is the normal result.

Status is `fail` when any confirmed finding is Blocker or High and reachable today. Otherwise status is `pass`. Medium and Low stay in the comment and do not fail the run.
