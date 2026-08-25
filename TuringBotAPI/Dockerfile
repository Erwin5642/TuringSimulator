FROM python:3.12-slim-bookworm

WORKDIR /app

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PORT=3000 \
    RAG_CACHE_PATH=/tmp/embeddings.sqlite

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

# Unity contract on /ask, /session/new, /health. Web tester at /web-tester/.
EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD python -c "import os,urllib.request; urllib.request.urlopen('http://127.0.0.1:%s/health' % os.environ.get('PORT','3000'))"

CMD ["sh", "-c", "exec uvicorn main:app --host 0.0.0.0 --port ${PORT}"]
