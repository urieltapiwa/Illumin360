# Business Case — v2.0 Refresh Patch

| Document detail | Value |
|---|---|
| Target document | ILLM-01-001_Business_Case (currently v1.0) |
| Patch version | v2.0 |
| Patch date | 14 May 2026 |
| Source authority | Illumin360 Complete Technical Specification v3.6 |
| Patch type | Targeted — additive sections; existing v1.0 content retained |
| Apply order | Apply once corrected source (Source Corrections Register) is signed off |

This patch document lists the additions and revisions required to bring the v1.0 Business Case to v2.0 reflecting the v3.6 spec. Each section below points to the place in the v1.0 doc where the new content lands.

## 1. Add new section — Go-to-Market: Illumin360 Founder Programme

Insert as a new section after the existing "Market Opportunity" or equivalent GTM section.

> ### Go-to-Market — Illumin360 Founder Programme
>
> The platform launches with a quota-based Founder Programme to bootstrap a credible talent pool and committed employer base from day one.
>
> | Group | Quota | Permanent benefit | Commercial impact |
> |---|---|---|---|
> | Job seeker founders | First 300 to register | Permanent profile, no renewal fee | Forgone subscription revenue ≈ NAD 299–1,299 × 300 = NAD 89,700 – 389,700 over lifecycle, depending on plan substitution rate. Justified by scarcity of credible candidate pool at launch. |
> | Employer founding partners | First 50 to register | Permanent account, no expiry | Zero direct revenue impact — employers always pay per-request. Founding Partner badge is the recognition. |
>
> Quota closes automatically when filled. Founders are permanently recognised on the platform — a marketing asset in itself. See ILLM-03-011 for detailed design.

## 2. Update financial model — AI services operating cost

Insert into the cost structure section. Adopt the following table from Section 29.4 of the spec:

> | AI service line | Year 1 | Year 3 |
> |---|---|---|
> | CV analysis | USD 0.75/mo | USD 12.00/mo |
> | Justification engine | USD 0.50/mo | USD 8.00/mo |
> | Platform Assistant | USD 3.00/mo | USD 40.00/mo |
> | OCR (scanned CVs) | USD 0.00 (free tier) | USD 2.00/mo |
> | **Total** | **≈ NAD 77/mo** | **≈ NAD 1,116/mo** |
>
> At Year 3 projected revenue of NAD 7,070,000, total annual AI services cost of ~NAD 13,400 represents 0.19% of revenue. This is the most cost-efficient operating component of the platform.

## 3. Revenue model — incorporate Internal Recruitment and Candidate Unlock

If not already present at v1.0 (the v1.0 Business Case predates v2.0 introduction of internal recruitment), add the four employer pay-per-request models per Section 2.3 of the spec:

> | Service | Total incl. VAT |
> |---|---|
> | Platform talent pool — report unlock | NAD 1,725.00 |
> | Employer-uploaded CV search — report unlock | NAD 1,725.00 |
> | Combined search — report unlock | NAD 1,725.00 |
> | Internal recruitment link — portal + report (consolidated) | NAD 2,300.00 |
> | Candidate profile unlock (optional, per candidate) | NAD 402.50 |

## 4. Revenue model — incorporate Student CSR Free Subscription

If not present at v1.0, add a subsection describing the student CSR programme: free for the duration of enrolment plus a 60-day grace period, with paid-upgrade conversion at graduation as a defined revenue funnel.

## 5. Section 21 social features — strategic rationale

Add a new section summarising the strategic intent of the eight social features:

> ### Social and community features — strategic rationale
>
> The eight features in F1–F8 (shareable profile card, demand feed, insights blog, badges, graduate spotlight, employer reviews, referral programme, talent report) shift Illumin360 from a transactional shortlisting tool to a daily-engagement platform with organic reach. Their effect on the business model:
>
> - **Acquisition** — Public profile cards, Open Graph previews, Career Insights blog, and the Talent Report drive non-paid traffic.
> - **Retention** — Badges, demand feed, and engagement features increase frequency of platform visits.
> - **Word-of-mouth** — Referral programme directly rewards organic acquisition; one free month per converted referral.
> - **Brand authority** — Career Insights and the Talent Report position Illumin as a Namibian labour-market thought leader.

## 6. Risk register additions

Add risks introduced by v2.0–v3.6 scope:

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Founder quota race condition under launch traffic | Medium | Medium | SELECT FOR UPDATE serialisation, load-tested per ILLM-03-011 |
| Section 31 branding policy violation in client-facing content leaking technology stack | Medium | Medium | Post-generation filter on AI Assistant, quarterly audit, pre-publication review per ILLM-03-012 |
| Photo blind-screening bypass — accidental column inclusion in shortlist projection | Low | High | Structural enforcement (column not present in projection), audit query plan |
| Transcription accuracy on Namibian English/Afrikaans/Oshiwambo | Medium | Low–Medium | 30% keyword weight cap, candidate notification of failure, provider evaluation pre-launch |
| AI Assistant identity disclosure (responds with "I am Claude") | Medium | Medium | System prompt enforcement, post-generation filter, fallback wording per ILLM-03-013 §7 |
| Compliant Recruiter badge dispute | Low | Medium | Admin revocation flow with documented reason, 90-day cooling-off period |
| AI services cost overrun if usage spikes | Low | Low | Monthly cost monitoring, 200% YoY alert, prompt caching reducing assistant cost 90% |

## 7. Phase plan alignment

Update the project plan section to reflect the eight-phase plan from Section 27 (replace any earlier six-phase table):

| Phase | Features | Estimate | Status |
|---|---|---|---|
| 1 | Core talent pool, job seeker, employer search, matching engine, **Founder quota live** | 8–10 wks | Authorised |
| 2 | Reporting, payments, notifications, subscription reminders | 6–8 wks | Authorised |
| 3 | Privacy, compliance, candidate unlock, admin analytics | 4–6 wks | Authorised |
| 4 | Internal recruitment link, consolidated billing | 3–4 wks | Authorised |
| 5 | Student CSR + social features (F1–F5, F7) + badges + spotlight + referrals | 4–6 wks | Authorised |
| 6 | AI evolution (adaptive weighting, gap analysis, RLHF collection), Asset management (logos, photos with blind screening), PWA, social badges (Verified Student, Compliant Recruiter), AI Platform Assistant, employer reviews | 5–7 wks | Authorised |
| 7 | Video integration (candidate elevator pitch + transcription at 30% weight) | 4–5 wks | Premium |
| 8 | RLHF model refinement (post 500-record threshold), marketplace expansion | TBD | Future |

## 8. Change log entry

Add a new row to the Business Case document control table:

| Version | Date | Changes |
|---|---|---|
| 1.0 | (existing) | Initial issue |
| 2.0 | 14 May 2026 | Refreshed against v3.6 spec. Added Founder Programme GTM, AI services cost model, internal recruitment revenue, candidate unlock revenue, student CSR programme, social features rationale, expanded risk register, updated eight-phase plan. |

## 9. Cross-references to update

- Reference ILLM-03-011 Founder Programme Design throughout the GTM section.
- Reference ILLM-03-012 Branding Policy in risk register row 2.
- Reference ILLM-03-017 Asset Management in risk register row 3.
- Reference ILLM-03-021 Video Integration in risk register row 4.
- Reference ILLM-03-013 AI Platform Assistant in risk register row 5.

## 10. Sign-off

Before this patch is integrated into the v2.0 Business Case docx, obtain sign-off from:
- Platform Sponsor
- CFO / Finance Lead (cost model)
- Marketing Lead (GTM and social features)
- Risk Manager (risk register additions)
