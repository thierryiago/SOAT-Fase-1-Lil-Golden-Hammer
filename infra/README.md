# Infraestrutura (Terraform + LocalStack)

Toda a infraestrutura do projeto é provisionada no **LocalStack**; a AWS real não é usada. As IAM roles são criadas pelo próprio Terraform.

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
terraform -chdir=infra init
terraform -chdir=infra plan
terraform -chdir=infra apply
```

## Observações

- **Endpoint duplicado:** blocos `backend` não aceitam variáveis. Se mudar `localstack_endpoint`, altere também `endpoints.s3` no bloco `backend` de `versions.tf` e em `bootstrap/variables.tf`.
- **Lock do state:** o backend usa `use_lockfile = true` (sem DynamoDB).
- **Credenciais:** `test`/`test` fixas no provider e no backend; valem só para o LocalStack.
