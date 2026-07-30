# Oficina API

MVP de back-end em .NET 10 para gestao de clientes, pecas e ordens de servico, organizado em camadas com conceitos de DDD.

## Arquitetura

- `Oficina.Domain`: entidades e regras centrais do negocio.
- `Oficina.Application`: casos de uso, DTOs e contratos de repositorio.
- `Oficina.Infrastructure`: implementacoes de persistencia. O MVP usa repositorios em memoria.
- `Oficina.Api`: API RESTful, Swagger e health check.
- `Oficina.Tests`: testes de dominio e aplicacao.

## Rotas principais

- `GET /api/clientes`
- `POST /api/clientes`
- `GET /api/pecas`
- `POST /api/pecas`
- `GET /api/ordens-servico`
- `POST /api/ordens-servico`
- `POST /api/ordens-servico/{id}/pecas`
- `GET /health`
- `GET /swagger`

## Docker

```bash
docker compose up --build
```

A API fica em `http://localhost:8080` e a documentacao Swagger em `http://localhost:8080/swagger`.

O `docker-compose.yml` inclui PostgreSQL como banco padrao temporario. Como o banco definitivo ainda sera definido, a aplicacao ainda usa persistencia em memoria e a troca futura deve ficar concentrada em `Oficina.Infrastructure`.

## Visual Studio

Abra `Oficina.sln` na raiz do repositorio. Defina `Oficina.Api` como projeto de inicializacao para executar a API e acessar o Swagger.
