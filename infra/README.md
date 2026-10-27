# Infraestrutura (Terraform + LocalStack)

Toda a infraestrutura do projeto é provisionada no **LocalStack**; a AWS real não é usada. As IAM roles são criadas pelo próprio Terraform.

## Pré-requisitos

- Terraform ≥ 1.10.
- Docker e `kubectl`.
- LocalStack rodando no Docker em `http://localhost:4566` (publicando também a faixa `4510-4559`, usada pelo RDS e pelo EKS), **com o socket do Docker montado** e `LOCALSTACK_AUTH_TOKEN` definido (veja [EKS no LocalStack](#eks-no-localstack)). Confira com `curl http://localhost:4566/_localstack/health`.

```powershell
docker run -d --name localstack -p 4566:4566 -p 4510-4559:4510-4559 -e LOCALSTACK_AUTH_TOKEN=$env:LOCALSTACK_AUTH_TOKEN -v /var/run/docker.sock:/var/run/docker.sock -v localstack-data:/var/lib/localstack localstack/localstack
```

## Recursos criados

| Arquivo | Recursos |
|---|---|
| `bootstrap/` | Bucket S3 do state remoto |
| `network.tf` | VPC, subnets públicas/privadas, internet gateway, rotas, security group do cluster |
| `iam.tf` | IAM roles do cluster EKS e do node group (criadas pelo Terraform) |
| `eks.tf` | Cluster EKS e node group |
| `rds.tf` | PostgreSQL 16 (`aws_db_instance`), subnet group, security group, senha (`random_password`) |
| `ecr.tf` | Repositórios `oficina-api` e `oficina-migrations`, com lifecycle de 10 imagens |

## Ordem de execução

> **Atenção:** o bootstrap (bucket S3 do state) **deve ser executado primeiro**. Sem ele, o `terraform init` da pasta `infra` falha com `NoSuchBucket`.

1. **Bootstrap** — cria o bucket S3 do state remoto (configuração separada, com state local não versionado):

   ```bash
   terraform -chdir=infra/bootstrap init
   terraform -chdir=infra/bootstrap apply
   ```

2. **Infra** — VPC, IAM, cluster EKS, node group, banco RDS e repositórios ECR:

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

## ECR no LocalStack

Decisão: os repositórios são criados **pelo Terraform no próprio LocalStack** (`aws_ecr_repository`, com `scan_on_push` e lifecycle policy de 10 imagens). O fallback com `registry:2` só será necessário se o ECR não estiver disponível no plano do LocalStack (`ecr` em `/_localstack/health`); nesse caso mantenha a URL do repositório configurável. **Status: o código passa em `terraform validate`, mas push e pull ainda não foram validados contra um LocalStack real.**

```bash
terraform -chdir=infra output ecr_repository_urls
```

No LocalStack a URL tem o formato `<account>.dkr.ecr.us-east-1.localhost.localstack.cloud:4566/<repo>`. Login, build e push (o LocalStack não valida a senha):

```powershell
$api = (terraform -chdir=infra output -json ecr_repository_urls | ConvertFrom-Json).'oficina-api'
$mig = (terraform -chdir=infra output -json ecr_repository_urls | ConvertFrom-Json).'oficina-migrations'
docker login -u AWS -p x ($api -split '/')[0]
docker build -t oficina-api:dev .
docker build --target migrations -t oficina-migrations:dev .
docker tag oficina-api:dev "${api}:dev";        docker push "${api}:dev"
docker tag oficina-migrations:dev "${mig}:dev"; docker push "${mig}:dev"
```

O cluster (k3d do LocalStack) acessa o ECR pela rede Docker; use `image: <url>:dev` nos manifests. A policy `AmazonEC2ContainerRegistryReadOnly` já está anexada à role dos nodes.

## Outputs e Secret do Kubernetes

Os outputs `db_*` alimentam o Secret `oficina-secrets` usado pela API. Ele é criado por `k8s/scripts/create-secrets.ps1`, que lê `db_connection_string` do Terraform (veja [`k8s/README.md`](../k8s/README.md#configuração-e-secrets)):

```powershell
. .\infra\scripts\kubeconfig.ps1
.\k8s\scripts\create-secrets.ps1 -Overlay aws
```

## Destroy

```bash
terraform -chdir=infra destroy
terraform -chdir=infra/bootstrap destroy   # opcional: remove o bucket do state
```

Destrua primeiro a infra e só depois o bootstrap, senão o state fica sem backend.

## Custo

Tudo roda localmente: **custo zero na AWS**. Os únicos requisitos são recursos da máquina (Docker, vários containers k3d/Postgres) e o `LOCALSTACK_AUTH_TOKEN` (EKS exige plano com token). Não há cobrança por dia, mas pare ou destrua o ambiente ao fim das demos para liberar CPU/memória.

## Troubleshooting

| Sintoma | Causa / solução |
|---|---|
| `NoSuchBucket` no `terraform init` | Bootstrap não executado ou LocalStack recriado; repita o passo 1 |
| `connection refused` em `localhost:4566` | LocalStack parado ou Docker Desktop fechado; suba e confira `/_localstack/health` |
| `Docker not available` / cluster `FAILED` | Falta montar `/var/run/docker.sock` no container do LocalStack |
| EKS indisponível | `LOCALSTACK_AUTH_TOKEN` ausente ou inválido |
| `kubectl` não conecta | Cluster recriado muda a porta; rode `kubeconfig.ps1` de novo |
| Erro no bootstrap por state local | Apague `infra/bootstrap/terraform.tfstate*` |

## Observações

- **Endpoint duplicado:** blocos `backend` não aceitam variáveis. Se mudar `localstack_endpoint`, altere também `endpoints.s3` no bloco `backend` de `versions.tf` e em `bootstrap/variables.tf`.
- **Lock do state:** o backend usa `use_lockfile = true` (sem DynamoDB).
- **Credenciais:** `test`/`test` fixas no provider e no backend; valem só para o LocalStack.
