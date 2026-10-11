FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WTExpCalc.sln .
COPY WTExpCalc.csproj .

RUN dotnet restore WTExpCalc.sln

COPY . .

RUN dotnet publish WTExpCalc.csproj -c Release -o /app/publish --no-restore

RUN BUILD_ID=$(sha256sum \
        /app/publish/wwwroot/css/app.css \
        /app/publish/wwwroot/css/bootstrap/bootstrap.min.css \
        /app/publish/wwwroot/WTExpCalc.styles.css \
        /app/publish/wwwroot/js/techTreeFunctions.js \
        /app/publish/wwwroot/_framework/blazor.webassembly.js \
    | sha256sum | cut -c1-12) \
    && sed -i "s|__BUILD__|${BUILD_ID}|g" /app/publish/wwwroot/index.html \
    && rm -f /app/publish/wwwroot/index.html.gz /app/publish/wwwroot/index.html.br

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

RUN rm -f index.html

COPY --from=build /app/publish/wwwroot .
COPY nginx.conf /etc/nginx/nginx.conf
COPY entrypoint.sh /entrypoint.sh
RUN apk add --no-cache dos2unix
RUN dos2unix /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 80
ENTRYPOINT ["/bin/sh", "/entrypoint.sh"]