---
title: Connected Movement Quality v1
createdAt: 2026-08-06T06:46:50.1079220Z
modifiedAt: 2026-08-07T19:32:02.2417400Z
---

## Deliverable

Connected Movement Quality v1 is a completable quality milestone, not an ongoing bucket. It proves bounded remote interpolation and explicit local correction diagnostics under deterministic network conditions.

## Activation and internal dependency path

Completed `CLIENT-0009` and `CLIENT-0023` activate `connected_snapshot_presentation_available`. M7 requires that trigger, so its tasks may consume the delivered local-player and remote-monster snapshot-to-presentation adapters without repeating cross-milestone task dependencies.

Internal implementation order remains:

~~~text
CLIENT-0033  remote snapshot buffering and interpolation
CLIENT-0034  local correction diagnostics
  -> CLIENT-0035  latency/loss/reordering/correction fixtures
  -> CLIENT-0036  macOS before/after native validation
~~~

`CLIENT-0033` consumes the delivered remote connected-snapshot adapter. `CLIENT-0034` consumes the delivered local connected-snapshot/correction adapter. This milestone remains independent of Connected Basic Arrow and must not block combat delivery.

## Policy split

Remote non-local actors, initially connected monsters, use buffered interpolation. The same policy may extend to remote players only when a real remote-player snapshot consumer exists.

The local player continues to present the newest accepted authoritative state while diagnostics expose corrections. V1 does not blindly add interpolation delay to local control.

Prediction and reconciliation are evidence-gated. If deterministic fixtures or native validation demonstrate a need, a later separately planned v2 deliverable may add them. A broader movement-quality initiative may remain milestone-free and `priority: none`; the M7 milestone itself must close.

## Validation

Use reproducible seeds and settings for representative latency, loss, reordering and correction fixtures. Compare before and after behavior on macOS ARM64. Record visible judder, correction frequency and failure modes without smuggling prediction or reconciliation into v1.