#!/bin/sh
set -e
envsubst '${KEYCLOAK_AUTHORITY}' \
  < /usr/share/nginx/html/env.template.js \
  > /usr/share/nginx/html/env.js
