# Этап 1: сборка статического SPA
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# Пустой VITE_API_URL → фронтенд обращается к /api относительно (same-origin через nginx)
ARG VITE_API_URL=""
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# Этап 2: nginx — отдача статики, проксирование /api, поддержка TLS
FROM nginx:1.27-alpine
COPY --from=build /app/build /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
# Подстановка переменной DOMAIN в nginx.conf перед стартом контейнера
COPY docker-entrypoint.d/40-domain.sh /docker-entrypoint.d/40-domain.sh
RUN chmod +x /docker-entrypoint.d/40-domain.sh
EXPOSE 80 443
