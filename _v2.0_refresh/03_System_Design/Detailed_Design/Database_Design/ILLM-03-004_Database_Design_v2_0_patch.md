# Database Design — v2.0 Refresh Patch

| Document detail | Value |
|---|---|
| Target document | ILLM-03-004_Database_Design (currently v1.0) |
| Patch version | v2.0 |
| Patch date | 14 May 2026 |
| Source authority | v3.6 Spec Sections 13, 21, 22, 23, 24, 26, 29, 30 |
| Patch type | Additive — new tables and columns; no destructive changes to v1.0 schema |

This patch lists the concrete DDL additions required for the v3.6 feature set. Apply via numbered migrations; each new column is nullable to permit live deployment on an existing dataset.

## 1. New tables

### 1.1 `founder_registrations`

```sql
CREATE TYPE founder_user_type AS ENUM ('job_seeker', 'employer');

CREATE TABLE founder_registrations (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id             UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE RESTRICT,
  user_type           founder_user_type NOT NULL,
  founder_number      INTEGER NOT NULL,
  granted_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
  granted_by          UUID REFERENCES users(id),
  grant_reason        TEXT,
  revoked_at          TIMESTAMPTZ,
  revoked_by          UUID REFERENCES users(id),
  revocation_reason   TEXT,
  UNIQUE (user_type, founder_number)
);

CREATE INDEX idx_founder_user_type ON founder_registrations(user_type, founder_number);
```

### 1.2 `employer_badges`

```sql
CREATE TYPE employer_badge_type AS ENUM (
  'founding_partner', 'compliant_recruiter', 'active_employer',
  'university_partner', 'top_employer'
);

CREATE TABLE employer_badges (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  employer_id     UUID NOT NULL REFERENCES employers(id) ON DELETE CASCADE,
  badge_type      employer_badge_type NOT NULL,
  earned_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
  is_displayed    BOOLEAN NOT NULL DEFAULT true,
  is_permanent    BOOLEAN NOT NULL DEFAULT false,
  revoked_at      TIMESTAMPTZ,
  revoked_by      UUID REFERENCES users(id),
  revoked_reason  TEXT,
  metadata        JSONB
);

CREATE UNIQUE INDEX uq_employer_badge_active
  ON employer_badges(employer_id, badge_type)
  WHERE revoked_at IS NULL;
```

### 1.3 `match_feedback`

```sql
CREATE TYPE feedback_source AS ENUM ('email_link', 'dashboard_prompt', 'manual');

CREATE TABLE match_feedback (
  id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  request_id            UUID NOT NULL REFERENCES recruitment_requests(id),
  match_id              UUID NOT NULL REFERENCES candidate_matches(id),
  employer_id           UUID NOT NULL REFERENCES employers(id),
  accuracy_rating       INTEGER NOT NULL CHECK (accuracy_rating BETWEEN 1 AND 5),
  justification_rating  INTEGER NOT NULL CHECK (justification_rating BETWEEN 1 AND 5),
  employer_notes        TEXT,
  scoring_model         VARCHAR(64) NOT NULL,
  weights_used          JSONB NOT NULL,
  industry              VARCHAR(128),
  role_category         VARCHAR(128),
  feedback_source       feedback_source NOT NULL,
  created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at            TIMESTAMPTZ,
  UNIQUE (employer_id, match_id)
);

CREATE INDEX idx_match_feedback_model ON match_feedback(scoring_model, created_at);
CREATE INDEX idx_match_feedback_industry ON match_feedback(industry, role_category);
```

### 1.4 `candidate_videos`

```sql
CREATE TYPE video_transcription_status AS ENUM
  ('pending', 'processing', 'completed', 'failed', 'flagged');
CREATE TYPE video_visibility AS ENUM ('public', 'private');

CREATE TABLE candidate_videos (
  id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  job_seeker_id               UUID NOT NULL UNIQUE REFERENCES job_seekers(id) ON DELETE CASCADE,
  file_url                    VARCHAR(500) NOT NULL,
  file_name                   VARCHAR(255) NOT NULL,
  file_size_bytes             BIGINT NOT NULL,
  mime_type                   VARCHAR(64) NOT NULL,
  duration_seconds            INTEGER NOT NULL,
  transcription_status        video_transcription_status NOT NULL DEFAULT 'pending',
  transcription_text          TEXT,
  transcription_keywords      JSONB,
  visibility                  video_visibility NOT NULL DEFAULT 'public',
  content_flagged             BOOLEAN NOT NULL DEFAULT false,
  content_flag_reason         TEXT,
  uploaded_at                 TIMESTAMPTZ NOT NULL DEFAULT now(),
  transcription_completed_at  TIMESTAMPTZ
);
```

