FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Oficina.Domain/Oficina.Domain.csproj src/Oficina.Domain/
COPY src/Oficina.Application/Oficina.Application.csproj src/Oficina.Application/
COPY src/Oficina.Infrastructure/Oficina.Infrastructure.csproj src/Oficina.Infrastructure/
COPY src/Oficina.Api/Oficina.Api.csproj src/Oficina.Api/

RUN dotnet restore src/Oficina.Api/Oficina.Api.csproj

COPY . .
RUN dotnet publish src/Oficina.Api/Oficina.Api.csproj -c Release -o /app/publish --no-restore

# dotnet-ef em stage próprio para não ser reinstalado a cada mudança de código.
# Manter EF_VERSION igual à versão do EF Core referenciada nos projetos.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS ef-tools
ARG EF_VERSION=10.0.10
RUN dotnet tool install --tool-path /tools dotnet-ef --version ${EF_VERSION}

# Gera o executável de migração (efbundle). Nenhuma migration é aplicada aqui:
# o banco só é acessado quando a imagem "migrations" é executada.
FROM build AS migrations-build
COPY --from=ef-tools /tools /tools
ENV PATH="${PATH}:/tools"
# O bundle self-contained exige os assets restaurados também para o RID alvo.
RUN dotnet restore src/Oficina.Api/Oficina.Api.csproj --runtime linux-x64
RUN dotnet ef migrations bundle \
      --project src/Oficina.Infrastructure/Oficina.Infrastructure.csproj \
      --startup-project src/Oficina.Api/Oficina.Api.csproj \
      --configuration Release \
      --runtime linux-x64 \
      --self-contained \
      --output /app/efbundle \
      --force

# Imagem de migração: docker build --target migrations
# Requer ConnectionStrings__Database; aplica as migrations pendentes e encerra.
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrations
WORKDIR /app
COPY --from=migrations-build /app/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["/bin/sh", "-c", "if [ -z \"$ConnectionStrings__Database\" ]; then echo 'ConnectionStrings__Database is required' >&2; exit 1; fi; exec ./efbundle --connection \"$ConnectionStrings__Database\""]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \n    HttpsRedirection__Enabled=false
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Oficina.Api.dll"]
