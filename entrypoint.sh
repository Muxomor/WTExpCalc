NGINX_ROOT=/usr/share/nginx/html

echo "Запуск Nginx..."
exec nginx -g 'daemon off;'