### 1.5 `assistant_conversations`

```sql
CREATE TYPE assistant_user_type AS ENUM ('job_seeker', 'employer', 'student', 'public', 'admin');

CREATE TABLE assistant_conversations (
  id                       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id                  UUID REFERENCES users(id) ON DELETE SET NULL,
  user_type                assistant_user_type NOT NULL,
  session_id               VARCHAR(128) NOT NULL,
  messages                 JSONB NOT NULL DEFAULT '[]'::jsonb,
  context_snapshot         JSONB NOT NULL,
  started_at               TIMESTAMPTZ NOT NULL DEFAULT now(),
  last_message_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
  message_count            INTEGER NOT NULL DEFAULT 0,
  escalated_to_human       BOOLEAN NOT NULL DEFAULT false,
  escalation_notified_at   TIMESTAMPTZ,
  filter_triggered_count   INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX idx_assistant_user ON assistant_conversations(user_id, started_at DESC);
CREATE INDEX idx_assistant_session ON assistant_conversations(session_id);
CREATE INDEX idx_assistant_escalated ON assistant_conversations(escalated_to_human)
  WHERE escalated_to_human = true;
```

### 1.6 `assistant_prompts`

```sql
CREATE TABLE assistant_prompts (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  instance      assistant_user_type NOT NULL,
  version       INTEGER NOT NULL,
  active        BOOLEAN NOT NULL DEFAULT false,
  prompt_text   TEXT NOT NULL,
  created_by    UUID NOT NULL REFERENCES users(id),
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  activated_at  TIMESTAMPTZ,
  UNIQUE (instance, version)
);

CREATE UNIQUE INDEX uq_assistant_prompts_active
  ON assistant_prompts(instance)
  WHERE active = true;
```

### 1.7 `referrals`

```sql
CREATE TYPE referral_status AS ENUM (
  'pending', 'referred_registered', 'converted_paid', 'reward_applied'
);

CREATE TABLE referrals (
  id                                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  referrer_id                          UUID NOT NULL REFERENCES users(id),
  referred_user_id                     UUID REFERENCES users(id),
  referral_code                        VARCHAR(16) NOT NULL UNIQUE,
  status                               referral_status NOT NULL DEFAULT 'pending',
  converted_at                         TIMESTAMPTZ,
  reward_applied_at                    TIMESTAMPTZ,
  reward_subscription_extension_days   INTEGER NOT NULL DEFAULT 30,
  created_at                           TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_referrals_referrer ON referrals(referrer_id);
CREATE INDEX idx_referrals_code ON referrals(referral_code);
```

### 1.8 `insights`

```sql
CREATE TYPE insights_status AS ENUM ('draft', 'published', 'archived');

CREATE TABLE insights (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug                VARCHAR(255) NOT NULL UNIQUE,
  title               VARCHAR(255) NOT NULL,
  body_md             TEXT NOT NULL,
  category            VARCHAR(64) NOT NULL,
  tags                TEXT[],
  author_name         VARCHAR(128) NOT NULL,
  cover_image_url     VARCHAR(500),
  meta_description    TEXT,
  og_image_url        VARCHAR(500),
  status              insights_status NOT NULL DEFAULT 'draft',
  published_at        TIMESTAMPTZ,
  view_count          INTEGER NOT NULL DEFAULT 0,
  created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### 1.9 `spotlight_features`

```sql
CREATE TABLE spotlight_features (
  id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  student_id            UUID NOT NULL REFERENCES job_seekers(id),
  month                 INTEGER NOT NULL CHECK (month BETWEEN 1 AND 12),
  year                  INTEGER NOT NULL,
  quote                 VARCHAR(280),
  photo_url             VARCHAR(500),
  consent_confirmed     BOOLEAN NOT NULL DEFAULT false,
  consent_confirmed_at  TIMESTAMPTZ,
  consent_revoked_at    TIMESTAMPTZ,
  published_at          TIMESTAMPTZ,
  removed_at            TIMESTAMPTZ
);
```

### 1.10 `demand_feed_cache`

```sql
CREATE TYPE demand_signal_type AS ENUM ('role_x_city', 'top_skill', 'qualification_level');

