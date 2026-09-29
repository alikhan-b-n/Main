#!/usr/bin/env python3
"""Получение refresh token для календаря IconicU (одноразовая операция).

Нужен только стандартный Python 3.9+.

    python deploy/get-google-refresh-token.py путь/к/client_secret_....json

Файл client_secret_*.json скачивается кнопкой загрузки рядом с OAuth-клиентом
в Google Cloud Console. Без аргумента скрипт спросит Client ID и Client secret
вручную. Скрипт откроет окно согласия Google в браузере, получит refresh token,
запишет его в user-secrets проекта Lama.Api и в файл deploy/.env.google (для сервера).
На экран токен не выводится, и никуда не отправляется: обмен кода идёт напрямую
с oauth2.googleapis.com, всё остальное остаётся на вашей машине.

Что сделать заранее в https://console.cloud.google.com (бесплатно):
  1. Создать проект, например "IconicU CRM".
  2. APIs & Services → Library → включить "Google Calendar API".
  3. OAuth consent screen: тип External, заполнить название и почту,
     добавить scope'ы calendar.events и calendar.readonly,
     затем Publishing status → PUBLISH APP (в режиме Testing токен живёт 7 дней).
  4. Credentials → Create credentials → OAuth client ID → тип **Desktop app**.
     Полученные Client ID и Client secret понадобятся скрипту.

Войти в окне согласия нужно тем аккаунтом, чей календарь используется.
Предупреждение "Google hasn't verified this app" ожидаемо: приложение личное,
проверка Google для него не требуется. Нажмите Advanced → Go to ... (unsafe).
"""

from __future__ import annotations

import http.server
import io
import json
import os
import pathlib
import secrets
import socket
import subprocess
import sys
import threading
import time
import urllib.error
import urllib.parse
import urllib.request
import webbrowser

AUTH_URL = "https://accounts.google.com/o/oauth2/v2/auth"
TOKEN_URL = "https://oauth2.googleapis.com/token"
# events — создавать и отменять встречи, readonly — видеть занятое время (freebusy)
SCOPES = (
    "https://www.googleapis.com/auth/calendar.events "
    "https://www.googleapis.com/auth/calendar.readonly"
)

PAGE = """<!doctype html><html lang="ru"><meta charset="utf-8">
<title>IconicU</title>
<body style="font-family:system-ui;padding:3rem;max-width:32rem">
<h1>{title}</h1><p>{text}</p></body></html>"""


class Handler(http.server.BaseHTTPRequestHandler):
    """Ловит один редирект от Google и складывает код в Handler.result."""

    result: dict[str, str] = {}

    def do_GET(self) -> None:  # noqa: N802 — имя задано базовым классом
        query = urllib.parse.parse_qs(urllib.parse.urlparse(self.path).query)
        Handler.result = {k: v[0] for k, v in query.items()}

        ok = "code" in Handler.result
        body = PAGE.format(
            title="Готово ✅" if ok else "Не получилось",
            text=(
                "Доступ выдан. Вернитесь в терминал — там refresh token."
                if ok
                else f"Google вернул ошибку: {Handler.result.get('error', 'неизвестно')}"
            ),
        )
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.end_headers()
        self.wfile.write(body.encode())

    def log_message(self, *args) -> None:
        """Тишина: служебные строки http.server только мешают."""


def free_port() -> int:
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


def from_file(path: str) -> tuple[str, str]:
    """Читает client_secret_*.json, скачанный из Google Cloud Console."""
    try:
        data = json.load(io.open(path, encoding="utf-8"))
    except (OSError, ValueError) as error:
        sys.exit(f"Не удалось прочитать {path}: {error}")

    # Desktop app лежит под ключом "installed", Web application — под "web"
    section = data.get("installed") or data.get("web")
    if not section or not section.get("client_id") or not section.get("client_secret"):
        sys.exit(f"В {path} нет client_id и client_secret — это не файл OAuth-клиента")

    print(f"Клиент из файла: ...{section['client_id'][-30:]}")
    print()
    return section["client_id"], section["client_secret"]


