# Rollback Strategy

> **Status:** Draft · **Owner:** _TBD_ · **Last updated:** 2026-05-29 · **Related ADRs:** —

Backward-compatible migrations (expand/contract) so app rollback never requires a DB rollback. Redeploy the
previous image tag; Helm `rollback`. Feature flags to disable new paths without redeploy. Verify via
`/health/ready` + smoke. Database restore tested per `11_Operations_Maintenance/Backup_Recovery_Procedures/`.
