# Environments & Parity

> **Status:** Draft · **Owner:** _TBD_ · **Last updated:** 2026-05-29 · **Related ADRs:** —

| Env | Topology | Notes |
| --- | --- | --- |
| local | Docker Compose (`deploy/docker` + observability + apps overlay) / .NET Aspire | Mirrors prod topology |
| dev | Kubernetes + Helm | Auto-deploy on merge; DAST + smoke |
| staging | Kubernetes + Helm | Prod-like; manual gate |
| prod | Kubernetes + Helm | Canary/blue-green; Vault-backed secrets |
