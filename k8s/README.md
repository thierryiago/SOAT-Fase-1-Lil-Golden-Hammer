# Kubernetes (Kustomize)

Manifestos da aplicação em Kustomize puro (sem Helm), aplicados com `kubectl`. O Kustomize embutido no `kubectl` é suficiente; não é preciso instalar o `kustomize`.

## Estrutura

```text
k8s/
  base/                  recursos comuns a todos os ambientes
    kustomization.yaml
    namespace.yaml       Namespace "oficina"
  overlays/
    localstack/          cluster EKS criado pelo Terraform no LocalStack (único ambiente)
```

A base declara `namespace: oficina`, então os recursos adicionados a ela herdam o namespace. O overlay `localstack` concentra os valores reais do ambiente (imagem, ConfigMap, label `app.kubernetes.io/environment`) e a base guarda só o que não depende dele.

Convenção de labels dos recursos: `app.kubernetes.io/name`, `app.kubernetes.io/part-of: oficina` e `app.kubernetes.io/component`.

## Overlay

O projeto tem uma única infraestrutura: o **LocalStack**. O overlay `localstack` mira o cluster EKS criado pelo Terraform (k3d do LocalStack, veja [`infra/README.md`](../infra/README.md)) e traz os valores reais: imagens do ECR do LocalStack, `ASPNETCORE_ENVIRONMENT` e `App__PublicBaseUrl`.

## Como aplicar

Renderizar sem aplicar (não precisa de cluster):

```bash
kubectl kustomize k8s/overlays/localstack
```

Obtenha o kubeconfig do cluster do LocalStack (o `.` na frente é necessário no PowerShell) e aplique:

```powershell
. .\infra\scripts\kubeconfig.ps1
kubectl apply -k k8s/overlays/localstack
```

## Configuração e Secrets

A API recebe a configuração por variáveis de ambiente (`envFrom`), de duas origens:

| Origem | Conteúdo | Quem cria |
|---|---|---|
| ConfigMap `oficina-config-<hash>` | `ASPNETCORE_ENVIRONMENT`, `Smtp__Host/Port/From/EnableSsl`, `Jwt__Issuer/Audience`, `App__PublicBaseUrl` | Kustomize (`configMapGenerator` na base, com overrides no overlay) |
| Secret `oficina-secrets` | `ConnectionStrings__Database`, `Jwt__SigningKey`, `Webhooks__BudgetDecisionToken` e, opcionalmente, `Smtp__Username`/`Smtp__Password` | `k8s/scripts/create-secrets.ps1` |

O nome do ConfigMap leva um hash do conteúdo: alterar um valor gera um novo ConfigMap e dispara rollout dos pods que o referenciam. A migration **não** roda na subida da API (não existe `Database__MigrateOnStartup`); ela é aplicada pelo Job de migrations.

