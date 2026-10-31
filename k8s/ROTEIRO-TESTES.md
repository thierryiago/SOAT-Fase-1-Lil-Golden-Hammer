# Roteiro de testes do cluster Kubernetes

Passo a passo para subir a aplicação no cluster EKS do LocalStack (k3d) **apenas com os manifestos de `k8s/` e `kubectl`**, sem Docker Compose, e para testar a API chamando o Service `oficina-api` diretamente.

Todos os comandos são de PowerShell, rodados na raiz do repositório e **no mesmo terminal** (o `KUBECONFIG` vale só para a sessão). Cada passo traz o resultado esperado; se algo divergir, veja [Diagnóstico](#diagnóstico).

## Visão geral

| Fase | O que acontece | Script/comando |
|---|---|---|
| 0 | Infra: LocalStack, EKS (k3d) e RDS | `docker run` + `terraform` |
| 1 | Acesso ao cluster | `infra/scripts/kubeconfig.ps1` |
| 2 | Secrets da aplicação e de pull | `create-secrets.ps1`, `create-pull-secret.ps1` |
| 3 | Migrations no RDS | `run-migrations.ps1` |
| 4 | API, Mailpit, Service e HPA | `deploy-api.ps1` |
| 5 | Dados de demonstração (opcional) | `run-seed.ps1` |
| 6 | Conferência dos recursos | `kubectl get` |
| 7 | Testes batendo no Service | `curl` dentro do cluster e `port-forward` |
| 8 | Testes automatizados no cluster | `run-e2e.ps1`, `run-loadtest.ps1` |
| 9 | Resiliência e falhas | `kubectl delete pod`, tag inexistente |
| 10 | Limpeza | `kubectl delete`, `terraform destroy` |

## Pré-requisitos

- Docker Desktop em execução.
- `kubectl`, `terraform` e PowerShell (5.1 ou 7).
- `LOCALSTACK_AUTH_TOKEN` válido (o EKS do LocalStack exige).
- Access Token de **leitura** do Docker Hub para o repositório privado `thierryiago27/fiap-oficina-image-docker-hub`.
- As imagens `api-*` e `migrations-*` publicadas pela esteira (`publish-images.yml`). Por padrão os scripts usam `*-latest`.

## Fase 0: infraestrutura (uma vez por ambiente)

Detalhes e troubleshooting em [`infra/README.md`](../infra/README.md). Pule esta fase se o cluster já existe (`docker ps` mostra containers `k3d-oficina-local-*`).

```powershell
docker run -d --name localstack -p 4566:4566 -p 4510-4559:4510-4559 -e LOCALSTACK_AUTH_TOKEN=$env:LOCALSTACK_AUTH_TOKEN -v /var/run/docker.sock:/var/run/docker.sock -v localstack-data:/var/lib/localstack localstack/localstack
curl.exe http://localhost:4566/_localstack/health

terraform -chdir=infra/bootstrap init
terraform -chdir=infra/bootstrap apply
terraform -chdir=infra init
terraform -chdir=infra apply
```

**Esperado:** `"eks": "running"` (ou `available`) no health do LocalStack e `Apply complete!` nos dois `terraform apply`.

## Fase 1: acesso ao cluster

```powershell
. .\infra\scripts\kubeconfig.ps1      # o ponto na frente é obrigatório
kubectl get nodes -o wide
```

**Esperado:** `KUBECONFIG=...\kubeconfig-oficina` e os nós (`server-0` e os `agent`) em `Ready`.

> A porta da API do Kubernetes muda quando o cluster é recriado. Se o `kubectl` responder `connection refused` em `localhost:8080`, o `KUBECONFIG` não foi carregado neste terminal: rode o comando acima de novo.

## Fase 2: Secrets

```powershell
.\k8s\scripts\create-secrets.ps1 -DryRun      # mostra as chaves, valores mascarados
.\k8s\scripts\create-secrets.ps1              # connection string vem do terraform output; chaves geradas se ausentes

$env:DOCKERHUB_USERNAME = "thierryiago27"
$env:DOCKERHUB_TOKEN    = "<access token de leitura>"
.\k8s\scripts\create-pull-secret.ps1

kubectl -n oficina get secrets
```

**Esperado:** `oficina-secrets` (`Opaque`) e `dockerhub-pull` (`kubernetes.io/dockerconfigjson`) listados. Nenhum valor é impresso.

## Fase 3: migrations

```powershell
.\k8s\scripts\run-migrations.ps1                     # última imagem da main (migrations-latest)
# .\k8s\scripts\run-migrations.ps1 -ImageTag <sha7>  # ou uma versão fixa
```

**Esperado:** a linha `Imagem: ...:migrations-latest`, os logs do EF Core e `Migrations aplicadas com sucesso.` (código de saída 0).

```powershell
kubectl -n oficina get job oficina-migrations     # COMPLETIONS 1/1
```

## Fase 4: API e Mailpit

```powershell
.\k8s\scripts\deploy-api.ps1                     # mesma tag usada nas migrations
# .\k8s\scripts\deploy-api.ps1 -ImageTag <sha7>
```

**Esperado:** `Imagem: ...:api-latest`, `Deployment oficina-api pronto.` e `Deployment mailpit pronto.`.

## Fase 5: seed (opcional)

```powershell
.\k8s\scripts\run-seed.ps1
```

**Esperado:** `Seed aplicado.`. Pode ser repetido: o SQL usa `ON CONFLICT DO NOTHING`.

## Fase 6: conferência dos recursos

```powershell
kubectl -n oficina get deploy,rs,pods,svc,hpa,configmap,jobs
kubectl -n oficina get endpointslices -l kubernetes.io/service-name=oficina-api
kubectl -n oficina get deployment oficina-api -o jsonpath="{.spec.template.spec.containers[0].image}"; ""
kubectl top pods -n oficina
```

**Esperado:**

| Recurso | Estado |
|---|---|
| `deployment/oficina-api` e `deployment/mailpit` | `READY 1/1` (a API pode ter até 3 réplicas pelo HPA) |
| `service/oficina-api` | `LoadBalancer`, `EXTERNAL-IP <pending>` (normal no k3d), porta `80` |
| `service/mailpit` | `ClusterIP`, portas `1025` e `8025` |
| `hpa/oficina-api` | `MINPODS 1`, `MAXPODS 3`, alvos de CPU e memória com valores (não `<unknown>`) |
| EndpointSlice | um endereço por pod pronto da API |
| Imagem | a tag escolhida na Fase 4 |

## Fase 7: testes batendo no Service

O `EXTERNAL-IP` fica `<pending>` e os IPs dos nós não são alcançáveis do host (veja [Acesso](README.md#acesso)). Há duas formas de chegar ao Service.

| Forma | Caminho | Balanceia entre réplicas? |
|---|---|---|
| 7.1 Pod `curl` dentro do cluster | DNS `oficina-api.oficina.svc.cluster.local` ou ClusterIP → kube-proxy → pods | Sim: é o caminho real do Service |
| 7.2 `port-forward svc/oficina-api` | host → API do Kubernetes → **um** pod escolhido pelo Service | Não: fica preso a um pod |

### 7.1 De dentro do cluster (Service de verdade)

Abra um shell num pod descartável no namespace `oficina`:

```powershell
kubectl -n oficina run curl --rm -it --restart=Never --image=curlimages/curl:8.11.1 -- sh
```

Dentro do pod (shell `sh`):

```sh
API=http://oficina-api.oficina.svc.cluster.local     # nome completo; "http://oficina-api" também resolve

# Health: /health e /health/live não consultam dependências; /health/ready consulta o Postgres
curl -s -w ' -> %{http_code}\n' $API/health
curl -s -w ' -> %{http_code}\n' $API/health/live
curl -s -w ' -> %{http_code}\n' $API/health/ready

# Endpoint protegido sem token
curl -s -o /dev/null -w 'sem token -> %{http_code}\n' "$API/api/v1/customers?page=1&pageSize=5"

# Token técnico (endpoint anônimo)
TOKEN=$(curl -s -X POST $API/api/v1/auth/token | sed -E 's/.*"accessToken":"([^"]+)".*/\1/')
echo "token com ${#TOKEN} caracteres"

# Cadastro e consulta de cliente
curl -s -w ' -> %{http_code}\n' -X POST $API/api/v1/customers \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Maria da Silva","email":"maria.k8s@example.com","telephoneNumber":"+5511999990001","document":"529.982.247-25"}'
curl -s -w ' -> %{http_code}\n' "$API/api/v1/customers?search=Maria&page=1&pageSize=5" -H "Authorization: Bearer $TOKEN"

# Mailpit pelo Service interno (a API envia SMTP para mailpit:1025)
curl -s -w ' -> %{http_code}\n' http://mailpit.oficina.svc.cluster.local:8025/api/v1/messages | head -c 300; echo

exit
```

**Esperado:**

| Chamada | Status |
|---|---|
| `/health`, `/health/live` | `200` (`Healthy`) |
| `/health/ready` | `200`; `503` indica que a API não alcança o RDS |
| Cliente sem token | `401` |
| Token | um JWT com algumas centenas de caracteres |
| `POST /customers` | `201` com o `id`; numa segunda execução, `409` (documento duplicado, regra de domínio) |
| `GET /customers` | `200` com a cliente na lista |
| Mailpit | `200` com o JSON das mensagens |

Para conferir que o Service aponta para os pods, compare o ClusterIP com o DNS e o EndpointSlice:

```powershell
kubectl -n oficina get svc oficina-api -o jsonpath="{.spec.clusterIP}"; ""
kubectl -n oficina run curl --rm -i --restart=Never --image=curlimages/curl:8.11.1 -- `
  curl -s -w " -> %{http_code}`n" http://<CLUSTER-IP>/health
```

### 7.2 Do host, com port-forward para o Service

Num terminal separado (com o `KUBECONFIG` carregado), deixe rodando:

```powershell
kubectl -n oficina port-forward svc/oficina-api 8080:80
```

No terminal de testes:

```powershell
$api = "http://localhost:8080"

curl.exe -s -w " -> %{http_code}`n" "$api/health/ready"
curl.exe -s -o NUL -w "sem token -> %{http_code}`n" "$api/api/v1/customers?page=1&pageSize=5"

$token = (Invoke-RestMethod -Method Post "$api/api/v1/auth/token").accessToken
$headers = @{ Authorization = "Bearer $token" }

$body = @{ name = "Joao Souza"; email = "joao.k8s@example.com"; telephoneNumber = "+5511999990002"; document = "111.444.777-35" } | ConvertTo-Json
$customer = Invoke-RestMethod -Method Post "$api/api/v1/customers" -Headers $headers -ContentType "application/json" -Body $body
$customer

Invoke-RestMethod "$api/api/v1/customers/$($customer.id)" -Headers $headers
Invoke-RestMethod "$api/api/v1/customers?search=Joao&page=1&pageSize=5" -Headers $headers
```

**Esperado:** `Healthy -> 200`, `sem token -> 401`, o cliente criado com `id` e as duas consultas devolvendo o mesmo cliente. Repetir o `POST` com o mesmo documento lança erro `409 Conflict`.

Com o port-forward ativo também dá para usar:

- **Swagger:** http://localhost:8080/swagger;
- **Fluxo manual completo:** o [`oficina.http`](../oficina.http) (`@baseUrl = http://localhost:8080`), que percorre cadastros, estoque e a OS de `Created` a `Delivered`;
- **Mailpit:** em outro terminal, `kubectl -n oficina port-forward svc/mailpit 8025:8025` e http://localhost:8025, onde aparecem os e-mails das OS.

> O port-forward cai quando o pod escolhido é substituído (rollout, HPA, `delete pod`). Basta rodar o comando de novo.

## Fase 8: testes automatizados no cluster

Os dois rodam como Job k6 dentro do cluster, contra o Service (não pelo port-forward).

```powershell
.\k8s\e2e\run-e2e.ps1                 # fluxo de negócio completo; relatório em %TEMP%\oficina-e2e\relatorio-e2e.html
```

**Esperado:** todos os passos aprovados, código de saída 0 e o relatório HTML gerado.

Autoscaling, com dois terminais de observação:

```powershell
kubectl -n oficina get hpa oficina-api -w
kubectl -n oficina get pods -l app.kubernetes.io/name=oficina-api -w
```

E num terceiro:

```powershell
.\k8s\loadtest\run-loadtest.ps1        # 30 VUs, ~2m15s
```

**Esperado:** o HPA sobe para 2 ou 3 réplicas durante a carga, o EndpointSlice passa a listar os novos pods, `http_req_failed < 5%` e, alguns minutos depois, a volta para 1 réplica. Detalhes em [Autoscaling e teste de carga](README.md#autoscaling-e-teste-de-carga).

## Fase 9: resiliência e falhas

| Cenário | Comando | Esperado |
|---|---|---|
| Pod da API removido | `kubectl -n oficina delete pod -l app.kubernetes.io/name=oficina-api` e depois `kubectl -n oficina get pods -w` | O ReplicaSet recria o pod; ele só entra no Service (EndpointSlice) depois do readiness `/health/ready` |
| Rollout de outra versão | `.\k8s\scripts\deploy-api.ps1 -ImageTag <sha7>` | Rollout sem indisponibilidade; a imagem do Deployment passa a ser `api-<sha7>` |
| Tag inexistente | `.\k8s\scripts\run-migrations.ps1 -ImageTag naoexiste` | Para em segundos com `ImagePullBackOff` e os eventos do pod (sem esperar o timeout de 300 s) |
| Secret de pull ausente | `kubectl -n oficina delete secret dockerhub-pull` e depois `.\k8s\scripts\run-migrations.ps1` | Para antes de aplicar, com a instrução de rodar `create-pull-secret.ps1`. Recrie o Secret depois |
| Migrations idempotentes | `.\k8s\scripts\run-migrations.ps1` de novo | Sucesso, sem migrations pendentes aplicadas |

> Depois do cenário de tag inexistente, rode `.\k8s\scripts\run-migrations.ps1` de novo, para o Job voltar a um estado concluído.

## Diagnóstico

| Sintoma | Causa provável | Ação |
|---|---|---|
| `kubectl` tenta `localhost:8080` | `KUBECONFIG` não carregado neste terminal | `. .\infra\scripts\kubeconfig.ps1` |
| Pod em `ErrImagePull`/`ImagePullBackOff` com `pull access denied` | `dockerhub-pull` ausente, token inválido ou tag não publicada | `create-pull-secret.ps1`; conferir a tag no Docker Hub |
| Pod em `CreateContainerConfigError` | `oficina-secrets` ausente ou sem a chave | `create-secrets.ps1` |
| `/health/ready` em `503` ou migrations com timeout de conexão | RDS recriado (porta mudou) | `create-secrets.ps1` e `kubectl -n oficina rollout restart deployment/oficina-api` |
| HPA com `<unknown>` | métricas ainda não coletadas | esperar ~1 min; `kubectl top pods -n oficina` |
| Port-forward caiu | pod substituído | repetir o `port-forward` |

Comandos úteis:

```powershell
kubectl -n oficina describe pod -l app.kubernetes.io/name=oficina-api
kubectl -n oficina logs deploy/oficina-api --tail=100
kubectl -n oficina logs job/oficina-migrations
kubectl -n oficina get events --sort-by=.lastTimestamp | Select-Object -Last 20
```

## Fase 10: limpeza

Remover só a aplicação, mantendo o cluster:

```powershell
kubectl delete namespace oficina
```

Destruir toda a infraestrutura:

```powershell
terraform -chdir=infra destroy
terraform -chdir=infra/bootstrap destroy
docker rm -f localstack
```
