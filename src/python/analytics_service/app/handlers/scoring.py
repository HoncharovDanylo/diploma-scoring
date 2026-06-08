from __future__ import annotations

import json
import logging
import uuid
from typing import TYPE_CHECKING

import httpx

from app.messaging import JsonPublisher, envelope

if TYPE_CHECKING:
    from app.ml.inference import ScoringEngine
    from app.settings import Settings

log = logging.getLogger("analytics.handlers.scoring")

EVENT_REQUESTED = "ScoringRequested"
EVENT_COMPLETED = "ScoringCompleted"
EVENT_FAILED = "ScoringFailed"


async def handle_scoring(body: bytes, settings: Settings, engine: ScoringEngine, publisher: JsonPublisher) -> None:
    data = json.loads(body.decode("utf-8"))
    payload = data["payload"]
    app_id = payload["applicationId"]
    attempt_id = payload["scoringAttemptId"]
    correlation_id = data.get("correlationId", str(uuid.uuid4()))

    url = f"{settings.origination_base_url.rstrip('/')}/internal/v1/applications/{app_id}/scoring-feature-package"
    try:
        async with httpx.AsyncClient(timeout=30.0) as client:
            r = await client.get(url, headers={"X-Internal-Api-Key": settings.internal_api_key})
            r.raise_for_status()
            package = r.json()
    except Exception as ex:
        log.exception("feature package failed")
        fail = envelope(
            EVENT_FAILED,
            "Analytics",
            {
                "scoringAttemptId": attempt_id,
                "applicationId": app_id,
                "errorCode": "FEATURE_PACKAGE_ERROR",
                "retryable": True,
                "message": str(ex)[:500],
            },
            correlation_id,
        )
        await publisher.publish(EVENT_FAILED, fail)
        return

    try:
        pd, final, factors = engine.predict_with_explanation(package, settings.scoring_pd_threshold)
    except Exception as ex:
        log.exception("scoring inference failed")
        fail = envelope(
            EVENT_FAILED,
            "Analytics",
            {
                "scoringAttemptId": attempt_id,
                "applicationId": app_id,
                "errorCode": "SCORING_INFERENCE_ERROR",
                "retryable": True,
                "message": str(ex)[:500],
            },
            correlation_id,
        )
        await publisher.publish(EVENT_FAILED, fail)
        return

    completed = envelope(
        EVENT_COMPLETED,
        "Analytics",
        {
            "scoringAttemptId": attempt_id,
            "applicationId": app_id,
            "probabilityOfDefault": f"{pd:.6f}",
            "finalDecision": final,
            "modelId": engine.model_id,
            "modelVersion": engine.model_version,
            "topFactors": factors,
        },
        correlation_id,
    )
    await publisher.publish(EVENT_COMPLETED, completed)
