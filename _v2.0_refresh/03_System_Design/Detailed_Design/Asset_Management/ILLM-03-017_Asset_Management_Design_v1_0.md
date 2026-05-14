# Asset Management and Profile Branding — Detailed Design

| Document detail | Value |
|---|---|
| Document title | Illumin360 Asset Management — Detailed Design |
| Document ID | ILLM-03-017_Asset_Management_Design |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft |
| Source authority | Section 24, Illumin360 Complete Technical Specification v3.6 |
| Phase | Phase 6 |
| Owner | Platform Engineering |

## 1. Purpose

This document specifies the upload, storage, processing, and display of three asset types that brand and differentiate platform content: employer logos, university and institution logos, and job-seeker professional headshots. The blind-screening rule for candidate photos is a compliance-critical element and is addressed in §4.

## 2. Common asset handling

All asset uploads share a common pipeline:

| Step | Behaviour |
|---|---|
| Upload | Presigned URL — client uploads directly to object storage. Server never proxies bytes. |
| Virus scan | Asynchronous scan on object-storage upload event. Files failing scan are deleted and the user is notified. |
| MIME validation | Server-side allowed-list enforced. Client MIME headers not trusted. |
| Dimension and size validation | Per asset type — bounds defined below. |
| Storage path | `assets/<asset_type>/<owner_id>/<uuid>.<ext>` — segmented to prevent enumeration. |
| Public URL | CDN-fronted signed URLs for performance. Signature expiry depends on asset type (see below). |
| Audit | Upload, replacement, and deletion logged with `(asset_type, owner_id, file_uuid, action, timestamp)`. |

## 3. Employer logo

Employers upload a company logo for inclusion in shortlist report covers and the employer's public profile in the social feed.

| Specification | Value |
|---|---|
| Path | Employer dashboard → Profile → "Company logo" |
| Formats | PNG, JPG, SVG |
| Maximum file size | 5 MB |
| Recommended dimensions | 800×800 px square or 1200×400 banner — auto-cropped to square for report cover use |
| Database fields | `employers.logo_url` (existing), `employers.logo_uploaded_at` (new), `employers.logo_file_size_bytes` (new) |
| Public URL expiry | 7 days for signed URL — refreshed by report generation pipeline as needed |
| Visibility | Public — visible on report covers and employer profile page |

### 3.1 Report cover integration

When a shortlist report is generated, the employer logo (if present) is rendered alongside the Illumin360 logo in the cover header. If no logo is uploaded, the cover header shows only the Illumin360 logo and the employer's name in text. No placeholder graphic.

### 3.2 Branding compliance

Logo rendering does not include any "Powered by AI" or similar attribution. The cover header reads simply:

> Illumin360 Shortlist Report
> Prepared for: [Employer Name with optional logo]

## 4. Job seeker professional photo — blind screening

This is the most compliance-sensitive asset. The platform supports candidate photos for use post-unlock, but **never** in shortlist scoring or in the pre-unlock preview.

| Specification | Value |
|---|---|
| Path | Job seeker dashboard → Profile → "Professional photo (optional)" |
| Formats | JPG, PNG (no SVG — risk of scripted content) |
| Maximum file size | 5 MB |
| Recommended dimensions | 600×600 px square minimum |
| Database fields | `job_seekers.photo_url`, `job_seekers.photo_uploaded_at`, `job_seekers.photo_file_size_bytes` |
| Used in scoring? | **No.** The matching engine receives no photo data. |
| Visible at shortlist? | **No.** The anonymous preview shows no photo. The full unlocked report shows no photo. |
| Visible after candidate unlock? | **Yes** — only when an employer has paid the per-candidate unlock fee. |
| Public profile card | Optional — candidate consent toggle controls whether the photo appears on the public profile (illumin360.com/p/[username]) |

### 4.1 Why blind at shortlist

The blind-screening rule protects the platform against discrimination claims and preserves the legal defensibility of the shortlist as a documented-criteria-only assessment. The matching engine and the report are produced without the photo even available — there is no risk of inadvertent demographic-correlated bias being introduced via image features.

### 4.2 Enforcement

Enforcement is **structural** — the photo is not a column on the candidate-matches projection, and the report templates have no slot for a photo at any stage other than the post-unlock candidate-details page. The shortlist report generator does not query `job_seekers.photo_url`. There is no operational toggle to enable photos earlier in the flow.

### 4.3 Public profile card opt-in

A candidate may opt to display their photo on the public profile card (illumin360.com/p/[username]). The default is off. When opted in, the photo is publicly visible. The opt-in toggle is in the candidate dashboard and is fully reversible.

## 5. University / institution logo

Institutional partners upload logos for display on co-branded student registration pages and in the Graduate Spotlight feature (Section 21 F5).

