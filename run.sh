#!/usr/bin/env bash
set -e

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Start Ollama if not running
if ! lsof -i :11434 >/dev/null 2>&1; then
  echo "Starting Ollama..."
  (ollama serve >/dev/null 2>&1 &)
  sleep 1
else
  echo "Ollama already running."
fi

# Start embeddings server if not running
if ! lsof -i :8001 >/dev/null 2>&1; then
  echo "Starting embeddings server..."
  cd "$ROOT_DIR/ai"
  # shellcheck disable=SC1091
  source .venv/bin/activate
  (python -m uvicorn embeddings_server:app --host 127.0.0.1 --port 8001 >/dev/null 2>&1 &)
  sleep 1
else
  echo "Embeddings server already running."
fi

# Start .NET API (foreground)
echo "Starting .NET API..."
cd "$ROOT_DIR/Ragline.RagApi"
dotnet run
