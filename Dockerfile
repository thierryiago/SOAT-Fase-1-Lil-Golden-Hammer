# Versões fixadas (tags de patch). Para atualizar: ajustar os dois ARGs abaixo, validar o build
# e conferir que SDK e runtime pertencem à mesma linha (docker run --rm <imagem> --info).
ARG SDK_VERSION=10.0.402
ARG RUNTIME_VERSION=10.0.13
# Preenchidos pelo CI (--build-arg); alimentam as labels OCI.
ARG VERSION=dev
ARG VCS_REF=unknown

FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS build
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
FROM mcr.microsoft.com/dotnet/sdk:${SDK_VERSION} AS ef-tools
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
FROM mcr.microsoft.com/dotnet/runtime-deps:${RUNTIME_VERSION} AS migrations
ARG VERSION
ARG VCS_REF
LABEL org.opencontainers.image.title="oficina-migrations" \
      org.opencontainers.image.source="https://github.com/thierryiago/SOAT-Fase-1-Lil-Golden-Hammer" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.revision="${VCS_REF}"
WORKDIR /app
COPY --from=migrations-build /app/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["/bin/sh", "-c", "if [ -z \"$ConnectionStrings__Database\" ]; then echo 'ConnectionStrings__Database is required' >&2; exit 1; fi; exec ./efbundle --connection \"$ConnectionStrings__Database\""]

FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_VERSION} AS runtime
ARG VERSION
ARG VCS_REF
LABEL org.opencontainers.image.title="oficina-api" \
      org.opencontainers.image.source="https://github.com/thierryiago/SOAT-Fase-1-Lil-Golden-Hammer" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.revision="${VCS_REF}"
WORKDIR /app
# TLS termina no proxy/ingress; o redirecionamento HTTPS fica desligado no container.
ENV ASPNETCORE_URLS=http://+:8080 \
    HttpsRedirection__Enabled=false
EXPOSE 8080

USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Oficina.Api.dll"]
