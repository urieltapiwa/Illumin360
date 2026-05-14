# Illumin360 Platform Assistant — Detailed Design

| Document detail | Value |
|---|---|
| Document title | Illumin360 Platform Assistant — Detailed Design |
| Document ID | ILLM-03-013_AI_Platform_Assistant_Design |
| Version | 1.0 |
| Date | 14 May 2026 |
| Classification | CONFIDENTIAL |
| Status | Draft — for technical review |
| Source authority | Section 29, Illumin360 Complete Technical Specification v3.6 |
| Phase | Phase 6 |
| Owner | Platform Engineering |

## 1. Purpose

This document specifies the design of the Illumin360 Platform Assistant — a conversational support and guidance tool embedded across the platform for all user types. It covers assistant instances per audience, context assembly, system prompt design, technical architecture, data model, API surface, UI, content moderation, branding compliance, and cost model.

The Platform Assistant is read-only in Phase 6: it answers questions and explains the platform but does not take actions on the user's behalf. Agentic expansion is deferred to Phase 7+ subject to security review.

## 2. Assistant instances — one per user type

There is not one generic chatbot. Each user type interacts with a specialised assistant carrying that user's context. Five instances are defined.

| Instance | Audience | Personal context surfaced into the system prompt | Placement |
|---|---|---|---|
| Job Seeker Assistant | Logged-in job seekers and students | First name, profile completion %, subscription status and expiry date, latest CV upload status, badge progress summary, referral stats, recent notification highlights | Floating chat button bottom-right on candidate dashboard |
| Employer Assistant | Logged-in employers | Company name, active and recent requests with status, latest reports and payment statuses, candidate unlock count this billing period, current invoice totals | Floating chat button on employer dashboard |
| Student Assistant | Logged-in students | First name, institution, programme, verification status, expected graduation date, grace period countdown, upgrade options | Floating chat button on student dashboard |
| Public Assistant | Non-logged-in website visitors | No personal data. Static platform feature, pricing, and sign-up guidance only. | Floating chat button on public homepage and pricing page |
| Admin Assistant | Admin users only | Platform-wide stats summary, pending compliance reviews, student verification queue counts — read-only summary | Floating chat button on admin dashboard |

Each instance has its own system prompt, its own permitted context categories, and its own audit trail.

## 3. Capabilities by instance

### 3.1 Job seeker / student assistant

- Explain platform mechanics: how employers find candidates, what match scores mean, what discoverability requires
- Profile completion guidance: "Your profile is 68% complete. Adding your skills section would meaningfully increase your visibility."
- CV upload status: "Your CV was received. Your profile is being updated — this takes about 5 minutes."
- Subscription guidance: "Your subscription expires in 14 days. Here are your renewal options."
- Badge progression: "You need one more shortlist appearance to earn your Top Candidate badge."
- Referral programme: "Here is your referral link. You have earned 2 free months from 2 successful referrals."
- Student graduation upgrade: "Your grace period ends 30 June 2026. Here is how to upgrade."
- General career guidance specific to the Namibian job market

### 3.2 Employer assistant

- Guide through creating a recruitment request — including which of the four search modes fits the situation
- Explain shortlist scores: "An 87% score means the candidate met 87% of your weighted criteria. Here is the per-factor breakdown."
- Explain gap analysis sub-blocks (per ILLM-03-015)
- Internal portal setup guidance — closing dates, application caps, HR notifications
- Billing questions: "Invoice INV-2026-000048 for NAD 2,300 was issued on 26 April 2026. Download it from your billing history."
- Compliance guidance: "The gender filter requires a written justification because it is a sensitive criterion under Namibian labour law."

### 3.3 What the assistant will NOT do (across all instances)

- Make hiring decisions or recommend hiring of any specific candidate
- Give legal advice — it will explain platform requirements but always defers to attorney consultation for legal questions
- Override compliance controls — never bypasses sensitive filter declarations or compliance gates
- Access another user's private data — each assistant only sees the logged-in user's own data
- Process payments, change subscriptions, or modify profile data — guides users to the correct platform screen but does not take actions on their behalf in Phase 6
- Identify the underlying technology provider (per Section 31 branding policy and §6 below)

## 4. Context assembly

At the start of every conversation, the platform assembles a context package and sends it as the system prompt for that conversation. The package has three layers.

| Layer | Source | Refresh cadence |
|---|---|---|
| Static — assistant identity and rules | Stored config per instance | Versioned in git; changes require code release |
| Static — platform knowledge (features, pricing, policies) | Stored markdown digest loaded at server start | Refreshed when platform changes — daily reload acceptable |
| Dynamic — user-specific data | Live query against the user's own database tables only | Per-conversation — assembled at session start, not per message |

