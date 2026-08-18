# Parte IV - Linguagem Ubíqua

## 15. Glossário oficial

| Termo | Significado no domínio | Não confundir com |
|---|---|---|
| Cliente | Pessoa física ou jurídica responsável pelo atendimento | Usuário administrativo |
| Documento | CPF ou CNPJ usado para identificar o Cliente | Identificador interno |
| Veículo | Bem atendido pela oficina | Ordem de Serviço |
| Placa | Identificação validada do Veículo | Código interno |
| Serviço | Tipo de trabalho oferecido pela oficina | Execução de uma OS específica |
| Peça | Componente aplicado ao Veículo | Serviço ou mão de obra |
| Insumo | Material consumido na execução | Peça rastreável, quando houver distinção |
| Item de Estoque | Peça ou Insumo administrado pelo estoque | Item já incluído na OS |
| Ordem de Serviço | Agregado que representa o atendimento operacional do Veículo | Orçamento |
| Item de Serviço da OS | Serviço específico incluído na OS | Serviço do Catálogo |
| Item de Peça da OS | Snapshot de Peça/Insumo incluído na OS | Cadastro atual do Estoque |
| Diagnóstico | Conclusão técnica registrada pelo Mecânico | Reclamação informada pelo Cliente |
| Orçamento | Composição financeira versionada da OS | Aprovação |
| Versão do Orçamento | Conteúdo imutável enviado ao Cliente | Rascunho em edição |
| Aprovação | Decisão positiva do Cliente sobre o Orçamento vigente | Envio do Orçamento |
| Reprovação | Decisão negativa do Cliente sobre o Orçamento | Falha técnica do Serviço |
| Execução | Realização dos itens autorizados | Diagnóstico |
| Andamento | Registro cronológico do progresso | Estado oficial da OS |
| OS Recebida | OS criada e aceita para atendimento | Veículo entregue |
| Em diagnóstico | Diagnóstico técnico em andamento | Em execução |
| Aguardando aprovação | Orçamento enviado e decisão pendente | Orçamento aprovado |
| Em execução | Serviços autorizados em realização | Finalizada |
| Finalizada | Trabalho autorizado concluído | Entregue |
| Entregue | Veículo devolvido ao Cliente | Finalizada |
| Estoque disponível | Quantidade que pode ser utilizada | Quantidade histórica |
| Baixa de Estoque | Redução confirmada por consumo/uso | Consulta ou inclusão na OS |
| Tempo de execução | Intervalo entre início e término da execução | Tempo total desde a criação da OS |