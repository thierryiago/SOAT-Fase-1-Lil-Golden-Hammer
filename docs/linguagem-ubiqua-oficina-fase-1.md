# Linguagem Ubíqua — Sistema de Oficina | Fase 1

Este documento é a referência de nomes e regras usadas por negócio, produto e desenvolvimento. Ele se aplica ao MVP monolítico modular e ao fluxo de atendimento até a entrega do veículo.

## Fluxo oficial da oficina

`Contato ou agendamento → Recebimento do veículo e checklist → OS recebida → Diagnóstico → Orçamento versionado → Aprovação/autorização → Reserva ou compra de peças → Execução → Inspeção final ou retrabalho → OS finalizada → Pagamento confirmado → Veículo entregue → Histórico atualizado`

## Termos oficiais

| Termo | Definição no domínio | Não confundir com |
| --- | --- | --- |
| **Cliente** | Pessoa física ou jurídica responsável pelo atendimento e pela autorização do serviço. É identificado por CPF ou CNPJ válido. | Condutor; o condutor pode não ser o responsável financeiro. |
| **Veículo** | Bem atendido pela oficina, identificado pela placa, marca, modelo e ano. Possui histórico de atendimentos. | “Carro”; usar *veículo* em regras, APIs e documentação. |
| **Agendamento** | Reserva de data/horário para recepção ou avaliação. Pode ser confirmado, remarcado, cancelado ou não comparecido. | Ordem de Serviço; um agendamento não cria uma OS automaticamente. |
| **Recebimento do veículo** | Ato de aceitar o veículo na oficina e registrar suas condições iniciais. | Diagnóstico. |
| **Checklist de entrada** | Registro das condições, acessórios, avarias aparentes e quilometragem no recebimento. É uma evidência imutável da entrada. | Inspeção final. |
| **Ordem de Serviço (OS)** | Registro operacional que acompanha o atendimento de um veículo, desde o recebimento até a entrega. É o agregado central do MVP. | Orçamento; o orçamento é uma proposta vinculada à OS. |
| **Solicitação de serviço** | Relato do cliente sobre sintoma, necessidade ou serviço desejado. | Diagnóstico; ainda não é uma conclusão técnica. |
| **Diagnóstico** | Conclusão técnica produzida após inspeção/teste pelo mecânico. Pode originar serviços e peças recomendados. | Solicitação de serviço ou execução. |
| **Serviço** | Trabalho técnico que pode ser proposto, autorizado e executado em uma OS. | Serviço adicional; este é um serviço incluído após o orçamento inicial. |
| **Serviço adicional** | Serviço identificado após o diagnóstico ou durante a execução. Exige inclusão em nova versão do orçamento e autorização antes de ser executado. | Alteração informal da OS. |
| **Peça** | Componente instalado, substituído ou reparado no veículo. | Insumo. |
| **Insumo** | Material consumido na execução, como óleo, fluido, filtro ou item de baixo consumo conforme a política da oficina. | Peça; ambos podem ser controlados no estoque. |
| **Item de orçamento** | Linha versionada do orçamento, de tipo serviço, peça ou insumo, com quantidade, valor e decisão do cliente. | Item de estoque; não representa saldo físico. |
| **Orçamento** | Proposta comercial versionada com itens, valores, total, validade e condições. | Autorização; orçamento é proposta, não decisão. |
| **Aprovação** | Decisão de negócio do cliente sobre o orçamento: total, parcial ou recusada. | Status da OS. |
| **Autorização** | Registro auditável da aprovação ou recusa, com canal, data/hora e responsável. É vinculada ao orçamento ou a seus itens. | Aprovação; é a evidência da decisão, não um status da OS. |
| **Reserva de estoque** | Separação de quantidade disponível para uma OS autorizada. Ainda não reduz a quantidade física utilizada. | Consumo de estoque. |
| **Consumo de estoque** | Baixa efetiva da peça ou insumo quando utilizado na execução. | Reserva ou compra. |
| **Compra de peça** | Processo de obtenção de item indisponível para atender uma OS. | Entrada de peça; a compra só conclui no recebimento. |
| **Inspeção final** | Validação de qualidade após a execução dos itens autorizados. | Checklist de entrada. |
| **Retrabalho** | Correção necessária quando a inspeção final reprova o resultado ou identifica não conformidade. | Novo serviço solicitado pelo cliente. |
| **Pagamento** | Registro da quitação ou confirmação do meio de pagamento para os itens autorizados e executados. | Entrega. |
| **Entrega do veículo** | Devolução do veículo ao cliente responsável, com documentos e resumo dos procedimentos executados. | Finalização técnica. |
| **Histórico do veículo** | Consulta dos atendimentos, diagnósticos, serviços, peças, pagamentos e entregas realizados no veículo. | Dados temporários do atendimento atual. |

