# Plano de execução do MVP da Oficina

## 1. Objetivo

Completar o monólito em camadas para atender aos fluxos obrigatórios de clientes,
veículos, serviços, peças, estoque, ordens de serviço, orçamento, aprovação,
acompanhamento, segurança, métricas e qualidade.

O banco recomendado permanece PostgreSQL, acessado por Entity Framework Core e
Npgsql. O modelo é relacional e exige integridade entre cliente, veículo, ordem,
serviços, peças, orçamento e movimentações de estoque.

## 2. Diagnóstico do estado atual

| Requisito | Estado | Evidência ou lacuna |
|---|---|---|
| Monólito em camadas | Feito | Projetos `Domain`, `Application`, `Infrastructure` e `Api` |
| Swagger | Parcial | Swagger existe, mas faltam autenticação, contratos completos e todos os endpoints |
| Dockerfile | Feito | Build e runtime multi-stage configurados |
| Docker Compose | Parcial | API e PostgreSQL estão declarados, mas a aplicação ainda não usa o banco |
| Persistência PostgreSQL | Não iniciado | Repositórios são `InMemory` |
| CRUD de clientes | Parcial | Existem criação, listagem e busca por ID; faltam atualização e exclusão |
| CRUD de veículos | Parcial | Existe apenas cadastro a partir do documento do cliente; veículo não possui ID |
| CRUD de serviços | Não iniciado | Não há entidade nem endpoints para serviços de oficina |
| CRUD de peças e insumos | Parcial | Existem criação, listagem e busca; faltam atualização, exclusão e movimentações |
| Controle de estoque | Parcial | Há baixa em memória, sem persistência, transação, entrada ou auditoria |
| Criação completa de OS | Parcial | OS não referencia veículo e aceita apenas descrição e peças |
| Orçamento automático | Não iniciado | Não há cálculo de serviços + peças nem entidade de orçamento |
| Envio e aprovação | Não iniciado | Não há notificação, token ou registro de aprovação |
| Fluxo automático de status | Parcial | Enum existe; transições estão incompletas e não possuem endpoints/histórico |
| Consulta do cliente | Não iniciado | Não há API pública ou código seguro de acompanhamento |
| Listagem e detalhe de OS | Parcial | Existem endpoints básicos, sem filtros, paginação ou visão completa |
| Tempo médio de execução | Não iniciado | Não há histórico de status nem endpoint de métricas |
| JWT administrativo | Não iniciado | Não há autenticação, autorização ou usuários administrativos |
| Validação CPF/CNPJ e placa | Não iniciado | Há somente verificação de campos vazios e normalização parcial |
| Testes unitários | Parcial | Existem 9 testes para uma parte do domínio e aplicação |
| Testes de integração | Não iniciado | Não há testes da API com PostgreSQL |
| Cobertura mínima de 80% | Não comprovado | O projeto possui coletor, mas não há relatório nem gate de cobertura |
| README de execução local | Parcial | Existe, porém as rotas estão desatualizadas e o banco descrito não é usado |
| Repositório privado e acesso | Ação externa pendente | É necessário confirmar privacidade e conceder acesso a `soatarchitecture` |

## 3. Modelo de domínio necessário

Antes da persistência, o domínio deve ser ajustado para conter:

- `Customer`: cliente identificado por CPF ou CNPJ normalizado e único.
- `Vehicle`: entidade com ID próprio, placa única e vínculo obrigatório com cliente.
- `CatalogService`: serviço oferecido, preço-base, duração estimada e estado ativo.
- `Part`: peça ou insumo, código único, preço e saldo atual.
- `StockMovement`: entrada, reserva, liberação, consumo ou ajuste de estoque.
- `ServiceOrder`: ordem vinculada obrigatoriamente a cliente e veículo.
- `ServiceOrderServiceItem`: serviço incluído na OS com nome e preço congelados.
- `ServiceOrderPartItem`: peça incluída na OS com nome e preço congelados.
- `Quote`: orçamento versionado, total, validade e estado de aprovação.
- `ServiceOrderStatusHistory`: histórico de mudanças de status e seus horários.
- `AdminUser`: usuário administrativo e credenciais protegidas.

