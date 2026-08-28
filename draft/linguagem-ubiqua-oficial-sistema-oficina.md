# Linguagem Ubíqua Oficial — Sistema de Oficina

> Documento oficial do projeto para alinhamento entre especialistas do domínio, produto, desenvolvimento, testes e documentação da Fase 1.

## 1. Propósito e escopo

Esta linguagem descreve o vocabulário do sistema de oficina no fluxo de atendimento do veículo: contato/agendamento, recebimento, abertura e acompanhamento da Ordem de Serviço, diagnóstico, orçamento, autorização, gestão de peças e insumos, execução, qualidade, pagamento e entrega.

Os termos deste documento devem ser usados de forma consistente em conversas de negócio, Event Storming, código, APIs, banco de dados, testes e documentação. Quando houver outra expressão no uso informal, prevalece o termo oficial abaixo.

## 2. Fluxo de negócio oficial

`Contato ou agendamento → Recebimento do veículo e checklist de entrada → OS recebida → Diagnóstico → Orçamento versionado → Aprovação/autorização → Reserva ou compra de peças/insumos → Execução → Inspeção final ou retrabalho → OS finalizada → Pagamento confirmado → Veículo entregue → Histórico atualizado`

## 3. Papéis do domínio

| Termo | Significado no domínio | Não confundir com |
| --- | --- | --- |
| **Cliente** | Pessoa física ou jurídica responsável pelo atendimento e pela decisão comercial sobre o orçamento. É identificada por documento válido. | Usuário administrativo ou, necessariamente, condutor do veículo. |
| **Condutor** | Pessoa que leva ou retira o veículo quando não é o cliente responsável. | Cliente responsável financeiro. |
| **Atendente** | Colaborador que realiza recepção, registra informações, comunica-se com o cliente e acompanha a OS. | Mecânico; não produz diagnóstico técnico. |
| **Mecânico** | Profissional que inspeciona o veículo, registra diagnóstico e executa os serviços autorizados. | Atendente. |
| **Usuário administrativo** | Usuário interno autenticado para acessar funções administrativas conforme sua permissão. | Cliente. |
| **Oficina** | Organização que presta o serviço de manutenção ou reparo e administra a operação. | Estoque, catálogo ou OS. |

## 4. Identificação e atendimento

| Termo | Significado no domínio | Não confundir com |
| --- | --- | --- |
| **Documento** | CPF ou CNPJ usado para identificar o cliente. Deve ser válido e normalizado. | Identificador interno do sistema. |
| **Veículo** | Bem atendido pela oficina, identificado por placa, marca, modelo e ano. Possui histórico de atendimentos. | “Carro”; em regras, APIs e documentação, usar *veículo*. |
| **Placa** | Identificação validada do veículo, conforme padrão aceito pela oficina. | Código interno do veículo. |
| **Agendamento** | Reserva de data e horário para recepção ou avaliação. Pode ser confirmado, remarcado, cancelado ou não comparecido. | Ordem de Serviço; agendar não abre uma OS automaticamente. |
| **Recebimento do veículo** | Ato de aceitar o veículo na oficina e registrar suas condições iniciais. | Diagnóstico ou entrega. |
| **Checklist de entrada** | Registro imutável de condições, acessórios, avarias aparentes e quilometragem no recebimento. | Inspeção final. |
| **Solicitação de serviço** | Relato do cliente sobre sintoma, necessidade ou serviço desejado. | Diagnóstico; não é conclusão técnica. |
| **Histórico do veículo** | Consulta dos atendimentos, diagnósticos, serviços, peças, pagamentos e entregas realizados no veículo. | Dados temporários da OS atual. |

## 5. Ordem de Serviço, diagnóstico e execução

| Termo | Significado no domínio | Não confundir com |
| --- | --- | --- |
| **Ordem de Serviço (OS)** | Registro operacional que acompanha o atendimento de um veículo do recebimento à entrega. É o agregado central do MVP. | Orçamento; o orçamento é vinculado à OS. |
| **Item de Serviço da OS** | Serviço específico proposto ou autorizado para uma OS. Preserva os dados aplicados naquele atendimento. | Serviço do Catálogo. |
| **Item de Peça da OS** | Snapshot da peça ou insumo proposto ou utilizado na OS, com quantidade e valor aplicados. | Cadastro atual do estoque. |
| **Diagnóstico** | Conclusão técnica registrada pelo mecânico após inspeção, teste ou avaliação. Pode originar serviços, peças e insumos recomendados. | Solicitação de serviço ou execução. |
| **Serviço** | Tipo de trabalho técnico oferecido pela oficina e que pode ser proposto, autorizado e executado. | Execução de um item de serviço específico. |
| **Serviço adicional** | Serviço identificado após o orçamento inicial ou durante a execução. Exige novo orçamento ou nova versão e nova autorização antes de ser executado. | Alteração informal da OS. |
| **Execução** | Realização dos itens de serviço autorizados na OS. | Diagnóstico ou inspeção final. |
| **Andamento** | Registro cronológico de ocorrência, comunicação ou progresso da OS. | Status oficial da OS. |
| **Tempo de execução** | Intervalo entre o início e o término da execução dos itens autorizados. | Tempo total desde a abertura da OS. |

