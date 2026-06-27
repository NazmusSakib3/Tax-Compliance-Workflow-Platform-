FROM node:20-alpine AS build
ARG NG_APP_API_BASE_URL=/api
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend .
ENV NG_APP_API_BASE_URL=$NG_APP_API_BASE_URL
RUN node scripts/write-production-environment.js
RUN npm run build

FROM nginx:1.27-alpine
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/tax-compliance-frontend/browser /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

