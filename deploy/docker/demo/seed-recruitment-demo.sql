-- Illumin360 — live-learned-ranking demo seed (idempotent).
--
-- Seeds the Recruitment database with:
--   1. A labelled training set (24 match_outcomes, 12 hired / 12 rejected) where the match score does
--      NOT separate the classes (both hover ~45–59) but the pipeline signals do (interviews, rating,
--      offer, talent-side signals). This lets the learned model beat the match-score heuristic on the
--      deterministic hold-out, so GET …/applications/ranked actually uses the model.
--   2. One demo requisition with six in-pipeline applicants whose *heuristic* order (by match score)
--      deliberately differs from their *learned* order (by predicted hire-likelihood) — the two
--      high-match-but-cold applicants sink, the interviewed/offered ones rise.
--
-- The externally-owned recruitment_requests/applications tables are excluded from EF migrations, so this
-- creates them if absent. Everything else is service-owned (created by the recruitment-api startup
-- migration). Re-running is a no-op once the demo requisition exists.

\set ON_ERROR_STOP on

CREATE SCHEMA IF NOT EXISTS recruitment;

-- Externally-owned tables (never created by migrations) — shape mirrors the EF model.
CREATE TABLE IF NOT EXISTS recruitment.recruitment_requests (
    id          uuid PRIMARY KEY,
    city        varchar(100) NOT NULL,
    company_id  uuid NOT NULL,
    created_at  timestamptz NOT NULL,
    filled_at   timestamptz NULL,
    positions   integer NOT NULL,
    status      varchar(20) NOT NULL,
    title       varchar(150) NOT NULL
);

CREATE TABLE IF NOT EXISTS recruitment.applications (
    id          uuid PRIMARY KEY,
    applied_at  timestamptz NOT NULL,
    decided_at  timestamptz NULL,
    is_hire     boolean NOT NULL,
    match_score numeric(5,2) NOT NULL,
    request_id  uuid NOT NULL,
    status      varchar(20) NOT NULL,
    talent_id   uuid NOT NULL,
    talent_type varchar(20) NOT NULL
);

DO $demo$
DECLARE
    req_id     uuid := '11111111-1111-1111-1111-111111111111';
    company_id uuid := '22222222-2222-2222-2222-222222222222';
    v_app      uuid;
    v_rating   int;
