FROM python:3.12-slim
WORKDIR /app

COPY pyproject.toml README.md ./
COPY app ./app

RUN pip install --no-cache-dir .
RUN python -m app.train

ENV PYTHONUNBUFFERED=1
CMD ["python", "-m", "app.main"]
