from __future__ import annotations
import json
import uuid
from datetime import datetime, timezone
from typing import Any
import aio_pika

def envelope(event_type: str, producer: str, payload: dict[str, Any], correlation_id: str) -> dict[str, Any]:
    return {'eventId': str(uuid.uuid4()), 'eventType': event_type, 'occurredAtUtc': datetime.now(timezone.utc).isoformat(), 'correlationId': correlation_id, 'causationId': None, 'producer': producer, 'schemaVersion': 1, 'payload': payload}

class JsonPublisher:

    def __init__(self, exchange: aio_pika.Exchange) -> None:
        self._exchange = exchange

    async def publish(self, routing_key: str, message: dict[str, Any]) -> None:
        body = json.dumps(message, ensure_ascii=False).encode('utf-8')
        await self._exchange.publish(aio_pika.Message(body=body, content_type='application/json'), routing_key=routing_key)