BEGIN
    IF EXISTS (SELECT 1 FROM recruitment.recruitment_requests WHERE id = req_id) THEN
        RAISE NOTICE 'Illumin360 demo already seeded — skipping.';
        RETURN;
    END IF;

    -- ---------------------------------------------------------------- Requisition + detail
    INSERT INTO recruitment.recruitment_requests (id, city, company_id, created_at, positions, status, title)
    VALUES (req_id, 'Windhoek', company_id, now() - interval '45 days', 2, 'open', 'Senior Software Engineer');

    INSERT INTO recruitment.requisition_details (id, created_at, currency, employment_type, internal, remote, request_id, salary_min, salary_max)
    VALUES (gen_random_uuid(), now() - interval '45 days', 'NAD', 'full-time', false, true, req_id, 45000, 70000);

    -- ---------------------------------------------------------------- Six in-pipeline applicants
    -- Heuristic order (match desc):  Aria 82, Efe 78, Cade 74, Bela 68, Dana 60, Faye 55
    -- Learned order (hire signals):  Bela, Dana, Faye, Cade, Aria, Efe
    -- Each applicant: application + arrival source + talent-side signals, then optional interviews/offer.

    -- Aria — high match, cold pipeline.
    v_app := 'a0000001-0000-0000-0000-000000000001';
    INSERT INTO recruitment.applications (id, applied_at, is_hire, match_score, request_id, status, talent_id, talent_type)
    VALUES (v_app, now() - interval '18 days', false, 82.00, req_id, 'reviewed', gen_random_uuid(), 'professional');
    INSERT INTO recruitment.application_sources (id, application_id, channel, created_at) VALUES (gen_random_uuid(), v_app, 'careers', now() - interval '18 days');
    INSERT INTO recruitment.application_features (id, application_id, city_signal, role_signal, skill_signal, created_at) VALUES (gen_random_uuid(), v_app, 20, 25, 20, now() - interval '18 days');

    -- Bela — modest match, strong pipeline (3 interviews, offer).
    v_app := 'a0000002-0000-0000-0000-000000000002';
    INSERT INTO recruitment.applications (id, applied_at, is_hire, match_score, request_id, status, talent_id, talent_type)
    VALUES (v_app, now() - interval '30 days', false, 68.00, req_id, 'shortlisted', gen_random_uuid(), 'professional');
    INSERT INTO recruitment.application_sources (id, application_id, channel, created_at) VALUES (gen_random_uuid(), v_app, 'referral', now() - interval '30 days');
    INSERT INTO recruitment.application_features (id, application_id, city_signal, role_signal, skill_signal, created_at) VALUES (gen_random_uuid(), v_app, 92, 90, 95, now() - interval '30 days');
    FOR v_rating IN SELECT unnest(ARRAY[5,5,4]) LOOP
        INSERT INTO recruitment.interviews (id, application_id, created_at, duration_minutes, feedback_rating, location, required_skills, status, scheduled_at)
        VALUES (gen_random_uuid(), v_app, now() - interval '20 days', 45, v_rating, 'Windhoek HQ', 'C#,SQL,Azure', 'completed', now() - interval '20 days');
    END LOOP;
    INSERT INTO recruitment.offers (id, application_id, created_at, currency, salary_amount, start_date, status, title)
    VALUES (gen_random_uuid(), v_app, now() - interval '10 days', 'NAD', 62000, current_date + 30, 'extended', 'Senior Software Engineer');

    -- Cade — high match, lukewarm pipeline (1 interview, no offer).
    v_app := 'a0000003-0000-0000-0000-000000000003';
    INSERT INTO recruitment.applications (id, applied_at, is_hire, match_score, request_id, status, talent_id, talent_type)
    VALUES (v_app, now() - interval '22 days', false, 74.00, req_id, 'reviewed', gen_random_uuid(), 'professional');
    INSERT INTO recruitment.application_sources (id, application_id, channel, created_at) VALUES (gen_random_uuid(), v_app, 'careers', now() - interval '22 days');
    INSERT INTO recruitment.application_features (id, application_id, city_signal, role_signal, skill_signal, created_at) VALUES (gen_random_uuid(), v_app, 55, 50, 60, now() - interval '22 days');
    INSERT INTO recruitment.interviews (id, application_id, created_at, duration_minutes, feedback_rating, location, required_skills, status, scheduled_at)
    VALUES (gen_random_uuid(), v_app, now() - interval '15 days', 45, 3, 'Remote', 'C#,SQL', 'completed', now() - interval '15 days');

    -- Dana — low match, strong pipeline (2 interviews, offer).
    v_app := 'a0000004-0000-0000-0000-000000000004';
    INSERT INTO recruitment.applications (id, applied_at, is_hire, match_score, request_id, status, talent_id, talent_type)
    VALUES (v_app, now() - interval '28 days', false, 60.00, req_id, 'shortlisted', gen_random_uuid(), 'professional');
    INSERT INTO recruitment.application_sources (id, application_id, channel, created_at) VALUES (gen_random_uuid(), v_app, 'referral', now() - interval '28 days');
    INSERT INTO recruitment.application_features (id, application_id, city_signal, role_signal, skill_signal, created_at) VALUES (gen_random_uuid(), v_app, 88, 85, 90, now() - interval '28 days');
    FOR v_rating IN SELECT unnest(ARRAY[4,5]) LOOP
        INSERT INTO recruitment.interviews (id, application_id, created_at, duration_minutes, feedback_rating, location, required_skills, status, scheduled_at)
        VALUES (gen_random_uuid(), v_app, now() - interval '18 days', 45, v_rating, 'Windhoek HQ', 'C#,React', 'completed', now() - interval '18 days');
    END LOOP;
    INSERT INTO recruitment.offers (id, application_id, created_at, currency, salary_amount, start_date, status, title)
    VALUES (gen_random_uuid(), v_app, now() - interval '8 days', 'NAD', 58000, current_date + 30, 'extended', 'Senior Software Engineer');

    -- Efe — high match, no pipeline activity at all.
    v_app := 'a0000005-0000-0000-0000-000000000005';
    INSERT INTO recruitment.applications (id, applied_at, is_hire, match_score, request_id, status, talent_id, talent_type)
    VALUES (v_app, now() - interval '12 days', false, 78.00, req_id, 'applied', gen_random_uuid(), 'professional');
    INSERT INTO recruitment.application_sources (id, application_id, channel, created_at) VALUES (gen_random_uuid(), v_app, 'board', now() - interval '12 days');
    INSERT INTO recruitment.application_features (id, application_id, city_signal, role_signal, skill_signal, created_at) VALUES (gen_random_uuid(), v_app, 15, 20, 10, now() - interval '12 days');

    -- Faye — lowest match, strong pipeline (2 interviews, offer).
    v_app := 'a0000006-0000-0000-0000-000000000006';
    INSERT INTO recruitment.applications (id, applied_at, is_hire, match_score, request_id, status, talent_id, talent_type)
    VALUES (v_app, now() - interval '26 days', false, 55.00, req_id, 'shortlisted', gen_random_uuid(), 'professional');
    INSERT INTO recruitment.application_sources (id, application_id, channel, created_at) VALUES (gen_random_uuid(), v_app, 'careers', now() - interval '26 days');
    INSERT INTO recruitment.application_features (id, application_id, city_signal, role_signal, skill_signal, created_at) VALUES (gen_random_uuid(), v_app, 80, 78, 82, now() - interval '26 days');
    FOR v_rating IN SELECT unnest(ARRAY[5,4]) LOOP
        INSERT INTO recruitment.interviews (id, application_id, created_at, duration_minutes, feedback_rating, location, required_skills, status, scheduled_at)
        VALUES (gen_random_uuid(), v_app, now() - interval '16 days', 45, v_rating, 'Remote', 'C#,SQL', 'completed', now() - interval '16 days');
    END LOOP;
    INSERT INTO recruitment.offers (id, application_id, created_at, currency, salary_amount, start_date, status, title)
    VALUES (gen_random_uuid(), v_app, now() - interval '6 days', 'NAD', 54000, current_date + 30, 'extended', 'Senior Software Engineer');

    -- ---------------------------------------------------------------- Labelled training set (24 rows)
    -- Alternating hire/reject so any hold-out slice carries both classes. Match score overlaps between the
    -- classes (~45–59); the separation lives entirely in interviews/rating/offer/signals.
    INSERT INTO recruitment.match_outcomes
        (id, application_id, request_id, talent_id, talent_type, match_score, outcome, decided_at,
         source, remote, interview_count, avg_interview_rating, had_offer, days_to_decision,
         city_signal, role_signal, skill_signal)
    SELECT
        gen_random_uuid(), gen_random_uuid(), req_id, gen_random_uuid(), 'professional',
        (CASE WHEN g % 2 = 0 THEN 45 ELSE 47 END) + (g % 12),        -- overlapping match score
        CASE WHEN g % 2 = 0 THEN 'hired' ELSE 'rejected' END,
        now() - (g || ' hours')::interval,
        'demo-seed',
        (g % 3 = 0),                                                  -- remote: noise, not class-aligned
        CASE WHEN g % 2 = 0 THEN 3 ELSE 1 END,                        -- interview count
        CASE WHEN g % 2 = 0 THEN 4.5 ELSE 2.0 END,                    -- avg rating
        (g % 2 = 0),                                                  -- had offer
        7 + (g % 6),                                                  -- days to decision
        CASE WHEN g % 2 = 0 THEN 80 + (g % 15) ELSE 10 + (g % 15) END,
        CASE WHEN g % 2 = 0 THEN 82 + (g % 12) ELSE 12 + (g % 12) END,
        CASE WHEN g % 2 = 0 THEN 85 + (g % 10) ELSE 15 + (g % 10) END
    FROM generate_series(1, 24) AS g;

    RAISE NOTICE 'Illumin360 demo seeded: 1 requisition, 6 applicants, 24 labelled outcomes.';