CREATE TABLE demand_feed_cache (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  signal_type     demand_signal_type NOT NULL,
  label           VARCHAR(255) NOT NULL,
  count           INTEGER NOT NULL,
  city            VARCHAR(128),
  week_starting   DATE NOT NULL,
  is_suppressed   BOOLEAN NOT NULL DEFAULT false,
  generated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### 1.11 `employer_reviews`

```sql
CREATE TYPE review_moderation_status AS ENUM ('pending', 'approved', 'rejected');

CREATE TABLE employer_reviews (
  id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  employer_id          UUID NOT NULL REFERENCES employers(id),
  job_seeker_id        UUID NOT NULL REFERENCES job_seekers(id),
  communication        INTEGER NOT NULL CHECK (communication BETWEEN 1 AND 5),
  interview_process    INTEGER NOT NULL CHECK (interview_process BETWEEN 1 AND 5),
  conduct              INTEGER NOT NULL CHECK (conduct BETWEEN 1 AND 5),
  would_recommend      BOOLEAN NOT NULL,
  comment              TEXT,
  moderation_status    review_moderation_status NOT NULL DEFAULT 'pending',
  moderated_at         TIMESTAMPTZ,
  created_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (employer_id, job_seeker_id)
);
```

### 1.12 Update `candidate_badges` enum

```sql
ALTER TYPE candidate_badge_type ADD VALUE 'illumin360_founder';
ALTER TYPE candidate_badge_type ADD VALUE 'verified_student';
ALTER TYPE candidate_badge_type ADD VALUE 'graduate_spotlight';
ALTER TYPE candidate_badge_type ADD VALUE 'top_referrer';
-- (existing values preserved)
```

## 2. Column additions to existing tables

### 2.1 `job_seekers`

```sql
ALTER TABLE job_seekers
  ADD COLUMN is_founder           BOOLEAN NOT NULL DEFAULT false,
  ADD COLUMN photo_url            VARCHAR(500),
  ADD COLUMN photo_uploaded_at    TIMESTAMPTZ,
  ADD COLUMN photo_file_size_bytes INTEGER;
```

### 2.2 `employers`

```sql
ALTER TABLE employers
  ADD COLUMN is_founder           BOOLEAN NOT NULL DEFAULT false,
  ADD COLUMN logo_uploaded_at     TIMESTAMPTZ,
  ADD COLUMN logo_file_size_bytes INTEGER;
-- logo_url assumed already present in v1.0 per spec §24.1
```

### 2.3 `institution_email_domains`

```sql
ALTER TABLE institution_email_domains
  ADD COLUMN logo_url          VARCHAR(500),
  ADD COLUMN logo_uploaded_at  TIMESTAMPTZ;
```

### 2.4 `recruitment_requests`

```sql
ALTER TABLE recruitment_requests
  ADD COLUMN custom_weights   JSONB,
  ADD COLUMN weights_locked   BOOLEAN NOT NULL DEFAULT false;
```

### 2.5 `candidate_matches`

```sql
ALTER TABLE candidate_matches
  ADD COLUMN weights_used               JSONB,
  ADD COLUMN gap_analysis               JSONB,
  ADD COLUMN gap_analysis_displayed     BOOLEAN NOT NULL DEFAULT false;

-- Backfill historic weights_used with the standard schedule frozen at v1.0:
UPDATE candidate_matches
   SET weights_used = jsonb_build_object(
         'qualification', 20, 'skills', 20, 'experience', 15,
         'location', 15, 'availability', 10, 'language', 8,
         'certifications', 7, 'cv_recency', 5
       )
 WHERE weights_used IS NULL;

ALTER TABLE candidate_matches ALTER COLUMN weights_used SET NOT NULL;
```

### 2.6 `candidate_profiles`

```sql
ALTER TABLE candidate_profiles
  ADD COLUMN has_video              BOOLEAN NOT NULL DEFAULT false,
  ADD COLUMN video_keyword_weight   DECIMAL(3,2) NOT NULL DEFAULT 0.30;
```

### 2.7 `subscriptions`

```sql
ALTER TYPE subscription_plan_type ADD VALUE 'founder_permanent';

-- And add a row in pricing_plans:
INSERT INTO pricing_plans (plan_type, name, base_price, vat_rate, notes)
VALUES ('founder_permanent',
        'Illumin360 Founder — Permanent Subscription',
        0.00,
        0.15,
        'First 300 job seekers. No invoice generated. Reminder cron skips.');
```

## 3. Triggers

### 3.1 Verified Student badge award

```sql
CREATE OR REPLACE FUNCTION trg_award_verified_student_badge()
RETURNS TRIGGER AS $$
BEGIN
  IF NEW.verification_status = 'verified' AND
     (OLD IS NULL OR OLD.verification_status IS DISTINCT FROM 'verified') THEN
    INSERT INTO candidate_badges (job_seeker_id, badge_type, earned_at, is_displayed)
    VALUES (NEW.job_seeker_id, 'verified_student', now(), true)
    ON CONFLICT DO NOTHING;

  ELSIF NEW.verification_status IN ('rejected', 'expired') AND
        OLD.verification_status = 'verified' THEN
    UPDATE candidate_badges
       SET revoked_at = now(),
           revoked_reason = 'Verification ' || NEW.verification_status
     WHERE job_seeker_id = NEW.job_seeker_id
       AND badge_type = 'verified_student'
       AND revoked_at IS NULL;
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER award_verified_student_badge
AFTER INSERT OR UPDATE ON student_verifications
FOR EACH ROW EXECUTE FUNCTION trg_award_verified_student_badge();
```

### 3.2 `has_video` sync trigger

```sql
CREATE OR REPLACE FUNCTION trg_sync_has_video()
RETURNS TRIGGER AS $$
BEGIN
  UPDATE candidate_profiles
     SET has_video = EXISTS (
           SELECT 1 FROM candidate_videos cv
            WHERE cv.job_seeker_id = COALESCE(NEW.job_seeker_id, OLD.job_seeker_id)
              AND cv.transcription_status = 'completed'
              AND cv.content_flagged = false
         )
   WHERE candidate_profiles.job_seeker_id = COALESCE(NEW.job_seeker_id, OLD.job_seeker_id);
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER sync_has_video
AFTER INSERT OR UPDATE OR DELETE ON candidate_videos
FOR EACH ROW EXECUTE FUNCTION trg_sync_has_video();
```

## 4. Migration order

Migrations apply in this order, each as a separate file for rollback granularity:

1. New enums (founder_user_type, employer_badge_type, etc.)
2. Tables 1.1–1.11 in dependency order
3. Column additions 2.1–2.7
4. Backfill of `candidate_matches.weights_used` then NOT NULL constraint
5. Pricing plan row
6. Trigger functions and triggers
7. Initial assistant_prompts seed rows (one per instance)

Each migration file follows the pattern `ILLM-07-010_v2.0_NN_<name>.sql` per Section 7 conventions and includes a corresponding rollback in `ILLM-07-012`.

## 5. Index review

After the additions, review `EXPLAIN ANALYZE` for the matching engine query path. Verify:
- The shortlist projection does **not** include `job_seekers.photo_url` (compliance with blind-screening structural enforcement)
- `assistant_conversations(user_id, started_at DESC)` index serves admin conversation lookups
- `match_feedback(scoring_model)` index serves the Phase 8 coverage view

## 6. Privacy and retention

| Table | Retention |
|---|---|
| founder_registrations | Permanent (audit trail of permanent benefit) |
| match_feedback | 7 years (compliance/audit per §15.3) |
| assistant_conversations | 90 days non-escalated; 7 years escalated |
| candidate_videos | Bound to candidate account lifecycle plus 30 days |
| employer_reviews | Bound to employer account lifecycle |

## 7. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | (existing) | Initial schema covering Identity, Candidate Profile, Recruitment, Finance, System Logs domains |
| 2.0 | 14 May 2026 | Added 11 new tables (founder_registrations, employer_badges, match_feedback, candidate_videos, assistant_conversations, assistant_prompts, referrals, insights, spotlight_features, demand_feed_cache, employer_reviews), column additions across 7 existing tables, two new triggers, founder_permanent plan, expanded candidate_badge_type enum. |
