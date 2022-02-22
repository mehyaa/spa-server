# Build
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

WORKDIR /src

COPY . .

RUN dotnet publish \
    ./src/Spa.Server/Spa.Server.csproj \
    --output /app \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained false \
    --verbosity minimal

# Image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine

LABEL maintainer="mehyaa@gmail.com"

WORKDIR /app

COPY --from=build /app .

USER root

ENTRYPOINT ["dotnet", "Spa.Server.dll"]

CMD []
