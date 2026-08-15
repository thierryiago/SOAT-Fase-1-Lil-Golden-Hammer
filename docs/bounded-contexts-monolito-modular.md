# Bounded Contexts — Oficina em Monólito Modular | Fase 1

## Decisão arquitetural

O MVP será um **monólito modular**, não uma coleção de microserviços. Cada Bounded Context é um módulo com linguagem, regras, aplicação e persistência próprias; módulos não acessam diretamente as tabelas ou entidades internas uns dos outros.

O **Core Domain** é a **Gestão da Ordem de Serviço**. Os demais contextos fornecem identidade, catálogo, saldo/reserva, autorização de acesso ou modelos de leitura para esse núcleo.

## Contextos do MVP

| Bounded Context | Papel | Responsabilidade e dados que possui | Relação com a Gestão da OS |
| --- | --- | --- | --- |
| **Gestão da Ordem de Serviço** | **Core Domain** | OS, checklist de entrada, solicitação, diagnóstico, itens propostos/autorizados, orçamento versionado, autorização, execução, inspeção, retrabalho, pagamento e entrega. | Orquestra o fluxo operacional; referencia dados externos por identificador e consome contratos dos demais módulos. |
| **Clientes e Veículos** | Supporting | Cliente, CPF/CNPJ, contatos, veículo, placa, marca, modelo, ano e histórico de identificação. | Identifica o responsável e o veículo na abertura da OS. Não possui o ciclo da OS. |
| **Catálogo de Serviços** | Supporting | Serviço catalogado, descrição, tempo estimado, preço de referência e regras comerciais básicas. | Fornece sugestões para itens do orçamento; o preço aprovado é copiado como *snapshot* no item da OS. |
| **Peças e Estoque** | Supporting | Item de estoque, saldo, disponibilidade, reserva, consumo, entrada e necessidade de compra. | Recebe solicitação de reserva para itens autorizados e devolve disponibilidade, reserva ou indisponibilidade. |
| **Acesso Administrativo** | Generic | Usuários internos, perfis, permissões e autenticação JWT. | Autoriza comandos administrativos; não participa de regras de negócio da OS. |
| **API do Cliente** | Consumer / Published API | Consulta segura do cliente ao status e ao histórico permitido de sua OS. | Consome projeções publicadas pela Gestão da OS; não altera o agregado diretamente. |
| **Consultas e Métricas** | Read Model / Reporting | Listagens, detalhamento, tempo médio de execução, indicadores operacionais e telas de acompanhamento. | Recebe eventos da OS e produz modelos de leitura. Nunca é fonte de verdade para transações. |

## O que permanece dentro do Core Domain no MVP

Para não fragmentar o MVP antes da hora, estas capacidades ficam como submódulos ou vertical slices de **Gestão da Ordem de Serviço**:

- recepção e priorização da OS;
- diagnóstico técnico;
- orçamento, aprovação e autorização;
- execução de serviços;
- qualidade, inspeção final e retrabalho;
- financeiro da OS, pagamento e entrega;
- comunicação operacional e histórico do atendimento.

Elas poderão se tornar contextos separados apenas quando houver equipe, ciclo de mudança, modelo de linguagem ou consistência transacional claramente independentes.

## Agregados e limites de consistência

| Contexto | Agregado / raiz | Invariantes principais |
| --- | --- | --- |
| Gestão da OS | `OrdemServico` | Não executar, reservar ou cobrar item sem autorização; não entregar sem inspeção aprovada e pagamento confirmado. |
| Clientes e Veículos | `Cliente` e `Veiculo` | CPF/CNPJ e placa válidos; veículo vinculado ao responsável conforme regra de negócio. |
| Catálogo de Serviços | `ServicoCatalogado` | Serviço possui descrição e referência comercial consistentes. |
| Peças e Estoque | `ItemEstoque` | Saldo disponível não pode ficar negativo; reserva e consumo são rastreáveis. |
| Acesso Administrativo | `UsuarioAdministrativo` | Permissões determinam quais comandos administrativos podem ser executados. |

`Orcamento`, `ItemOrcamento`, `ChecklistEntrada` e `InspecaoFinal` são entidades ou objetos internos da `OrdemServico` no MVP. Não devem virar agregados independentes sem uma necessidade real de consistência ou concorrência separada.

## Contratos entre módulos

| Origem (upstream) | Destino (downstream) | Contrato e consistência esperada |
| --- | --- | --- |
| Clientes e Veículos | Gestão da OS | Consulta de cliente/veículo e referências por ID. A OS guarda *snapshots* mínimos necessários para auditoria. |
| Catálogo de Serviços | Gestão da OS | Proposta de serviço e preço de referência. A OS copia descrição e valor no orçamento, preservando a versão comercial aplicada. |
| Peças e Estoque | Gestão da OS | Comandos `Reservar Estoque` e `Consumir Estoque`; respostas/eventos `Estoque Reservado` ou `Estoque Indisponível`. |
| Acesso Administrativo | Gestão da OS | Claims JWT e autorização de comando. Não vazar o modelo de usuários para o domínio. |
| Gestão da OS | API do Cliente | Projeção de status, orçamento e histórico permitido ao cliente. Interface de leitura; sem comandos administrativos. |
| Gestão da OS | Consultas e Métricas | Eventos de domínio alimentam projeções como fila de OS e tempo médio de execução. Consistência eventual é aceitável. |

## Regras de integração do monólito modular

1. Um módulo expõe contratos de aplicação, não repositórios ou entidades internas.
2. A comunicação transacional usa interfaces explícitas; eventos internos publicam fatos já confirmados.
3. Um módulo não realiza `join`, escrita ou migração sobre tabelas privadas de outro módulo.
4. O contexto consumidor traduz termos externos quando houver diferença de significado; essa tradução é uma **ACL (Anti-Corruption Layer)**.
5. Preço, descrição e dados comerciais usados em um orçamento são *snapshots* da OS; mudanças posteriores no catálogo não reescrevem o passado.
6. Relatórios e API do cliente usam read models; não mudam o estado da OS.

## Estrutura sugerida de módulos

```text
src/
  Oficina.Modules.ClientesVeiculos/
  Oficina.Modules.CatalogoServicos/
  Oficina.Modules.Estoque/
  Oficina.Modules.OrdensServico/        # Core Domain
  Oficina.Modules.AcessoAdministrativo/
  Oficina.Modules.ConsultasMetricas/
  Oficina.Api.Admin/
  Oficina.Api.Cliente/
```

Cada módulo deve manter, quando aplicável, suas camadas `Domain`, `Application` e `Infrastructure`. A solução continua um único deploy com Docker, mas as fronteiras já preparam crescimento sem acoplamento indevido.

## Escopo técnico obrigatório da Fase 1

- APIs REST documentadas com Swagger/OpenAPI;
- JWT nas APIs administrativas;
- validação de CPF/CNPJ e placa;
- CRUD de clientes, veículos, serviços e peças/insumos;
- abertura, acompanhamento e consulta de OS pelo cliente;
- controle de estoque e rastreabilidade de peças;
- métricas de execução, incluindo tempo médio;
- testes unitários e de integração, com cobertura mínima de 80% nos domínios críticos;
- Dockerfile, `docker-compose.yml`, README e justificativa do banco de dados.

