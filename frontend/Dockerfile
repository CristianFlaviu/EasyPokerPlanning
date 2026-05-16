# syntax=docker/dockerfile:1.7
# Multi-stage build for Angular frontend served by nginx
# Build context: frontend/

FROM node:22-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration=production

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/poker-planning-frontend/browser /usr/share/nginx/html

EXPOSE 8080

# nginx config listens on 8080 to match Fly/most PaaS conventions
CMD ["nginx", "-g", "daemon off;"]