def ask(label: str) -> str:
    value = input(f"{label}: ").strip()
    if not value:
        sys.exit(f"{label} обязателен")
    return value


def exchange(client_id: str, client_secret: str, code: str, redirect_uri: str) -> dict:
    data = urllib.parse.urlencode(
        {
            "code": code,
            "client_id": client_id,
            "client_secret": client_secret,
            "redirect_uri": redirect_uri,
            "grant_type": "authorization_code",
        }
    ).encode()
    try:
        with urllib.request.urlopen(urllib.request.Request(TOKEN_URL, data=data)) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        sys.exit(f"Google отклонил запрос: {error.read().decode(errors='replace')}")


def save(client_id: str, client_secret: str, refresh_token: str) -> None:
    """Пишет значения в файл рядом с compose — на экран они не попадают.

    Токен даёт полный доступ к календарю, поэтому он не должен оседать в истории
    терминала, в буфере обмена и тем более в переписке.
    """
    root = pathlib.Path(__file__).resolve().parent.parent
    values = {
        "GOOGLE_CLIENT_ID": client_id,
        "GOOGLE_CLIENT_SECRET": client_secret,
        "GOOGLE_REFRESH_TOKEN": refresh_token,
    }

    env_file = root / "deploy" / ".env.google"
    env_file.write_text(
        "# Скопируйте эти строки в deploy/.env на сервере. Файл не попадает в git.\n"
        + "".join(f"{key}={value}\n" for key, value in values.items()),
        encoding="utf-8",
    )

    print("\n" + "=" * 70)
    print(f"Токен получен (…{refresh_token[-6:]}) и сохранён в файл:")
    print(f"  {env_file}")
    print("\nДальше:")
    print("  * на сервере — скопируйте три строки из файла в deploy/.env;")
    print("  * локально — запустите deploy/set-google-secrets.ps1,")
    print("    он положит те же значения в user-secrets проекта Lama.Api.")
    print("\nФайл секретный: не коммитьте его и не пересылайте в мессенджерах.")
    print("=" * 70)


def main() -> None:
    # Консоль Windows не всегда умеет печатать стрелки и тире: заменяем, но не падаем
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(errors="replace")
        except (AttributeError, OSError):
            pass

    if len(sys.argv) > 1:
        client_id, client_secret = from_file(sys.argv[1])
    else:
        print(__doc__)
        client_id = ask("Client ID")
        client_secret = ask("Client secret")

    port = free_port()
    redirect_uri = f"http://127.0.0.1:{port}"
    state = secrets.token_urlsafe(16)

    url = f"{AUTH_URL}?" + urllib.parse.urlencode(
        {
            "client_id": client_id,
            "redirect_uri": redirect_uri,
            "response_type": "code",
            "scope": SCOPES,
            # offline + consent — иначе Google не выдаст refresh token при повторном входе
            "access_type": "offline",
            "prompt": "consent",
            "state": state,
        }
    )

    server = http.server.HTTPServer(("127.0.0.1", port), Handler)
    threading.Thread(target=server.handle_request, daemon=True).start()

    print("\nОткрываю окно согласия. Если браузер не открылся — скопируйте ссылку:\n")
    print(url, "\n")
    webbrowser.open(url)

    # Ждём редирект от Google не дольше пяти минут
    deadline = time.monotonic() + 300
    while not Handler.result and time.monotonic() < deadline:
        time.sleep(0.1)

    if "code" not in Handler.result:
        sys.exit(f"Код не получен: {Handler.result.get('error', 'истекло время ожидания')}")
    if Handler.result.get("state") != state:
        sys.exit("Не совпал state — запрос пришёл не из этого окна, начните заново")

    tokens = exchange(client_id, client_secret, Handler.result["code"], redirect_uri)
    refresh_token = tokens.get("refresh_token")
    if not refresh_token:
        sys.exit(
            "Google не вернул refresh token. Обычно это значит, что доступ уже выдавался: "
            "отзовите его на https://myaccount.google.com/permissions и запустите скрипт снова."
        )

    save(client_id, client_secret, refresh_token)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\nОтменено.")
