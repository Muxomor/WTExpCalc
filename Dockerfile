
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WTExpCalc.sln .
COPY WTExpCalc.csproj .

RUN dotnet restore WTExpCalc.sln

COPY . .

WORKDIR /src/WTExpCalc
RUN dotnet publish WTExpCalc.csproj -c Release -o /app/publish --no-restore

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

RUN rm -f index.html

COPY --from=build /app/publish/wwwroot .
COPY nginx.conf /etc/nginx/nginx.conf
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 80

ENTRYPOINT ["/entrypoint.sh"]