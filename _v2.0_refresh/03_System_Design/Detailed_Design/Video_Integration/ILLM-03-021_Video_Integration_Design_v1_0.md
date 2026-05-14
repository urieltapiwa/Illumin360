# Video Integration — Detailed Design

| Document detail | Value |
|---|---|
| Document title | Illumin360 Video Integration — Detailed Design |
| Document ID | ILLM-03-021_Video_Integration_Design |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft |
| Source authority | Section 23, Illumin360 Complete Technical Specification v3.6 |
| Phase | Phase 7 — Premium |
| Owner | Platform Engineering — Media |

## 1. Purpose

The Video Integration Module is a Phase 7 premium feature allowing subscribed job seekers and students to upload a 60-second professional video pitch. Automated transcription extracts soft-skill keywords from the audio. These keywords contribute to candidate scoring at 30% of standard CV keyword weight — a conservative cap that mitigates transcription error impact.

Employer video-request features (e.g., requesting a tailored video from a candidate) are explicitly **out of scope** per the v3.6 Phase 7 scope.

## 2. Video specifications

| Specification | Requirement |
|---|---|
| Maximum duration | 60 seconds — hard limit enforced at upload time |
| Maximum file size | 150 MB |
| Accepted formats | MP4 (H.264), MOV, WebM |
| Minimum resolution | 480p (transcription accuracy degrades below this) |
| Transcription keyword weight | 30% of standard CV keyword weight (admin-configurable in `candidate_profiles.video_keyword_weight`) |
| Visibility | Public (default — visible on public profile and social feed) or Private (only after candidate unlock) |
| Content policy | Professional content only. Admin can flag and remove non-compliant videos. |

## 3. Upload pipeline

1. Candidate selects video file via the dashboard.
2. Client validates file size, format, and (best-effort) duration before upload.
3. Server issues a presigned URL; client uploads directly to object storage.
4. Server triggers virus scan asynchronously.
5. On successful scan, transcription job is queued.
6. Transcription completes typically within 5–15 minutes.
7. Keywords extracted from transcript are stored on the candidate record.
8. Candidate is notified by email when video is live and transcription complete.

If transcription fails (low audio quality, unsupported language), the video remains uploaded but the candidate is notified that transcription was unsuccessful and the video does not contribute to keyword scoring until reuploaded. Visibility remains the candidate's choice.

## 4. Transcription

Transcription provider selection (between Google Cloud Speech-to-Text and AWS Transcribe) requires evaluation against Namibian English, Afrikaans, and Oshiwambo accuracy. Provider selection is deferred to the implementation phase; the system is designed to be provider-agnostic — pipeline interfaces with a `TranscriptionProvider` abstraction.

Once transcript is available:
1. Stored in `candidate_videos.transcription_text`.
2. Keyword extraction runs (same NLP pipeline used for CV text, with provider-tracked confidence scores).
3. Top-N keywords with confidence ≥ 0.70 are stored in `candidate_videos.transcription_keywords`.
4. The candidate's effective keyword pool for matching becomes: `CV_keywords ∪ (video_keywords × 0.30 weight)`.

The 0.30 weight is applied at scoring time, not at extraction time. The candidate-keyword union is recomputed on every match.

## 5. Branding compliance

Per Section 31:
- Feature label: "Video pitch" or "Candidate elevator pitch" — never "AI video", "AI transcription", "AI keywords"
- Notification: "Your video is live and processed" — never "Our AI has analysed your video"
- Public-facing copy: "Video is automatically processed for keyword indexing" — generic, no provider names

## 6. Data model

### 6.1 New table — `candidate_videos`

| Column | Type | Constraints | Description |
|---|---|---|---|
| id | UUID PK | | |
| job_seeker_id | UUID FK | UNIQUE | One active video per candidate |
| file_url | VARCHAR(500) | NOT NULL | Object storage path |
| file_name | VARCHAR(255) | NOT NULL | Original filename |
| file_size_bytes | INTEGER | NOT NULL | |
| mime_type | VARCHAR(64) | NOT NULL | |
| duration_seconds | INTEGER | NOT NULL | Server-detected duration |
| transcription_status | ENUM | NOT NULL | `pending`, `processing`, `completed`, `failed`, `flagged` |
| transcription_text | TEXT | NULL | Full transcript |
| transcription_keywords | JSONB | NULL | Array of `{keyword, confidence}` |
| visibility | ENUM | NOT NULL DEFAULT `public` | `public`, `private` |
| content_flagged | BOOLEAN | DEFAULT false | Admin review queue trigger |
| content_flag_reason | TEXT | NULL | Admin or auto-detected reason |
| uploaded_at | TIMESTAMPTZ | NOT NULL | |
| transcription_completed_at | TIMESTAMPTZ | NULL | |