The user-specific layer is a deterministic projection — the platform queries a fixed set of fields and formats them into a structured summary. There is no general "fetch anything about the user" capability; this is a security and predictability requirement.

### 4.1 Per-instance user data fields

| Instance | Fields included in context |
|---|---|
| Job Seeker | first_name, email, profile_complete_pct, subscription.plan_type, subscription.end_date, latest_cv.uploaded_at, latest_cv.processing_status, badges[], referrals.count, referrals.rewards_earned, last_5_notifications[type, timestamp] |
| Student | first_name, email, institution, programme, verification_status, expected_graduation_date, grace_period_end_date, upgrade_offer_id |
| Employer | company_name, contact_first_name, active_requests[id, role, status, created_at], recent_reports[id, status, paid_at], pending_invoices[id, total, due_date], candidate_unlocks_this_period |
| Admin | total_active_candidates, total_active_employers, pending_compliance_reviews, pending_student_verifications, last_24h_payment_volume, system_alerts_open |
| Public | (empty — static knowledge only) |

## 5. System prompt design

Each instance has a versioned system prompt. The structure follows a consistent template.

```
# IDENTITY
You are the Illumin360 {Instance Name} Assistant.

# PLATFORM
Illumin360 is a talent matching and recruitment platform for Namibia. It connects job
seekers and employers through Illumin360 matching.

# YOUR ROLE
{Instance-specific role}

# TONE
Professional, warm, helpful. Plain English. No jargon. Use NAD for currency.
Refer to Namibian institutions by their common abbreviations (UNAM, NUST, IUM).

# WHAT YOU CAN DO
{Instance-specific list — bulleted}

# WHAT YOU CANNOT DO
- Make or recommend hiring decisions for any specific candidate
- Give legal advice (defer to an attorney)
- Override compliance controls
- Access any user's data other than the currently logged-in user
- Take actions on the user's behalf (changes to profile, subscription, payment) — direct them
  to the correct platform screen
- Identify the technology that powers you (see Identity Policy below)

# IDENTITY POLICY
You are the Illumin360 Platform Assistant. You must not identify yourself as Claude or
any named AI model. You must not name Anthropic, OpenAI, Google, or any third-party
provider. If asked "what AI are you?" or "are you Claude?", respond with:
"I'm the Illumin360 Platform Assistant. I'm built to help with the Illumin360
platform specifically. I'm not able to discuss the technology that powers me."
Do confirm you are an automated system — automated decision-making transparency is a
legal requirement. Do not name what underlies you.

# CURRENT USER CONTEXT
{Dynamic layer — JSON or structured plain text describing the user's current state}

# CONVERSATION RULES
- Keep responses concise. Default to 2-4 sentences for factual questions; more for
  explanatory ones.
- If you do not know an answer with confidence, say so and direct the user to support.
- Cite specific numbers from the user's context when relevant (profile completion %,
  badge progress, days to expiry) — be accurate.
- Do not invent facts. If a feature is not described in your platform knowledge, say
  you are not sure rather than guessing.
- For sensitive topics (discrimination, mental health, legal threats from third parties),
  do not improvise — offer to escalate to a human via the "Connect me with the
  Illumin team" button.
```

System prompts are versioned. The active version for each instance is stored in `assistant_prompts(instance, version, active, prompt_text, created_at)`.

## 6. Branding compliance

Per Section 31 branding policy (ILLM-03-012):

- The assistant identifies as "Illumin360 Platform Assistant", never as "AI assistant", "chatbot", or "Claude".
- When asked what model powers it: §5 IDENTITY POLICY response.
- The assistant does confirm it is automated when asked directly — required for transparency.
- The assistant never uses phrases from the prohibited list in §3 of the branding policy.
- The system prompt itself is the enforcement mechanism — model-level rules. Application-level validation rejects responses containing prohibited terms before delivery to the user.

A post-generation filter scans every assistant response for the prohibited terms list. If a prohibited term appears, the response is rewritten through a clean-up pass before delivery; the filter triggers an admin alert so the system prompt can be hardened.

## 7. Technical architecture

### 7.1 Sequence

1. User opens the chat widget and submits a message.
2. Platform assembles the context package for the session if first message; otherwise reuses the cached context for the session.
3. Conversation history (system prompt + prior user/assistant messages + new user message) is sent to the inference provider with `stream: true`.
4. Response streams back token-by-token to the chat widget for real-time typing effect.
5. The complete response is persisted to `assistant_conversations.messages` JSONB.
6. Post-generation filter (§6) runs against the complete response.
7. UI displays the response with the typing animation completed.

### 7.2 Caching

The static layers of the system prompt (identity + platform knowledge) are identical across all users of a given instance and are eligible for prompt caching at the inference provider. This yields approximately 90% cost reduction on the input tokens for these layers. The dynamic user-context layer is unique per conversation and is not cached.

