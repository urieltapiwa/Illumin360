-- =============================================================================
-- ILLUMIN360 TALENT MATCH & RECRUITMENT PLATFORM
-- MASTER DATABASE MIGRATION FILE — ALL VERSIONS v1.0 THROUGH v3.4
-- Illumin Investments CC (CC 2016/08234 | VAT 07851437-015)
-- Trading as Illumin | Product: Illumin360
-- www.illumininvestments.com | projects@illumininvestments.com
-- =============================================================================
-- v1.0  Migrations 001-010  Core platform
-- v2.0  Migrations 011-028  Internal recruitment link
-- v3.0  Migrations 029-042  Student CSR programme
-- v3.3  Migrations 043-050  Social and community features
-- v3.4  Migrations 051-060  AI evolution, video, assets, PWA, badges
-- =============================================================================
-- Run: psql -U postgres -d illumin360 -f illumin360_master_migrations_v3.4.sql
-- Safe on a fresh database. All changes are additive and idempotent.
-- =============================================================================

BEGIN;

-- =============================================================================
-- ILLUMIN360 TALENT MATCH & RECRUITMENT PLATFORM
-- Complete Database Migration — All Versions Consolidated
-- Illumin Investments CC (CC 2016/08234 | VAT 07851437-015)
-- Trading as Illumin | Product: Illumin360
-- www.illumininvestments.com | projects@illumininvestments.com
-- =============================================================================
-- Version history:
--   v1.0 — Migrations 001-010 — Core platform
--   v2.0 — Migrations 011-028 — Internal recruitment link
--   v3.0 — Migrations 029-042 — Student CSR programme
--   v3.3 — Migrations 043-050 — Social and community features
-- =============================================================================
-- Database: PostgreSQL 15+
-- Run order: This single file contains all migrations in sequence.
-- Safe to run on a fresh database. Idempotent where possible.
-- =============================================================================


-- =============================================================================
-- EXTENSIONS
-- =============================================================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================================
-- V1.0 — MIGRATIONS 001-010 — CORE PLATFORM
-- =============================================================================