> `App__PublicBaseUrl` e `Webhooks__BudgetDecisionToken` ainda não são lidos pela API (issue #110). As chaves já existem para não exigir novo deploy de configuração quando forem usadas. No overlay, `App__PublicBaseUrl` é um placeholder até a exposição da API ser definida.

### Criar o Secret

Nenhum valor real é versionado: apenas [`base/secret.example.yaml`](base/secret.example.yaml), que **não** faz parte do Kustomize. O Secret é criado por script, depois do Namespace:

```powershell
. .\infra\scripts\kubeconfig.ps1                    # cluster do LocalStack
$env:Jwt__SigningKey = "<chave com pelo menos 32 bytes>"
$env:Webhooks__BudgetDecisionToken = "<token>"
.\k8s\scripts\create-secrets.ps1                  # connection string vem do terraform output
.\k8s\scripts\create-secrets.ps1 -DryRun          # só mostra as chaves, valores mascarados
```

Cada chave vem da primeira origem disponível: variável de ambiente, valor já existente no cluster ou valor gerado. O script é idempotente (rodar de novo não troca chaves já criadas) e nunca imprime valores.

`ConnectionStrings__Database` vem de `terraform -chdir=infra output -raw db_connection_string`, com o host trocado por `host.k3d.internal` (veja [Banco de dados](#banco-de-dados)). Uma variável de ambiente `ConnectionStrings__Database` tem precedência e é usada como está.

Variáveis opcionais: `ConnectionStrings__Database` (sobrepõe o Terraform), `Smtp__Username`, `Smtp__Password`.

### Rotacionar

O Secret fica fora do Kustomize, então mudar um valor **não** reinicia os pods. Rode o script com a nova variável e reinicie:

```bash
kubectl -n oficina rollout restart deployment/oficina-api
```

### No CD

O workflow de CD (CICD-4) faz o mesmo passo, lendo os secrets do GitHub como variáveis de ambiente e rodando `kubectl create secret generic oficina-secrets ... --dry-run=client -o yaml | kubectl apply -f -`.

## Banco de dados

O banco é o **RDS do LocalStack** (criado pelo Terraform, veja [`infra/README.md`](../infra/README.md)); não há Postgres dentro do cluster. Nenhum Service, Ingress ou LoadBalancer expõe o banco e o RDS é `publicly_accessible = false`. O LocalStack **não aplica** security groups ao tráfego real, então essa garantia vem do desenho (nada no cluster publica o banco), não de enforcement de rede.

**Por que não funciona o endpoint do output:** `db_endpoint` é `localhost` e a porta (faixa `4510-4559`) está publicada no **host** pelo container do LocalStack. Dentro de um pod, `localhost` é o próprio pod. O `create-secrets.ps1` troca o host por `host.k3d.internal` (parâmetro `-DbHost`, que também aceita outro valor).

Hosts testados a partir de um pod (`psql` contra a porta do RDS):

| Host | Resultado |
|---|---|
| `host.k3d.internal` | funciona; injetado pelo k3d no DNS do cluster, não depende do Docker Desktop (**escolhido**) |
| `host.docker.internal` | funciona, mas existe só no Docker Desktop |
| `localstack` (nome do container) | falha por timeout: o nome resolve para o IP em outra rede Docker (k3d antiga) |

Para testar a conexão, confira o readiness da API, que consulta o Postgres (`kubectl -n oficina port-forward svc/oficina-api 8080:80` e `curl http://localhost:8080/health/ready`, esperado `200`). Para testar o banco direto de um pod, use uma imagem `postgres:16-alpine` com `psql` apontando para `host.k3d.internal` e a porta do output `db_port`.

> Se a porta do RDS mudar (instância recriada), rode `create-secrets.ps1` de novo e `kubectl -n oficina rollout restart deployment/oficina-api`.

## API (Deployment e Service)

`base/api/` define o Deployment e o Service `oficina-api` (ClusterIP, porta 80 → 8080). Pré-requisito: o Secret `oficina-secrets` (veja acima); sem ele os pods ficam em `CreateContainerConfigError`.

| Item | Valor |
|---|---|
| Réplicas | 2, com `RollingUpdate` (`maxUnavailable: 0`, `maxSurge: 1`) |
| Probes | `startupProbe` e `livenessProbe` em `/health/live`; `readinessProbe` em `/health/ready` (verifica o Postgres) |
| Recursos | requests 100m CPU / 192Mi; limits 500m CPU / 384Mi (os requests são a base do HPA) |
| Segurança | `runAsNonRoot`, UID 1654, `seccompProfile: RuntimeDefault`, sem escalada de privilégio, `drop: ALL` |

A API não aplica migrations na subida; com o banco inacessível o readiness falha.

### Imagem

O manifest usa `image: oficina-api` e o overlay resolve nome e tag em `images`, com a URL do ECR do LocalStack (`terraform -chdir=infra output ecr_repository_urls`). O CD sobrescreve a tag com a do commit.

> **Pull do ECR no LocalStack:** dentro dos nós do k3d o hostname `*.localhost.localstack.cloud` resolve para `127.0.0.1`, então o `kubelet` não consegue puxar a imagem (`ImagePullBackOff`, `connection refused` em `127.0.0.1:4566`), apesar de o push pelo host funcionar. Contorno: importar a imagem nos nós com o **mesmo nome** do ECR; como o `imagePullPolicy` é `IfNotPresent`, o manifest continua igual ao de uma AWS real.
>
> ```powershell
> $img = "000000000000.dkr.ecr.us-east-1.localhost.localstack.cloud:4566/oficina-api:dev"
> docker tag oficina-api:dev $img
> foreach ($n in docker ps --format "{{.Names}}" | Select-String "k3d-oficina-local-.*(agent|server-0)") {
>   docker save $img | docker exec -i $n ctr -n k8s.io images import -
> }
> ```
>

### Verificar

```bash
kubectl apply -k k8s/overlays/localstack
kubectl -n oficina rollout status deployment/oficina-api
kubectl -n oficina port-forward svc/oficina-api 8080:80    # http://localhost:8080/swagger
```

Nova imagem com a mesma tag: `kubectl -n oficina rollout restart deployment/oficina-api`.
