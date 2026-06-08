from __future__ import annotations
import asyncio
import logging
import aio_pika
from app.handlers.portfolio import EVENT_REQUESTED as PORT_REQ
from app.handlers.portfolio import handle_portfolio
from app.handlers.scoring import EVENT_REQUESTED as SCORE_REQ
from app.handlers.scoring import handle_scoring
from app.messaging import JsonPublisher
from app.ml.inference import ScoringEngine
from app.settings import Settings
logging.basicConfig(level=logging.INFO)
log = logging.getLogger('analytics')

async def consume_loop(settings: Settings) -> None:
    artifacts = settings.resolved_artifacts_path()
    engine = ScoringEngine(artifacts, top_k=settings.scoring_top_factors)
    engine.load()
    connection = await aio_pika.connect_robust(host=settings.rabbit_host, port=settings.rabbit_port, login=settings.rabbit_user, password=settings.rabbit_password)
    channel = await connection.channel()
    await channel.set_qos(prefetch_count=10)
    ex = await channel.declare_exchange(settings.exchange, aio_pika.ExchangeType.TOPIC, durable=True)
    publisher = JsonPublisher(ex)
    scoring_q = await channel.declare_queue('analytics.scoring.work', durable=True)
    await scoring_q.bind(ex, routing_key=SCORE_REQ)
    portfolio_q = await channel.declare_queue('analytics.portfolio.work', durable=True)
    await portfolio_q.bind(ex, routing_key=PORT_REQ)

    async def on_scoring(msg: aio_pika.IncomingMessage):
        async with msg.process():
            await handle_scoring(msg.body, settings, engine, publisher)

    async def on_portfolio(msg: aio_pika.IncomingMessage):
        async with msg.process():
            await handle_portfolio(msg.body, settings, publisher)
    await scoring_q.consume(on_scoring)
    await portfolio_q.consume(on_portfolio)
    log.info('Analytics workers ready (XGBoost+SHAP scoring, GA portfolio).')
    await asyncio.Future()

def main() -> None:
    settings = Settings()
    asyncio.run(consume_loop(settings))
if __name__ == '__main__':
    main()
