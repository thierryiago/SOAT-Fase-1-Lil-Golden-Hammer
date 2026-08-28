# Plano de desenvolvimento — NotificationsController

## Objetivo

Criar a `NotificationsController` para disponibilizar o endpoint `POST /api/v1/notifications/email`. O endpoint receberá o endereço do destinatário e solicitará o envio de um e-mail por SMTP simples, com assunto e corpo de texto previamente definidos pela aplicação.

A controller não deverá conhecer detalhes de SMTP, credenciais ou criação de mensagens: ela apenas recebe a requisição HTTP e delega o caso de uso à camada de aplicação.

## Contrato HTTP proposto

```http
POST /api/v1/notifications/email
Content-Type: application/json

{
  "email": "cliente@example.com"
}
```

- Resposta de sucesso: `204 No Content` após o provedor SMTP confirmar o aceite da mensagem.
- Endereço ausente ou inválido: `400 Bad Request`.
- Falha na configuração ou no envio SMTP: erro `500`, produzido pelo middleware global como `application/problem+json`; os detalhes de credenciais e do servidor não devem ser expostos.

O conteúdo inicial da mensagem deverá ser simples e centralizado no caso de uso, por exemplo:

```text
Assunto: Notificação da Oficina
Corpo: Esta é uma notificação enviada pela Oficina.
```

## Etapas de implementação

1. Criar o caso de uso na camada `Oficina.Application`.

   - Criar a pasta `Notifications`.
   - Adicionar a DTO de entrada `SendEmailNotificationRequest`, contendo a propriedade obrigatória `Email`.
   - Validar o endereço antes de acionar a infraestrutura (valor não vazio e formato de e-mail válido); lançar `ArgumentException` para integrar com o tratamento global atual de `400`.
   - Criar `INotificationEmailSender` como porta de saída, com método assíncrono para receber destinatário, assunto, corpo e `CancellationToken`.
   - Criar `NotificationService`, responsável por definir o assunto e corpo simples e chamar a porta SMTP.
   - Registrar `NotificationService` em `Oficina.Application/DependencyInjection.cs` com o mesmo ciclo de vida scoped usado pelos demais serviços.

2. Implementar o adaptador SMTP na camada `Oficina.Infrastructure`.

   - Criar `Notifications/SmtpNotificationEmailSender.cs` que implemente `INotificationEmailSender` usando `System.Net.Mail.SmtpClient` e `MailMessage`.
   - Criar `SmtpOptions` com host, porta, remetente, usuário, senha e opção de SSL/TLS, vinculada à seção `Smtp` da configuração.
   - Construir a mensagem como texto simples (`IsBodyHtml = false`), com um único destinatário, assunto e corpo recebidos pela interface.
   - Usar envio assíncrono, propagar o `CancellationToken` quando suportado e descartar corretamente `MailMessage` e `SmtpClient`.
   - Registrar opções e a implementação de `INotificationEmailSender` em `Oficina.Infrastructure/DependencyInjection.cs`.

3. Expor a controller na camada `Oficina.Api`.

   - Criar `Controllers/NotificationsController.cs`, com `[ApiController]` e rota base `api/v1/notifications`.
   - Implementar `POST email`, recebendo `SendEmailNotificationRequest` no corpo e `CancellationToken`.
   - Delegar para `NotificationService` e devolver `NoContent()` somente após a conclusão do envio.
   - Declarar no Swagger os retornos `204` e `400`; manter o padrão de `IActionResult` das controllers existentes.

4. Configurar o ambiente sem versionar segredos.

   - Adicionar ao `appsettings.json` somente uma estrutura segura de exemplo para `Smtp` (host, porta, remetente e SSL); não inserir senha real no repositório.
   - Configurar `Smtp__Password` e, quando necessário, os demais valores sensíveis por variáveis de ambiente ou User Secrets em desenvolvimento.
   - Documentar no README as variáveis necessárias e que a conta deve permitir autenticação SMTP. Se o provedor exigir senha de aplicativo, usar essa senha no ambiente, nunca no código.

5. Testar e documentar o recurso.

   - Criar testes unitários para `NotificationService`: encaminhamento de destinatário, assunto e corpo ao contrato; rejeição de e-mail vazio ou inválido; e propagação de falhas do adaptador.
   - Criar um fake de `INotificationEmailSender` nos testes, evitando conexão SMTP real.
   - Criar teste de contrato para `POST /api/v1/notifications/email`, cobrindo ao menos contrato OpenAPI e entrada inválida. Para o cenário de sucesso, substituir o sender SMTP no host de testes por um fake.
   - Atualizar `oficina.http`, README e a lista de rotas administrativas, se aplicável.
   - Executar `dotnet build Oficina.sln` e `dotnet test Oficina.sln`.

## Decisões arquiteturais

| Responsabilidade | Camada |
| --- | --- |
| Endpoint, binding HTTP e status de resposta | `Oficina.Api` |
| Validação, texto da notificação e orquestração do envio | `Oficina.Application` |
| Contrato para envio de e-mail | `Oficina.Application` |
| SMTP, `SmtpClient` e leitura de opções | `Oficina.Infrastructure` |
| Host, credenciais e parâmetros do servidor | Configuração/variáveis de ambiente |

Essa separação permite trocar SMTP por outro provedor posteriormente sem alterar a controller nem o caso de uso.
