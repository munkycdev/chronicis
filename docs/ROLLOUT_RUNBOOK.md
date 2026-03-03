# Chronicis Rollout Runbook

## Purpose

This runbook defines staged rollout checkpoints, go/no-go criteria, and rollback actions for architecture workstreams.

## Scoped Rollout Controls

- Infrastructure rollout control: Azure Container Apps revision traffic splitting.
- Checkpoint control: `scripts/rollout-checkpoint.ps1` with a checkpoint config file.
- Risk gate: any failed checkpoint evaluation triggers rollback.

## Staged Rollout Sequence

1. Deploy new revision at `0%` traffic.
2. Run validation at `10%` traffic.
3. Run validation at `50%` traffic.
4. Run validation at `100%` traffic.
5. Keep prior stable revision available until post-100% stabilization window completes.

## Checkpoint Command

```powershell
.\scripts\rollout-checkpoint.ps1 -ConfigPath .\scripts\rollout-checkpoint.sample.json
```

Exit codes:
- `0`: proceed to next rollout stage.
- `1`: rollback immediately.

## Required Operational Indicators

- Error rate (%): source from Datadog APM service dashboard.
- P95 latency (ms): source from Datadog endpoint/request latency.
- Authorization denials (%): source from Datadog logs/APM status-code split (401/403).
- Data consistency delta: source from post-deploy validation queries and parity spot checks.

## Default Rollback Criteria

- `readiness` is not `healthy`.
- `unhealthy services` > threshold.
- `degraded services` > threshold.
- `p95 latency` > threshold.
- `error rate` > threshold.
- `authorization denials` > threshold.
- `data consistency delta` > threshold.

## Rollback Actions

1. Shift traffic back to last stable revision (100%).
2. Disable affected rollout stage advancement.
3. Capture checkpoint output + Datadog snapshot links.
4. Open incident ticket with failing indicators and revision IDs.
5. Block further rollout until root cause is resolved and checkpoint is green.

## Evidence to Store Per Stage

- Checkpoint config used.
- Checkpoint script output.
- Revision IDs and traffic split.
- Datadog links for latency/error/authorization dashboards.
- Data consistency query output.