## 6. Orçamento e autorização

| Termo | Significado no domínio | Não confundir com |
| --- | --- | --- |
| **Orçamento** | Proposta comercial versionada da OS, composta por itens de serviço, peças e/ou insumos, valores, total, validade e condições. | Autorização; orçamento é proposta, não decisão. |
| **Item de orçamento** | Linha da versão do orçamento, de tipo serviço, peça ou insumo, com quantidade, valor e decisão correspondente do cliente. | Item de estoque; não representa saldo físico. |
| **Versão do orçamento** | Conteúdo imutável do orçamento emitido ou enviado ao cliente. Uma alteração relevante cria nova versão. | Rascunho em edição. |
| **Orçamento vigente** | Última versão emitida, ainda válida e passível de decisão do cliente. | Versão histórica ou rascunho. |
| **Aprovação** | Decisão positiva do cliente sobre o orçamento vigente, total ou parcial. | Envio do orçamento ou status da OS. |
| **Aprovação total** | Decisão que autoriza todos os itens da versão vigente. | Conclusão da OS. |
| **Aprovação parcial** | Decisão que autoriza somente os itens selecionados da versão vigente. | Aprovação total; os demais itens continuam recusados ou pendentes de nova composição. |
| **Recusa** | Decisão negativa do cliente sobre todo o orçamento ou sobre determinado item. | Reprovação de inspeção; recusa é comercial. |
| **Autorização** | Registro auditável de aprovação ou recusa, contendo canal, data/hora, responsável e itens abrangidos. | Aprovação; autorização é a evidência da decisão. |

## 7. Peças, insumos e estoque

| Termo | Significado no domínio | Não confundir com |
| --- | --- | --- |
| **Peça** | Componente instalado, substituído ou reparado no veículo. | Serviço ou mão de obra. |
| **Insumo** | Material consumido na execução, como óleo, fluido ou outro material definido pela política da oficina. | Peça; ambos podem ser administrados no estoque. |
| **Item de estoque** | Peça ou insumo administrado pelo estoque, com saldo e movimentações rastreáveis. | Item já incluído na OS. |
| **Estoque disponível** | Quantidade que pode ser reservada para novas demandas, considerando o saldo físico e as reservas ativas. | Quantidade histórica ou simplesmente quantidade cadastrada. |
| **Reserva de estoque** | Separação de quantidade disponível para uma OS autorizada. Não reduz o saldo físico utilizado. | Consumo ou baixa de estoque. |
| **Consumo de estoque / baixa de estoque** | Redução confirmada do saldo por uso de peça ou insumo durante a execução. | Consulta, cadastro ou reserva de estoque. |
| **Compra de peça** | Processo de obtenção de item indisponível para atender a OS. | Entrada de estoque; a compra conclui-se operacionalmente no recebimento do item. |
| **Entrada de estoque** | Registro do recebimento que disponibiliza a quantidade comprada no estoque. | Reserva ou consumo. |

## 8. Qualidade, financeiro e encerramento

| Termo | Significado no domínio | Não confundir com |
| --- | --- | --- |
| **Inspeção final** | Validação de qualidade realizada após a execução dos itens autorizados. | Checklist de entrada. |
| **Aprovação da inspeção final** | Resultado de qualidade que libera a finalização técnica da OS. | Aprovação de orçamento; são decisões de naturezas diferentes. |
| **Reprovação da inspeção final** | Resultado de qualidade que identifica não conformidade e impede a finalização e a entrega. | Recusa de orçamento; reprovação é técnica. |
| **Retrabalho** | Correção necessária após reprovação da inspeção final ou identificação de não conformidade. | Serviço adicional solicitado pelo cliente. |
| **Pagamento** | Registro da quitação ou confirmação do meio de pagamento dos itens autorizados e executados. | Orçamento ou entrega. |
| **Finalização da OS** | Conclusão técnica dos itens autorizados, com inspeção final aprovada. | Entrega do veículo. |
| **Entrega do veículo** | Devolução do veículo ao cliente responsável ou condutor autorizado, com documentos e resumo dos procedimentos realizados. | Finalização técnica. |

