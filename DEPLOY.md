# Руководство по развертыванию

Стек: **web** (nginx: TLS + статические файлы SPA + `/api` reverse proxy) → **backend** (ASP.NET Core) → **sqlserver** (MS SQL Server).  
Только контейнер `web` доступен извне (порты 80/443). Backend и SQL Server остаются внутри сети Docker.

---

## Требования (одноразовая настройка)

1. **DNS:** Создайте A-запись, указывающую `YOUR_DOMAIN` на IP вашего сервера.
2. **Брандмауэр:** Разрешите входящий трафик на порты **80** и **443** (требуется как для HTTPS, так и для HTTP-01 проверки Let's Encrypt):
   ```bash
   sudo ufw allow 80/tcp && sudo ufw allow 443/tcp
   ```
3. **Docker без sudo** для пользователя runner:
   ```bash
   sudo usermod -aG docker $USER   # затем перезайдите в систему
   ```

---

## Вариант 1 — Развертывание через GitHub self-hosted runner (рекомендуется)

Runner использует только **исходящие** подключения к GitHub — для развертывания не нужно открывать входящие порты. Код и сборки находятся в рабочем каталоге runner на сервере.

### 1.1 Подготовка сервера
```bash
sudo usermod -aG docker $USER
newgrp docker
docker ps      # должно выполниться без ошибок доступа
git --version  # должен быть установлен
```

### 1.2 Установка runner
Перейдите в GitHub → репозиторий → **Settings → Actions → Runners → New self-hosted runner → Linux / x64**.  
GitHub покажет команды с **текущей версией и одноразовым токеном** — используйте их. Форма ниже приведена только для справки:
```bash
mkdir -p ~/actions-runner && cd ~/actions-runner
curl -o runner.tar.gz -L https://github.com/actions/runner/releases/download/vX.Y.Z/actions-runner-linux-x64-X.Y.Z.tar.gz
tar xzf runner.tar.gz
./config.sh --url https://github.com/<OWNER>/<REPO> --token <TOKEN_FROM_UI>
```

### 1.3 Запуск в качестве службы
```bash
sudo ./svc.sh install $USER
sudo ./svc.sh start
sudo ./svc.sh status   # должно показать: Active: running
```

### 1.4 Секреты репозитория
Перейдите в GitHub → **Settings → Secrets and variables → Actions → New repository secret**:

| Секрет           | Значение                                    |
|------------------|---------------------------------------------|
| `DOMAIN`         | Ваш домен (A-запись, указывающая на сервер) |
| `CERTBOT_EMAIL`  | Электронная почта для уведомлений Let's Encrypt |
| `SA_PASSWORD`    | Пароль SA для MS SQL (8+ символов, смешанный) |
| `JWT_KEY`        | `openssl rand -base64 48`                   |

### 1.5 Первый деплой и SSL
1. Запушьте в `main` (или запустите через Actions → Deploy → Run workflow). Workflow проверяет код, создает `.env` и запускает `up -d --build`. **При самом первом запуске контейнер `web` (nginx) упадет — сертификат еще не существует — это ожидаемо.** Контейнеры `backend` и `sqlserver` запустятся успешно.
2. На сервере получите сертификат один раз из рабочего каталога runner:
   ```bash
   cd ~/actions-runner/_work/<REPO>/<REPO>  # убедитесь, что docker-compose.prod.yml присутствует
   ./init-letsencrypt.sh
   ```
3. Все последующие `git push` события просто пересобирают стек. Сертификат и `.env` сохраняются между деплоями (workflow использует `clean: false`); обновление обрабатывается автоматически службой `certbot`.

---

## Вариант 2 — Ручное развертывание через SSH

```bash
git clone <repo> vetclinic && cd vetclinic
cp .env.example .env && nano .env      # заполните DOMAIN / CERTBOT_EMAIL / SA_PASSWORD / JWT_KEY
docker compose -f docker-compose.prod.yml build
./init-letsencrypt.sh                  # начальный SSL (поднимает web)
docker compose -f docker-compose.prod.yml up -d
```

Для последующего обновления:
```bash
git pull && docker compose -f docker-compose.prod.yml up -d --build
```

---

## Проверка

```bash
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f backend   # ожидайте "Now listening on: http://[::]:8080" и вывод сидера
curl -k https://YOUR_DOMAIN/api/branches                    # 401 (unauthenticated) — ожидаемо, эндпоинт защищен
```

Откройте `https://YOUR_DOMAIN`, войдите с помощью демо-аккаунта (см. `README.md`).

---

## Полезные команды

- **Логи:** `docker compose -f docker-compose.prod.yml logs -f [web|backend|sqlserver]`
- **Сброс базы данных** (потеря данных): `docker compose -f docker-compose.prod.yml down -v && docker compose -f docker-compose.prod.yml up -d`
- **Тестирование SSL без расхода лимитов Let's Encrypt:** `STAGING=1 ./init-letsencrypt.sh`
- **Секреты** (`.env`) исключены из контроля версий (см. `.gitignore`).