### 7.3 Streaming

All inference calls use streaming. The UI starts rendering the response within ~200ms of the user submitting a message, even though full response generation may take 1–3 seconds. This is critical for perceived responsiveness.

### 7.4 Session boundary

A session is one browser-tab conversation. Conversation history is retained for the duration of the session. When the user closes the tab or after 30 minutes of inactivity the session expires. Phase 6 does not persist conversation history across sessions; each new session starts fresh.

### 7.5 Rate limiting

- 10 messages per minute per authenticated user
- 30 messages per session before the user is prompted to start a new session or escalate
- 5 messages per minute per IP for the public assistant (no auth)
- 50 sessions per IP per day for the public assistant — abuse prevention

### 7.6 Failure handling

| Failure | Behaviour |
|---|---|
| Inference provider timeout (>30s) | Single retry. If still failing, response: "I'm having trouble responding right now. Please try again in a moment, or click 'Connect me with the Illumin team' for human support." Logged as failure for monitoring. |
| Inference provider error 4xx/5xx | Same fallback message, logged with error code. |
| Post-generation filter rejects response | Re-prompt internally with stricter instruction; on second rejection, fallback message; alert admin. |
| Network disconnect mid-stream | UI shows reconnect attempt; if reconnection fails, last partial response is discarded and user is prompted to retry. |

## 8. Data model

### 8.1 New table — `assistant_conversations`

| Column | Type | Constraints | Description |
|---|---|---|---|
| id | UUID | PK | |
| user_id | UUID | FK → users, NULL for public | Logged-in user or NULL for public |
| user_type | ENUM | NOT NULL — `job_seeker`, `employer`, `student`, `public`, `admin` | Which assistant instance |
| session_id | VARCHAR(128) | NOT NULL | Unique per browser session — opaque token issued by the platform |
| messages | JSONB | NOT NULL DEFAULT '[]' | Array of `{role, content, timestamp}` — full conversation transcript |
| context_snapshot | JSONB | NOT NULL | The dynamic-layer user context captured at session start — for audit and reproducibility |
| started_at | TIMESTAMPTZ | NOT NULL DEFAULT now() | |
| last_message_at | TIMESTAMPTZ | NOT NULL DEFAULT now() | |
| message_count | INTEGER | NOT NULL DEFAULT 0 | Total messages (user + assistant) this session |
| escalated_to_human | BOOLEAN | DEFAULT false | True if user clicked "Connect me with the Illumin team" |
| escalation_notified_at | TIMESTAMPTZ | NULL | When escalation email was sent to info@illumininvestments.com |
| filter_triggered_count | INTEGER | DEFAULT 0 | How many times post-generation filter rewrote a response in this session |

Indexes: `(user_id, started_at DESC)`, `(session_id)`, `(escalated_to_human) WHERE escalated_to_human = true`.

Retention: 90 days for non-escalated conversations; 7 years for escalated ones (treated as support correspondence).

### 8.2 New table — `assistant_prompts`

| Column | Type | Description |
|---|---|---|
| id | UUID PK | |
| instance | ENUM | `job_seeker`, `employer`, `student`, `public`, `admin` |
| version | INTEGER | Sequential per instance |
| active | BOOLEAN | Only one row per instance is active at a time |
| prompt_text | TEXT | The full system prompt template |
| created_by | UUID | Admin user |
| created_at | TIMESTAMPTZ | |
| activated_at | TIMESTAMPTZ NULL | When this version became the active one |

## 9. API endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | /assistant/message | Bearer (any user) OR none (public) | Send a message. Body: `{message, session_id}`. Returns SSE stream of tokens, terminated by a complete-response event. |
| GET | /assistant/session/:session_id | Bearer | Get conversation history for a session — owner only |
| POST | /assistant/escalate | Bearer | Flag the conversation for human follow-up. Sends conversation transcript to info@illumininvestments.com. Response: confirmation. |
| GET | /admin/assistant/conversations | Admin | Filterable list of conversations — by date, instance, escalation status, filter-trigger count |
| GET | /admin/assistant/conversations/:id | Admin | Full transcript with context snapshot |
| GET | /admin/assistant/prompts | Admin | List system prompts and versions |
| POST | /admin/assistant/prompts | Admin | Stage a new system prompt version (not yet active) |
| PUT | /admin/assistant/prompts/:id/activate | Admin | Activate a staged prompt version. Previous active version moves to inactive. |

## 10. UI implementation

