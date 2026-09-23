#!/usr/bin/env bash
# Builds the API and the frontend on your machine and ships them to the server.
# Building locally keeps the 2–4 GB VPS free of Node and the .NET SDK.
#
#   ./deploy/publish.sh crm.example.kz
#
# Expects: SSH access as a sudo-capable user, the frontend checked out next to
# this repository (override with WEB_DIR), and the server prepared per DEPLOYMENT.md.
set -euo pipefail

SERVER="${1:?usage: publish.sh <user@host or host>}"
WEB_DIR="${WEB_DIR:-../lama-crm-redesign}"
API_DIR="/opt/iconicu/api"
WEB_TARGET="/opt/iconicu/web"

here=$(cd "$(dirname "$0")/.." && pwd)
cd "$here"

echo "==> Building the API"
rm -rf artifacts/api
dotnet publish Lama.Api -c Release -o artifacts/api

echo "==> Building the frontend"
(cd "$WEB_DIR" && npm ci && npm run build)

echo "==> Uploading"
# --delete keeps the server free of files removed from the build
rsync -az --delete artifacts/api/ "$SERVER:/tmp/iconicu-api/"
rsync -az --delete "$WEB_DIR/dist/" "$SERVER:/tmp/iconicu-web/"

echo "==> Switching over"
ssh "$SERVER" "sudo systemctl stop lama-api \
  && sudo rsync -a --delete /tmp/iconicu-api/ $API_DIR/ \
  && sudo rsync -a --delete /tmp/iconicu-web/ $WEB_TARGET/ \
  && sudo chown -R iconicu:iconicu $API_DIR $WEB_TARGET \
  && sudo systemctl start lama-api \
  && sleep 3 && curl -fsS http://127.0.0.1:5185/health && echo"

echo "==> Done"