END
$demo$;

-- ==================================================================================================
-- Dashboard-stats seed (idempotent, independent of the learned-ranking block above).
-- Feeds GET /api/recruitment/stats — the Employer portal dashboard reads OpenRequests, FilledRequests,
-- TotalApplications, TotalHires, the funnel (by status), and the 6-month applications trend from here.
-- 7 requisitions (4 open / 3 filled) across five cities + 30 applications spread over 6 months with a
-- full pipeline funnel (applied → reviewed → shortlisted → rejected → hired) and three hires.
-- ==================================================================================================
DO $stats$
DECLARE
    sentinel uuid := '33333333-3333-3333-3333-333333333333';
    reqs uuid[] := ARRAY[
        '3a000001-0000-0000-0000-000000000001',  -- open   Windhoek
        '3a000002-0000-0000-0000-000000000002',  -- open   Walvis Bay
        '3a000003-0000-0000-0000-000000000003',  -- open   Windhoek
        '3a000004-0000-0000-0000-000000000004',  -- open   Swakopmund
        '3a000005-0000-0000-0000-000000000005',  -- filled Oshakati
        '3a000006-0000-0000-0000-000000000006',  -- filled Windhoek
        '3a000007-0000-0000-0000-000000000007'   -- filled Rundu
    ]::uuid[];
    company_id uuid := '22222222-2222-2222-2222-222222222222';
