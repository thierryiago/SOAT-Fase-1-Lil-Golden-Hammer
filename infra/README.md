# Infraestrutura (Terraform + LocalStack)

Toda a infraestrutura do projeto é provisionada no **LocalStack**; a AWS real não é usada. As IAM roles são criadas pelo próprio Terraform.

## Pré-requisitos

- Terraform ≥ 1.10.
- Docker e `kubectl`.
- LocalStack rodando no Docker em `http://localhost:4566` (publicando também a faixa `4510-4559`, usada pelo RDS e pelo EKS), **com o socket do Docker montado** e `LOCALSTACK_AUTH_TOKEN` definido (veja [EKS no LocalStack](#eks-no-localstack)). Confira com `curl http://localhost:4566/_localstack/health`.

```powershell
docker run -d --name localstack -p 4566:4566 -p 4510-4559:4510-4559 -e LOCALSTACK_AUTH_TOKEN=$env:LOCALSTACK_AUTH_TOKEN -v /var/run/docker.sock:/var/run/docker.sock -v localstack-data:/var/lib/localstack localstack/localstack
```

## Ordem de execução

> **Atenção:** o bootstrap (bucket S3 do state) **deve ser executado primeiro**. Sem ele, o `terraform init` da pasta `infra` falha com `NoSuchBucket`.

1. **Bootstrap** — cria o bucket S3 do state remoto (configuração separada, com state local não versionado):

   ```bash
   terraform -chdir=infra/bootstrap init
   terraform -chdir=infra/bootstrap apply
   ```

2. **Infra** — VPC, IAM, cluster EKS, node group e banco RDS:

   ```bash
   cp infra/terraform.tfvars.example infra/terraform.tfvars   # opcional
   terraform -chdir=infra init
   terraform -chdir=infra plan
   terraform -chdir=infra apply
   ```

3. **Kubeconfig** — para usar o `kubectl` (PowerShell, com o ponto na frente):

   ```powershell
   . .\infra\scripts\kubeconfig.ps1
   kubectl get nodes
   ```

O LocalStack guarda os dados em memória: **sempre que o container for recriado, o bucket e o state somem** e é preciso repetir o passo 1 (se o bootstrap reclamar do state local, apague `infra/bootstrap/terraform.tfstate*`) e depois o passo 2.

## EKS no LocalStack

Decisão: o cluster é criado **pelo Terraform no próprio LocalStack** (`aws_eks_cluster` + `aws_eks_node_group`), os mesmos recursos que seriam usados na AWS real. Não foi necessário o fallback com kind/k3d externo.

- O LocalStack sobe um cluster Kubernetes real via **k3d**, em containers Docker vizinhos. Por isso o container do LocalStack precisa do socket do Docker (`-v /var/run/docker.sock:/var/run/docker.sock`). Sem ele o log mostra `Docker not available` e o cluster termina em `unexpected state 'FAILED'`.
- O EKS exige `LOCALSTACK_AUTH_TOKEN` válido (`eks: running` em `/_localstack/health`).
- A versão do Kubernetes (`kubernetes_version`, padrão `1.31`) foi aceita pelo LocalStack.
- Não há AWS CLI/`awslocal` no projeto: o `infra/scripts/kubeconfig.ps1` extrai o kubeconfig do container k3d e aponta para a porta publicada no host. O arquivo é gravado em `%USERPROFILE%\kubeconfig-oficina` e não altera o seu `~/.kube/config`. A porta é sorteada pelo Docker; se o cluster for recriado, rode o script de novo.

## RDS no LocalStack

Decisão: o PostgreSQL é criado **pelo Terraform no próprio LocalStack** (`aws_db_instance`, PostgreSQL 16). O LocalStack executa o Postgres dentro do container dele, numa porta da faixa `4510-4559`; não foi necessário o fallback com `postgres:16` em container.

- A faixa `4510-4559` precisa estar publicada no `docker run` do LocalStack (`-p 4510-4559:4510-4559`). Sem ela o banco só é alcançável de dentro da rede do container do LocalStack.
- Os outputs `db_*` são `sensitive`. Para lê-los: `terraform -chdir=infra output -raw db_connection_string` (também `db_endpoint`, `db_port`, `db_name`, `db_username`, `db_password`).
- O security group do banco só libera a 5432 para o security group do cluster. O LocalStack **não aplica** security groups ao tráfego real; a regra existe por paridade com a AWS.
- A senha é gerada por `random_password` e fica apenas no state.

Teste de conexão (a partir do host, com a faixa publicada):

```powershell
$p = terraform -chdir=infra output -raw db_port
$u = terraform -chdir=infra output -raw db_username
$d = terraform -chdir=infra output -raw db_name
$env:PGPASSWORD = terraform -chdir=infra output -raw db_password
docker run --rm -e PGPASSWORD=$env:PGPASSWORD postgres:16-alpine psql -h host.docker.internal -p $p -U $u -d $d -c "select version();"
```

Sem a faixa publicada, use a rede do container: `docker run --rm --network container:localstack ... psql -h localhost ...`.

## Observações

- **Endpoint duplicado:** blocos `backend` não aceitam variáveis. Se mudar `localstack_endpoint`, altere também `endpoints.s3` no bloco `backend` de `versions.tf` e em `bootstrap/variables.tf`.
- **Lock do state:** o backend usa `use_lockfile = true` (sem DynamoDB).
- **Credenciais:** `test`/`test` fixas no provider e no backend; valem só para o LocalStack.
