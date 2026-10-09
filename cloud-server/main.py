"""
FarmQuest cloud server (Phase 6, §43).

Optional backend — the game is local-first and fully playable without it.
Provides:
  PUT  /saves/{user_id}     upsert a save (latest timestamp wins)
  GET  /saves/{user_id}     download a save (404 if none)
  POST /analytics/batch     receive privacy-safe gameplay events
  GET  /config              remote-config values for clients

Run:
  pip install -r requirements.txt
  export DATABASE_URL=postgresql://user:pass@localhost/farmquest
  export CLOUD_API_KEY=your-secret-key   # optional; if unset, API is open (dev only!)
  uvicorn main:app --host 0.0.0.0 --port 8000

Then apply schema.sql to the database.
"""
import json
import os
import time
from typing import Any, Dict, List, Optional

import psycopg
from fastapi import FastAPI, Header, HTTPException, Response
from pydantic import BaseModel

DATABASE_URL = os.environ.get("DATABASE_URL", "")
API_KEY = os.environ.get("CLOUD_API_KEY", "")

# Remote-config values served to clients. Tune without a client update.
REMOTE_CONFIG: Dict[str, Any] = {
    "max_rewarded_ads_per_day": 5,
    "rewarded_ad_cooldown_seconds": 300,
    "market_day_enabled": True,
    "xp_event_multiplier": 1.0,
    "cloud_save_endpoint": "",
}

app = FastAPI(title="FarmQuest Cloud")


def check_auth(x_api_key: Optional[str]):
    if API_KEY and x_api_key != API_KEY:
        raise HTTPException(status_code=401, detail="bad api key")


def db():
    if not DATABASE_URL:
        raise HTTPException(status_code=500, detail="DATABASE_URL not configured")
    return psycopg.connect(DATABASE_URL)


class SaveUpload(BaseModel):
    save_json: str
    updated_at: int


class AnalyticsBatch(BaseModel):
    events: List[Dict[str, Any]]


@app.put("/saves/{user_id}")
def upload_save(user_id: str, payload: SaveUpload, x_api_key: Optional[str] = Header(None)):
    check_auth(x_api_key)
    with db() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                INSERT INTO saves (user_id, save_json, updated_at)
                VALUES (%s, %s, %s)
                ON CONFLICT (user_id) DO UPDATE
                SET save_json = EXCLUDED.save_json,
                    updated_at = EXCLUDED.updated_at
                WHERE EXCLUDED.updated_at >= saves.updated_at
                """,
                (user_id, payload.save_json, payload.updated_at),
            )
            conn.commit()
    return {"ok": True}


@app.get("/saves/{user_id}")
def download_save(user_id: str, x_api_key: Optional[str] = Header(None)):
    check_auth(x_api_key)
    with db() as conn:
        with conn.cursor() as cur:
            cur.execute(
                "SELECT save_json, updated_at FROM saves WHERE user_id = %s",
                (user_id,),
            )
            row = cur.fetchone()
    if row is None:
        return Response(status_code=404)
    return {"save_json": row[0], "updated_at": row[1]}


@app.post("/analytics/batch")
def analytics_batch(payload: AnalyticsBatch, x_api_key: Optional[str] = Header(None)):
    """Privacy-safe gameplay events only. The client never sends PII,
    device IDs, or advertising identifiers (see CHILD_SAFETY_AUDIT.md)."""
    check_auth(x_api_key)
    now = int(time.time())
    with db() as conn:
        with conn.cursor() as cur:
            for event in payload.events:
                cur.execute(
                    "INSERT INTO analytics_events (received_at, payload) VALUES (%s, %s)",
                    (now, json.dumps(event)),
                )
            conn.commit()
    return {"ok": True, "received": len(payload.events)}


@app.get("/config")
def get_config(x_api_key: Optional[str] = Header(None)):
    check_auth(x_api_key)
    return REMOTE_CONFIG


@app.get("/health")
def health():
    return {"ok": True}
