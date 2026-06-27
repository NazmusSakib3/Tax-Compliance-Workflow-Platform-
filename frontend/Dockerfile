FROM node:22-alpine AS build
ARG NG_APP_API_BASE_URL=/api
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend .
ENV NG_APP_API_BASE_URL=$NG_APP_API_BASE_URL
RUN node scripts/write-production-environment.js
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.31-alpine
USER root
RUN apk upgrade --no-cache
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/tax-compliance-frontend/browser /usr/share/nginx/html
USER nginx
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost:8080/ || exit 1
CMD ["nginx", "-g", "daemon off;"]
