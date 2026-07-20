FROM node:22-alpine AS build
RUN corepack enable && corepack prepare pnpm@latest --activate
WORKDIR /app
COPY package.json pnpm-lock.yaml* ./
RUN pnpm install --frozen-lockfile || pnpm install
COPY . .
RUN npx ng build --configuration=production

FROM nginx:alpine AS runtime
COPY --from=build /app/dist/cartera-proyectos/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY docker-entrypoint.d/15-envsubst-env.sh /docker-entrypoint.d/15-envsubst-env.sh
RUN chmod +x /docker-entrypoint.d/15-envsubst-env.sh
EXPOSE 80
