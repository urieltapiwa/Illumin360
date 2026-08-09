# syntax=docker/dockerfile:1
# Production build of the Business portal SPA: vite build → static assets served by nginx.
ARG NODE_VERSION=22

FROM node:${NODE_VERSION}-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# When served behind the Business BFF, build in BFF mode so auth.ts targets /bff/* (tokens stay server-side).
ARG VITE_USE_BFF=1
ENV VITE_USE_BFF=${VITE_USE_BFF}
RUN npm run build

# Static runtime — nginx serving the built SPA with history fallback.
FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