## Estados oficiais e estados paralelos

Os estados oficiais da **OS** devem permanecer exatamente assim no MVP, inclusive em APIs e modelos de consulta:

`Recebida → Em diagnóstico → Aguardando aprovação → Em execução → Finalizada → Entregue`

- **Recebida**: veículo e dados mínimos foram registrados; a OS existe.
- **Em diagnóstico**: o mecânico está investigando a solicitação do cliente.
- **Aguardando aprovação**: há um orçamento válido aguardando decisão do cliente.
- **Em execução**: existem itens autorizados em realização.
- **Finalizada**: todos os itens autorizados foram concluídos e a inspeção final foi aprovada. A OS ainda pode aguardar pagamento e retirada.
- **Entregue**: pagamento confirmado e veículo devolvido ao cliente; é o estado terminal.

Não transformar aprovação, estoque ou pagamento em sinônimos de estado da OS. Eles são estados paralelos:

| Elemento | Estados paralelos |
| --- | --- |
| Orçamento | Em elaboração, emitido, enviado, aprovado totalmente, aprovado parcialmente, recusado, expirado. |
| Item de orçamento | Pendente, autorizado, recusado, executado, cancelado. |
| Estoque para a OS | Não consultado, disponível, reservado, indisponível, aguardando compra, consumido. |
| Situação operacional | Aguardando peça, em inspeção, em retrabalho, pendente de pagamento. |

## Regras de negócio invariáveis

1. Uma OS só pode existir para um cliente identificado e um veículo identificado.
2. A OS deve registrar checklist de entrada antes do início da execução.
3. O diagnóstico pode propor itens; ele não autoriza sua execução.
4. Somente itens autorizados podem ser reservados, executados e cobrados.
5. A aprovação parcial libera exclusivamente os itens aprovados.
6. Serviço adicional sempre gera uma nova versão de orçamento e nova autorização.
7. A reserva não reduz o estoque físico; o consumo reduz.
8. Indisponibilidade de peça bloqueia somente os itens dependentes e deixa rastreável a situação `Aguardando peça`.
9. Inspeção final reprovada gera retrabalho e bloqueia a finalização e a entrega.
10. Pagamento considera somente itens autorizados e executados.
11. O veículo só pode ser entregue quando a OS estiver finalizada e o pagamento confirmado.

## Convenção para Event Storming e implementação

| Elemento | Forma correta | Exemplo |
| --- | --- | --- |
| **Comando** | Verbo no infinitivo; expressa intenção. | `Emitir Orçamento`, `Registrar Aprovação`, `Iniciar Execução`. |
| **Evento de domínio** | Fato no passado; não deve ser reescrito. | `Orçamento Emitido`, `Orçamento Aprovado Parcialmente`, `Serviço Executado`. |
| **Política** | Regra que reage a evento e dispara comando. | Quando `Orçamento Aprovado`, então `Reservar Peças Autorizadas`. |
| **Consulta / read model** | Informação para leitura sem alterar o domínio. | `Status da OS para o Cliente`, `Tempo Médio de Execução`. |

Exemplos de substituição no board:

- “Atendente criou OS” → comando `Abrir OS` → evento `OS Aberta`.
- “Cliente aprovou orçamento” → comando `Registrar Aprovação` → evento `Orçamento Aprovado Totalmente` ou `Orçamento Aprovado Parcialmente`.
- “Mecânico realizou serviço” → comando `Registrar Execução do Serviço` → evento `Serviço Executado`.
- “Atendente atualizou a OS como reprovada” → remover; usar `Orçamento Recusado` e decidir se a OS permanece sem itens autorizados ou é encerrada sem execução.

## Padronização obrigatória

- Usar **OS** exclusivamente como sigla de **Ordem de Serviço**.
- Usar **veículo**, não “carro”, nas regras e contratos técnicos.
- Usar **recusado**, não “reprovado”, para a decisão do cliente sobre orçamento.
- Distinguir **diagnóstico**, **orçamento**, **aprovação**, **autorização**, **execução**, **inspeção final**, **finalização** e **entrega**.
- Não usar “peça cadastrada” como sinônimo de “peça consumida”.
- Manter os nomes oficiais de estado da OS sem variações como “pronta”, “concluída” ou “encerrada”; esses podem ser rótulos de leitura, não novos estados do domínio.

