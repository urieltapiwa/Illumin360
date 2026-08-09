# OSS Licence Inventory & SBOM

Charter Part 20: record every dependency's licence; generate a CycloneDX SBOM per build.

| Component | Licence | Notes |
| --- | --- | --- |
| .NET 10 / ASP.NET Core | MIT | Runtime + framework |
| YARP | MIT | Gateway |
| EF Core / Npgsql | MIT / PostgreSQL | Data |
| MassTransit | Apache-2.0 | Messaging |
| OpenTelemetry .NET | Apache-2.0 | Telemetry |
| Serilog | Apache-2.0 | Logging |
| Grafana / Loki / Tempo / Mimir / Alloy | AGPL-3.0 / Apache-2.0 | **AGPL note:** used as deployed services, not linked into our code — acceptable; confirm before redistributing. |
| Prometheus | Apache-2.0 | Metrics |
| Keycloak | Apache-2.0 | IAM |
| PostgreSQL | PostgreSQL (BSD-like) | Database |
| Redis | RSALv2/SSPLv1 (7.4+) / BSD (≤7.2) | **Pin BSD-licensed line (≤7.2) or use Valkey** — confirm in an ADR. |
| RabbitMQ | MPL-2.0 | Broker |
| MinIO | AGPL-3.0 | **AGPL note:** deployed as a service; confirm usage terms before redistribution. |
| Temporal | MIT | Workflow |
| ClamAV | GPL-2.0 | Deployed as a service (network-invoked). |
| HashiCorp Vault | BUSL-1.1 (1.15+) | **Not OSS since 1.15** — pin a pre-BUSL version or evaluate OpenBao (MPL-2.0). Decide in an ADR. |

> ⚠️ **Action items flagged for ADRs:** Redis licensing (consider **Valkey**), Vault licensing (consider
> **OpenBao**), and AGPL components (Grafana stack, MinIO) used as services. These don't block local dev but
> must be resolved before redistribution per the open-source-only principle.

Generate SBOM: `dotnet CycloneDX src/Illumin360.sln -o 00-governance/licenses/` (wired in CI).
