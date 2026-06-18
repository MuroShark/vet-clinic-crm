#!/bin/bash
# Первичное получение SSL-сертификата Let's Encrypt.
# Запускать ОДИН раз после первого деплоя, когда DNS уже указывает на сервер и порт 80 открыт.
set -e

if [ ! -f .env ]; then
  echo "Файл .env не найден — скопируйте из .env.example и заполните."
  exit 1
fi
set -a; . ./.env; set +a

COMPOSE="docker compose -f docker-compose.prod.yml"
DATA="./certbot"
RSA=4096
# STAGING=1 — тестовый сертификат (не расходует лимиты Let's Encrypt). Для прода оставьте 0.
STAGING="${STAGING:-0}"

echo "### Домен: $DOMAIN | Email: $CERTBOT_EMAIL | Staging: $STAGING"
mkdir -p "$DATA/conf/live/$DOMAIN" "$DATA/www"

echo "### Создаю временный самоподписанный сертификат (чтобы nginx мог стартовать)..."
$COMPOSE run --rm --entrypoint "\
  openssl req -x509 -nodes -newkey rsa:$RSA -days 1 \
    -keyout '/etc/letsencrypt/live/$DOMAIN/privkey.pem' \
    -out '/etc/letsencrypt/live/$DOMAIN/fullchain.pem' \
    -subj '/CN=localhost'" certbot

echo "### Запускаю nginx..."
$COMPOSE up -d web
sleep 3

echo "### Удаляю временный сертификат..."
$COMPOSE run --rm --entrypoint "\
  rm -Rf /etc/letsencrypt/live/$DOMAIN /etc/letsencrypt/archive/$DOMAIN /etc/letsencrypt/renewal/$DOMAIN.conf" certbot

echo "### Запрашиваю настоящий сертификат..."
STAGING_ARG=""
[ "$STAGING" != "0" ] && STAGING_ARG="--staging"
$COMPOSE run --rm --entrypoint "\
  certbot certonly --webroot -w /var/www/certbot $STAGING_ARG \
    --email $CERTBOT_EMAIL -d $DOMAIN \
    --rsa-key-size $RSA --agree-tos --no-eff-email --force-renewal" certbot

echo "### Перезагружаю nginx..."
$COMPOSE exec web nginx -s reload
echo "### Готово. Сертификат для $DOMAIN установлен."