| Specification | Value |
|---|---|
| Path | Admin-managed partner profile (institutions are not self-service) |
| Formats | PNG, JPG, SVG |
| Maximum file size | 5 MB |
| Recommended dimensions | 800×800 px |
| Database fields | `institution_email_domains.logo_url`, `institution_email_domains.logo_uploaded_at` |
| Visibility | Public — student registration co-branding, Graduate Spotlight content |

Institutional logos are uploaded by an admin on the partner's behalf as part of the institutional onboarding workflow. There is no self-service institution upload in Phase 6.

## 6. Profile completion impact

| Asset | Affects `profile_complete_pct`? | Notes |
|---|---|---|
| Employer logo | Yes — adds 5% to employer profile completion | Optional but recommended |
| Job seeker photo | Yes — adds 5% to candidate profile completion | Optional; clearly labelled "Optional — does not affect your match scores" |
| Institution logo | No (institution-level, not user-level) | — |

Profile completion percentage drives badge unlocks (per ILLM-03-019).

## 7. Storage and CDN

| Element | Specification |
|---|---|
| Object storage | S3-compatible bucket per environment (dev / staging / prod) |
| Encryption | Server-side AES-256 at rest. TLS 1.2+ in transit. |
| CDN | CloudFront (or equivalent) in front of the bucket with signed-URL access control |
| Signed URL expiry | 7 days default. Job-seeker photo URLs issued only via authenticated endpoints — never publicly indexable. |
| Backup | Cross-region replication daily |

## 8. Deletion behaviour

| Asset | Behaviour on user-initiated delete | Behaviour on account deletion |
|---|---|---|
| Employer logo | Hard delete from object storage within 24h. Existing reports with embedded logo retain the embedded copy. | Hard delete with account |
| Job seeker photo | Hard delete from object storage within 24h | Hard delete with account, alongside CV per Section 12 retention rules |
| Institution logo | Hard delete only on partnership termination; admin-controlled | Not applicable |

Deletion is irrevocable. No soft-delete. Audit log retains the action record.

## 9. API endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | /employers/me/logo | Bearer (employer) | Returns presigned URL for upload |
| PUT | /employers/me/logo | Bearer | Confirms upload complete — server triggers virus scan |
| DELETE | /employers/me/logo | Bearer | Removes employer logo |
| POST | /job-seekers/me/photo | Bearer (candidate) | Returns presigned URL for upload |
| PUT | /job-seekers/me/photo | Bearer | Confirms upload, sets `photo_uploaded_at` |
| DELETE | /job-seekers/me/photo | Bearer | Removes photo. Also removes from any cached public profile card. |
| GET | /employers/me/requests/:id/candidates/:cid/unlock | Bearer (employer, after payment) | Response now includes `photo_url` (signed URL, 7-day expiry) when unlock is paid |
| POST | /admin/institutions/:id/logo | Admin | Admin upload on behalf of institution |
| DELETE | /admin/institutions/:id/logo | Admin | — |

## 10. UI — candidate photo consent and clarity

The candidate dashboard photo upload card displays a clear notice:

> *"Your photo will not be used to match you with jobs. It will not appear in any shortlist or report unless an employer has paid to unlock your full profile. You can optionally display it on your public profile card."*

This text is part of the v3.6-compliant client-facing copy and contains no AI references.

## 11. Acceptance criteria

1. Employer logo uploads accept PNG/JPG/SVG up to 5 MB and reject larger or other formats with a clear error.
2. Job seeker photo upload accepts JPG/PNG only (no SVG) up to 5 MB.
3. Shortlist report generator query plan does not access `job_seekers.photo_url` — verified by query audit.
4. The anonymous preview screen renders no photo for any candidate.
5. The full unlocked report renders no photo in Section 3 candidate cards or anywhere else in the report.
6. Post-unlock candidate detail page renders the photo when present, with a 7-day signed URL.
7. Public profile card respects the candidate's opt-in flag — photo absent by default; only present when explicitly enabled.
8. Photo deletion removes the file from object storage within 24h, removes the URL from any cached responses, and logs the action.
9. Profile completion percentage updates correctly when each asset is uploaded or deleted.
10. No client-facing copy includes "AI" or third-party provider references.

## 12. Cross-references

| Document | Section |
|---|---|
| Spec v3.6 | Section 24 (canonical), Section 8 (matching engine — photo not used), Section 15 (compliance — anti-discrimination) |
| Database Design (ILLM-03-004 v2.0) | New columns on `employers`, `job_seekers`, `institution_email_domains` |
| API Design (ILLM-03-005 v2.0) | Asset endpoints per §9 |
| Branding Policy (ILLM-03-012) | Logo and copy compliance |
| Compliance Anti-Discrimination Controls (ILLM-09-016 v2.0) | Blind screening enforcement |
| Social Features Design (ILLM-03-020) | Public profile card photo display logic |

## 13. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue. |
