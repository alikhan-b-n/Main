# Развёртывание IconicU CRM на сервере

Один VPS: Ubuntu 24.04, PostgreSQL, .NET-приложение за nginx, статика фронтенда и
Telegram-бот. Проверено на конфигурации 2 ядра / 4 ГБ, минимум — 2 ГБ и 2 ГБ swap.

Домен ниже — `crm.example.kz`, замените на свой. Он должен уже указывать A-записью
на IP сервера.

## 1. Базовая подготовка

```bash
sudo apt update && sudo apt upgrade -y && sudo apt install -y nginx postgresql ufw certbot python3-certbot-nginx rsync
```

Пользователь, от которого всё работает, без права входа в систему:

```bash
sudo adduser --system --group --home /opt/iconicu --shell /usr/sbin/nologin iconicu
```

```bash
sudo install -d -o iconicu -g iconicu /opt/iconicu/api /opt/iconicu/api/logs /opt/iconicu/web /opt/iconicu/bot
```

Файрвол: наружу только SSH и веб.

```bash
sudo ufw allow OpenSSH && sudo ufw allow 'Nginx Full' && sudo ufw enable
```

Среда выполнения .NET (SDK на сервере не нужен, сборка идёт на вашей машине):

```bash
sudo apt install -y aspnetcore-runtime-9.0
```

## 2. База данных

```bash
sudo -u postgres psql -c "CREATE USER iconicu WITH PASSWORD 'ПРИДУМАЙТЕ_ПАРОЛЬ';" -c "CREATE DATABASE \"LamaCRM\" OWNER iconicu;"
```

PostgreSQL слушает только localhost (по умолчанию), наружу порт закрыт файрволом.
На сервере с 2 ГБ памяти уменьшите аппетит базы в `/etc/postgresql/16/main/postgresql.conf`:
`shared_buffers = 128MB`, `max_connections = 50`, затем `sudo systemctl restart postgresql`.

## 3. Секреты

```bash
sudo install -d -m 750 /etc/iconicu
```

```bash
sudo install -m 600 -o iconicu -g iconicu deploy/api.env.example /etc/iconicu/api.env && sudo nano /etc/iconicu/api.env
```

Заполните пароль базы, домен в `Cors__AllowedOrigins__0`, почту и пароль администратора,
ключ подписи токенов и ключ для бота. Случайные значения:

```bash
openssl rand -base64 48
```

`Auth__Admin__*` — это учётка первого администратора: при старте она создаётся, а если
пароль в файле изменился, обновляется. Остальных пользователей заводит админ в интерфейсе.
`Integrations__TelegramBot__ApiKey` должен совпадать с `CRM_API_KEY` у бота.

## 4. Выкладка приложения

На вашем компьютере, из корня этого репозитория:

```bash
./deploy/publish.sh crm.example.kz
```

Скрипт соберёт API и фронтенд локально, зальёт их в `/opt/iconicu/api` и `/opt/iconicu/web`,
перезапустит сервис и проверит `/health`. Перед первым запуском поставьте сервис:

```bash
sudo cp deploy/lama-api.service /etc/systemd/system/ && sudo systemctl daemon-reload && sudo systemctl enable --now lama-api
```

Миграции применяются самим приложением при старте (`Database__MigrateOnStartup=true`),
поэтому схема базы всегда соответствует коду.

## 5. nginx и сертификат

```bash
sudo cp deploy/nginx-crm.conf /etc/nginx/sites-available/iconicu-crm && sudo ln -s /etc/nginx/sites-available/iconicu-crm /etc/nginx/sites-enabled/
```

Замените `crm.example.kz` на свой домен, проверьте и перезапустите:

```bash
sudo nginx -t && sudo systemctl reload nginx
```

```bash
sudo certbot --nginx -d crm.example.kz
```

Certbot пропишет сертификат и настроит автопродление. Откройте `https://crm.example.kz`
и войдите почтой и паролем из `/etc/iconicu/api.env`.

## 6. Бэкапы

```bash
sudo install -m 755 deploy/backup-db.sh /usr/local/bin/iconicu-backup && sudo install -d -o iconicu -g iconicu /var/backups/iconicu
```

Пароль базы для скрипта — в `~/.pgpass` пользователя `iconicu` (права `600`), строка вида
`127.0.0.1:5432:LamaCRM:iconicu:пароль`. Затем добавьте в `crontab -u iconicu -e`:

```
15 3 * * * /usr/local/bin/iconicu-backup >> /opt/iconicu/api/logs/backup.log 2>&1
```

Дампы лежат 14 дней. Копируйте их и на другую машину: снапшот VPS и бэкап у того же
провайдера пропадут вместе с сервером. Раз в пару месяцев проверяйте восстановление:

```bash
gunzip -c /var/backups/iconicu/LamaCRM_ДАТА.sql.gz | psql -h 127.0.0.1 -U iconicu -d LamaCRM_restore_test
```

## 7. Бот

См. `IconicUTelegramBot/deploy/DEPLOYMENT.md`. Главное: `CRM_WEBHOOK_URL` указывает на
`https://crm.example.kz/api/integrations/telegram/leads`, `CRM_API_KEY` совпадает с ключом
в `/etc/iconicu/api.env`, и бот запущен ровно в одном экземпляре.

## Эксплуатация

| Задача | Команда |
|---|---|
| Статус и логи API | `systemctl status lama-api`, `journalctl -u lama-api -f` |
| Статус и логи бота | `systemctl status iconicu-bot`, `journalctl -u iconicu-bot -f` |
| Обновить CRM | `./deploy/publish.sh crm.example.kz` |
| Перезапустить | `sudo systemctl restart lama-api` |
| Проверка живости | `curl -fsS https://crm.example.kz/health` |

## Что проверить после первого выката

1. `https://crm.example.kz` открывается по HTTPS и просит войти.
2. Вход под администратором работает, в меню есть «Админ-панель».
3. `curl https://crm.example.kz/api/leads` без токена отвечает 401.
4. `https://crm.example.kz/swagger` отвечает 404 (на сервере он закрыт).
5. Бот принимает `/start`, заявка доходит до раздела «Заявки».
6. Наутро в `/var/backups/iconicu` появился дамп.

## Безопасность

- В репозитории нет ни одного пароля: строка подключения, ключи и учётка администратора
  задаются только через `/etc/iconicu/api.env`.
- Вход в CRM обязателен для всех страниц и всех эндпоинтов, кроме приёма заявок от бота
  (у него свой ключ) и `/health`.
- Токен входа живёт 12 часов (`Auth__Jwt__ExpiresHours`) и хранится в браузере,
  поэтому CRM обязательно открывать только по HTTPS.
- Смените пароль администратора после первого входа и заведите отдельные учётки
  менеджерам: общий аккаунт не даст понять, кто что менял.
