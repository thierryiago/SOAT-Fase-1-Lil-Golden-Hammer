# Oficina API

MVP de back-end em .NET 10 para gestao de clientes, pecas e ordens de servico, organizado em camadas com conceitos de DDD.

## Arquitetura

- `Oficina.Domain`: entidades e regras centrais do negocio.
- `Oficina.Application`: casos de uso, DTOs e contratos de repositorio.
- `Oficina.Infrastructure`: implementacoes de persistencia. O MVP usa repositorios em memoria.
- `Oficina.Api`: API RESTful, Swagger e health check.
- `Oficina.Tests`: testes de dominio e aplicacao.

## Rotas principais

- `/api/v1/customers`
- `/api/v1/vehicles`
- `/api/v1/mechanics`
- `/api/v1/workshop-services`
- `/api/v1/parts`
- `/api/v1/stocks`
- `/api/v1/service-orders`
- `/api/v1/service-order-history`
- `GET /health`
- `GET /swagger`

## Docker

```bash
docker compose up --build
```

A API fica em `http://localhost:8080` e a documentacao Swagger em `http://localhost:8080/swagger`.

O `docker-compose.yml` inclui PostgreSQL e a aplicacao executa as migrations automaticamente ao iniciar.

## Visual Studio

Abra `Oficina.sln` na raiz do repositorio. Defina `Oficina.Api` como projeto de inicializacao para executar a API e acessar o Swagger.

## Requisicoes HTTP

O arquivo [`oficina.http`](oficina.http) documenta e permite executar todos os endpoints publicados no Swagger com dados mockados. Ele usa `http://localhost:8080` como URL base e encadeia os IDs retornados pelas requests de criacao.

Antes de usa-lo, inicie a API com `docker compose up --build` e confirme que `http://localhost:8080/health` responde. Em seguida, abra o arquivo no Visual Studio, no JetBrains Rider ou no VS Code com a extensao REST Client e execute o fluxo principal de cima para baixo. As requests de limpeza ficam isoladas no fim do arquivo e devem ser executadas individualmente somente depois das demais.

Se uma execucao for interrompida, dados com documento, e-mail, placa ou codigo unicos podem permanecer no banco. Conclua a limpeza usando os IDs retornados, altere os valores mockados ou recrie o banco de desenvolvimento antes de repetir o fluxo completo.