BEGIN
    IF EXISTS (SELECT 1 FROM recruitment.recruitment_requests WHERE id = sentinel) THEN
        RAISE NOTICE 'Illumin360 dashboard-stats demo already seeded — skipping.';
        RETURN;
    END IF;

    -- Marker row so re-runs are a no-op (kept as a valid open requisition too).
    INSERT INTO recruitment.recruitment_requests (id, city, company_id, created_at, positions, status, title)
    VALUES (sentinel, 'Windhoek', company_id, now() - interval '90 days', 1, 'open', 'Talent Acquisition Lead');

    -- 7 requisitions: 4 open, 3 filled.
    INSERT INTO recruitment.recruitment_requests (id, city, company_id, created_at, filled_at, positions, status, title) VALUES
        (reqs[1], 'Windhoek',   company_id, now() - interval '80 days', NULL,                      2, 'open',   'Senior Accountant'),
        (reqs[2], 'Walvis Bay', company_id, now() - interval '72 days', NULL,                      1, 'open',   'Logistics Coordinator'),
        (reqs[3], 'Windhoek',   company_id, now() - interval '60 days', NULL,                      3, 'open',   'Registered Nurse'),
        (reqs[4], 'Swakopmund', company_id, now() - interval '50 days', NULL,                      1, 'open',   'Frontend Developer'),
        (reqs[5], 'Oshakati',   company_id, now() - interval '150 days', now() - interval '40 days', 1, 'filled', 'Sales Manager'),
        (reqs[6], 'Windhoek',   company_id, now() - interval '130 days', now() - interval '25 days', 1, 'filled', 'HR Officer'),
        (reqs[7], 'Rundu',      company_id, now() - interval '120 days', now() - interval '15 days', 2, 'filled', 'Civil Engineer');

    -- 30 applications spread over 6 months; status drives the funnel, is_hire drives hires/trend.
    INSERT INTO recruitment.applications (id, applied_at, decided_at, is_hire, match_score, request_id, status, talent_id, talent_type)
    SELECT
        gen_random_uuid(),
        now() - (((g % 6) + 1) || ' months')::interval - ((g % 20) || ' days')::interval,
        CASE WHEN g % 10 = 0 THEN now() - interval '5 days' ELSE NULL END,
        (g % 10 = 0),                                             -- 3 hires (g = 10,20,30)
        45 + (g % 45),                                            -- match score 45..89
        reqs[1 + (g % 7)],
        (ARRAY['applied','reviewed','shortlisted','rejected','hired'])[
            CASE WHEN g % 10 = 0 THEN 5
                 WHEN g % 4 = 0  THEN 4
                 WHEN g % 3 = 0  THEN 3
                 WHEN g % 2 = 0  THEN 2
                 ELSE 1 END],
        gen_random_uuid(),
        (ARRAY['professional','student'])[1 + (g % 2)]
    FROM generate_series(1, 30) AS g;

    RAISE NOTICE 'Illumin360 dashboard-stats demo seeded: 8 requisitions (5 open / 3 filled), 30 applications, 3 hires.';
END
$stats$;
