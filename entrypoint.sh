NGINX_ROOT=/usr/share/nginx/html


# Запускаем Nginx в режиме foreground (чтобы контейнер не завершался)
echo "[ENTRYPOINT] Запуск Nginx..."
exec nginx -g 'daemon off;'