O diagrama atual deverá ser atualizado para incluir serviços, orçamento, usuário,
histórico de status e os movimentos de reserva/liberação de estoque.

## 4. Ordem de execução

### Fase 0 — Fechar regras e contratos

**Objetivo:** eliminar ambiguidades que alteram o modelo ou o fluxo.

Tarefas:

1. Definir se o cliente aprova o orçamento por link/token ou por ação de um atendente.
2. Escolher o canal de envio do orçamento, como e-mail, mantendo uma abstração de notificação.
3. Definir a política de estoque:
   - reservar ao aprovar o orçamento;
   - consumir ao iniciar a execução;
   - liberar ao rejeitar ou substituir o orçamento.
4. Definir tratamento de orçamento rejeitado e revisão de orçamento.
5. Definir exclusão lógica para registros com histórico.
6. Padronizar idioma e versão das rotas REST, por exemplo `/api/v1`.
7. Atualizar o diagrama de classes e registrar as decisões arquiteturais.

**Critério de aceite:** regras documentadas, transições de status fechadas e
contratos de API definidos antes das migrations.

### Fase 1 — Remodelar o domínio com testes

**Objetivo:** implementar as regras críticas sem depender de banco ou HTTP.

Tarefas:

1. Transformar `Vehicle` em entidade com `Id` e `CustomerId`.
2. Criar `CatalogService`, itens de serviço, orçamento, histórico e movimentação de estoque.
3. Alterar `ServiceOrder` para exigir `CustomerId` e `VehicleId`.
4. Separar itens de serviço e itens de peça.
5. Implementar cálculo:
   - subtotal de serviços;
   - subtotal de peças;
   - total do orçamento.
6. Implementar transições permitidas:

| Ação | Estado anterior | Estado posterior |
|---|---|---|
| Criar OS | — | Recebida |
| Iniciar diagnóstico | Recebida | Em diagnóstico |
| Gerar e enviar orçamento | Em diagnóstico | Aguardando aprovação |
| Aprovar orçamento | Aguardando aprovação | Em execução |
| Concluir execução | Em execução | Finalizada |
| Entregar veículo | Finalizada | Entregue |

7. Registrar cada transição no histórico com data e hora.
8. Criar value objects/validadores de CPF, CNPJ e placa Mercosul/legada.
9. Impedir edição de ordens finalizadas ou entregues.
10. Criar testes unitários antes de cada regra crítica.

**Critério de aceite:** todas as regras do domínio possuem testes positivos,
negativos e de transição inválida.

### Fase 2 — Implementar PostgreSQL

**Objetivo:** substituir os repositórios em memória por persistência real.

Tarefas:

1. Adicionar EF Core e `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Criar `OficinaDbContext` e configurações Fluent API.
3. Mapear agregados, coleções privadas, enums, datas e valores monetários.
4. Criar índices únicos para:
   - documento normalizado;
   - placa normalizada;
   - código da peça;
   - nome/código do serviço, conforme regra definida;
   - código público de acompanhamento.
5. Criar chaves estrangeiras e regras de exclusão.
6. Adicionar constraints para preço, quantidade, ano e estoque.
7. Implementar controle de concorrência para evitar saldo negativo.
8. Criar repositórios EF Core e remover o registro dos repositórios `InMemory`.
9. Criar migration inicial.
10. Incluir health check real do PostgreSQL.
11. Executar baixa/reserva de estoque, gravação da OS e mudança de status na mesma transação.

**Critério de aceite:** os dados sobrevivem ao reinício da API, relações inválidas
são rejeitadas pelo banco e operações de estoque são atômicas.

### Fase 3 — Completar os CRUDs administrativos

**Objetivo:** entregar manutenção completa dos cadastros.

Tarefas:

1. Clientes: criar, listar, detalhar, atualizar e inativar/excluir.
2. Veículos: criar, listar, detalhar, atualizar e inativar/excluir.
3. Serviços: criar, listar, detalhar, atualizar preço/duração e inativar.
4. Peças e insumos: criar, listar, detalhar, atualizar e inativar.
5. Estoque: registrar entrada e ajuste com motivo e usuário responsável.
6. Adicionar paginação, ordenação e filtros às listagens.
7. Usar DTOs de entrada e saída; não expor entidades do domínio diretamente.
8. Padronizar erros com `ProblemDetails` e códigos HTTP adequados.

**Critério de aceite:** cada recurso possui CRUD documentado no Swagger, validação,
paginação e testes de aplicação/API.

### Fase 4 — Implementar a criação completa da OS

**Objetivo:** cobrir o fluxo da recepção até o orçamento.

Tarefas:

1. Identificar cliente por CPF/CNPJ.
2. Selecionar veículo existente ou cadastrar um novo.
3. Abrir OS vinculada ao cliente e ao veículo.
4. Incluir, alterar e remover serviços solicitados.
5. Incluir, alterar e remover peças/insumos necessários.
6. Congelar descrição, preço e quantidade nos itens da OS.
7. Gerar orçamento automaticamente com serviços e peças.
8. Versionar o orçamento quando houver alteração após o primeiro envio.
9. Impedir aprovação de orçamento vencido ou substituído.
10. Validar disponibilidade de estoque antes do envio/aprovação.

**Critério de aceite:** uma OS completa pode ser criada por API e seu total é
recalculado corretamente sempre que os itens mudam.

### Fase 5 — Envio, aprovação e acompanhamento

**Objetivo:** concluir o fluxo operacional e permitir consulta pelo cliente.

Tarefas:

1. Criar interface de envio de orçamento e implementação do canal escolhido.
2. Gerar token de aprovação com expiração e uso único.
3. Registrar tentativas, sucesso e falha de envio sem expor dados sensíveis.
4. Criar endpoints públicos mínimos:
   - consultar andamento por código/token seguro;
   - consultar orçamento;
   - aprovar orçamento;
   - rejeitar orçamento.
5. Minimizar os dados retornados na API pública.
6. Automatizar as mudanças de status conforme a tabela da Fase 1.
7. Criar endpoints administrativos para diagnóstico, finalização e entrega.
8. Garantir idempotência na aprovação, finalização e entrega.

**Critério de aceite:** o cliente recebe o orçamento, aprova uma única vez e
acompanha o histórico sem acessar endpoints administrativos.

### Fase 6 — Autenticação e autorização

**Objetivo:** proteger todas as operações administrativas.

Tarefas:

1. Criar usuário administrativo e armazenamento de senha com hash seguro.
2. Implementar login e emissão de JWT com expiração, issuer, audience e chave externa.
3. Aplicar `[Authorize]` globalmente aos endpoints administrativos.
4. Manter anônimos somente health check, login, Swagger conforme ambiente e APIs públicas do cliente.
5. Adicionar roles/policies para operações sensíveis de estoque e entrega, se necessário.
6. Configurar Swagger com esquema Bearer.
7. Proteger segredos via variáveis de ambiente ou secret store; não versionar senha/chave real.
8. Testar 401, 403, token expirado, token inválido e acesso autorizado.

**Critério de aceite:** nenhuma operação administrativa é executada sem JWT válido.

### Fase 7 — Métricas administrativas

**Objetivo:** monitorar o tempo médio de execução.

Tarefas:

1. Calcular tempo de execução entre entrada em `Em execução` e entrada em `Finalizada`.
2. Criar consulta por intervalo de datas.
3. Retornar média geral, quantidade de ordens consideradas e, se necessário, média por serviço.
4. Excluir ordens ainda abertas do cálculo.
5. Definir comportamento para reabertura ou múltiplos períodos de execução.
6. Criar endpoint administrativo de métricas com consulta eficiente no PostgreSQL.

**Critério de aceite:** a métrica é reproduzível a partir do histórico e possui
testes para ordens abertas, finalizadas e intervalos sem dados.

### Fase 8 — Testes, cobertura e qualidade

**Objetivo:** comprovar os fluxos principais e aplicar o gate mínimo de 80%.

Tarefas:

1. Expandir testes unitários de domínio para:
   - CPF/CNPJ e placa;
   - transições de status;
   - cálculo e versionamento do orçamento;
   - estoque e concorrência;
   - vínculo cliente-veículo;
   - bloqueio de alterações após encerramento.
2. Criar testes de aplicação para todos os casos de uso.
3. Criar projeto de testes de integração com `WebApplicationFactory`.
4. Executar os testes de integração contra PostgreSQL isolado.
5. Cobrir autenticação, CRUDs e fluxo completo da OS por HTTP.
6. Configurar Coverlet para gerar relatório e falhar abaixo de 80% nos projetos/nomes
   críticos de `Domain` e `Application`.
7. Excluir migrations, DTOs triviais e bootstrap da medição apenas com justificativa documentada.
8. Adicionar teste de concorrência de estoque e teste de persistência após reinício de contexto.

**Critério de aceite:** testes verdes e cobertura de linhas mínima de 80% nos
domínios críticos, comprovada por relatório gerado no pipeline.

### Fase 9 — Docker, documentação, CI e entrega

**Objetivo:** tornar o ambiente reproduzível e pronto para avaliação.

Tarefas:

1. Atualizar Compose para aguardar o banco e aplicar migrations de forma controlada.
2. Remover credenciais fixas de produção; manter apenas valores locais documentados.
3. Adicionar health checks da API e do PostgreSQL.
4. Validar build e inicialização completa com `docker compose up --build`.
5. Atualizar README com:
   - pré-requisitos;
   - variáveis de ambiente;
   - execução local e Docker;
   - migrations;
   - testes e cobertura;
   - autenticação;
   - exemplos do fluxo completo;
   - URLs corretas do Swagger e health check.
6. Criar pipeline de CI para restore, build, testes, cobertura e build da imagem.
7. Confirmar que o repositório é privado.
8. Conceder acesso ao usuário `soatarchitecture`.
9. Fazer uma revisão final requisito por requisito.

**Critério de aceite:** um ambiente limpo inicia o sistema completo com um único
comando e o pipeline bloqueia regressões de build, testes ou cobertura.

## 5. Sequência de entregas sugerida

| Entrega | Fases | Resultado demonstrável |
|---|---|---|
| E1 — Base consistente | 0 a 2 | Domínio final e PostgreSQL persistindo dados |
| E2 — Administração | 3 | CRUDs completos e estoque auditável |
| E3 — Fluxo principal | 4 e 5 | OS, orçamento, aprovação e acompanhamento ponta a ponta |
| E4 — Segurança e gestão | 6 e 7 | JWT e métrica de tempo médio |
| E5 — Qualidade e entrega | 8 e 9 | Integração, cobertura, Docker, CI e documentação |

Para uma pessoa desenvolvedora, a estimativa inicial é de 15 a 20 dias úteis,
dependendo do canal escolhido para envio do orçamento e da profundidade das regras
de estoque. Essa estimativa deve ser revisada após a Fase 0.

## 6. Prioridade do backlog

### Prioridade crítica

1. Fechar regras da OS, orçamento, aprovação e estoque.
2. Corrigir o modelo cliente-veículo-OS.
3. Implementar testes de domínio.
4. Implementar PostgreSQL e transações.
5. Entregar o fluxo completo da OS.
6. Proteger APIs administrativas com JWT.

### Prioridade alta

1. Completar CRUDs.
2. Implementar consulta pública segura.
3. Implementar métricas.
4. Criar testes de integração e gate de cobertura.

### Finalização

1. Validar Docker Compose.
2. Criar CI.
3. Atualizar Swagger e README.
4. Confirmar privacidade e acesso ao repositório.

## 7. Definição de pronto do MVP

O MVP somente estará pronto quando:

- cliente e veículo estiverem vinculados e persistidos;
- a OS aceitar serviços, peças e orçamento calculado;
- o orçamento puder ser enviado, aprovado ou rejeitado;
- os seis status forem aplicados automaticamente e registrados em histórico;
- o cliente puder acompanhar a OS por uma API pública segura;
- os CRUDs administrativos e o estoque estiverem completos;
- a métrica de tempo médio estiver disponível;
- os endpoints administrativos exigirem JWT;
- CPF/CNPJ e placa forem validados;
- PostgreSQL for usado em execução real;
- testes unitários e de integração estiverem verdes;
- a cobertura crítica comprovada for igual ou superior a 80%;
- Swagger, Dockerfile, Compose e README estiverem validados;
- o repositório privado tiver acesso concedido a `soatarchitecture`.
