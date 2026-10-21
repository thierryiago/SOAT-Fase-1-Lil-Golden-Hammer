# Infraestrutura (Terraform + LocalStack)

A infraestrutura é provisionada no **LocalStack**. O código segue compatível com a AWS real: basta usar `-var use_localstack=false`. O `LabRole` do AWS Academy foi descartado; as IAM roles serão criadas pelo próprio Terraform.

## Pré-requisitos

- Terraform ≥ 1.10.
- LocalStack rodando no Docker em `http://localhost:4566`. Confira com `curl http://localhost:4566/_localstack/health`.

## Bootstrap do state remoto (uma única vez)

O bucket S3 do state é criado por uma configuração separada, com state local (não versionado):

```bash
terraform -chdir=infra/bootstrap init
terraform -chdir=infra/bootstrap apply
```

## Uso

```bash
cp infra/terraform.tfvars.example infra/terraform.tfvars   # opcional
terraform -chdir=infra init -backend-config=backend.localstack.hcl
terraform -chdir=infra plan
terraform -chdir=infra apply
```

## Observações

- **Endpoint duplicado:** blocos `backend` não aceitam variáveis. Se mudar `localstack_endpoint`, altere também `endpoints.s3` em `backend.localstack.hcl`.
- **Lock do state:** o backend usa `use_lockfile = true` (sem DynamoDB). Se a versão do LocalStack não suportar escrita condicional no S3, remova essa linha apenas do `backend.localstack.hcl`.
- **AWS real:** use `-var use_localstack=false` e um arquivo `-backend-config` próprio, sem `endpoints`.
