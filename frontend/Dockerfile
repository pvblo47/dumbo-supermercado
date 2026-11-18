FROM node:24.15.0-alpine AS build
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-fund
COPY frontend/ ./
RUN npm run build

FROM nginx:stable-alpine AS runtime
ENV API_UPSTREAM=http://api:8080
ENV NGINX_ENVSUBST_FILTER=^API_UPSTREAM$
COPY deploy/nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/dumbo-web/browser/ /usr/share/nginx/html/
EXPOSE 80
