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
- `/api/v1/services`
- `/api/v1/parts`
- `/api/v1/stocks`
- `/api/v1/service-orders`
- `/api/v1/service-order-history`
- `POST /api/v1/notifications/email`
- `GET /health`
- `POST /api/v1/auth/token`
- `GET /swagger`

## JWT administrativo

As rotas `/api/v1/*` são administrativas e exigem um JWT Bearer, com exceção de
`POST /api/v1/auth/token`. Nesta fase, esse endpoint emite um token técnico sem
usuário, senha, roles ou login.

Para simplificar os testes locais, `appsettings.json` contém uma chave descartável de
desenvolvimento. Portanto, basta iniciar a aplicação:

```powershell
docker compose up --build
```

Essa chave não deve ser reutilizada em homologação ou produção. Nesses ambientes,
substitua-a por `Jwt__SigningKey` fornecida por variável de ambiente ou secret manager.

Obtenha o token com `POST /api/v1/auth/token` e envie-o nas demais requisições:

```http
Authorization: Bearer <accessToken>
```

No Swagger, use **Authorize** e informe somente o token. `GET /health` e o endpoint
de emissão permanecem anônimos. Não adicione chaves ou tokens reais aos arquivos
versionados.

## Docker

```bash
docker compose up --build
```

A API fica em `http://localhost:8080`, a documentacao Swagger em `http://localhost:8080/swagger` e a caixa de e-mails de teste Mailpit em `http://localhost:8025`.

O `docker-compose.yml` inclui PostgreSQL e Mailpit. No ambiente Docker, o endpoint de notificações envia mensagens para o Mailpit via SMTP interno (`mailpit:1025`), sem credenciais e sem enviar e-mails reais. Para executar a API pelo Visual Studio, o Mailpit está acessível em `localhost:1025`. A aplicação executa as migrations automaticamente ao iniciar.

## E-mails de desenvolvimento com Mailpit

O Mailpit é um servidor SMTP apenas para desenvolvimento: ele captura as mensagens recebidas e as exibe em uma interface web, sem encaminhá-las a destinatários reais. Por isso, não requer credenciais SMTP nem senha de aplicativo.

### Validando o endpoint

Envie uma requisição para a API:

```http
POST /api/v1/notifications/email
Content-Type: application/json

{
  "email": "cliente@example.com"
}
```

Após receber `204 No Content`, abra `http://localhost:8025` e confirme a mensagem capturada pelo Mailpit.

Se a API local retornar erro de conexão recusada, recrie o container para garantir que a porta SMTP esteja publicada:

```bash
docker compose up -d --force-recreate mailpit
```

## Visual Studio

Abra `Oficina.sln` na raiz do repositorio. Defina `Oficina.Api` como projeto de inicializacao para executar a API e acessar o Swagger.

## Requisicoes HTTP

O arquivo [`oficina.http`](oficina.http) documenta e permite executar todos os endpoints publicados no Swagger com dados mockados. Ele usa `http://localhost:8080` como URL base e encadeia os IDs retornados pelas requests de criacao.

Antes de usa-lo, inicie a API com `docker compose up --build` e confirme que `http://localhost:8080/health` responde. Em seguida, abra o arquivo no Visual Studio, no JetBrains Rider ou no VS Code com a extensao REST Client e execute o fluxo principal de cima para baixo. O arquivo emite o token técnico primeiro e o reutiliza nas requests administrativas. As requests de limpeza ficam isoladas no fim do arquivo e devem ser executadas individualmente somente depois das demais.

Se uma execucao for interrompida, dados com documento, e-mail, placa ou codigo unicos podem permanecer no banco. Conclua a limpeza usando os IDs retornados, altere os valores mockados ou recrie o banco de desenvolvimento antes de repetir o fluxo completo.

## SMTP externo

Para utilizar um provedor SMTP real fora do ambiente de desenvolvimento, altere no `.env` os valores `Smtp__Host`, `Smtp__Port`, `Smtp__From`, `Smtp__Username`, `Smtp__Password` e `Smtp__EnableSsl`. O `.env` é ignorado pelo Git e variáveis de ambiente já definidas têm precedência sobre ele. Não versione credenciais; quando aplicável, use uma senha de aplicativo do provedor SMTP.

O template usa Mailpit para desenvolvimento local (`localhost:1025`). Ao executar via Docker Compose, essas variáveis são substituídas pela configuração interna do container. Em ambos os casos, envie `POST /api/v1/notifications/email` e abra `http://localhost:8025` para conferir a mensagem capturada.
