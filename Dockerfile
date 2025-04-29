# Файл: Dockerfile

# --- Стадия сборки ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файл решения и файл проекта в корень /src
COPY WTExpCalc.sln .
COPY WTExpCalc.csproj .

# Восстанавливаем зависимости для всего решения
RUN dotnet restore WTExpCalc.sln

# Копируем весь остальной код ПОСЛЕ restore
COPY . .

RUN dotnet publish WTExpCalc.csproj -c Release -o /app/publish --no-restore # Эта команда выполнится в /src

# --- Стадия выполнения ---
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Используем -f чтобы не было ошибки, если index.html уже удален
RUN rm -f index.html

COPY --from=build /app/publish/wwwroot .
COPY nginx.conf /etc/nginx/nginx.conf
COPY entrypoint.sh /entrypoint.sh
RUN apk add --no-cache dos2unix
RUN dos2unix /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 80
ENTRYPOINT ["/bin/sh", "/entrypoint.sh"]