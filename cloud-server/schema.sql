-- FarmQuest cloud server schema (PostgreSQL).

CREATE TABLE IF NOT EXISTS saves (
    user_id    TEXT PRIMARY KEY,
    save_json  TEXT NOT NULL,
    updated_at BIGINT NOT NULL
);

-- Privacy-safe gameplay events. Payloads contain gameplay facts only
-- (event name, level, counters) — never PII, device IDs, or ad identifiers.
CREATE TABLE IF NOT EXISTS analytics_events (
    id          BIGSERIAL PRIMARY KEY,
    received_at BIGINT NOT NULL,
    payload     JSONB NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_analytics_received ON analytics_events (received_at);
