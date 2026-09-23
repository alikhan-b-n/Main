# Развёртывание через Docker Compose

Четыре контейнера: PostgreSQL, API, Telegram-бот и веб (nginx с собранным фронтендом,
он же проксирует `/api` в API). Наружу смотрит только веб; при варианте с HTTPS —
Caddy, который сам получает и продлевает сертификат.

Файлы лежат в `deploy/`: `docker-compose.yml`, `docker-compose.tls.yml`,
`docker-compose.http.yml`, `Caddyfile`, `.env.docker.example`, `backup-db-docker.sh`.

## 1. Подготовка сервера

Всё ниже — на свежей Ubuntu 22.04/24.04 или Debian 12/13 под пользователем с sudo
(под root `sudo` можно опускать).

Обновление системы и базовые пакеты:

```bash
sudo apt update && sudo apt upgrade -y && sudo apt install -y git curl ca-certificates
```

Swap на 2 ГБ — обязателен, если памяти 2 ГБ, полезен и на 4 ГБ (сборка .NET требовательна):

```bash
sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile && echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

Docker из официального репозитория. У Ubuntu и Debian разные пути, поэтому дистрибутив
берётся из `/etc/os-release` — команды одинаковы для обоих:

```bash
. /etc/os-release && sudo install -m 0755 -d /etc/apt/keyrings && sudo curl -fsSL "https://download.docker.com/linux/$ID/gpg" -o /etc/apt/keyrings/docker.asc && sudo chmod a+r /etc/apt/keyrings/docker.asc
```

```bash
. /etc/os-release && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/$ID $VERSION_CODENAME stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
```

```bash
sudo apt update && sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

Если `apt update` ругается `404 ... does not have a Release file` — в `docker.list`
попал чужой дистрибутив. Посмотрите `cat /etc/apt/sources.list.d/docker.list`: путь
должен быть `linux/debian` на Debian и `linux/ubuntu` на Ubuntu, а кодовое имя —
совпадать с `lsb_release -cs`. Перезапишите файл командой выше и повторите.

Чтобы не писать sudo перед каждой командой docker (под root не нужно):

```bash
sudo usermod -aG docker $USER
```

После этого **перезайдите по SSH** — иначе группа не применится. Проверка:

```bash
docker run --rm hello-world && docker compose version
```

Файрвол: наружу только SSH и веб.

```bash
sudo ufw allow OpenSSH && sudo ufw allow 80 && sudo ufw allow 443 && sudo ufw --force enable
```

На Debian ufw обычно не установлен: `sudo apt install -y ufw`.

## 2. Код

Три репозитория рядом — на эти пути рассчитаны настройки по умолчанию:

```bash
sudo install -d -o $USER -g $USER /opt/iconicu && cd /opt/iconicu
```

```bash
git clone https://github.com/alikhan-b-n/Main.git && git clone https://github.com/alikhan-b-n/lama-crm-redesign.git && git clone https://github.com/alikhan-b-n/IconicUTelegramBot.git
```

Ветки: `IconicU_edition` в первых двух, `feature/lead-qualification-survey` в боте.

```bash
cd Main && git checkout IconicU_edition && cd ../lama-crm-redesign && git checkout IconicU_edition && cd ../IconicUTelegramBot && git checkout feature/lead-qualification-survey
```

## 3. Настройки

```bash
cd /opt/iconicu/Main/deploy && cp .env.docker.example .env && chmod 600 .env && nano .env
```

Заполните: пароль базы, домен и почту для сертификата, `PUBLIC_URL`, ключ подписи
токенов, почту и пароль администратора, токен бота и общий `CRM_API_KEY`.
Случайные значения для `JWT_KEY` и `CRM_API_KEY`:

```bash
openssl rand -base64 48 && openssl rand -base64 32
```

`.env` в git не попадает и при обновлении кода не затрагивается.

## 4. Запуск

Домен должен уже указывать A-записью на сервер — иначе Caddy не получит сертификат.

```bash
docker compose -f docker-compose.yml -f docker-compose.tls.yml up -d --build
```

Сборка занимает несколько минут: .NET и фронтенд собираются внутри образов.
Проверка:

```bash
docker compose ps && docker compose logs --tail=30 api bot
```

```bash
curl -fsS https://crm.example.kz/health
```

Откройте `https://crm.example.kz` и войдите почтой и паролем из `.env`.

Без сертификата (проверка на IP, во внутренней сети):

```bash
docker compose -f docker-compose.yml -f docker-compose.http.yml up -d --build
```

Так CRM в интернет выставлять нельзя: токен входа пойдёт незашифрованным.

## 5. Бэкапы

```bash
sudo install -m 755 deploy/backup-db-docker.sh /usr/local/bin/iconicu-backup && sudo install -d -o $USER -g $USER /var/backups/iconicu
```

В `crontab -e`:

```
15 3 * * * COMPOSE_DIR=/opt/iconicu/Main/deploy /usr/local/bin/iconicu-backup >> /var/log/iconicu-backup.log 2>&1
```

Том Docker — это не бэкап: он умрёт вместе с сервером. Копируйте дампы на другую
машину и раз в пару месяцев проверяйте восстановление:

```bash
gunzip -c /var/backups/iconicu/LamaCRM_ДАТА.sql.gz | docker compose exec -T db psql -U iconicu -d LamaCRM
```

## 6. Обновление

```bash
cd /opt/iconicu/Main && git pull && cd ../lama-crm-redesign && git pull && cd ../IconicUTelegramBot && git pull
```

```bash
cd /opt/iconicu/Main/deploy && docker compose -f docker-compose.yml -f docker-compose.tls.yml up -d --build
```

Миграции базы API применяет сам при старте, отдельная команда не нужна.

## Эксплуатация

| Задача | Команда |
|---|---|
| Статус | `docker compose ps` |
| Логи | `docker compose logs -f api` (или `bot`, `web`, `db`) |
| Перезапуск одного сервиса | `docker compose restart api` |
| Консоль базы | `docker compose exec db psql -U iconicu -d LamaCRM` |
| Остановить всё | `docker compose down` (тома с данными останутся) |

## Что важно помнить

- **Бот только в одном экземпляре.** Telegram разрешает одного получателя обновлений
  на токен. Не запускайте `docker compose up --scale bot=2` и остановите бота на
  своём компьютере, иначе сообщения будут теряться.
- **Порт базы наружу не открыт** и открывать его не нужно: API ходит к ней внутри сети Docker.
- **Секреты только в `.env`.** В образы они не попадают, поэтому один и тот же образ
  можно спокойно собирать где угодно.
- **Данные живут в томах** `pgdata` (база) и `botdata` (копия заявок в `leads.jsonl`).
  `docker compose down -v` удалит их вместе с данными — эта команда не для продакшена.
- **Фронтенд обращается к `/api` на своём домене**, адрес API внутрь образа не вшит:
  один и тот же образ работает на любом домене.
