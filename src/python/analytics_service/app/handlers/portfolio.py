from __future__ import annotations

import json
import logging
import uuid
from typing import TYPE_CHECKING

import httpx

from app.messaging import JsonPublisher, envelope
from app.optimization.ga_selector import select_portfolio_ga

if TYPE_CHECKING:
    from app.settings import Settings

log = logging.getLogger("analytics.handlers.portfolio")

EVENT_REQUESTED = "PortfolioOptimizationRequested"
EVENT_COMPLETED = "PortfolioOptimizationCompleted"
EVENT_FAILED = "PortfolioOptimizationFailed"


async def handle_portfolio(body: bytes, settings: Settings, publisher: JsonPublisher) -> None:
    data = json.loads(body.decode("utf-8"))
    payload = data["payload"]
    run_id = uuid.UUID(payload["portfolioRunId"])
    business_date = payload["businessDate"]
    budget_cap = float(payload["budgetCap"])
    correlation_id = data.get("correlationId", str(uuid.uuid4()))
    _ = business_date

    url = f"{settings.portfolio_base_url.rstrip('/')}/internal/v1/portfolio-runs/{run_id}/optimization-input"
    try:
        async with httpx.AsyncClient(timeout=60.0) as client:
            r = await client.get(url, headers={"X-Internal-Api-Key": settings.internal_api_key})
            r.raise_for_status()
            inp = r.json()
    except Exception as ex:
        log.exception("optimization input failed")
        fail = envelope(
            EVENT_FAILED,
            "Analytics",
            {
                "portfolioRunId": str(run_id),
                "errorCode": "OPTIMIZATION_INPUT_ERROR",
                "retryable": True,
                "message": str(ex)[:500],
            },
            correlation_id,
        )
        await publisher.publish(EVENT_FAILED, fail)
        return

    candidates = inp.get("candidates") or []
    try:
        picked_rows, used, profit = select_portfolio_ga(
            candidates,
            budget_cap,
            population_size=settings.ga_population_size,
            generations=settings.ga_generations,
            crossover_rate=settings.ga_crossover_rate,
            mutation_rate=settings.ga_mutation_rate,
            elite_count=settings.ga_elite_count,
            seed=settings.ga_seed,
        )
    except Exception as ex:
        log.exception("GA portfolio failed")
        fail = envelope(
            EVENT_FAILED,
            "Analytics",
            {
                "portfolioRunId": str(run_id),
                "errorCode": "PORTFOLIO_GA_ERROR",
                "retryable": True,
                "message": str(ex)[:500],
            },
            correlation_id,
        )
        await publisher.publish(EVENT_FAILED, fail)
        return

    selections = []
    for row in picked_rows:
        pd_raw = row.get("probabilityOfDefault")
        selections.append(
            {
                "applicationId": row["applicationId"],
                "principal": f"{float(row['allocatedPrincipal']):.2f}",
                "expectedProfit": f"{float(row['expectedProfitAllocated']):.2f}",
                "probabilityOfDefault": None if pd_raw is None else str(pd_raw),
            }
        )

    completed = envelope(
        EVENT_COMPLETED,
        "Analytics",
        {
            "portfolioRunId": str(run_id),
            "objectiveValue": f"{profit:.6f}",
            "usedBudget": f"{used:.6f}",
            "expectedPortfolioProfit": f"{profit:.6f}",
            "selectedApplicationIds": [s["applicationId"] for s in selections],
            "selections": selections,
        },
        correlation_id,
    )
    await publisher.publish(EVENT_COMPLETED, completed)