| Element | Specification |
|---|---|
| Floating action button | Bottom-right, 56×56px, Illumin green `#1D9E75`. Icon: chat bubble. Persistent across all authenticated pages and public homepage. |
| Chat widget | Slides up from the button. 380×520px desktop. Full-screen modal on mobile. Header shows "Illumin360 Assistant" with close button. |
| Suggested starter questions | Three clickable starters shown when chat opens for the first time in a session. Per instance. Examples for job seeker: "How do employers find me?", "What does my match score mean?", "How do I renew my subscription?" |
| Typing indicator | Three animated dots while inference is in progress |
| Message bubbles | User: right-aligned, green background, white text. Assistant: left-aligned, white background, dark text. Both within rounded containers. |
| Long-message handling | Messages over 1000 chars use a "Read more" expand control |
| Code/list rendering | Markdown rendered for assistant messages — lists, bold, inline code, links |
| Escalate button | Persistent at bottom of chat window: "Connect me with the Illumin team" |
| Session persistence | Within the browser tab — conversation does not reset on navigation. Closing the tab ends the session. |

## 11. Content moderation

| Layer | Behaviour |
|---|---|
| Inference-provider built-in safety | Underlying provider's safety measures apply automatically — Illumin360 does not disable them. |
| Platform-specific rules in system prompt | §5 IDENTITY POLICY and CANNOT DO sections enforce the strictest platform rules |
| Post-generation filter | Scans for prohibited terms (Section 31 list) and personally-identifiable data leakage (e.g., assistant should not echo another user's name from cross-contamination — defence in depth) |
| User-side abuse | Rate limiting + abuse logging. Repeated abusive prompts from the same authenticated user trigger an admin alert and may suspend assistant access for that user. |
| Escalation | The escalate button is always available. Users in distress (mental health, legal threats) should be directed to escalate rather than continuing the assistant conversation. |

## 12. Cost model

Year 1 estimates from Section 29.3.6 of the spec:

| Scenario | Volume | Cost USD |
|---|---|---|
| Year 1 light usage | 200 conversations × 8 messages | USD 3.00/month |
| Year 2 growing | 600 conversations × 10 messages | USD 12.00/month |
| Year 3 active | 2,000 conversations × 10 messages | USD 40.00/month |

Effective per-conversation cost with prompt caching: USD 0.015 ≈ NAD 0.27.

Monitoring: a daily report aggregates token usage by instance and surfaces it on the admin dashboard. An alert fires if monthly spend exceeds 200% of the same month in the prior year.

## 13. Acceptance criteria

1. The Job Seeker Assistant correctly reports the user's profile completion percentage matching the dashboard widget.
2. When a user explicitly asks "what AI are you?" or "are you Claude?", the response matches the §5 IDENTITY POLICY wording and does not name Claude, Anthropic, or any third-party provider.
3. The assistant correctly identifies as automated when asked "are you a person?" — required for transparency.
4. The Employer Assistant cannot retrieve information about a different employer's account, even when prompted to.
5. The Public Assistant cannot retrieve any user-specific information — it has no `user_id` in its context.
6. Conversation history persists across page navigation within the same tab; closing the tab ends the session.
7. The escalate button generates an email to `info@illumininvestments.com` with the full transcript and the user's context snapshot.
8. Streaming response begins within 1 second of message submission (P95).
9. Post-generation filter blocks any response containing prohibited terms from §3 of the branding policy.
10. Audit log captures every conversation start, message, escalation, and prompt-version activation.
11. Rate limit (10 messages/minute/user) enforces with 429 response.
12. Sessions exceeding 50 messages display a "start a new session" prompt and disable further sending until a new session begins.

## 14. Open questions

| # | Question | Action |
|---|---|---|
| 1 | Should escalated conversations create a ticket in a CRM/helpdesk system or remain as email-only? | Decision required pre-launch |
| 2 | Should the assistant be available on mobile native app once that exists, with same identity and capability scope? | Phase 8 |
| 3 | Should the public assistant offer to capture an email for follow-up after answering pre-sign-up questions? | Marketing decision |
| 4 | Conversation history persistence across sessions for logged-in users — confirm Phase 6 leaves this off | Confirmed off in v3.6 spec |

## 15. Cross-references

| Document | Link |
|---|---|
| Spec v3.6 | Section 29 (canonical) + Section 31 (branding) |
| Branding Policy (ILLM-03-012) | §7 assistant identity policy |
| Gap Analysis Design (ILLM-03-015) | Assistant explains gap analysis blocks |
| API Design (ILLM-03-005 v2.0) | Endpoints in §9 added in v2.0 refresh |
| Database Design (ILLM-03-004 v2.0) | Tables in §8 added in v2.0 refresh |
| Security Design (ILLM-03-007 v2.0) | Context isolation and rate limiting |

## 16. Document control

| Version | Date | Changes |
|---|---|---|
| 1.0 | 14 May 2026 | Initial issue addressing AI Platform Assistant coverage gap. |
