# ADR-0005: CI platform GitHub Actions; IaC Terraform

- **Status:** Accepted
- **Date:** 2026-05-29

## Context
Charter Part 15 lists GitHub Actions (default) / GitLab CI and Terraform / Pulumi as decision points.

## Decision
**GitHub Actions** for CI/CD and **Terraform** for infrastructure-as-code, with Helm for app deployment.
GitOps (Argo CD/Flux) optional later.

## Consequences
**Positive:** Ubiquitous, strong marketplace, OIDC to cloud, free for the team's scale.
**Negative:** Vendor coupling to GitHub for pipelines (pipelines are portable YAML; mitigated).