## 9. Estados oficiais da Ordem de Serviço

Os únicos estados oficiais da OS no MVP, inclusive em contratos de API e modelos de consulta, são:

`Recebida → Em diagnóstico → Aguardando aprovação → Em execução → Finalizada → Entregue`

| Estado | Significado |
| --- | --- |
| **Recebida** | Veículo e dados mínimos foram registrados; a OS existe e foi aceita para atendimento. |
| **Em diagnóstico** | O mecânico investiga a solicitação de serviço e registra a conclusão técnica. |
| **Aguardando aprovação** | Há orçamento vigente aguardando a decisão do cliente. |
| **Em execução** | Há itens autorizados em realização. |
| **Finalizada** | Todos os itens autorizados foram concluídos e a inspeção final foi aprovada; ainda pode haver pagamento ou retirada pendente. |
| **Entregue** | Pagamento confirmado e veículo devolvido; é o estado terminal. |

Os itens abaixo são situações paralelas, detalhamentos operacionais ou estados de outros conceitos; não substituem o status oficial da OS:

| Elemento | Situações ou estados próprios |
| --- | --- |
| Orçamento | Em elaboração, emitido, enviado, aprovado totalmente, aprovado parcialmente, recusado, expirado. |
| Item de orçamento | Pendente, autorizado, recusado, executado, cancelado. |
| Estoque para a OS | Não consultado, disponível, reservado, indisponível, aguardando compra, consumido. |
| Situação operacional | Aguardando peça, em inspeção, em retrabalho, pendente de pagamento, interrompido ou em testes. |

## 10. Regras de domínio invariáveis

1. Uma OS só existe para cliente identificado por documento válido e veículo identificado por placa válida.
2. O checklist de entrada deve ser registrado antes do início da execução.
3. O diagnóstico pode propor serviços, peças e insumos, mas não autoriza a execução.
4. Somente itens autorizados podem ser reservados, executados e cobrados.
5. A aprovação parcial libera exclusivamente os itens aprovados.
6. Serviço adicional exige nova versão de orçamento e nova autorização antes de ser executado.
7. Alterar preço, descrição ou composição de um orçamento já emitido cria uma nova versão; versões enviadas permanecem imutáveis.
8. A reserva de estoque não reduz o saldo físico; o consumo reduz.
9. Indisponibilidade de peça bloqueia somente os itens dependentes e mantém a situação `Aguardando peça` rastreável.
10. Reprovação da inspeção final gera retrabalho e bloqueia a finalização e a entrega.
11. O pagamento considera apenas itens autorizados e executados.
12. O veículo só pode ser entregue quando a OS estiver finalizada e o pagamento confirmado.

## 11. Convenção para Event Storming, código e APIs

| Elemento | Convenção | Exemplo |
| --- | --- | --- |
| **Comando** | Verbo no infinitivo; expressa intenção. | `Abrir OS`, `Emitir Orçamento`, `Registrar Aprovação`, `Iniciar Execução`. |
| **Evento de domínio** | Fato no passado; não deve ser reescrito. | `OS Aberta`, `Diagnóstico Concluído`, `Orçamento Emitido`, `Serviço Executado`. |
| **Política** | Regra que reage a evento e dispara um comando. | Quando `Orçamento Aprovado`, então `Reservar Itens Autorizados`. |
| **Consulta / read model** | Informação para leitura; não altera o domínio. | `Status da OS para o Cliente`, `Tempo Médio de Execução`. |

## 12. Padronização obrigatória

- Usar **OS** exclusivamente como sigla de **Ordem de Serviço**.
- Usar **veículo**, não “carro”, em regras, código, APIs e documentação.
- Usar **recusa** ou **recusado** para decisão negativa do cliente sobre orçamento; reservar **reprovação** para resultado técnico de inspeção/qualidade.
- Distinguir **solicitação de serviço**, **diagnóstico**, **orçamento**, **aprovação**, **autorização**, **execução**, **inspeção final**, **finalização** e **entrega**.
- Distinguir **serviço do catálogo** de **item de serviço da OS** e **item de estoque** de **item de peça da OS**.
- Não usar “peça cadastrada” como sinônimo de “peça consumida”.
- Não criar variações de status da OS, como “pronta”, “concluída” ou “encerrada”. Esses termos podem aparecer como rótulos de interface, mas não como novos estados do domínio.