### 6.2 Additions to `candidate_profiles`

| Column | Type | Default | Description |
|---|---|---|---|
| has_video | BOOLEAN | false | Maintained by `trg_sync_has_video` trigger on candidate_videos |
| video_keyword_weight | DECIMAL(3,2) | 0.30 | Admin-configurable globally — but stored per profile to allow per-candidate override if needed for fairness |

The trigger keeps `has_video` synchronised — true if there is a non-flagged completed video, false otherwise.

## 7. Visibility and unlock behaviour

| State | Public visibility | Behaviour |
|---|---|---|
| `visibility = public`, `transcription_status = completed`, `content_flagged = false` | Visible on public profile card and social feed | Anyone (including unauthenticated) can view |
| `visibility = private` | Hidden everywhere except in the unlocked-candidate detail page once an employer has paid the unlock fee | Same blind-screening principle as photos |
| `content_flagged = true` | Hidden everywhere pending admin review | Admin queue |
| Transcription failed | Visible per candidate's visibility choice; keywords do not contribute to scoring | — |

## 8. API endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | /job-seekers/me/video | Bearer | Returns presigned upload URL + signed POST data |
| PUT | /job-seekers/me/video | Bearer | Confirms upload — server queues virus scan + transcription |
| DELETE | /job-seekers/me/video | Bearer | Removes video and clears keywords from scoring pool |
| PUT | /job-seekers/me/video/visibility | Bearer | Body: `{visibility: 'public'|'private'}` |
| GET | /public/p/:username/video | None | Returns video stream URL if `visibility=public` and not flagged |
| GET | /employers/me/requests/:id/candidates/:cid | Bearer (paid unlock) | Now includes `video_url` if candidate has a video (regardless of visibility setting, after unlock) |
| GET | /admin/videos/flagged | Admin | Moderation queue |
| POST | /admin/videos/:id/moderate | Admin | Approve or remove |

## 9. Content moderation

| Layer | Mechanism |
|---|---|
| Automated content scanning | Provider's safe-content classifier flags adult, violent, or hateful content |
| Audio content scan | Transcript scanned against a banned-terms list — flag if present |
| Admin review queue | All flagged videos held until admin decision |
| User reporting | Public-facing report button on the social feed; reported videos enter admin queue |
| Action | Admin may remove the video (which deletes from object storage and clears keywords) or approve (clears flag) |

Removal logs the action with timestamp, admin, and reason. Candidate is notified.

## 10. Acceptance criteria

1. Videos longer than 60 seconds are rejected at upload with a clear error.
2. Videos larger than 150 MB are rejected at upload.
3. Transcription completes within 15 minutes for 95% of uploads.
4. Keywords from the transcript contribute to candidate scoring at exactly 30% weight.
5. `visibility=public` videos are reachable from the public profile card without authentication.
6. `visibility=private` videos do not appear in any public surface and are only accessible to employers who have paid the unlock fee.
7. Flagged videos disappear from all public surfaces within 1 minute and enter the admin moderation queue.
8. Removing a video clears its keywords from the candidate's effective scoring pool on next match.
9. No client-facing copy references "AI" or any specific transcription provider.

## 11. Cross-references

| Document | Section |
|---|---|
| Spec v3.6 | Section 23 (canonical) |
| Asset Management Design (ILLM-03-017) | Common asset pipeline |
| Database Design (ILLM-03-004 v2.0) | candidate_videos table |
| API Design (ILLM-03-005 v2.0) | Video endpoints |
| AI Services Design (ILLM-03-008 v2.0) | Transcription provider abstraction (provider TBD) |
| Branding Policy (ILLM-03-012) | Language compliance |

## 12. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue. |