-- MIGRATION 001: USERS
-- =============================================================================
CREATE TABLE IF NOT EXISTS users (
  id                UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  email             VARCHAR(320)  NOT NULL UNIQUE,
  password_hash     VARCHAR(255)  NOT NULL,
  role              VARCHAR(20)   NOT NULL CHECK (role IN ('job_seeker','employer','admin')),
  is_active         BOOLEAN       NOT NULL DEFAULT true,
  email_verified    BOOLEAN       NOT NULL DEFAULT false,
  email_verify_token VARCHAR(100),
  email_verify_expires_at TIMESTAMPTZ,
  password_reset_token    VARCHAR(100),
  password_reset_expires_at TIMESTAMPTZ,
  last_login_at     TIMESTAMPTZ,
  login_count       INTEGER       NOT NULL DEFAULT 0,
  created_at        TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at        TIMESTAMPTZ   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_role ON users(role);

-- MIGRATION 002: JOB SEEKERS
-- =============================================================================
CREATE TABLE IF NOT EXISTS job_seekers (
  id                  UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id             UUID          NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
  profile_type        VARCHAR(20)   NOT NULL DEFAULT 'job_seeker'
                        CHECK (profile_type IN ('job_seeker','student')),
  first_name          VARCHAR(100)  NOT NULL,
  last_name           VARCHAR(100)  NOT NULL,
  mobile              VARCHAR(20)   NOT NULL,
  city                VARCHAR(100)  NOT NULL,
  nationality         VARCHAR(100)  NOT NULL,
  gender              VARCHAR(20),
  dob                 DATE          NOT NULL,
  id_number           TEXT,
  linkedin_url        VARCHAR(500),
  availability_status VARCHAR(30)   NOT NULL DEFAULT 'actively_looking'
                        CHECK (availability_status IN ('actively_looking','open_to_opportunities','not_available')),
  willing_to_relocate BOOLEAN       NOT NULL DEFAULT false,
  salary_expectation  INTEGER,
  notice_period       VARCHAR(50),
  employment_status   VARCHAR(30)
                        CHECK (employment_status IN ('employed','unemployed','freelance','student') OR employment_status IS NULL),
  has_drivers_licence BOOLEAN       NOT NULL DEFAULT false,
  has_own_vehicle     BOOLEAN       NOT NULL DEFAULT false,
  preferred_communication VARCHAR(20) DEFAULT 'email'
                        CHECK (preferred_communication IN ('email','phone','whatsapp')),
  -- Social / community fields (v3.3)
  username            VARCHAR(30)   UNIQUE,
  public_profile_enabled BOOLEAN    NOT NULL DEFAULT true,
  public_headline     VARCHAR(150),
  public_statement    TEXT,
  referral_code       VARCHAR(8)    UNIQUE,
  created_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at          TIMESTAMPTZ   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_js_user_id ON job_seekers(user_id);
CREATE INDEX IF NOT EXISTS idx_js_city ON job_seekers(city);
CREATE INDEX IF NOT EXISTS idx_js_availability ON job_seekers(availability_status);
CREATE INDEX IF NOT EXISTS idx_js_profile_type ON job_seekers(profile_type);
CREATE INDEX IF NOT EXISTS idx_js_username ON job_seekers(username) WHERE username IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_js_referral_code ON job_seekers(referral_code) WHERE referral_code IS NOT NULL;

-- MIGRATION 003: EMPLOYERS
-- =============================================================================
CREATE TABLE IF NOT EXISTS employers (
  id                  UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id             UUID          NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
  company_name        VARCHAR(255)  NOT NULL,
  contact_person      VARCHAR(200)  NOT NULL,
  mobile              VARCHAR(20)   NOT NULL,
  city                VARCHAR(100)  NOT NULL,
  address             TEXT,
  industry            VARCHAR(100),
  vat_number          VARCHAR(50),
  registration_number VARCHAR(50),
  logo_url            VARCHAR(500),
  billing_contact     VARCHAR(200),
  billing_email       VARCHAR(320),
  purchase_order_ref  VARCHAR(100),
  total_requests      INTEGER       NOT NULL DEFAULT 0,
  total_spent         DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  created_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at          TIMESTAMPTZ   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_employers_user_id ON employers(user_id);
CREATE INDEX IF NOT EXISTS idx_employers_city ON employers(city);

-- MIGRATION 004: CANDIDATE PROFILES
-- =============================================================================
CREATE TABLE IF NOT EXISTS candidate_profiles (
  id                        UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_seeker_id             UUID          NOT NULL UNIQUE REFERENCES job_seekers(id) ON DELETE CASCADE,
  -- Qualification (standard)
  qualification_status      VARCHAR(20)   NOT NULL DEFAULT 'completed'
                              CHECK (qualification_status IN ('completed','in_progress')),
  highest_qualification     VARCHAR(150),
  qualification_field       VARCHAR(100),
  nqf_level                 INTEGER       CHECK (nqf_level BETWEEN 1 AND 10),
  institution               VARCHAR(255),
  years_experience          INTEGER       NOT NULL DEFAULT 0,
  industry_experience       TEXT,
  certifications            TEXT,
  professional_memberships  TEXT,
  tools_systems             TEXT,
  professional_summary      TEXT,
  profile_complete_pct      INTEGER       NOT NULL DEFAULT 0 CHECK (profile_complete_pct BETWEEN 0 AND 100),
  -- Student-specific fields (v3.0)
  expected_completion_date  DATE,
  current_year_of_study     VARCHAR(30),
  total_years_of_study      INTEGER       CHECK (total_years_of_study BETWEEN 1 AND 10),
  modules_completed         TEXT,
  academic_achievements     TEXT,
  gpa_range                 VARCHAR(20)   CHECK (gpa_range IN ('distinction','merit','pass') OR gpa_range IS NULL),
  open_to_graduate_programmes BOOLEAN    NOT NULL DEFAULT false,
  internship_experience     TEXT,
  extracurricular_activities TEXT,
  personal_statement        TEXT,
  created_at                TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at                TIMESTAMPTZ   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_cp_js_id ON candidate_profiles(job_seeker_id);
CREATE INDEX IF NOT EXISTS idx_cp_nqf ON candidate_profiles(nqf_level);
CREATE INDEX IF NOT EXISTS idx_cp_qual_status ON candidate_profiles(qualification_status);
CREATE INDEX IF NOT EXISTS idx_cp_expected_completion ON candidate_profiles(expected_completion_date)
  WHERE qualification_status = 'in_progress';
CREATE INDEX IF NOT EXISTS idx_cp_grad_programmes ON candidate_profiles(open_to_graduate_programmes)
  WHERE open_to_graduate_programmes = true;

-- MIGRATION 005: CANDIDATE CITY PREFERENCES
-- =============================================================================
CREATE TABLE IF NOT EXISTS candidate_city_preferences (
  id            UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_seeker_id UUID        NOT NULL REFERENCES job_seekers(id) ON DELETE CASCADE,
  city          VARCHAR(100) NOT NULL,
  all_cities    BOOLEAN     NOT NULL DEFAULT false,
  UNIQUE(job_seeker_id, city)
);
CREATE INDEX IF NOT EXISTS idx_ccp_js_id ON candidate_city_preferences(job_seeker_id);
CREATE INDEX IF NOT EXISTS idx_ccp_city ON candidate_city_preferences(city);

-- MIGRATION 006: CANDIDATE SKILLS, LANGUAGES, QUALIFICATIONS
-- =============================================================================
CREATE TABLE IF NOT EXISTS candidate_skills (
  id            UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_seeker_id UUID        NOT NULL REFERENCES job_seekers(id) ON DELETE CASCADE,
  skill_name    VARCHAR(100) NOT NULL,
  proficiency   VARCHAR(20) NOT NULL DEFAULT 'intermediate'
                  CHECK (proficiency IN ('beginner','intermediate','advanced','expert')),
  UNIQUE(job_seeker_id, skill_name)
);

CREATE TABLE IF NOT EXISTS candidate_languages (
  id            UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_seeker_id UUID        NOT NULL REFERENCES job_seekers(id) ON DELETE CASCADE,
  language      VARCHAR(80) NOT NULL,
  proficiency   VARCHAR(20) NOT NULL DEFAULT 'fluent'
                  CHECK (proficiency IN ('basic','conversational','fluent','native')),
  UNIQUE(job_seeker_id, language)
);

CREATE TABLE IF NOT EXISTS candidate_qualifications (
  id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_seeker_id   UUID        NOT NULL REFERENCES job_seekers(id) ON DELETE CASCADE,
  qualification   VARCHAR(200) NOT NULL,
  field           VARCHAR(100),
  institution     VARCHAR(255),
  nqf_level       INTEGER     CHECK (nqf_level BETWEEN 1 AND 10),
  year_completed  INTEGER,
  is_highest      BOOLEAN     NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS idx_cq_js_id ON candidate_qualifications(job_seeker_id);

-- MIGRATION 007: CANDIDATE DOCUMENTS (CV)
-- =============================================================================
CREATE TABLE IF NOT EXISTS candidate_documents (
  id                  UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_seeker_id       UUID        NOT NULL REFERENCES job_seekers(id) ON DELETE CASCADE,
  document_type       VARCHAR(30) NOT NULL DEFAULT 'cv'
                        CHECK (document_type IN ('cv','portfolio','certificate','other')),
  file_url            VARCHAR(500) NOT NULL,
  file_name           VARCHAR(255) NOT NULL,
  file_size_bytes     INTEGER     NOT NULL,
  mime_type           VARCHAR(100) NOT NULL,
  cv_text_extracted   TEXT,
  is_active           BOOLEAN     NOT NULL DEFAULT true,
  uploaded_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_cd_js_id ON candidate_documents(job_seeker_id);
CREATE INDEX IF NOT EXISTS idx_cd_active ON candidate_documents(job_seeker_id, is_active)
  WHERE is_active = true;

-- MIGRATION 008: SUBSCRIPTIONS
-- =============================================================================
CREATE TABLE IF NOT EXISTS subscriptions (
  id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_seeker_id   UUID        NOT NULL REFERENCES job_seekers(id) ON DELETE CASCADE,
  plan            VARCHAR(20) NOT NULL
                    CHECK (plan IN ('student_free','3m','6m','12m','18m','24m')),
  start_date      DATE        NOT NULL,
  end_date        DATE        NOT NULL,
  status          VARCHAR(20) NOT NULL DEFAULT 'active'
                    CHECK (status IN ('active','expiring','expired','suspended','cancelled')),
  payment_id      UUID,
  auto_renew      BOOLEAN     NOT NULL DEFAULT false,
  renewal_count   INTEGER     NOT NULL DEFAULT 0,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_sub_js_id ON subscriptions(job_seeker_id);
CREATE INDEX IF NOT EXISTS idx_sub_status ON subscriptions(status);
CREATE INDEX IF NOT EXISTS idx_sub_end_date ON subscriptions(end_date, status);

CREATE TABLE IF NOT EXISTS subscription_reminders (
  id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  subscription_id UUID        NOT NULL REFERENCES subscriptions(id) ON DELETE CASCADE,
  threshold_days  INTEGER     NOT NULL,
  sent_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(subscription_id, threshold_days)
);

-- MIGRATION 009: PRICING PLANS
-- =============================================================================
CREATE TABLE IF NOT EXISTS pricing_plans (
  id          UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  plan_type   VARCHAR(50)   NOT NULL UNIQUE,
  name        VARCHAR(200)  NOT NULL,
  base_price  DECIMAL(10,2) NOT NULL,
  vat_rate    DECIMAL(5,2)  NOT NULL DEFAULT 15.00,
  currency    VARCHAR(3)    NOT NULL DEFAULT 'NAD',
  description TEXT,
  is_active   BOOLEAN       NOT NULL DEFAULT true,
  created_at  TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at  TIMESTAMPTZ   NOT NULL DEFAULT now()
);

INSERT INTO pricing_plans (plan_type, name, base_price, description) VALUES
  ('student_free',        'Student Profile — Illumin CSR Initiative (Free)',         0.00,     'Completely free for enrolled Namibian students. Duration: full studies + 60-day grace period.'),
  ('subscription_3m',     '3-Month Candidate Subscription',                          299.00,   '3-month active discoverable profile subscription.'),
  ('subscription_6m',     '6-Month Candidate Subscription',                          499.00,   '6-month active discoverable profile subscription.'),
  ('subscription_12m',    '12-Month Candidate Subscription',                         799.00,   '12-month active discoverable profile subscription.'),
  ('subscription_18m',    '18-Month Candidate Subscription',                        1099.00,   '18-month active discoverable profile subscription.'),
  ('subscription_24m',    '24-Month Candidate Subscription',                        1299.00,   '24-month active discoverable profile subscription.'),
  ('report_unlock',       'AI Shortlist Report — Unlock & Download',                1500.00,   'Standard search modes: platform pool, uploaded CVs, combined.'),
  ('internal_recruitment','Internal Recruitment — Candidate Portal & AI Report',    2000.00,   'Consolidated billing: portal service NAD 500 (25%) + report unlock NAD 1500 (75%). Single payment at report unlock.'),
  ('candidate_unlock',    'Candidate Profile Unlock',                                350.00,   'Per-candidate unlock: full contact details and CV download. Optional for all search modes.')
ON CONFLICT (plan_type) DO UPDATE
  SET name=EXCLUDED.name, base_price=EXCLUDED.base_price, description=EXCLUDED.description, updated_at=now();

-- MIGRATION 010: PAYMENTS, INVOICES, RECEIPTS
-- =============================================================================
CREATE TABLE IF NOT EXISTS payments (
  id                  UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  payer_id            UUID          NOT NULL,
  payer_type          VARCHAR(20)   NOT NULL CHECK (payer_type IN ('job_seeker','employer')),
  payment_type        VARCHAR(30)   NOT NULL
                        CHECK (payment_type IN ('subscription','report_unlock','candidate_unlock')),
  entity_id           UUID,
  amount              DECIMAL(12,2) NOT NULL,
  vat_rate            DECIMAL(5,2)  NOT NULL DEFAULT 15.00,
  vat_amount          DECIMAL(12,2) NOT NULL,
  total               DECIMAL(12,2) NOT NULL,
  currency            VARCHAR(3)    NOT NULL DEFAULT 'NAD',
  status              VARCHAR(20)   NOT NULL DEFAULT 'pending'
                        CHECK (status IN ('pending','paid','failed','cancelled','refunded')),
  method              VARCHAR(30),
  gateway_reference   VARCHAR(200)  UNIQUE,
  internal_reference  VARCHAR(50)   NOT NULL UNIQUE,
  billing_mode        VARCHAR(20)   NOT NULL DEFAULT 'standard'
                        CHECK (billing_mode IN ('standard','consolidated')),
  paid_at             TIMESTAMPTZ,
  failed_at           TIMESTAMPTZ,
  failure_reason      TEXT,
  refunded_at         TIMESTAMPTZ,
  refunded_by         UUID          REFERENCES users(id),
  created_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at          TIMESTAMPTZ   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_pay_payer ON payments(payer_id, payer_type);
CREATE INDEX IF NOT EXISTS idx_pay_status ON payments(status);
CREATE INDEX IF NOT EXISTS idx_pay_internal_ref ON payments(internal_reference);
CREATE INDEX IF NOT EXISTS idx_pay_gateway_ref ON payments(gateway_reference);

CREATE TABLE IF NOT EXISTS invoices (
  id              UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  payment_id      UUID          NOT NULL REFERENCES payments(id),
  invoice_number  VARCHAR(30)   NOT NULL UNIQUE,
  issued_at       TIMESTAMPTZ   NOT NULL DEFAULT now(),
  payer_name      VARCHAR(255)  NOT NULL,
  payer_email     VARCHAR(320)  NOT NULL,
  payer_vat       VARCHAR(50),
  file_url        VARCHAR(500),
  emailed_at      TIMESTAMPTZ,
  created_at      TIMESTAMPTZ   NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS receipts (
  id              UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  payment_id      UUID          NOT NULL REFERENCES payments(id),
  invoice_id      UUID          REFERENCES invoices(id),
  receipt_number  VARCHAR(30)   NOT NULL UNIQUE,
  issued_at       TIMESTAMPTZ   NOT NULL DEFAULT now(),
  file_url        VARCHAR(500),
  emailed_at      TIMESTAMPTZ,
  created_at      TIMESTAMPTZ   NOT NULL DEFAULT now()
);

-- =============================================================================
-- V2.0 — MIGRATIONS 011-028 — INTERNAL RECRUITMENT LINK
-- =============================================================================

-- MIGRATION 011: RECRUITMENT REQUESTS
-- =============================================================================
CREATE TABLE IF NOT EXISTS recruitment_requests (
  id                        UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  employer_id               UUID          NOT NULL REFERENCES employers(id),
  reference_number          VARCHAR(20)   NOT NULL UNIQUE,
  job_title                 VARCHAR(200)  NOT NULL,
  job_summary               TEXT          NOT NULL,
  department                VARCHAR(100),
  contract_type             VARCHAR(50),
  work_model                VARCHAR(50),
  min_qualification         VARCHAR(150)  NOT NULL,
  min_nqf_level             INTEGER       CHECK (min_nqf_level BETWEEN 1 AND 10),
  min_experience            INTEGER       NOT NULL DEFAULT 0,
  industry                  VARCHAR(100)  NOT NULL,
  key_skills                TEXT,
  required_languages        TEXT,
  salary_range_min          INTEGER,
  salary_range_max          INTEGER,
  target_cities             TEXT,
  all_cities                BOOLEAN       NOT NULL DEFAULT false,
  -- Search and billing mode
  search_source_mode        VARCHAR(20)   NOT NULL DEFAULT 'pool'
                              CHECK (search_source_mode IN ('pool','upload','combined','internal')),
  billing_mode              VARCHAR(20)   NOT NULL DEFAULT 'standard'
                              CHECK (billing_mode IN ('standard','consolidated')),
  -- Student/pipeline options (v3.0)
  include_student_profiles  BOOLEAN       NOT NULL DEFAULT false,
  graduate_programme_only   BOOLEAN       NOT NULL DEFAULT false,
  available_from_date       DATE,
  available_to_date         DATE,
  -- Internal recruitment link fields (v2.0)
  internal_link_token       VARCHAR(20)   UNIQUE,
  internal_link_url         VARCHAR(500),
  closing_datetime          TIMESTAMPTZ,
  link_expires_at           TIMESTAMPTZ,
  max_applications          INTEGER       CHECK (max_applications BETWEEN 1 AND 5000),
  require_employee_number   BOOLEAN       NOT NULL DEFAULT false,
  require_department        BOOLEAN       NOT NULL DEFAULT false,
  custom_message            TEXT,
  portal_status             VARCHAR(20)
                              CHECK (portal_status IN ('open','closed','expired') OR portal_status IS NULL),
  auto_notify_hr            VARCHAR(20)   NOT NULL DEFAULT 'none'
                              CHECK (auto_notify_hr IN ('none','each_submission','daily_digest')),
  portal_closed_at          TIMESTAMPTZ,
  portal_closed_by          VARCHAR(20)
                              CHECK (portal_closed_by IN ('scheduled','manual','cap_reached') OR portal_closed_by IS NULL),
  shortlist_triggered_at    TIMESTAMPTZ,
  -- Compliance
  declaration_confirmed     BOOLEAN       NOT NULL DEFAULT false,
  -- Output config
  candidate_volume          INTEGER       NOT NULL DEFAULT 4,
  longlist_volume           INTEGER       NOT NULL DEFAULT 10,
  scoring_model             VARCHAR(20)   NOT NULL DEFAULT 'standard'
                              CHECK (scoring_model IN ('standard','student','graduate_programme')),
  -- Status
  status                    VARCHAR(30)   NOT NULL DEFAULT 'draft'
                              CHECK (status IN ('draft','pending','processing','shortlist_ready','report_generated','unlocked','closed')),
  admin_approved            BOOLEAN       NOT NULL DEFAULT false,
  admin_approved_by         UUID          REFERENCES users(id),
  admin_approved_at         TIMESTAMPTZ,
  created_at                TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at                TIMESTAMPTZ   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_rr_employer ON recruitment_requests(employer_id);
CREATE INDEX IF NOT EXISTS idx_rr_status ON recruitment_requests(status);
CREATE INDEX IF NOT EXISTS idx_rr_source_mode ON recruitment_requests(search_source_mode);
CREATE INDEX IF NOT EXISTS idx_rr_internal_token ON recruitment_requests(internal_link_token)
  WHERE internal_link_token IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_rr_auto_close ON recruitment_requests(closing_datetime, portal_status)
  WHERE portal_status = 'open';
CREATE INDEX IF NOT EXISTS idx_rr_billing_mode ON recruitment_requests(billing_mode)
  WHERE billing_mode = 'consolidated';

-- MIGRATION 012: RECRUITMENT REQUEST FILTERS
-- =============================================================================
CREATE TABLE IF NOT EXISTS recruitment_request_filters (
  id                  UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  request_id          UUID        NOT NULL REFERENCES recruitment_requests(id) ON DELETE CASCADE,
  filter_type         VARCHAR(50) NOT NULL,
  filter_value        TEXT        NOT NULL,
  is_sensitive        BOOLEAN     NOT NULL DEFAULT false,
  created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_rrf_request ON recruitment_request_filters(request_id);

CREATE TABLE IF NOT EXISTS compliance_justifications (
  id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  request_id      UUID        NOT NULL REFERENCES recruitment_requests(id),
  filter_type     VARCHAR(50) NOT NULL,
  filter_value    TEXT,
  justification   TEXT        NOT NULL,
  word_count      INTEGER     NOT NULL,
  submitted_by    UUID        NOT NULL REFERENCES users(id),
  submitted_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  reviewed_by     UUID        REFERENCES users(id),
  reviewed_at     TIMESTAMPTZ,
  review_outcome  VARCHAR(20) CHECK (review_outcome IN ('approved','rejected') OR review_outcome IS NULL)
);

-- MIGRATION 013: UPLOADED REQUEST CVs
-- =============================================================================
CREATE TABLE IF NOT EXISTS uploaded_request_cvs (
  id                  UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  request_id          UUID        NOT NULL REFERENCES recruitment_requests(id) ON DELETE CASCADE,
  file_url            VARCHAR(500) NOT NULL,
  file_name           VARCHAR(255) NOT NULL,
  file_size_bytes     INTEGER     NOT NULL,
  mime_type           VARCHAR(100) NOT NULL,
  cv_text_extracted   TEXT,
  candidate_name      VARCHAR(200),
  processed           BOOLEAN     NOT NULL DEFAULT false,
  uploaded_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_ucv_request ON uploaded_request_cvs(request_id);

-- MIGRATION 014: INTERNAL APPLICATIONS (v2.0)
-- =============================================================================
CREATE TABLE IF NOT EXISTS internal_applications (
  id                  UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  request_id          UUID          NOT NULL REFERENCES recruitment_requests(id) ON DELETE CASCADE,
  applicant_name      VARCHAR(200)  NOT NULL,
  applicant_email     VARCHAR(320)  NOT NULL,
  employee_number     VARCHAR(50),
  department          VARCHAR(100),
  motivation          TEXT,
  cv_file_url         VARCHAR(500)  NOT NULL,
  cv_file_name        VARCHAR(255)  NOT NULL,
  cv_file_size_bytes  INTEGER       NOT NULL,
  mime_type           VARCHAR(100)  NOT NULL,
  cv_text_extracted   TEXT,
  ip_address          INET,
  user_agent          TEXT,
  confirmation_sent   BOOLEAN       NOT NULL DEFAULT false,
  processed           BOOLEAN       NOT NULL DEFAULT false,
  processed_at        TIMESTAMPTZ,
  submitted_at        TIMESTAMPTZ   NOT NULL DEFAULT now(),
  CONSTRAINT uq_internal_app_email UNIQUE (request_id, applicant_email)
);
CREATE INDEX IF NOT EXISTS idx_ia_request ON internal_applications(request_id);
CREATE INDEX IF NOT EXISTS idx_ia_unprocessed ON internal_applications(request_id, processed)
  WHERE processed = false;

-- MIGRATION 015: CANDIDATE MATCHES
-- =============================================================================
CREATE TABLE IF NOT EXISTS candidate_matches (
  id                        UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  request_id                UUID          NOT NULL REFERENCES recruitment_requests(id),
  source                    VARCHAR(20)   NOT NULL
                              CHECK (source IN ('pool','uploaded','internal')),
  job_seeker_id             UUID          REFERENCES job_seekers(id),
  uploaded_cv_id            UUID          REFERENCES uploaded_request_cvs(id),
  internal_application_id   UUID          REFERENCES internal_applications(id),
  scoring_model             VARCHAR(20)   NOT NULL DEFAULT 'standard'
                              CHECK (scoring_model IN ('standard','student','graduate_programme')),
  -- Score components
  overall_score             DECIMAL(5,2)  NOT NULL,
  qualification_score       DECIMAL(5,2),
  skills_score              DECIMAL(5,2),
  experience_score          DECIMAL(5,2),
  location_score            DECIMAL(5,2),
  availability_score        DECIMAL(5,2),
  language_score            DECIMAL(5,2),
  certification_score       DECIMAL(5,2),
  recency_score             DECIMAL(5,2),
  module_relevance_score    DECIMAL(5,2),
  academic_achievement_score DECIMAL(5,2),
  internship_score          DECIMAL(5,2),
  institution_score         DECIMAL(5,2),
  -- Output
  rank_position             INTEGER       NOT NULL,
  ai_justification          TEXT,
  pass1_result              BOOLEAN       NOT NULL DEFAULT true,
  pass1_fail_reason         TEXT,
  created_at                TIMESTAMPTZ   NOT NULL DEFAULT now(),
  CONSTRAINT chk_match_source CHECK (
    (source = 'pool'     AND job_seeker_id IS NOT NULL) OR
    (source = 'uploaded' AND uploaded_cv_id IS NOT NULL) OR
    (source = 'internal' AND internal_application_id IS NOT NULL)
  )
);
CREATE INDEX IF NOT EXISTS idx_cm_request ON candidate_matches(request_id);
CREATE INDEX IF NOT EXISTS idx_cm_js ON candidate_matches(job_seeker_id) WHERE job_seeker_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_cm_score ON candidate_matches(request_id, overall_score DESC);
CREATE INDEX IF NOT EXISTS idx_cm_internal ON candidate_matches(internal_application_id)
  WHERE internal_application_id IS NOT NULL;

-- MIGRATION 016: SHORTLISTS
-- =============================================================================
CREATE TABLE IF NOT EXISTS shortlists (
  id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  request_id      UUID        NOT NULL REFERENCES recruitment_requests(id),
  match_id        UUID        NOT NULL REFERENCES candidate_matches(id),
  shortlist_type  VARCHAR(20) NOT NULL CHECK (shortlist_type IN ('shortlist','longlist')),
  rank_position   INTEGER     NOT NULL,
  is_unlocked     BOOLEAN     NOT NULL DEFAULT false,
  unlock_payment_id UUID      REFERENCES payments(id),
  unlocked_at     TIMESTAMPTZ,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_sl_request ON shortlists(request_id);

-- MIGRATION 017: REPORTS
-- =============================================================================
CREATE TABLE IF NOT EXISTS reports (
  id                  UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  request_id          UUID        NOT NULL UNIQUE REFERENCES recruitment_requests(id),
  status              VARCHAR(20) NOT NULL DEFAULT 'pending'
                        CHECK (status IN ('pending','generating','generated','unlocked','failed')),
  pdf_url             VARCHAR(500),
  word_url            VARCHAR(500),
  generated_at        TIMESTAMPTZ,
  unlock_payment_id   UUID        REFERENCES payments(id),
  unlocked_at         TIMESTAMPTZ,
  admin_notes         TEXT,
  admin_reviewed_by   UUID        REFERENCES users(id),
  admin_reviewed_at   TIMESTAMPTZ,
  download_count      INTEGER     NOT NULL DEFAULT 0,
  created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- MIGRATION 018: EMAIL AND NOTIFICATION LOGS
-- =============================================================================
CREATE TABLE IF NOT EXISTS email_logs (
  id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  recipient_id    UUID,
  recipient_email VARCHAR(320) NOT NULL,
  event_type      VARCHAR(100) NOT NULL,
  subject         VARCHAR(500),
  status          VARCHAR(20) NOT NULL DEFAULT 'sent'
                    CHECK (status IN ('sent','failed','bounced')),
  provider_ref    VARCHAR(200),
  sent_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_el_recipient ON email_logs(recipient_id);
CREATE INDEX IF NOT EXISTS idx_el_event ON email_logs(event_type);

CREATE TABLE IF NOT EXISTS notification_logs (
  id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id         UUID        REFERENCES users(id),
  channel         VARCHAR(20) NOT NULL CHECK (channel IN ('email','sms','push','in_app')),
  event_type      VARCHAR(100) NOT NULL,
  entity_type     VARCHAR(50),
  entity_id       UUID,
  sent_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- MIGRATION 019: AUDIT LOGS (IMMUTABLE)
-- =============================================================================
CREATE TABLE IF NOT EXISTS audit_logs (
  id          UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id     UUID        REFERENCES users(id),
  action      VARCHAR(100) NOT NULL,
  entity_type VARCHAR(50)  NOT NULL,
  entity_id   UUID,
  ip_address  INET,
  metadata    JSONB,
  created_at  TIMESTAMPTZ  NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_al_user ON audit_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_al_action ON audit_logs(action);
CREATE INDEX IF NOT EXISTS idx_al_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_al_created ON audit_logs(created_at DESC);

-- Make audit_logs immutable
CREATE RULE no_update_audit AS ON UPDATE TO audit_logs DO INSTEAD NOTHING;
CREATE RULE no_delete_audit AS ON DELETE TO audit_logs DO INSTEAD NOTHING;

-- MIGRATION 020: TRIGGERS (updated_at automation)
-- =============================================================================
CREATE OR REPLACE FUNCTION trigger_set_updated_at()
RETURNS TRIGGER AS $$
BEGIN NEW.updated_at = NOW(); RETURN NEW; END;
$$ LANGUAGE plpgsql;

DO $$ DECLARE tbl TEXT;
BEGIN
  FOREACH tbl IN ARRAY ARRAY['users','job_seekers','employers','candidate_profiles',
    'subscriptions','recruitment_requests','payments','pricing_plans']
  LOOP
    EXECUTE format('DROP TRIGGER IF EXISTS set_updated_at_%I ON %I', tbl, tbl);
    EXECUTE format('CREATE TRIGGER set_updated_at_%I BEFORE UPDATE ON %I
      FOR EACH ROW EXECUTE FUNCTION trigger_set_updated_at()', tbl, tbl);
  END LOOP;
END $$;

-- MIGRATION 021: INTERNAL PORTAL FUNCTIONS (v2.0)
-- =============================================================================
CREATE OR REPLACE FUNCTION close_expired_portals()
RETURNS INTEGER AS $$
DECLARE v_id UUID; v_count INTEGER := 0;
BEGIN
  FOR v_id IN
    SELECT id FROM recruitment_requests
    WHERE portal_status = 'open' AND closing_datetime <= NOW()
    FOR UPDATE SKIP LOCKED
  LOOP
    UPDATE recruitment_requests SET
      portal_status = 'closed', portal_closed_at = NOW(),
      portal_closed_by = 'scheduled', status = 'processing', updated_at = NOW()
    WHERE id = v_id;
    INSERT INTO audit_logs(action,entity_type,entity_id,metadata)
    VALUES('INTERNAL_PORTAL_AUTO_CLOSED','recruitment_request',v_id,
      jsonb_build_object('trigger','scheduled_cron','closed_at',NOW()));
    v_count := v_count + 1;
  END LOOP;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION check_application_cap()
RETURNS TRIGGER AS $$
DECLARE v_max INTEGER; v_count INTEGER; v_status VARCHAR(20);
BEGIN
  SELECT max_applications, portal_status INTO v_max, v_status
  FROM recruitment_requests WHERE id = NEW.request_id;
  IF v_max IS NULL OR v_status != 'open' THEN RETURN NEW; END IF;
  SELECT COUNT(*) INTO v_count FROM internal_applications WHERE request_id = NEW.request_id;
  IF v_count >= v_max THEN
    UPDATE recruitment_requests SET portal_status='closed',portal_closed_at=NOW(),
      portal_closed_by='cap_reached',status='processing',updated_at=NOW()
    WHERE id = NEW.request_id;
    INSERT INTO audit_logs(action,entity_type,entity_id,metadata)
    VALUES('INTERNAL_PORTAL_CAP_REACHED','recruitment_request',NEW.request_id,
      jsonb_build_object('max_applications',v_max,'count',v_count));
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_check_application_cap
  AFTER INSERT ON internal_applications
  FOR EACH ROW EXECUTE FUNCTION check_application_cap();

-- =============================================================================
-- V3.0 — MIGRATIONS 029-042 — STUDENT CSR PROGRAMME
-- =============================================================================

-- MIGRATION 029-030: Already handled above in candidate_profiles and job_seekers

-- MIGRATION 031: STUDENT VERIFICATIONS
-- =============================================================================
CREATE TABLE IF NOT EXISTS student_verifications (
  id                        UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_seeker_id             UUID          NOT NULL UNIQUE REFERENCES job_seekers(id) ON DELETE CASCADE,
  institution               VARCHAR(255)  NOT NULL,
  institution_code          VARCHAR(20),
  student_number            VARCHAR(50)   NOT NULL,
  programme                 VARCHAR(200)  NOT NULL,
  faculty                   VARCHAR(200),
  email_domain              VARCHAR(100),
  verification_status       VARCHAR(20)   NOT NULL DEFAULT 'pending'
                              CHECK (verification_status IN ('pending','verified','rejected','expired')),
  verification_method       VARCHAR(50),
  verification_notes        TEXT,
  verified_at               TIMESTAMPTZ,
  verified_by               UUID          REFERENCES users(id),
  graduation_prompt_sent_at TIMESTAMPTZ,
  grace_period_ends_at      TIMESTAMPTZ,
  graduated_confirmed_at    TIMESTAMPTZ,
  created_at                TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at                TIMESTAMPTZ   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_sv_js_id ON student_verifications(job_seeker_id);
CREATE INDEX IF NOT EXISTS idx_sv_status ON student_verifications(verification_status);
CREATE INDEX IF NOT EXISTS idx_sv_graduation_prompt ON student_verifications(graduation_prompt_sent_at, grace_period_ends_at)
  WHERE verification_status = 'verified' AND graduation_prompt_sent_at IS NULL;

-- MIGRATION 032: INSTITUTION EMAIL DOMAINS
-- =============================================================================
CREATE TABLE IF NOT EXISTS institution_email_domains (
  id                UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  institution       VARCHAR(255) NOT NULL,
  institution_code  VARCHAR(20)  NOT NULL,
  email_domain      VARCHAR(100) NOT NULL UNIQUE,
  is_active         BOOLEAN      NOT NULL DEFAULT true,
  added_at          TIMESTAMPTZ  NOT NULL DEFAULT now()
);

INSERT INTO institution_email_domains (institution, institution_code, email_domain) VALUES
  ('University of Namibia', 'UNAM', 'unam.edu.na'),
  ('Namibia University of Science and Technology', 'NUST', 'nust.na'),
  ('International University of Management', 'IUM', 'ium.edu.na'),
  ('Namibia Business School', 'NBS', 'nbs.edu.na')
ON CONFLICT (email_domain) DO NOTHING;

-- MIGRATION 033: STUDENT LIFECYCLE FUNCTIONS
-- =============================================================================
CREATE OR REPLACE FUNCTION activate_student_subscription(p_js_id UUID)
RETURNS UUID AS $$
DECLARE v_exp DATE; v_grace DATE; v_sub_id UUID;
BEGIN
  SELECT expected_completion_date INTO v_exp FROM candidate_profiles WHERE job_seeker_id = p_js_id;
  v_grace := v_exp + INTERVAL '60 days';
  INSERT INTO subscriptions(job_seeker_id,plan,start_date,end_date,status,payment_id,auto_renew)
  VALUES(p_js_id,'student_free',CURRENT_DATE,v_grace,'active',NULL,false) RETURNING id INTO v_sub_id;
  INSERT INTO audit_logs(action,entity_type,entity_id,metadata)
  VALUES('STUDENT_SUBSCRIPTION_ACTIVATED','subscription',v_sub_id,
    jsonb_build_object('job_seeker_id',p_js_id,'grace_period_ends',v_grace));
  RETURN v_sub_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION trigger_graduation_prompts()
RETURNS INTEGER AS $$
DECLARE v_count INTEGER := 0; v_rec RECORD;
BEGIN
  FOR v_rec IN
    SELECT sv.id, sv.job_seeker_id, cp.expected_completion_date
    FROM student_verifications sv
    JOIN candidate_profiles cp ON cp.job_seeker_id = sv.job_seeker_id
    WHERE sv.verification_status = 'verified'
      AND sv.graduation_prompt_sent_at IS NULL
      AND cp.expected_completion_date <= CURRENT_DATE
  LOOP
    UPDATE student_verifications SET graduation_prompt_sent_at=NOW(), updated_at=NOW() WHERE id=v_rec.id;
    INSERT INTO audit_logs(action,entity_type,entity_id,metadata)
    VALUES('STUDENT_GRADUATION_PROMPT_SENT','job_seeker',v_rec.job_seeker_id,
      jsonb_build_object('expected_completion_date',v_rec.expected_completion_date));
    v_count := v_count + 1;
  END LOOP;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION expire_student_grace_periods()
RETURNS INTEGER AS $$
DECLARE v_count INTEGER := 0; v_rec RECORD;
BEGIN
  FOR v_rec IN
    SELECT sv.job_seeker_id, sv.id AS ver_id, s.id AS sub_id
    FROM student_verifications sv
    JOIN subscriptions s ON s.job_seeker_id = sv.job_seeker_id AND s.plan = 'student_free'
    WHERE sv.verification_status = 'verified'
      AND sv.grace_period_ends_at <= NOW()
      AND sv.graduated_confirmed_at IS NULL
      AND s.status = 'active'
  LOOP
    UPDATE subscriptions SET status='expired', updated_at=NOW() WHERE id=v_rec.sub_id;
    UPDATE student_verifications SET verification_status='expired', updated_at=NOW() WHERE id=v_rec.ver_id;
    INSERT INTO audit_logs(action,entity_type,entity_id,metadata)
    VALUES('STUDENT_GRACE_PERIOD_EXPIRED','job_seeker',v_rec.job_seeker_id,jsonb_build_object('expired_at',NOW()));
    v_count := v_count + 1;
  END LOOP;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION graduate_student_to_job_seeker(p_js_id UUID, p_plan VARCHAR, p_payment_id UUID)
RETURNS VOID AS $$
DECLARE v_months INTEGER; v_end DATE;
BEGIN
  v_months := CASE p_plan WHEN '3m' THEN 3 WHEN '6m' THEN 6 WHEN '12m' THEN 12
    WHEN '18m' THEN 18 WHEN '24m' THEN 24 ELSE 12 END;
  v_end := CURRENT_DATE + (v_months || ' months')::INTERVAL;
  UPDATE job_seekers SET profile_type='job_seeker', updated_at=NOW() WHERE id=p_js_id;
  UPDATE candidate_profiles SET qualification_status='completed',
    expected_completion_date=NULL, updated_at=NOW() WHERE job_seeker_id=p_js_id;
  UPDATE subscriptions SET status='expired', updated_at=NOW()
    WHERE job_seeker_id=p_js_id AND plan='student_free';
  INSERT INTO subscriptions(job_seeker_id,plan,start_date,end_date,status,payment_id,auto_renew)
  VALUES(p_js_id,p_plan,CURRENT_DATE,v_end,'active',p_payment_id,false);
  UPDATE student_verifications SET graduated_confirmed_at=NOW(), updated_at=NOW() WHERE job_seeker_id=p_js_id;
  INSERT INTO audit_logs(action,entity_type,entity_id,metadata)
  VALUES('STUDENT_GRADUATED_UPGRADED','job_seeker',p_js_id,
    jsonb_build_object('new_plan',p_plan,'payment_id',p_payment_id,'end_date',v_end));
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- V3.3 — MIGRATIONS 043-050 — SOCIAL AND COMMUNITY FEATURES
-- =============================================================================

-- MIGRATION 043: SKILL BADGES
-- =============================================================================
CREATE TABLE IF NOT EXISTS candidate_badges (
  id            UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_seeker_id UUID        NOT NULL REFERENCES job_seekers(id) ON DELETE CASCADE,
  badge_type    VARCHAR(50) NOT NULL
                  CHECK (badge_type IN ('profile_complete','top_candidate','verified_professional',
                    'active_talent','graduate_ready','skill_champion','early_adopter','cv_star',
                    'graduate_spotlight','top_referrer')),
  earned_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
  is_displayed  BOOLEAN     NOT NULL DEFAULT true,
  UNIQUE(job_seeker_id, badge_type)
);
CREATE INDEX IF NOT EXISTS idx_cb_js_id ON candidate_badges(job_seeker_id);

-- MIGRATION 044: REFERRAL PROGRAMME
-- =============================================================================
CREATE TABLE IF NOT EXISTS referrals (
  id                UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  referrer_id       UUID        NOT NULL REFERENCES job_seekers(id),
  referred_user_id  UUID        NOT NULL REFERENCES users(id),
  referral_code     VARCHAR(8)  NOT NULL,
  status            VARCHAR(20) NOT NULL DEFAULT 'pending'
                      CHECK (status IN ('pending','confirmed','rewarded','expired')),
  reward_applied_at TIMESTAMPTZ,
  created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(referred_user_id)
);
CREATE INDEX IF NOT EXISTS idx_ref_referrer ON referrals(referrer_id);
CREATE INDEX IF NOT EXISTS idx_ref_code ON referrals(referral_code);
CREATE INDEX IF NOT EXISTS idx_ref_status ON referrals(status);

-- Award referral reward function
CREATE OR REPLACE FUNCTION apply_referral_reward(p_referred_user_id UUID, p_referral_code VARCHAR)
RETURNS VOID AS $$
DECLARE v_referrer_id UUID; v_sub_id UUID; v_ref_id UUID;
BEGIN
  SELECT js.id INTO v_referrer_id FROM job_seekers js WHERE js.referral_code = p_referral_code;
  IF v_referrer_id IS NULL THEN RETURN; END IF;
  SELECT id INTO v_ref_id FROM referrals WHERE referred_user_id = p_referred_user_id AND referral_code = p_referral_code;
  IF v_ref_id IS NULL THEN RETURN; END IF;
  SELECT id INTO v_sub_id FROM subscriptions WHERE job_seeker_id = v_referrer_id AND status = 'active' LIMIT 1;
  IF v_sub_id IS NOT NULL THEN
    UPDATE subscriptions SET end_date = end_date + INTERVAL '30 days', updated_at = NOW() WHERE id = v_sub_id;
    UPDATE referrals SET status = 'rewarded', reward_applied_at = NOW() WHERE id = v_ref_id;
    INSERT INTO audit_logs(action,entity_type,entity_id,metadata)
    VALUES('REFERRAL_REWARD_APPLIED','job_seeker',v_referrer_id,
      jsonb_build_object('referred_user_id',p_referred_user_id,'months_added',1));
  END IF;
END;
$$ LANGUAGE plpgsql;

-- MIGRATION 045: CAREER INSIGHTS BLOG
-- =============================================================================
CREATE TABLE IF NOT EXISTS insights (
  id                UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  title             VARCHAR(300) NOT NULL,
  slug              VARCHAR(300) NOT NULL UNIQUE,
  body              TEXT        NOT NULL,
  category          VARCHAR(50) NOT NULL
                      CHECK (category IN ('market_data','career_advice','employer_insights',
                        'graduate_content','industry_focus','platform_education')),
  featured_image_url VARCHAR(500),
  meta_description  VARCHAR(300),
  author            VARCHAR(100) NOT NULL DEFAULT 'Illumin360 Team',
  is_published      BOOLEAN     NOT NULL DEFAULT false,
  published_at      TIMESTAMPTZ,
  view_count        INTEGER     NOT NULL DEFAULT 0,
  created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_ins_slug ON insights(slug);
CREATE INDEX IF NOT EXISTS idx_ins_published ON insights(is_published, published_at DESC);
CREATE INDEX IF NOT EXISTS idx_ins_category ON insights(category, is_published);

-- MIGRATION 046: GRADUATE SPOTLIGHT
-- =============================================================================
CREATE TABLE IF NOT EXISTS spotlight_features (
  id                  UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  student_id          UUID        NOT NULL REFERENCES job_seekers(id),
  spotlight_month     INTEGER     NOT NULL CHECK (spotlight_month BETWEEN 1 AND 12),
  spotlight_year      INTEGER     NOT NULL,
  quote               TEXT,
  photo_url           VARCHAR(500),
  consent_confirmed   BOOLEAN     NOT NULL DEFAULT false,
  published_at        TIMESTAMPTZ,
  created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(spotlight_year, spotlight_month, student_id)
);
CREATE INDEX IF NOT EXISTS idx_sf_published ON spotlight_features(published_at DESC NULLS LAST);

-- MIGRATION 047: TALENT DEMAND FEED CACHE
-- =============================================================================
CREATE TABLE IF NOT EXISTS demand_feed_cache (
  id            UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  signal_type   VARCHAR(50) NOT NULL
                  CHECK (signal_type IN ('role_demand','skill_demand','qualification_demand','city_demand')),
  label         VARCHAR(200) NOT NULL,
  count         INTEGER     NOT NULL,
  city          VARCHAR(100),
  week_starting DATE        NOT NULL,
  is_suppressed BOOLEAN     NOT NULL DEFAULT false,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(signal_type, label, city, week_starting)
);
CREATE INDEX IF NOT EXISTS idx_dfc_week ON demand_feed_cache(week_starting DESC);
CREATE INDEX IF NOT EXISTS idx_dfc_active ON demand_feed_cache(week_starting, is_suppressed)
  WHERE is_suppressed = false;

-- MIGRATION 048: GENERATE REFERRAL CODES FOR EXISTING JOB SEEKERS
-- =============================================================================
UPDATE job_seekers
SET referral_code = UPPER(SUBSTRING(MD5(RANDOM()::TEXT) FROM 1 FOR 8))
WHERE referral_code IS NULL;

-- MIGRATION 049: VIEWS
-- =============================================================================
DROP VIEW IF EXISTS v_active_candidates CASCADE;
CREATE VIEW v_active_candidates AS
SELECT
  js.id AS job_seeker_id, js.first_name, js.last_name, js.city,
  js.availability_status, js.willing_to_relocate, js.salary_expectation,
  js.notice_period, js.employment_status, js.profile_type,
  js.username, js.public_profile_enabled, js.public_headline,
  cp.highest_qualification, cp.qualification_field, cp.nqf_level,
  cp.years_experience, cp.industry_experience, cp.certifications,
  cp.professional_memberships, cp.tools_systems, cp.profile_complete_pct,
  cp.qualification_status, cp.expected_completion_date, cp.modules_completed,
  cp.academic_achievements, cp.gpa_range, cp.internship_experience,
  cp.open_to_graduate_programmes,
  s.plan AS subscription_plan, s.end_date AS subscription_end_date, s.status AS subscription_status,
  cd.file_url AS active_cv_url, cd.cv_text_extracted, cd.uploaded_at AS cv_uploaded_at
FROM job_seekers js
JOIN subscriptions s ON s.job_seeker_id = js.id AND s.status IN ('active','expiring')
JOIN candidate_profiles cp ON cp.job_seeker_id = js.id
LEFT JOIN candidate_documents cd ON cd.job_seeker_id = js.id AND cd.is_active = true AND cd.document_type = 'cv'
WHERE js.id IN (SELECT u.id FROM users u JOIN job_seekers jsi ON jsi.user_id = u.id WHERE u.is_active = true);

CREATE VIEW v_standard_candidates AS SELECT * FROM v_active_candidates
  WHERE profile_type = 'job_seeker' AND qualification_status = 'completed';

CREATE VIEW v_student_candidates AS SELECT * FROM v_active_candidates
  WHERE profile_type = 'student' AND qualification_status = 'in_progress';

CREATE VIEW v_student_stats AS
SELECT sv.institution_code, sv.institution,
  COUNT(sv.id) AS total_registered,
  COUNT(sv.id) FILTER (WHERE sv.verification_status = 'verified') AS verified,
  COUNT(sv.id) FILTER (WHERE sv.verification_status = 'pending') AS pending_verification,
  COUNT(sv.id) FILTER (WHERE sv.graduation_prompt_sent_at IS NOT NULL) AS graduation_prompts_sent,
  COUNT(sv.id) FILTER (WHERE sv.graduated_confirmed_at IS NOT NULL) AS converted_to_paid,
  ROUND(COUNT(sv.id) FILTER (WHERE sv.graduated_confirmed_at IS NOT NULL)::DECIMAL /
    NULLIF(COUNT(sv.id) FILTER (WHERE sv.graduation_prompt_sent_at IS NOT NULL),0) * 100, 1
  ) AS graduation_conversion_pct
FROM student_verifications sv
GROUP BY sv.institution_code, sv.institution ORDER BY total_registered DESC;

-- MIGRATION 050: ROW LEVEL SECURITY
-- =============================================================================
ALTER TABLE internal_applications ENABLE ROW LEVEL SECURITY;
ALTER TABLE student_verifications ENABLE ROW LEVEL SECURITY;
ALTER TABLE institution_email_domains ENABLE ROW LEVEL SECURITY;
ALTER TABLE candidate_badges ENABLE ROW LEVEL SECURITY;
ALTER TABLE referrals ENABLE ROW LEVEL SECURITY;

-- =============================================================================
-- COMMIT
-- =============================================================================

-- =============================================================================
-- POST-MIGRATION VERIFICATION QUERIES (run manually to confirm)
-- =============================================================================
-- SELECT COUNT(*) FROM users;
-- SELECT COUNT(*) FROM job_seekers;
-- SELECT COUNT(*) FROM employers;
-- SELECT COUNT(*) FROM pricing_plans;
-- SELECT plan_type, name, base_price FROM pricing_plans ORDER BY base_price;
-- SELECT institution, email_domain FROM institution_email_domains;
-- SELECT proname FROM pg_proc WHERE proname IN (
--   'trigger_set_updated_at','close_expired_portals','check_application_cap',
--   'activate_student_subscription','trigger_graduation_prompts',
--   'expire_student_grace_periods','graduate_student_to_job_seeker',
--   'apply_referral_reward'
-- );
-- SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name;
-- SELECT viewname FROM pg_views WHERE schemaname = 'public';
-- =============================================================================
-- END OF COMPLETE MIGRATION FILE
-- Illumin Investments CC | Trading as Illumin | Illumin360 Platform
-- CC 2016/08234 | VAT 07851437-015 | www.illumininvestments.com
-- =============================================================================


-- === V3.4 MIGRATIONS ===

-- =============================================================================
-- ILLUMIN360 TALENT MATCH & RECRUITMENT PLATFORM
-- Database Migration — Version 3.4
-- Features: AI Engine Evolution, Video Integration, Asset Management,
--           PWA (no DB changes), Social Badges
-- Illumin Investments CC (CC 2016/08234 | VAT 07851437-015)
-- Trading as Illumin | Product: Illumin360
-- www.illumininvestments.com | projects@illumininvestments.com
-- =============================================================================
-- Run after: illumin360_migrations_complete.sql (v1.0 through v3.3)
-- All changes are ADDITIVE — no existing tables, columns, or constraints removed
-- =============================================================================


-- =============================================================================
-- MIGRATION 051: ADAPTIVE EMPLOYER-DRIVEN WEIGHTING
-- Section 22.1 of v3.4 specification
-- =============================================================================

-- Add custom_weights to recruitment_requests
ALTER TABLE recruitment_requests
  ADD COLUMN IF NOT EXISTS custom_weights JSONB,
  -- Stores employer-adjusted weight values as JSON object
  -- Example: {"qualification":30,"skills":30,"experience":15,"location":10,"availability":5,"language":5,"certifications":3,"recency":2}
  -- NULL means standard weights apply — backward compatible with all existing requests
  ADD COLUMN IF NOT EXISTS weights_locked BOOLEAN NOT NULL DEFAULT false;
  -- Once a request is submitted, weights cannot be changed — locks on submission

-- Add weights_used to candidate_matches (records exactly what was used to generate each match)
ALTER TABLE candidate_matches
  ADD COLUMN IF NOT EXISTS weights_used JSONB;
  -- Populated at match generation time — immutable record for audit and report methodology

-- Constraint: custom_weights values must sum to 100 (enforced at application layer)
-- Min/max per factor also enforced at application layer (not SQL to allow flexibility)

-- Index for analytics queries on custom vs standard weighting usage
CREATE INDEX IF NOT EXISTS idx_rr_custom_weights
  ON recruitment_requests ((custom_weights IS NOT NULL));

COMMENT ON COLUMN recruitment_requests.custom_weights IS
  'Employer-adjusted scoring weights. NULL = standard weights apply. Values must sum to 100. Min/max bounds per factor enforced at API layer.';

COMMENT ON COLUMN candidate_matches.weights_used IS
  'Immutable record of the exact weights used to generate this match. Either standard weights or employer custom weights. Stored for audit trail and report methodology disclosure.';

-- =============================================================================
-- MIGRATION 052: SKILL-GAP AND GROWTH NARRATIVE LOGIC
-- Section 22.2 of v3.4 specification
-- =============================================================================

-- Add gap analysis columns to candidate_matches
ALTER TABLE candidate_matches
  ADD COLUMN IF NOT EXISTS gap_analysis JSONB,
  -- Stores identified gaps and compensating strengths
  -- Example structure:
  -- {
  --   "trigger_score": 78.5,
  --   "gaps": [
  --     {"factor": "qualification", "detail": "Candidate holds Diploma (NQF 6) vs required Degree (NQF 7)"},
  --     {"factor": "certification", "detail": "OHS certification not found in profile"}
  --   ],
  --   "compensating_strengths": [
  --     {"factor": "skills", "detail": "Strong skills alignment (94%) offsets qualification gap"},
  --     {"factor": "experience", "detail": "8 years experience exceeds minimum requirement by 3 years"}
  --   ]
  -- }
  ADD COLUMN IF NOT EXISTS gap_analysis_displayed BOOLEAN NOT NULL DEFAULT false;
  -- Whether gap analysis was included in the report output for this match

-- Index for admin analytics on gap analysis patterns
CREATE INDEX IF NOT EXISTS idx_cm_gap_analysis
  ON candidate_matches (request_id, gap_analysis_displayed)
  WHERE gap_analysis IS NOT NULL;

COMMENT ON COLUMN candidate_matches.gap_analysis IS
  'Gap analysis content for candidates scoring 70-85%. NULL for candidates outside this band or those not shortlisted. Contains structured arrays of identified gaps and compensating strengths.';

-- =============================================================================
-- MIGRATION 053: REINFORCEMENT LEARNING FEEDBACK TABLE
-- Section 22.3 of v3.4 specification
-- =============================================================================

CREATE TABLE IF NOT EXISTS match_feedback (
  id                    UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  request_id            UUID          NOT NULL REFERENCES recruitment_requests(id),
  match_id              UUID          NOT NULL REFERENCES candidate_matches(id),
  employer_id           UUID          NOT NULL REFERENCES employers(id),
  -- Ratings (1-5 stars)
  accuracy_rating       INTEGER       NOT NULL CHECK (accuracy_rating BETWEEN 1 AND 5),
  justification_rating  INTEGER       NOT NULL CHECK (justification_rating BETWEEN 1 AND 5),
  -- Optional free-text
  employer_notes        TEXT,
  -- Context stored at feedback time for model refinement correlation
  scoring_model         VARCHAR(20)   NOT NULL
                          CHECK (scoring_model IN ('standard','student','graduate_programme')),
  weights_used          JSONB         NOT NULL,
  -- Metadata
  industry              VARCHAR(100),  -- Copied from request at feedback time for analytics
  role_category         VARCHAR(100),  -- Derived from job_title for grouping
  feedback_source       VARCHAR(20)   NOT NULL DEFAULT 'email_link'
                          CHECK (feedback_source IN ('email_link','dashboard_prompt','manual')),
  created_at            TIMESTAMPTZ   NOT NULL DEFAULT now(),
  -- One feedback record per employer per match
  UNIQUE(employer_id, match_id)
);

CREATE INDEX IF NOT EXISTS idx_mf_request ON match_feedback(request_id);
CREATE INDEX IF NOT EXISTS idx_mf_employer ON match_feedback(employer_id);
CREATE INDEX IF NOT EXISTS idx_mf_scoring_model ON match_feedback(scoring_model);
CREATE INDEX IF NOT EXISTS idx_mf_accuracy ON match_feedback(accuracy_rating, scoring_model);
CREATE INDEX IF NOT EXISTS idx_mf_created ON match_feedback(created_at DESC);

-- Admin analytics view for feedback patterns
CREATE VIEW IF NOT EXISTS v_feedback_summary AS
SELECT
  mf.scoring_model,
  mf.industry,
  COUNT(mf.id)                                              AS total_feedback_records,
  ROUND(AVG(mf.accuracy_rating)::DECIMAL, 2)               AS avg_accuracy_rating,
  ROUND(AVG(mf.justification_rating)::DECIMAL, 2)          AS avg_justification_rating,
  COUNT(mf.id) FILTER (WHERE mf.accuracy_rating >= 4)      AS high_accuracy_count,
  COUNT(mf.id) FILTER (WHERE mf.accuracy_rating <= 2)      AS low_accuracy_count,
  MIN(mf.created_at)                                        AS earliest_feedback,
  MAX(mf.created_at)                                        AS latest_feedback
FROM match_feedback mf
GROUP BY mf.scoring_model, mf.industry
ORDER BY total_feedback_records DESC;

-- Notification trigger tracking (prevents duplicate feedback requests)
ALTER TABLE reports
  ADD COLUMN IF NOT EXISTS feedback_requested_at TIMESTAMPTZ,
  -- When the feedback request email was sent (14 days after unlock)
  ADD COLUMN IF NOT EXISTS feedback_received BOOLEAN NOT NULL DEFAULT false;
  -- Whether at least one feedback record exists for this report

-- =============================================================================
-- MIGRATION 054: VIDEO INTEGRATION — CANDIDATE ELEVATOR PITCH
-- Section 23.1 of v3.4 specification
-- =============================================================================

CREATE TABLE IF NOT EXISTS candidate_videos (
  id                        UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  job_seeker_id             UUID          NOT NULL UNIQUE REFERENCES job_seekers(id) ON DELETE CASCADE,
  -- File details
  file_url                  VARCHAR(500)  NOT NULL,
  file_name                 VARCHAR(255)  NOT NULL,
  file_size_bytes           BIGINT        NOT NULL CHECK (file_size_bytes <= 157286400), -- 150MB limit
  mime_type                 VARCHAR(100)  NOT NULL
                              CHECK (mime_type IN ('video/mp4','video/quicktime','video/webm')),
  duration_seconds          INTEGER       NOT NULL CHECK (duration_seconds <= 60),
  resolution                VARCHAR(20),  -- e.g. '1920x1080', '1280x720'
  -- Transcription
  transcription_status      VARCHAR(20)   NOT NULL DEFAULT 'pending'
                              CHECK (transcription_status IN ('pending','processing','completed','failed','flagged')),
  transcription_text        TEXT,         -- Full transcript of the video audio
  transcription_keywords    JSONB,        -- Extracted keywords as array with confidence scores
  transcription_provider    VARCHAR(50),  -- Which speech-to-text service processed this video
  transcription_completed_at TIMESTAMPTZ,
  transcription_error       TEXT,         -- Error message if transcription failed
  -- Visibility and status
  visibility                VARCHAR(20)   NOT NULL DEFAULT 'public'
                              CHECK (visibility IN ('public','private')),
  is_active                 BOOLEAN       NOT NULL DEFAULT true,
  content_flagged           BOOLEAN       NOT NULL DEFAULT false,
  content_flag_reason       TEXT,
  content_reviewed_by       UUID          REFERENCES users(id),
  content_reviewed_at       TIMESTAMPTZ,
  -- Timestamps
  uploaded_at               TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at                TIMESTAMPTZ   NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_cv_js_id ON candidate_videos(job_seeker_id);
CREATE INDEX IF NOT EXISTS idx_cv_transcription_status ON candidate_videos(transcription_status)
  WHERE transcription_status IN ('pending','processing');
CREATE INDEX IF NOT EXISTS idx_cv_visibility ON candidate_videos(visibility, is_active)
  WHERE is_active = true;
CREATE INDEX IF NOT EXISTS idx_cv_flagged ON candidate_videos(content_flagged)
  WHERE content_flagged = true;

-- Add video quick-reference fields to candidate_profiles
ALTER TABLE candidate_profiles
  ADD COLUMN IF NOT EXISTS has_video BOOLEAN NOT NULL DEFAULT false,
  -- Quick lookup flag — avoids joining candidate_videos for every profile query
  ADD COLUMN IF NOT EXISTS video_keyword_weight DECIMAL(4,2) NOT NULL DEFAULT 0.30;
  -- Weight applied to transcription keywords vs standard CV keywords
  -- Admin-configurable globally. Default 30% to prevent over-reliance on transcription.

-- Trigger to keep has_video in sync
CREATE OR REPLACE FUNCTION sync_has_video()
RETURNS TRIGGER AS $$
BEGIN
  IF TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND NEW.is_active = true) THEN
    UPDATE candidate_profiles SET has_video = true
    WHERE job_seeker_id = NEW.job_seeker_id;
  ELSIF TG_OP = 'DELETE' OR (TG_OP = 'UPDATE' AND NEW.is_active = false) THEN
    UPDATE candidate_profiles SET has_video = false
    WHERE job_seeker_id = COALESCE(NEW.job_seeker_id, OLD.job_seeker_id)
      AND NOT EXISTS (
        SELECT 1 FROM candidate_videos
        WHERE job_seeker_id = COALESCE(NEW.job_seeker_id, OLD.job_seeker_id)
          AND is_active = true
      );
  END IF;
  RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_sync_has_video
  AFTER INSERT OR UPDATE OR DELETE ON candidate_videos
  FOR EACH ROW EXECUTE FUNCTION sync_has_video();

-- =============================================================================
-- MIGRATION 055: ASSET MANAGEMENT — EMPLOYER AND UNIVERSITY LOGOS,
--                JOB SEEKER PHOTOS
-- Section 24 of v3.4 specification
-- =============================================================================

-- Employer logo (logo_url already exists — add metadata columns)
ALTER TABLE employers
  ADD COLUMN IF NOT EXISTS logo_uploaded_at  TIMESTAMPTZ,
  ADD COLUMN IF NOT EXISTS logo_file_size_bytes INTEGER;

-- University / institution logos
ALTER TABLE institution_email_domains
  ADD COLUMN IF NOT EXISTS logo_url         VARCHAR(500),
  ADD COLUMN IF NOT EXISTS logo_uploaded_at TIMESTAMPTZ;

-- Job seeker professional headshot (blind screening — hidden until candidate unlock)
ALTER TABLE job_seekers
  ADD COLUMN IF NOT EXISTS photo_url             VARCHAR(500),
  ADD COLUMN IF NOT EXISTS photo_uploaded_at     TIMESTAMPTZ,
  ADD COLUMN IF NOT EXISTS photo_file_size_bytes INTEGER;

-- Index for candidates with photos (used when building unlocked profile response)
CREATE INDEX IF NOT EXISTS idx_js_has_photo
  ON job_seekers (photo_url)
  WHERE photo_url IS NOT NULL;

-- =============================================================================
-- MIGRATION 056: SOCIAL BADGES — VERIFIED STUDENT AND COMPLIANT RECRUITER
-- Section 26 of v3.4 specification
-- =============================================================================

-- Add verified_student to existing candidate badge types
-- The badge_type CHECK constraint already includes this from v3.3 migrations
-- Nothing to change in candidate_badges — already handled

-- Employer badges — new table
CREATE TABLE IF NOT EXISTS employer_badges (
  id            UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  employer_id   UUID        NOT NULL REFERENCES employers(id) ON DELETE CASCADE,
  badge_type    VARCHAR(50) NOT NULL
                  CHECK (badge_type IN ('compliant_recruiter')),
  earned_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
  is_displayed  BOOLEAN     NOT NULL DEFAULT true,
  revoked_at    TIMESTAMPTZ,
  revoked_by    UUID        REFERENCES users(id),
  revoked_reason TEXT,
  UNIQUE(employer_id, badge_type)
);

CREATE INDEX IF NOT EXISTS idx_eb_employer ON employer_badges(employer_id);
CREATE INDEX IF NOT EXISTS idx_eb_type ON employer_badges(badge_type, is_displayed);

-- Function to assess and award Compliant Recruiter badge
-- Called by monthly cron job
CREATE OR REPLACE FUNCTION assess_compliant_recruiter_badges()
RETURNS INTEGER AS $$
DECLARE
  v_count INTEGER := 0;
  v_rec   RECORD;
BEGIN
  -- Find employers who qualify:
  -- 10+ requests submitted with declaration_confirmed = true
  -- Zero compliance violations (no sensitive filters rejected by admin)
  FOR v_rec IN
    SELECT e.id AS employer_id
    FROM employers e
    WHERE (
      SELECT COUNT(*) FROM recruitment_requests rr
      WHERE rr.employer_id = e.id AND rr.declaration_confirmed = true
    ) >= 10
    AND NOT EXISTS (
      SELECT 1 FROM compliance_justifications cj
      JOIN recruitment_requests rr ON rr.id = cj.request_id
      WHERE rr.employer_id = e.id
        AND cj.review_outcome = 'rejected'
    )
    AND NOT EXISTS (
      SELECT 1 FROM employer_badges eb
      WHERE eb.employer_id = e.id
        AND eb.badge_type = 'compliant_recruiter'
        AND eb.revoked_at IS NOT NULL
        AND eb.revoked_at > NOW() - INTERVAL '90 days'
    )
  LOOP
    INSERT INTO employer_badges (employer_id, badge_type)
    VALUES (v_rec.employer_id, 'compliant_recruiter')
    ON CONFLICT (employer_id, badge_type) DO NOTHING;

    IF FOUND THEN
      INSERT INTO audit_logs(action, entity_type, entity_id, metadata)
      VALUES('COMPLIANT_RECRUITER_BADGE_AWARDED', 'employer', v_rec.employer_id,
        jsonb_build_object('assessed_at', NOW()));
      v_count := v_count + 1;
    END IF;
  END LOOP;

  RETURN v_count;
END;
$$ LANGUAGE plpgsql;

-- Function to award Verified Student badge when verification is confirmed
CREATE OR REPLACE FUNCTION award_verified_student_badge()
RETURNS TRIGGER AS $$
BEGIN
  IF NEW.verification_status = 'verified' AND
     (OLD.verification_status IS DISTINCT FROM 'verified') THEN
    INSERT INTO candidate_badges (job_seeker_id, badge_type)
    VALUES (NEW.job_seeker_id, 'verified_student')
    ON CONFLICT (job_seeker_id, badge_type) DO UPDATE
      SET is_displayed = true, earned_at = NOW();
  ELSIF NEW.verification_status IN ('rejected','expired') AND
        OLD.verification_status = 'verified' THEN
    -- Remove badge if verification is revoked
    UPDATE candidate_badges
    SET is_displayed = false
    WHERE job_seeker_id = NEW.job_seeker_id
      AND badge_type = 'verified_student';
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_award_verified_student_badge
  AFTER UPDATE OF verification_status ON student_verifications
  FOR EACH ROW EXECUTE FUNCTION award_verified_student_badge();

-- =============================================================================
-- MIGRATION 057: UPDATED CANDIDATE_BADGES ENUM
-- Add video_pitch badge type for Phase 7
-- =============================================================================

-- Drop and recreate the badge_type constraint to include video_pitch
ALTER TABLE candidate_badges
  DROP CONSTRAINT IF EXISTS candidate_badges_badge_type_check;

ALTER TABLE candidate_badges
  ADD CONSTRAINT candidate_badges_badge_type_check
  CHECK (badge_type IN (
    'profile_complete',
    'top_candidate',
    'verified_professional',
    'active_talent',
    'graduate_ready',
    'skill_champion',
    'early_adopter',
    'cv_star',
    'graduate_spotlight',
    'top_referrer',
    'verified_student',   -- v3.4 — awarded on student verification
    'video_pitcher'        -- v3.4 Phase 7 — awarded when video pitch uploaded and transcribed
  ));

-- =============================================================================
-- MIGRATION 058: NOTIFICATION TRIGGERS FOR NEW FEATURES
-- =============================================================================

-- New notification event types introduced in v3.4:
-- match_feedback_requested      — 14 days after report unlock, employer invited to rate
-- video_transcription_complete  — candidate notified when video is live and transcribed
-- video_transcription_failed    — candidate notified if transcription fails
-- video_content_flagged         — admin notified when video is flagged by content filter
-- compliant_recruiter_earned    — employer notified when badge is awarded
-- compliant_recruiter_revoked   — employer notified if badge is revoked

-- =============================================================================
-- MIGRATION 059: UPDATED VIEWS
-- =============================================================================

-- Update v_active_candidates to include video and photo fields
DROP VIEW IF EXISTS v_active_candidates CASCADE;

CREATE VIEW v_active_candidates AS
SELECT
  js.id AS job_seeker_id,
  js.first_name, js.last_name, js.city,
  js.availability_status, js.willing_to_relocate, js.salary_expectation,
  js.notice_period, js.employment_status, js.profile_type,
  js.username, js.public_profile_enabled, js.public_headline,
  js.photo_url,        -- v3.4 — included but only surfaced after candidate unlock
  cp.highest_qualification, cp.qualification_field, cp.nqf_level,
  cp.years_experience, cp.industry_experience, cp.certifications,
  cp.professional_memberships, cp.tools_systems, cp.profile_complete_pct,
  cp.qualification_status, cp.expected_completion_date, cp.modules_completed,
  cp.academic_achievements, cp.gpa_range, cp.internship_experience,
  cp.open_to_graduate_programmes,
  cp.has_video,        -- v3.4 — quick flag for video pitch availability
  s.plan AS subscription_plan,
  s.end_date AS subscription_end_date,
  s.status AS subscription_status,
  cd.file_url AS active_cv_url,
  cd.cv_text_extracted,
  cd.uploaded_at AS cv_uploaded_at
FROM job_seekers js
JOIN subscriptions s ON s.job_seeker_id = js.id AND s.status IN ('active','expiring')
JOIN candidate_profiles cp ON cp.job_seeker_id = js.id
LEFT JOIN candidate_documents cd ON cd.job_seeker_id = js.id
  AND cd.is_active = true AND cd.document_type = 'cv'
WHERE js.id IN (
  SELECT u.id FROM users u
  JOIN job_seekers jsi ON jsi.user_id = u.id
  WHERE u.is_active = true
);

CREATE VIEW v_standard_candidates AS
  SELECT * FROM v_active_candidates
  WHERE profile_type = 'job_seeker' AND qualification_status = 'completed';

CREATE VIEW v_student_candidates AS
  SELECT * FROM v_active_candidates
  WHERE profile_type = 'student' AND qualification_status = 'in_progress';

CREATE VIEW v_student_stats AS
SELECT sv.institution_code, sv.institution,
  COUNT(sv.id) AS total_registered,
  COUNT(sv.id) FILTER (WHERE sv.verification_status = 'verified') AS verified,
  COUNT(sv.id) FILTER (WHERE sv.verification_status = 'pending') AS pending_verification,
  COUNT(sv.id) FILTER (WHERE sv.graduation_prompt_sent_at IS NOT NULL) AS graduation_prompts_sent,
  COUNT(sv.id) FILTER (WHERE sv.graduated_confirmed_at IS NOT NULL) AS converted_to_paid,
  ROUND(COUNT(sv.id) FILTER (WHERE sv.graduated_confirmed_at IS NOT NULL)::DECIMAL /
    NULLIF(COUNT(sv.id) FILTER (WHERE sv.graduation_prompt_sent_at IS NOT NULL),0)*100,1
  ) AS graduation_conversion_pct
FROM student_verifications sv
GROUP BY sv.institution_code, sv.institution
ORDER BY total_registered DESC;

-- =============================================================================
-- MIGRATION 060: ROW LEVEL SECURITY
-- =============================================================================

ALTER TABLE match_feedback ENABLE ROW LEVEL SECURITY;
ALTER TABLE candidate_videos ENABLE ROW LEVEL SECURITY;
ALTER TABLE employer_badges ENABLE ROW LEVEL SECURITY;

GRANT SELECT, INSERT ON match_feedback TO app_employer;
GRANT SELECT, INSERT, UPDATE ON candidate_videos TO app_job_seeker;
GRANT SELECT ON employer_badges TO app_employer;
GRANT SELECT ON employer_badges TO app_job_seeker;
GRANT ALL ON match_feedback TO app_admin;
GRANT ALL ON candidate_videos TO app_admin;
GRANT ALL ON employer_badges TO app_admin;

-- =============================================================================
-- COMMIT
-- =============================================================================


-- =============================================================================
-- POST-MIGRATION VERIFICATION QUERIES
-- =============================================================================
-- 1. Confirm new columns on recruitment_requests
-- SELECT column_name FROM information_schema.columns
-- WHERE table_name='recruitment_requests'
--   AND column_name IN ('custom_weights','weights_locked');

-- 2. Confirm new columns on candidate_matches
-- SELECT column_name FROM information_schema.columns
-- WHERE table_name='candidate_matches'
--   AND column_name IN ('weights_used','gap_analysis','gap_analysis_displayed');

-- 3. Confirm new tables
-- SELECT table_name FROM information_schema.tables
-- WHERE table_schema='public'
--   AND table_name IN ('match_feedback','candidate_videos','employer_badges');

-- 4. Confirm new functions
-- SELECT proname FROM pg_proc
-- WHERE proname IN ('assess_compliant_recruiter_badges',
--   'award_verified_student_badge','sync_has_video');

-- 5. Confirm triggers
-- SELECT trigger_name, event_object_table FROM information_schema.triggers
-- WHERE event_object_table IN ('candidate_videos','student_verifications');

-- 6. Confirm updated badge constraint
-- SELECT conname, pg_get_constraintdef(oid) FROM pg_constraint
-- WHERE conname='candidate_badges_badge_type_check';

-- =============================================================================
-- END OF MIGRATION v3.4
-- illumin360_migrations_v3.4.sql
-- Run sequence: complete.sql (v1-v3.3) → this file (v3.4)
-- Illumin Investments CC | Trading as Illumin | Illumin360 Platform
-- CC 2016/08234 | VAT 07851437-015 | www.illumininvestments.com
-- =============================================================================


COMMIT;

-- =============================================================================
-- END OF MASTER MIGRATION FILE
-- All versions v1.0 through v3.4 consolidated in single transaction
-- Illumin Investments CC | Trading as Illumin | Illumin360 Platform
-- CC 2016/08234 | VAT 07851437-015 | www.illumininvestments.com
-- =============================================================================

-- =============================================================================
-- V3.5 MIGRATIONS — AI SERVICES AND PLATFORM ASSISTANT
-- =============================================================================

BEGIN;

-- MIGRATION 061: AI ASSISTANT CONVERSATIONS TABLE
-- Section 29 of v3.5 specification
-- =============================================================================
CREATE TABLE IF NOT EXISTS assistant_conversations (
  id                  UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id             UUID          REFERENCES users(id),
  -- NULL for public (non-logged-in) users
  user_type           VARCHAR(20)   NOT NULL
                        CHECK (user_type IN ('job_seeker','employer','student','public','admin')),
  session_id          VARCHAR(64)   NOT NULL UNIQUE,
  messages            JSONB         NOT NULL DEFAULT '[]'::JSONB,
  -- Array of {role: "user"|"assistant", content: "...", timestamp: "..."}
  started_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
  last_message_at     TIMESTAMPTZ   NOT NULL DEFAULT now(),
  message_count       INTEGER       NOT NULL DEFAULT 0,
  escalated_to_human  BOOLEAN       NOT NULL DEFAULT false,
  escalated_at        TIMESTAMPTZ,
  created_at          TIMESTAMPTZ   NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_ac_user ON assistant_conversations(user_id)
  WHERE user_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_ac_session ON assistant_conversations(session_id);
CREATE INDEX IF NOT EXISTS idx_ac_type ON assistant_conversations(user_type);
CREATE INDEX IF NOT EXISTS idx_ac_escalated ON assistant_conversations(escalated_to_human)
  WHERE escalated_to_human = true;

-- MIGRATION 062: AI PROCESSING LOG
-- Tracks all Claude Sonnet 4.6 and Google Cloud Vision API calls
-- for cost monitoring, debugging, and audit
-- =============================================================================
CREATE TABLE IF NOT EXISTS ai_processing_log (
  id                  UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
  service             VARCHAR(30)   NOT NULL
                        CHECK (service IN ('claude_cv_analysis','claude_justification','claude_assistant','google_vision_ocr')),
  entity_type         VARCHAR(30)   NOT NULL,
  -- 'candidate_document', 'candidate_match', 'assistant_conversation'
  entity_id           UUID          NOT NULL,
  status              VARCHAR(20)   NOT NULL DEFAULT 'pending'
                        CHECK (status IN ('pending','processing','completed','failed','fallback')),
  input_tokens        INTEGER,
  output_tokens       INTEGER,
  cost_usd            DECIMAL(10,6),
  -- Stored for cost reporting — calculated from token counts
  model_used          VARCHAR(50),
  -- e.g. 'claude-sonnet-4-6', 'google-cloud-vision-document-text-detection'
  processing_time_ms  INTEGER,
  error_message       TEXT,
  fallback_used       BOOLEAN       NOT NULL DEFAULT false,
  created_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
  completed_at        TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_apl_service ON ai_processing_log(service);
CREATE INDEX IF NOT EXISTS idx_apl_status ON ai_processing_log(status);
CREATE INDEX IF NOT EXISTS idx_apl_entity ON ai_processing_log(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_apl_created ON ai_processing_log(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_apl_cost ON ai_processing_log(service, created_at)
  WHERE cost_usd IS NOT NULL;

-- Cost monitoring view — monthly summary by service
CREATE VIEW IF NOT EXISTS v_ai_cost_summary AS
SELECT
  service,
  DATE_TRUNC('month', created_at)           AS month,
  COUNT(id)                                  AS total_calls,
  COUNT(id) FILTER (WHERE status='completed') AS successful_calls,
  COUNT(id) FILTER (WHERE status='failed')   AS failed_calls,
  COUNT(id) FILTER (WHERE fallback_used)     AS fallback_calls,
  SUM(input_tokens)                          AS total_input_tokens,
  SUM(output_tokens)                         AS total_output_tokens,
  ROUND(SUM(cost_usd)::DECIMAL, 4)          AS total_cost_usd,
  ROUND(AVG(processing_time_ms))             AS avg_processing_ms
FROM ai_processing_log
GROUP BY service, DATE_TRUNC('month', created_at)
ORDER BY month DESC, service;

-- MIGRATION 063: CANDIDATE DOCUMENTS — AI PROCESSING STATUS
-- Track Claude and OCR processing status per CV document
-- =============================================================================
ALTER TABLE candidate_documents
  ADD COLUMN IF NOT EXISTS ai_processing_status VARCHAR(20) DEFAULT 'pending'
    CHECK (ai_processing_status IN ('pending','processing','completed','failed','not_required')),
  ADD COLUMN IF NOT EXISTS ai_processed_at TIMESTAMPTZ,
  ADD COLUMN IF NOT EXISTS ocr_required BOOLEAN NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS ocr_completed_at TIMESTAMPTZ,
  ADD COLUMN IF NOT EXISTS ai_extracted_skills JSONB,
  -- Claude-extracted skills array — supplements cv_text_extracted
  ADD COLUMN IF NOT EXISTS ai_extracted_qualifications JSONB,
  -- Claude-extracted qualifications with NQF level mapping
  ADD COLUMN IF NOT EXISTS ai_extraction_confidence DECIMAL(4,2);
  -- 0.00 to 1.00 — Claude's confidence in extraction quality

-- MIGRATION 064: UPDATE CANDIDATE_PROFILES — AI FIELDS
-- =============================================================================
ALTER TABLE candidate_profiles
  ADD COLUMN IF NOT EXISTS ai_summary TEXT,
  -- Claude-generated professional summary from CV content
  ADD COLUMN IF NOT EXISTS ai_summary_generated_at TIMESTAMPTZ,
  ADD COLUMN IF NOT EXISTS ai_processing_version VARCHAR(20);
  -- Which Claude model version generated the current extraction
  -- Allows re-processing when model is upgraded

-- MIGRATION 065: ENVIRONMENT CONFIGURATION TABLE
-- Stores non-secret configuration for AI services
-- Secrets (API keys) must NEVER be stored in the database
-- =============================================================================
CREATE TABLE IF NOT EXISTS platform_config (
  id            UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  config_key    VARCHAR(100) NOT NULL UNIQUE,
  config_value  TEXT        NOT NULL,
  description   TEXT,
  updated_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_by    UUID        REFERENCES users(id)
);

INSERT INTO platform_config (config_key, config_value, description) VALUES
  ('claude_model',              'claude-sonnet-4-6',                         'Anthropic Claude model string for all AI tasks. Update deliberately — never use alias.'),
  ('claude_cv_max_tokens',      '4096',                                       'Maximum output tokens for CV analysis requests'),
  ('claude_assistant_max_tokens','2048',                                      'Maximum output tokens for assistant responses'),
  ('claude_max_messages_per_session','50',                                    'Maximum messages in a single assistant session before escalation prompt'),
  ('ocr_min_text_threshold',    '50',                                         'Minimum word count from standard extraction before OCR is triggered'),
  ('ocr_max_pages',             '5',                                          'Maximum CV pages to send to OCR — pages beyond this are ignored'),
  ('ocr_confidence_threshold',  '0.80',                                       'Minimum OCR confidence score — below this, candidate is flagged to review their upload'),
  ('ai_assistant_enabled',      'true',                                       'Master toggle for the AI Platform Assistant feature'),
  ('ai_cv_analysis_enabled',    'true',                                       'Master toggle for Claude CV analysis — false falls back to keyword matching'),
  ('google_vision_enabled',     'true',                                       'Master toggle for Google Cloud Vision OCR — false skips OCR on scanned docs')
ON CONFLICT (config_key) DO NOTHING;

COMMIT;

-- =============================================================================
-- V3.5 VERIFICATION QUERIES
-- =============================================================================
-- SELECT table_name FROM information_schema.tables
-- WHERE table_schema='public'
--   AND table_name IN ('assistant_conversations','ai_processing_log','platform_config');
--
-- SELECT config_key, config_value FROM platform_config ORDER BY config_key;
--
-- SELECT column_name FROM information_schema.columns
-- WHERE table_name='candidate_documents'
--   AND column_name IN ('ai_processing_status','ocr_required','ai_extracted_skills');
-- =============================================================================
-- END OF V3.5 MIGRATIONS
-- Illumin Investments CC | Trading as Illumin | Illumin360 Platform
-- =============================================================================

-- =============================================================================
-- V3.6 MIGRATIONS — FOUNDER PROGRAMME
-- =============================================================================

BEGIN;

-- MIGRATION 066: FOUNDER REGISTRATIONS TABLE
-- =============================================================================
CREATE TABLE IF NOT EXISTS founder_registrations (
  id              UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id         UUID        NOT NULL UNIQUE REFERENCES users(id),
  user_type       VARCHAR(20) NOT NULL CHECK (user_type IN ('job_seeker','employer')),
  founder_number  INTEGER     NOT NULL,
  -- Sequential: 1-300 for job seekers, 1-50 for employers
  granted_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
  granted_by      UUID        REFERENCES users(id),
  -- NULL = granted automatically by system. UUID = manually granted by admin.
  UNIQUE(user_type, founder_number)
);

CREATE INDEX IF NOT EXISTS idx_fr_user ON founder_registrations(user_id);
CREATE INDEX IF NOT EXISTS idx_fr_type ON founder_registrations(user_type);

-- MIGRATION 067: ADD IS_FOUNDER TO JOB_SEEKERS AND EMPLOYERS
-- =============================================================================
ALTER TABLE job_seekers
  ADD COLUMN IF NOT EXISTS is_founder BOOLEAN NOT NULL DEFAULT false;

ALTER TABLE employers
  ADD COLUMN IF NOT EXISTS is_founder BOOLEAN NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS idx_js_founder ON job_seekers(is_founder)
  WHERE is_founder = true;

CREATE INDEX IF NOT EXISTS idx_emp_founder ON employers(is_founder)
  WHERE is_founder = true;

-- MIGRATION 068: ADD FOUNDER_PERMANENT TO SUBSCRIPTION PLAN ENUM
-- =============================================================================
ALTER TABLE subscriptions
  DROP CONSTRAINT IF EXISTS subscriptions_plan_check;

ALTER TABLE subscriptions
  ADD CONSTRAINT subscriptions_plan_check
  CHECK (plan IN ('student_free','3m','6m','12m','18m','24m','founder_permanent'));

-- MIGRATION 069: ADD FOUNDER BADGE TYPES
-- =============================================================================
ALTER TABLE candidate_badges
  DROP CONSTRAINT IF EXISTS candidate_badges_badge_type_check;

ALTER TABLE candidate_badges
  ADD CONSTRAINT candidate_badges_badge_type_check
  CHECK (badge_type IN (
    'profile_complete','top_candidate','verified_professional','active_talent',
    'graduate_ready','skill_champion','early_adopter','cv_star',
    'graduate_spotlight','top_referrer','verified_student','video_pitcher',
    'illumin360_founder'  -- v3.6 — permanent, granted to first 300 job seekers
  ));

ALTER TABLE employer_badges
  DROP CONSTRAINT IF EXISTS employer_badges_badge_type_check;

ALTER TABLE employer_badges
  ADD CONSTRAINT employer_badges_badge_type_check
  CHECK (badge_type IN (
    'compliant_recruiter',
    'founding_partner'    -- v3.6 — permanent, granted to first 50 employers
  ));

-- MIGRATION 070: FOUNDER GRANT FUNCTION
-- Called within the registration transaction — uses SELECT FOR UPDATE
-- to prevent race conditions on the final founder slot
-- =============================================================================
CREATE OR REPLACE FUNCTION grant_founder_status(
  p_user_id   UUID,
  p_user_type VARCHAR(20)
)
RETURNS BOOLEAN AS $$
DECLARE
  v_quota         INTEGER;
  v_current_count INTEGER;
  v_founder_num   INTEGER;
  v_js_id         UUID;
  v_emp_id        UUID;
BEGIN
  -- Set quota based on user type
  v_quota := CASE p_user_type WHEN 'job_seeker' THEN 300 WHEN 'employer' THEN 50 ELSE 0 END;
  IF v_quota = 0 THEN RETURN false; END IF;

  -- Lock and count existing founders of this type
  SELECT COUNT(*) INTO v_current_count
  FROM founder_registrations
  WHERE user_type = p_user_type
  FOR UPDATE;

  -- Check if quota is already full
  IF v_current_count >= v_quota THEN
    RETURN false;
  END IF;

  -- Assign next founder number
  v_founder_num := v_current_count + 1;

  -- Insert founder registration record
  INSERT INTO founder_registrations (user_id, user_type, founder_number, granted_by)
  VALUES (p_user_id, p_user_type, v_founder_num, NULL);

  -- Mark the profile as founder
  IF p_user_type = 'job_seeker' THEN
    SELECT id INTO v_js_id FROM job_seekers WHERE user_id = p_user_id;
    UPDATE job_seekers SET is_founder = true WHERE id = v_js_id;

    -- Create permanent subscription (end_date = NULL)
    INSERT INTO subscriptions (job_seeker_id, plan, start_date, end_date, status, auto_renew)
    VALUES (v_js_id, 'founder_permanent', CURRENT_DATE, NULL, 'active', false);

    -- Award Illumin360 Founder badge
    INSERT INTO candidate_badges (job_seeker_id, badge_type)
    VALUES (v_js_id, 'illumin360_founder')
    ON CONFLICT (job_seeker_id, badge_type) DO NOTHING;

  ELSIF p_user_type = 'employer' THEN
    SELECT id INTO v_emp_id FROM employers WHERE user_id = p_user_id;
    UPDATE employers SET is_founder = true WHERE id = v_emp_id;

    -- Award Founding Partner badge
    INSERT INTO employer_badges (employer_id, badge_type)
    VALUES (v_emp_id, 'founding_partner')
    ON CONFLICT (employer_id, badge_type) DO NOTHING;
  END IF;

  -- Log to audit trail
  INSERT INTO audit_logs (user_id, action, entity_type, entity_id, metadata)
  VALUES (p_user_id, 'FOUNDER_STATUS_GRANTED', p_user_type, p_user_id,
    jsonb_build_object(
      'user_type', p_user_type,
      'founder_number', v_founder_num,
      'quota', v_quota,
      'granted_automatically', true
    ));

  RETURN true;
END;
$$ LANGUAGE plpgsql;

-- Admin manual grant function
CREATE OR REPLACE FUNCTION grant_founder_status_manual(
  p_user_id   UUID,
  p_user_type VARCHAR(20),
  p_admin_id  UUID,
  p_reason    TEXT
)
RETURNS BOOLEAN AS $$
DECLARE
  v_founder_num INTEGER;
  v_js_id UUID;
  v_emp_id UUID;
BEGIN
  -- Get next founder number (manual grants do not enforce quota)
  SELECT COALESCE(MAX(founder_number), 0) + 1 INTO v_founder_num
  FROM founder_registrations WHERE user_type = p_user_type;

  INSERT INTO founder_registrations (user_id, user_type, founder_number, granted_by)
  VALUES (p_user_id, p_user_type, v_founder_num, p_admin_id);

  IF p_user_type = 'job_seeker' THEN
    SELECT id INTO v_js_id FROM job_seekers WHERE user_id = p_user_id;
    UPDATE job_seekers SET is_founder = true WHERE id = v_js_id;
    INSERT INTO subscriptions (job_seeker_id, plan, start_date, end_date, status)
    VALUES (v_js_id, 'founder_permanent', CURRENT_DATE, NULL, 'active');
    INSERT INTO candidate_badges (job_seeker_id, badge_type)
    VALUES (v_js_id, 'illumin360_founder') ON CONFLICT DO NOTHING;
  ELSIF p_user_type = 'employer' THEN
    SELECT id INTO v_emp_id FROM employers WHERE user_id = p_user_id;
    UPDATE employers SET is_founder = true WHERE id = v_emp_id;
    INSERT INTO employer_badges (employer_id, badge_type)
    VALUES (v_emp_id, 'founding_partner') ON CONFLICT DO NOTHING;
  END IF;

  INSERT INTO audit_logs (user_id, action, entity_type, entity_id, metadata)
  VALUES (p_admin_id, 'FOUNDER_STATUS_GRANTED_MANUAL', p_user_type, p_user_id,
    jsonb_build_object('founder_number', v_founder_num, 'reason', p_reason));

  RETURN true;
END;
$$ LANGUAGE plpgsql;

-- Founder quota view for admin dashboard
CREATE VIEW IF NOT EXISTS v_founder_quota AS
SELECT
  'job_seeker'::VARCHAR  AS user_type,
  300                    AS quota,
  COUNT(id)              AS claimed,
  300 - COUNT(id)        AS remaining,
  COUNT(id) >= 300       AS quota_full
FROM founder_registrations WHERE user_type = 'job_seeker'
UNION ALL
SELECT
  'employer'::VARCHAR    AS user_type,
  50                     AS quota,
  COUNT(id)              AS claimed,
  50 - COUNT(id)         AS remaining,
  COUNT(id) >= 50        AS quota_full
FROM founder_registrations WHERE user_type = 'employer';

-- Update v_active_candidates to include founder status
DROP VIEW IF EXISTS v_active_candidates CASCADE;

CREATE VIEW v_active_candidates AS
SELECT
  js.id AS job_seeker_id,
  js.first_name, js.last_name, js.city,
  js.availability_status, js.willing_to_relocate, js.salary_expectation,
  js.notice_period, js.employment_status, js.profile_type,
  js.username, js.public_profile_enabled, js.public_headline,
  js.photo_url,
  js.is_founder,          -- v3.6 founder flag
  cp.highest_qualification, cp.qualification_field, cp.nqf_level,
  cp.years_experience, cp.industry_experience, cp.certifications,
  cp.professional_memberships, cp.tools_systems, cp.profile_complete_pct,
  cp.qualification_status, cp.expected_completion_date, cp.modules_completed,
  cp.academic_achievements, cp.gpa_range, cp.internship_experience,
  cp.open_to_graduate_programmes, cp.has_video,
  s.plan AS subscription_plan,
  s.end_date AS subscription_end_date,
  s.status AS subscription_status,
  cd.file_url AS active_cv_url,
  cd.cv_text_extracted, cd.uploaded_at AS cv_uploaded_at
FROM job_seekers js
JOIN subscriptions s ON s.job_seeker_id = js.id
  AND s.status IN ('active','expiring')
  -- Founder permanent subscriptions have NULL end_date — always active
JOIN candidate_profiles cp ON cp.job_seeker_id = js.id
LEFT JOIN candidate_documents cd ON cd.job_seeker_id = js.id
  AND cd.is_active = true AND cd.document_type = 'cv'
WHERE js.id IN (
  SELECT u.id FROM users u
  JOIN job_seekers jsi ON jsi.user_id = u.id
  WHERE u.is_active = true
);

CREATE VIEW v_standard_candidates AS
  SELECT * FROM v_active_candidates
  WHERE profile_type = 'job_seeker' AND qualification_status = 'completed';

CREATE VIEW v_student_candidates AS
  SELECT * FROM v_active_candidates
  WHERE profile_type = 'student' AND qualification_status = 'in_progress';

-- Re-create student stats view
CREATE VIEW v_student_stats AS
SELECT sv.institution_code, sv.institution,
  COUNT(sv.id) AS total_registered,
  COUNT(sv.id) FILTER (WHERE sv.verification_status = 'verified') AS verified,
  COUNT(sv.id) FILTER (WHERE sv.verification_status = 'pending') AS pending_verification,
  COUNT(sv.id) FILTER (WHERE sv.graduation_prompt_sent_at IS NOT NULL) AS graduation_prompts_sent,
  COUNT(sv.id) FILTER (WHERE sv.graduated_confirmed_at IS NOT NULL) AS converted_to_paid,
  ROUND(COUNT(sv.id) FILTER (WHERE sv.graduated_confirmed_at IS NOT NULL)::DECIMAL /
    NULLIF(COUNT(sv.id) FILTER (WHERE sv.graduation_prompt_sent_at IS NOT NULL),0)*100,1
  ) AS graduation_conversion_pct
FROM student_verifications sv
GROUP BY sv.institution_code, sv.institution ORDER BY total_registered DESC;

COMMIT;

-- =============================================================================
-- V3.6 VERIFICATION QUERIES
-- =============================================================================
-- SELECT * FROM v_founder_quota;
-- Expected: job_seeker quota=300 claimed=0 remaining=300 | employer quota=50 claimed=0 remaining=50
--
-- SELECT proname FROM pg_proc
-- WHERE proname IN ('grant_founder_status','grant_founder_status_manual');
--
-- SELECT conname FROM pg_constraint
-- WHERE conname IN ('subscriptions_plan_check','candidate_badges_badge_type_check');
-- =============================================================================
-- END OF V3.6 MIGRATIONS
-- =============================================================================
