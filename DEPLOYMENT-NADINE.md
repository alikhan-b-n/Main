# Развёртывание CRM Noah's Adventures

Ставится на тот же сервер, что и IconicU, отдельным набором контейнеров: своя база,
API и веб, без бота. Наружу CRM выходит через Caddy, который уже работает у IconicU:
он получает сертификат для нового поддомена и проксирует его в веб-контейнер Nadine.
IconicU при этом не перезапускается и не пересобирается.

Файлы: `deploy/nadine/docker-compose.yml`, `.env.example`, `nadine.caddy.example`.

## 1. Домен

У регистратора добавьте A-запись поддомена (например, `crm-nadine.ваш-домен`) на IP
сервера. Проверка с любого компьютера:

```bash
dig +short crm-nadine.ваш-домен
```

Должен вернуться IP сервера. Пока его нет, Caddy не получит сертификат.

## 2. Код

```bash
sudo install -d -o $USER -g $USER /opt/nadine && cd /opt/nadine
```

```bash
git clone -b Nadine_edition https://github.com/alikhan-b-n/Main.git && git clone -b Nadine_edition https://github.com/alikhan-b-n/lama-crm-redesign.git
```

## 3. Настройки

```bash
cd /opt/nadine/Main/deploy/nadine && cp .env.example .env && chmod 600 .env && nano .env
```

Заполните пароль базы, `PUBLIC_URL` (`https://` + поддомен), `JWT_KEY`, почту и пароль
Nadine. Случайные значения:

```bash
openssl rand -base64 24 && openssl rand -base64 48
```

`BRIDGE_IP` — адрес моста Docker, обычно `172.17.0.1`. Проверка:

```bash
ip -4 addr show docker0
```

## 4. Запуск

```bash
cd /opt/nadine/Main/deploy/nadine && docker compose up -d --build
```

Сборка занимает несколько минут. Проверка, что API поднялся и база мигрировала:

```bash
docker compose ps && docker compose logs --tail=30 api
```

```bash
curl -fsS http://172.17.0.1:8081/health
```

## 5. Домен в Caddy IconicU

Caddy IconicU должен быть с поддержкой `conf.d`:

```bash
grep -n "conf.d" /opt/iconicu/Main/deploy/Caddyfile
```

Если строки нет, обновите IconicU (`git pull` в `/opt/iconicu/Main`) и перезапустите
только Caddy: `docker compose -f docker-compose.yml -f docker-compose.tls.yml up -d caddy`.

Подключите сайт Nadine. В первой строке файла замените домен на настоящий:

```bash
cp /opt/nadine/Main/deploy/nadine/nadine.caddy.example /opt/iconicu/Main/deploy/conf.d/nadine.caddy && nano /opt/iconicu/Main/deploy/conf.d/nadine.caddy
```

```bash
cd /opt/iconicu/Main/deploy && docker compose -f docker-compose.yml -f docker-compose.tls.yml exec caddy caddy reload --config /etc/caddy/Caddyfile
```

Через минуту CRM открывается по `https://` + поддомен. Если нет — смотрите
`docker compose -f docker-compose.yml -f docker-compose.tls.yml logs --tail=50 caddy`.

## 6. Перенос Excel

На своём компьютере, в папке `Main`:

```bash
python3 -I tools/stockist-import/excel_to_import.py "Noahs Adventures- customer list.xlsx" stockists.json
```

Вход администратора (пароль спросит, на экран не выводится) и импорт:

```bash
read -rp "Email: " EMAIL && read -rsp "Password: " PASS && echo && TOKEN=$(curl -fsS https://crm-nadine.ваш-домен/api/auth/login -H 'Content-Type: application/json' -d "{\"email\":\"$EMAIL\",\"password\":\"$PASS\"}" | python3 -c 'import json,sys;print(json.load(sys.stdin)["token"])') && unset PASS
```

```bash
curl -fsS https://crm-nadine.ваш-домен/api/stockists/import -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' --data @stockists.json
```

Ответ `{"imported":56,"skipped":[]}`. Повторный запуск ничего не дублирует.
Файл `stockists.json` — данные клиента: после импорта удалите его.

## 7. Бэкапы

Тот же скрипт, что у IconicU, со своими путями:

```bash
sudo install -m 755 /opt/nadine/Main/deploy/backup-db-docker.sh /usr/local/bin/nadine-backup && sudo install -d -o $USER -g $USER /var/backups/nadine
```

В `crontab -e`:

```
30 3 * * * COMPOSE_DIR=/opt/nadine/Main/deploy/nadine BACKUP_DIR=/var/backups/nadine /usr/local/bin/nadine-backup >> /var/log/nadine-backup.log 2>&1
```

## 8. Обновление

```bash
cd /opt/nadine/Main && git pull && cd ../lama-crm-redesign && git pull && cd ../Main/deploy/nadine && docker compose up -d --build
```

Миграции API применяет сам при старте.

## Эксплуатация

| Задача | Команда (в `/opt/nadine/Main/deploy/nadine`) |
|---|---|
| Статус | `docker compose ps` |
| Логи | `docker compose logs -f api` |
| Консоль базы | `docker compose exec db psql -U nadine -d NadineCRM` |
| Остановить | `docker compose down` (данные в томе `nadine_pgdata` останутся) |

Наборы IconicU и Nadine независимы: `docker compose down` в одной папке не трогает другую.
