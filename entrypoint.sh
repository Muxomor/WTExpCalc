NGINX_ROOT=/usr/share/nginx/html
PLACEHOLDER="__POSTGREST_URL__" # Плейсхолдер, который мы ищем

# Ищем файл appsettings (может быть .json или .Production.json и т.д.)
APPSETTINGS_FILE=$(find $NGINX_ROOT -name 'appsettings*.json' -print -quit)

# Получаем значение переменной окружения POSTGREST_URL
# Если переменная не задана, используем значение по умолчанию
# ВАЖНО: Установите осмысленное значение по умолчанию или оставьте пустым, если это предпочтительнее
POSTGREST_URL_VALUE=${POSTGREST_URL:-"http://localhost:3000"}

if [ -z "$APPSETTINGS_FILE" ]; then
  echo "[ENTRYPOINT] ПРЕДУПРЕЖДЕНИЕ: Файл appsettings*.json не найден в $NGINX_ROOT."
elif [ -z "$POSTGREST_URL" ]; then
  echo "[ENTRYPOINT] ПРЕДУПРЕЖДЕНИЕ: Переменная окружения POSTGREST_URL не установлена. Используется значение по умолчанию: $POSTGREST_URL_VALUE"
  echo "[ENTRYPOINT] Замена '$PLACEHOLDER' на '$POSTGREST_URL_VALUE' в $APPSETTINGS_FILE"
  sed -i "s|$PLACEHOLDER|${POSTGREST_URL_VALUE}|g" "$APPSETTINGS_FILE"
else
  echo "[ENTRYPOINT] Замена '$PLACEHOLDER' на '$POSTGREST_URL_VALUE' (из ENV POSTGREST_URL) в $APPSETTINGS_FILE"
  # Используем '|' как разделитель в sed, чтобы избежать конфликтов с '/' в URL
  sed -i "s|$PLACEHOLDER|${POSTGREST_URL_VALUE}|g" "$APPSETTINGS_FILE"
fi

# Запускаем Nginx в режиме foreground (чтобы контейнер не завершался)
echo "[ENTRYPOINT] Запуск Nginx..."
exec nginx -g 'daemon off;'