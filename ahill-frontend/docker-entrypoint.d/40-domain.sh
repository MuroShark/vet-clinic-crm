#!/bin/sh
# Подставляет домен из переменной окружения DOMAIN в конфиг nginx перед стартом.
set -e
: "${DOMAIN:=localhost}"
sed -i "s/__DOMAIN__/${DOMAIN}/g" /etc/nginx/conf.d/default.conf
echo "[40-domain] nginx server_name = ${DOMAIN}"
